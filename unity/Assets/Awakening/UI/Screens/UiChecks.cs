#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;
using Awakening.Core.Save;
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

        static void Send(VisualElement root, EventType type, Vector2 at)
        {
            var e = new Event { type = type, mousePosition = at, button = 0, clickCount = 1 };
            EventBase evt = type == EventType.MouseMove ? PointerMoveEvent.GetPooled(e)
                : type == EventType.MouseDown ? PointerDownEvent.GetPooled(e) : (EventBase)PointerUpEvent.GetPooled(e);
            using (evt) root.panel.visualTree.SendEvent(evt);
        }

        /// <summary>
        /// Every paper button keeps the game's own hover and pressed inks. Unity's default theme
        /// colours `:hover:enabled` and `:active:enabled`, which outrank a single pseudo-class.
        /// </summary>
        static IEnumerator ButtonStates(VisualElement root)
        {
            var probes = new (Button button, Color hover, Color down)[]
            {
                (Kit.Button("Probe"), new Color32(182, 165, 129, 255), new Color(51 / 255f, 84 / 255f, 43 / 255f, .14f)),
                (Kit.Primary("Probe"), new Color32(77, 106, 70, 255), new Color32(42, 68, 35, 255)),
                (Kit.Stamp("Probe", null, Accent.Danger), new Color(142 / 255f, 29 / 255f, 20 / 255f, .16f), new Color(142 / 255f, 29 / 255f, 20 / 255f, .28f)),
                (Kit.Link("Probe", null), Color.clear, Color.clear),
                (Classed("aw-row"), new Color(33 / 255f, 24 / 255f, 14 / 255f, .07f), new Color(33 / 255f, 24 / 255f, 14 / 255f, .07f)),
                (Classed("aw-tab"), new Color32(198, 179, 140, 255), new Color32(198, 179, 140, 255)),
            };
            foreach (var probe in probes)
            {
                Button b = probe.button;
                b.style.position = Position.Absolute;
                b.style.left = 40;
                b.style.top = 120;
                root.Add(b);
                yield return Layout();
                Vector2 at = b.worldBound.center;
                Send(root, EventType.MouseMove, at);
                yield return Layout();
                Color hover = b.resolvedStyle.backgroundColor;
                Send(root, EventType.MouseDown, at);
                yield return Layout();
                Color down = b.resolvedStyle.backgroundColor;
                Send(root, EventType.MouseUp, at);
                Send(root, EventType.MouseMove, new Vector2(2, 2));
                yield return Layout();
                Debug.Log($"UI_STATE {string.Join(" ", b.GetClasses())}: hover={hover} down={down} text={b.resolvedStyle.color}");
                Check(Close(hover, probe.hover), $"{string.Join(" ", b.GetClasses())} hover keeps the paper ink: {hover}");
                Check(Close(down, probe.down), $"{string.Join(" ", b.GetClasses())} press keeps the paper ink: {down}");
                b.RemoveFromHierarchy();
            }
        }

        static Button Classed(string name)
        {
            var button = new Button { text = "Probe", focusable = false };
            button.AddToClassList(name);
            return button;
        }

        static bool Close(Color a, Color b) => Mathf.Abs(a.r - b.r) < .02f && Mathf.Abs(a.g - b.g) < .02f && Mathf.Abs(a.b - b.b) < .02f && Mathf.Abs(a.a - b.a) < .02f;
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

            var screen = (GameScreen)game.CurrentScreen;
            yield return Layout();
            var promptTexts = screen.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(promptTexts.Any(t => t.Contains("populous regions recruit well; small regions hold sites cheaply")), "placing subtitle explains recruitment vs ritual sites");
            Check(!promptTexts.Any(t => t.Contains("small regions are easier to hold")), "placing subtitle no longer pushes starting in a small region");

            game.Settings.Hints.Clear();
            game.Settings.HideGoals = false;
            game.Place("africa");
            Debug.Log("UI_CHECK: start and Continue");
            yield return Layout();
            yield return Layout();
            string expectedEcon = $"You have {Format.Count(game.State.Influence)} Influence. Gathering followers costs {GameData.Actions["recruit"].Influence}; opening a neighbouring cell costs {GameData.Actions["expand"].Influence}.";
            Check(screen.Query<Label>().ToList().Any(l => l.text.Contains(expectedEcon)), "first economic decision note reads live Influence and GameData costs");
            Check(game.HintSeen("first_cell"), "first economic note marks first_cell seen");
            screen.HandleKey(KeyCode.N, false);
            yield return Layout();

            Check(!game.HintSeen(Notes.TabsHintId), "file_tabs hint starts unseen");
            screen.Select("africa");
            yield return Layout();
            var tabGuide = screen.Panel.Q("tab-guide");
            Check(tabGuide != null && Kit.IsVisible(tabGuide), "first region file open shows tab explanation");
            string tabCopy = string.Join(" ", tabGuide.Query<Label>().ToList().Select(l => l.text));
            Check(tabCopy.Contains("CULT") && tabCopy.Contains("RITES") && tabCopy.Contains("OMENS"), "tab explanation covers CULT, RITES, and OMENS");
            Check(game.HintSeen(Notes.TabsHintId), "opening region file marks file_tabs seen");
            screen.Deselect();
            yield return Layout();
            screen.Select("africa");
            yield return Layout();
            Check(!Kit.IsVisible(screen.Panel.Q("tab-guide")), "tab explanation does not reappear once seen");
            screen.Deselect();
            yield return Layout();

            var goalsBox = screen.Q("first-run-goals");
            Check(goalsBox != null && Kit.IsVisible(goalsBox), "first-run goals appear during preparation");
            Check(goalsBox.Query<Label>().ToList().Any(l => l.text.Contains("Three cells before " + Format.Date(Format.MonthOf(1911)))), "first goal uses calendar date for 1911");
            // Verify chained progression and calendar overdue checks on isolated states.
            var gState = game.State.Clone();
            Check(Notes.ActiveGoal(gState).Id == "three_cells" && !Notes.ActiveGoal(gState).Overdue, "goal 1 active before 1911");
            gState.ElapsedMonths = Format.MonthOf(1911);
            Check(Notes.ActiveGoal(gState).Overdue, "goal 1 overdue in January 1911");
            gState.Regions["europe"].Population.Followers = 35;
            gState.Regions["latin_america"].Population.Followers = 35;
            Check(Notes.ActiveGoal(gState).Id == "first_sacrifice", "goal 2 active after three cells");
            gState.Telemetry.Sacrificed = Balance.SacrificeFollowers;
            var g3 = Notes.ActiveGoal(gState);
            Check(g3.Id == "first_site" && g3.Title.Contains(Format.Date(Format.MonthOf(1917))) && g3.Detail.ToLowerInvariant().Contains("small regions hold rites cheaply"), "goal 3 shows 1917 target and small-region hint");
            gState.ElapsedMonths = Format.MonthOf(1917);
            Check(Notes.ActiveGoal(gState).Overdue, "goal 3 overdue in January 1917");
            gState.Regions["oceania"].Site.Invested = true;
            var g4 = Notes.ActiveGoal(gState);
            Check(g4.Id == "three_sites" && g4.Title.Contains(Format.Date(Format.MonthOf(1923))), "goal 4 shows 1923 target");
            gState.ElapsedMonths = Format.MonthOf(1923);
            Check(Notes.ActiveGoal(gState).Overdue, "goal 4 overdue in January 1923");
            gState.Regions["africa"].Site.Invested = true;
            gState.Regions["europe"].Site.Invested = true;
            Check(Notes.ActiveGoal(gState) == null, "goals clear once three sites are funded");
            Press(goalsBox, "HIDE");
            yield return Layout();
            Check(game.Settings.HideGoals && !Kit.IsVisible(goalsBox), "HIDE persists HideGoals and hides goals card");
            // Verify SaveData backwards compatibility and HideGoals persistence.
            var legacySave = SaveData.DecodeSave("{\"version\":2,\"state\":null,\"settings\":{\"muted\":true,\"volume\":40},\"tutorialComplete\":false,\"lastResult\":null}");
            Check(legacySave != null && !legacySave.Settings.HideGoals, "legacy save defaults HideGoals to false");
            legacySave.Settings.HideGoals = true;
            var roundTripped = SaveData.DecodeSave(SaveData.EncodeSave(legacySave));
            Check(roundTripped != null && roundTripped.Settings.HideGoals, "SaveData round-trips HideGoals");
            Debug.Log("UI_CHECK: onboarding notes, tab guide, and first-run goals");

            yield return ButtonStates(screen);
            Debug.Log("UI_CHECK: button hover and press inks");
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
            Press(screen.Dialogs, "Shown");
            Check(!game.Settings.HideGoals, "menu restores first-run goals");
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
                var ending = Notes.Ending(game.State);
                Check(!string.IsNullOrEmpty(ending.Fact), outcome + " derives a dated ending fact");
                Check(screen.Dialogs.Query<Label>().ToList().Any(l => l.text.Contains(ending.Fact)), outcome + " displays dated ending fact");
                Check(Find(screen.Dialogs, "Try this world again") != null && Find(screen.Dialogs, "New World") != null, outcome + " offers retry and new world");
                var comment = screen.Dialogs.Q<TextField>();
                Check(comment != null && comment.maxLength == 1000, outcome + " offers optional comment");
                int pending = game.Lab.PendingEvents();
                comment.value = "A comment from the UI integration check.";
                Press(screen.Dialogs, "Queue Comment");
                Check(game.Lab.PendingEvents() == pending + 1, outcome + " queues the comment");
                Press(screen.Dialogs, "View the Map");
                Check(!screen.Dialogs.IsOpen && !game.ClockHeld && game.State.Status == outcome, outcome + " keeps finished map");
            }
            // Retry with the same seed and difficulty; also check awareness-loss dated fact and New World.
            var awareStart = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("hard", "retry-world"), "africa").State;
            awareStart.Awareness = 99.99;
            game.State = GameEngine.AdvanceSimulation(awareStart, 1);
            Check(game.State.Status == "lost" && !string.IsNullOrEmpty(Notes.Ending(game.State).Fact), "awareness loss produces a dated fact");
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Check(screen.Dialogs.Kind == "end", "awareness loss opens ending");
            Press(screen.Dialogs, "Try this world again");
            yield return Layout();
            Check(game.CurrentScreen is GameScreen && game.State.Status == "placing" && game.State.ElapsedMonths == 0
                && game.State.Difficulty == "hard" && game.State.Seed == "retry-world", "Try this world again keeps seed and difficulty");
            game.State = Awakening.Core.Dev.Playthrough.Play("early", "rlyeh", Balance.Deadline);
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Press(screen.Dialogs, "New World");
            yield return Layout();
            Check(game.State == null && game.CurrentScreen is StartScreen && !game.ClockHeld, "New World returns to start screen");
            Debug.Log("UI_CHECKS_PASSED: start, saved run, onboarding, goals, all sheets, clock, keyboard focus, participation, settings, doctrine, abandon, endings, retry");
        }
    }
}
#endif
