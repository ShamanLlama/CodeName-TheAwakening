#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Development-player integration checks: --shot /tmp/check.png --check-ui 1.</summary>
    internal static class UiChecks
    {
        static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException("UI_CHECK_FAILED: " + message);
        }

        static IEnumerator Layout()
        {
            for (int i = 0; i < 2; i++) yield return null;
        }

        static Button Find(VisualElement root, string text) => root.Query<Button>().ToList().Single(b => b.text == text);
        static void Press(VisualElement root, string text) => ((Action)Find(root, text).userData)();

        public static IEnumerator Run(GameController game)
        {
            game.Blocked = true;
            game.TutorialComplete = true;
            game.State = null;
            game.ShowStart();
            yield return Layout();
            var start = (StartScreen)game.CurrentScreen;
            Check(start.panel.focusController.focusedElement == Find(start, "Begin"), "Begin receives focus");
            Press(start, "Hard");
            var seed = start.Q<TextField>();
            seed.value = " keyboard seed ";
            start.HandleKey(KeyCode.Return);
            Check(game.State.Difficulty == "hard" && game.State.Seed == "keyboard seed", "difficulty and seed reach Begin");
            game.ShowStart();
            yield return Layout();
            start = (StartScreen)game.CurrentScreen;
            Check(start.panel.focusController.focusedElement == Find(start, "Continue"), "saved run focuses Continue");
            var saved = game.State;
            start.HandleKey(KeyCode.Return);
            Check(game.CurrentScreen is GameScreen && ReferenceEquals(saved, game.State), "Continue keeps the run");

            game.Place("africa");
            Debug.Log("UI_CHECK: start and Continue");
            var screen = (GameScreen)game.CurrentScreen;
            foreach (string kind in new[] { "menu", "doctrine", "chronicle", "guide", "alignment", "playtest" })
            {
                screen.OpenDialog(kind);
                yield return Layout();
                Check(screen.Dialogs.IsOpen && game.ClockHeld, kind + " holds the clock");
                var focused = screen.panel.focusController.focusedElement as VisualElement;
                Check(focused != null && screen.Dialogs.Contains(focused), kind + " focuses its own control");
                screen.HandleKey(KeyCode.Escape, false);
                if (kind == "guide")
                {
                    Check(screen.Dialogs.IsOpen, "guide requires Understood");
                    screen.HandleKey(KeyCode.Return, false);
                    Check(game.TutorialComplete, "Understood records the tutorial");
                }
                Check(!screen.Dialogs.IsOpen && !game.ClockHeld, kind + " releases the clock");
            }
            Debug.Log("UI_CHECK: modal keyboard and clock");
            screen.OpenDialog("menu");
            yield return Layout();
            Press(screen.Dialogs, "Sound On");
            Check(game.Settings.Muted, "sound toggle");
            screen.Dialogs.Q<Slider>().value = 63;
            Check(Math.Abs(game.Settings.Volume - 63) < .01, "volume setting");
            Press(screen.Dialogs, "Reduced");
            Check(game.Settings.ReducedMotion && Kit.ReducedMotion, "reduced motion setting");
            Press(screen.Dialogs, "GTC PLAYTEST LAB");
            Check(screen.Dialogs.Kind == "playtest" && game.ClockHeld, "switching sheets preserves the hold");
            screen.Dialogs.Close();
            Check(!game.ClockHeld, "one release after switching sheets");

            game.State.Influence = 100;
            screen.OpenDialog("doctrine");
            yield return Layout();
            var choose = screen.Dialogs.Query<Button>().ToList().First(b => b.text == "Choose · 40");
            ((Action)choose.userData)();
            Check(game.State.Specialization == GameData.SpecializationIds[0] && !screen.Dialogs.IsOpen, "doctrine choice");
            screen.OpenDialog("doctrine");
            Check(screen.Dialogs.Query<Button>().ToList().Where(b => b.ClassListContains("aw-primary")).All(b => !b.enabledSelf), "doctrine is permanent");
            screen.Dialogs.Close();

            Debug.Log("UI_CHECK: settings and doctrine");
            // A fresh configured round: Escape cancels without answering; Tab stays in the sheet.
            game.Lab.Configure(GameController.ShotLabOptions());
            game.State = null;
            game.ShowStart();
            yield return Layout();
            start = (StartScreen)game.CurrentScreen;
            start.AskParticipation();
            yield return Layout();
            start.HandleKey(KeyCode.Tab);
            Check(start.panel.focusController.focusedElement == Find(start, "Play without sharing"), "participation Tab reaches skip");
            start.HandleKey(KeyCode.Tab);
            Check(start.panel.focusController.focusedElement == Find(start, "Join the playtest & play"), "participation traps focus");
            start.HandleKey(KeyCode.LeftArrow);
            Check(start.panel.focusController.focusedElement == Find(start, "Play without sharing"), "participation left wraps");
            start.HandleKey(KeyCode.RightArrow);
            Check(start.panel.focusController.focusedElement == Find(start, "Join the playtest & play"), "participation right wraps");
            start.HandleKey(KeyCode.Escape);
            yield return Layout();
            Check(!game.Lab.Answered() && !game.Lab.Confirmed && start.Q("participation-sheet") == null, "Escape makes no participation choice");
            Check(start.panel.focusController.focusedElement == Find(start, "Begin"), "Escape restores form focus");
            start.AskParticipation();
            yield return Layout();
            start.HandleKey(KeyCode.Tab);
            start.HandleKey(KeyCode.Return);
            Check(game.State != null && game.Lab.Answered() && !game.Lab.Confirmed, "skip starts a run and remembers decline");

            screen = (GameScreen)game.CurrentScreen;
            screen.OpenDialog("menu");
            Press(screen.Dialogs, "ABANDON THIS RUN");
            Check(game.State != null && screen.Dialogs.IsOpen, "abandon requires confirmation");
            Press(screen.Dialogs, "CLICK AGAIN TO ABANDON");
            Check(game.State == null && game.CurrentScreen is StartScreen && !game.ClockHeld, "abandon returns to start");

            game.Blocked = false;
            // The accepted branch is independent of the remembered decline.
            game.Lab.Configure(GameController.ShotLabOptions());
            game.ShowStart();
            yield return Layout();
            start = (StartScreen)game.CurrentScreen;
            start.AskParticipation();
            yield return Layout();
            start.HandleKey(KeyCode.Return);
            Check(game.Lab.Confirmed && game.State != null, "join starts a run and records consent");

            foreach (string outcome in new[] { "won", "lost" })
            {
                game.State = Awakening.Core.Dev.Playthrough.Play("early", outcome == "won" ? "resume" : "rlyeh", Balance.Deadline);
                game.Lab.StartRun(GameController.Capture(GameEngine.CreateGameState("normal", "ending")));
                game.Lab.EmitEvent("run_finished", new Dictionary<string, object> { ["outcome"] = outcome, ["reason"] = outcome == "won" ? "awakened" : "deadline" }, GameController.Capture(game.State), "ended");
                game.ShowGame();
                yield return Layout();
                screen = (GameScreen)game.CurrentScreen;
                Check(screen.Dialogs.Kind == "end" && game.ClockHeld, outcome + " opens ending");
                var comment = screen.Dialogs.Q<TextField>();
                Check(comment != null && comment.maxLength == 1000, outcome + " offers optional comment");
                int pending = game.Lab.PendingEvents();
                comment.value = "A comment from the UI integration check.";
                Press(screen.Dialogs, "Queue Comment");
                Check(game.Lab.PendingEvents() == pending + 1, outcome + " queues the comment");
                Press(screen.Dialogs, "View the Map");
                Check(!screen.Dialogs.IsOpen && !game.ClockHeld && game.State.Status == outcome, outcome + " keeps finished map");
            }
            Debug.Log("UI_CHECKS_PASSED: start, saved run, all sheets, clock, keyboard focus, participation, settings, doctrine, abandon, endings");
        }
    }
}
#endif
