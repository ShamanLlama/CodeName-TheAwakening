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
                (Kit.Button("Probe"), new Color32(182, 165, 129, 255), new Color(36 / 255f, 66 / 255f, 30 / 255f, .14f)),
                (Kit.Primary("Probe"), new Color32(53, 86, 46, 255), new Color32(28, 51, 23, 255)),
                (Kit.Stamp("Probe", null, Accent.Danger), new Color(122 / 255f, 20 / 255f, 13 / 255f, .16f), new Color(122 / 255f, 20 / 255f, 13 / 255f, .28f)),
                (Kit.Link("Probe", null), Color.clear, Color.clear),
                (Classed("aw-row"), new Color(25 / 255f, 18 / 255f, 10 / 255f, .07f), new Color(25 / 255f, 18 / 255f, 10 / 255f, .07f)),
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
            var ruleLabel = start.Q<Label>("difficulty-rule-text");
            Check(ruleLabel != null && ruleLabel.text.Contains(GameData.Difficulties["normal"].Rule), "start screen shows Normal difficulty rules");
            Press(start, "Easy");
            Check(ruleLabel.text.Contains(GameData.Difficulties["easy"].Rule), "start screen shows Easy difficulty rules");
            Press(start, "Hard");
            Check(ruleLabel.text.Contains(GameData.Difficulties["hard"].Rule), "start screen shows Hard difficulty rules");
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
            screen.Select("africa");
            yield return Layout();
            Check(screen.Panel.Q<Label>("mysticism-note-profile")?.text == Derived.MysticismNote("africa"), "placing profile shows MysticismNote for Africa");
            screen.Select("europe");
            yield return Layout();
            Check(screen.Panel.Q<Label>("mysticism-note-profile")?.text == Derived.MysticismNote("europe"), "placing profile shows MysticismNote for Europe");
            screen.Deselect();
            yield return Layout();

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
            foreach (string kind in new[] { "menu", "talents", "chronicle", "guide", "alignment", "playtest" })
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

            // Talent tree: (1) no seals open yet, (2) unbroken seal + first-time note, (3) taking root, middle & exclusion.
            var treeState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "tree-ui"), "africa").State;
            treeState.ElapsedMonths = 12;
            treeState.Influence = 120;
            game.Settings.Hints = new List<string> { "first_cell", "threat", "omen", "awareness", "sacrifice", "unsupported", "madness", Notes.TabsHintId };
            game.State = treeState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Check(screen.Q<Label>("talents-note").text == "SEAL IN 12 MO", "ledger shows months until first talent seal");
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("talent-seals-summary").text.Contains("0/5 SEALS OPEN") && screen.Dialogs.Q<Label>("talent-seals-summary").text.Contains("0 UNBROKEN"), "tree header reports 0 open seals");
            Check(screen.Dialogs.Q<Label>("talent-next-seal").text.Contains(Format.Date(24).ToUpperInvariant()), "tree header reports next seal calendar date");
            Check(screen.Dialogs.Q<Label>("talent-reason-swelling_flock").text == "The next seal opens in 12 months.", "root node explains waiting for seal");
            Check(screen.Dialogs.Q<Label>("talent-reason-open_worship").text == "Requires Swelling flock.", "tier 2 node explains root requirement");
            Check(screen.Dialogs.Q<Button>("take-swelling_flock") == null, "locked root has no Take button");
            screen.Dialogs.Close();
            yield return Layout();

            // First seal opens at month 24: ledger flags unbroken seal and first-time talents note explains tree cannot be completed.
            game.State.ElapsedMonths = 24;
            game.State.Influence = 200;
            screen.Refresh();
            yield return Layout();
            yield return Layout();
            Check(screen.Q<Label>("talents-note").text == "1 SEAL UNBROKEN", "ledger shows unbroken seal notice");
            Check(screen.Query<Label>().ToList().Any(l => l.text.Contains("cannot be completed")), "first-time talents note explains tree cannot be completed in one run");
            Check(game.HintSeen("talents"), "talents note marks talents hint seen");
            screen.HandleKey(KeyCode.N, false);
            yield return Layout();
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("talent-seals-summary").text.Contains("1/5 SEALS OPEN") && screen.Dialogs.Q<Label>("talent-seals-summary").text.Contains("1 UNBROKEN"), "tree header shows 1 unbroken seal");
            var takeRoot = screen.Dialogs.Q<Button>("take-swelling_flock");
            Check(takeRoot != null && takeRoot.text == "Take · 40 Inf", "available root shows Take button with live price");
            ((Action)takeRoot.userData)();
            yield return Layout();
            Check(game.State.Talents.Contains("swelling_flock") && screen.Dialogs.IsOpen, "taking a talent records it and refreshes tree in place");
            Check(screen.Dialogs.Q<Label>("talent-state-swelling_flock").text.Contains("TAKEN"), "taken talent displays TAKEN state");
            Check(screen.Q<Label>("talents-note").text == "SEAL IN 40 MO", "ledger updates to next seal after spending seal");
            screen.Dialogs.Close();
            yield return Layout();

            // Second seal at month 64: taking Open worship closes Martyrs with its exact reason; third seal unlocks capstone.
            game.State.ElapsedMonths = 64;
            game.State.Influence = 300;
            screen.Refresh();
            screen.OpenDialog("talents");
            yield return Layout();
            var takeMid = screen.Dialogs.Q<Button>("take-open_worship");
            int midCost = GameEngine.TalentCost(game.State);
            Check(takeMid != null && takeMid.text == $"Take · {midCost} Inf", $"tier 2 middle becomes available at {midCost} Influence");
            ((Action)takeMid.userData)();
            yield return Layout();
            Check(game.State.Talents.Contains("open_worship"), "middle talent recorded");
            Check(screen.Dialogs.Q<Label>("talent-state-martyrs").text.Contains("CLOSED"), "excluded sibling displays CLOSED badge");
            Check(screen.Dialogs.Q<Label>("talent-reason-martyrs").text == "Closed for this run: the cult chose Open worship.", "excluded sibling shows exact exclusion reason");
            Check(screen.Dialogs.Q<Label>("talent-yield-swelling_flock")?.text == GameEngine.TalentYieldText(game.State, "swelling_flock"), "taken root displays TalentYieldText");
            Check(screen.Dialogs.Q<Label>("talent-yield-open_worship")?.text == GameEngine.TalentYieldText(game.State, "open_worship"), "taken tier 2 displays TalentYieldText");
            foreach (string capId in new[] { "great_procession", "world_forgets", "dreams_of_rlyeh" })
                Check(screen.Dialogs.Q("capstone-rules-" + capId) != null, capId + " capstone displays structured condition/effect/cost");
            game.State.ElapsedMonths = 104;
            screen.Dialogs.Close();
            screen.OpenDialog("talents");
            yield return Layout();
            var takeCap = screen.Dialogs.Q<Button>("take-great_procession");
            int capCost = GameEngine.TalentCost(game.State);
            Check(takeCap != null && takeCap.text == $"Take · {capCost} Inf", $"capstone unlocks after root and middle with {capCost} Inf price");
            ((Action)takeCap.userData)();
            yield return Layout();
            Check(game.State.Talents.Contains("great_procession"), "Great Procession capstone recorded");
            Check(screen.Dialogs.Q<Label>("procession-status")?.text.Contains("0 / 5") == true, "Great Procession shows 0/5 public regions before going public");
            screen.Dialogs.Close();
            Debug.Log("UI_CHECK: settings and talent tree");

            // Capstones: (1) Great Procession 5/5 public -> 8-month rite, (2) world_forgets tree + ledger + confirm + spent,
            // (3) Martyrs 0 Dread yield, (4) RITE HALTED with Forecast.Rite blockers + scatter halt warning, (5) Dreams of R'lyeh.
            var procState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "proc-ui"), "africa").State;
            procState.ElapsedMonths = Balance.PreparationMonths;
            procState.Status = "alignment";
            procState.Influence = 300;
            procState.Dread = 140;
            foreach (string tid in new[] { "swelling_flock", "open_worship", "great_procession" }) { procState.Talents.Add(tid); procState.TalentYield[tid] = 0; }
            foreach (string rid in new[] { "latin_america", "middle_east", "oceania" })
            {
                Derived.Recruit(procState.Regions[rid], Derived.SiteRequirement(procState.Regions[rid]) + 60);
                procState.Regions[rid].Site = new Site { Invested = true, Progress = GameEngine.SiteMonths(procState) };
            }
            foreach (string rid in new[] { "africa", "latin_america", "middle_east", "oceania" })
            {
                Derived.Recruit(procState.Regions[rid], 80);
                procState.Regions[rid].Public = true;
                procState.Regions[rid].Revealed = true;
            }
            game.State = procState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            screen.Dialogs.Close();
            yield return Layout();
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("procession-status")?.text.Contains("4 / 5") == true, "Great Procession shows 4/5 public regions");
            screen.Dialogs.Close();
            Derived.Recruit(game.State.Regions["south_asia"], 120);
            game.State.Regions["south_asia"].Public = true;
            game.State.Regions["south_asia"].Revealed = true;
            screen.Refresh();
            yield return Layout();
            Check(screen.Q<Label>("rite-margin-procession")?.text.Contains("5/5") == true, "rite margin strip shows 5/5 Procession readiness");
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("procession-status")?.text.Contains("CONDITION MET NOW") == true, "Great Procession flags condition met at 5 public regions");
            screen.Dialogs.Close();
            screen.OpenDialog("alignment");
            yield return Layout();
            Check(screen.Dialogs.Q("alignment-procession-check") != null, "Alignment dialog shows Great Procession check");
            var beginProc = screen.Dialogs.Q<Button>("alignment-begin-rite");
            Check(beginProc != null && beginProc.text.Contains("Great Procession") && beginProc.text.Contains("8 mo"), "Alignment button reflects 8-month Great Procession");
            ((Action)beginProc.userData)();
            yield return Layout();
            Check(game.State.Status == "awakening" && game.State.RiteMonthsRequired == Balance.ProcessionRiteMonths, "starting Procession fixes 8-month rite");
            Check(screen.Query<Label>().ToList().Any(l => l.text.Contains("0 / 8")), "active Procession displays Progress / 8");

            // The world forgets: invocable from ledger quick access and tree, confirmation shows InvokeRisk, spent afterwards.
            var forgetsState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "forgets-ui"), "africa").State;
            forgetsState.ElapsedMonths = 160;
            forgetsState.Influence = 200;
            forgetsState.Awareness = 70;
            Derived.Recruit(forgetsState.Regions["africa"], 200);
            forgetsState.Regions["africa"].Threat = new Threat { Kind = "raid", Severity = 2, DueMonth = 164 };
            foreach (string tid in new[] { "swelling_flock", "martyrs", "quiet_friends", "sleeper_cells", "world_forgets" })
            {
                forgetsState.Talents.Add(tid);
                forgetsState.TalentYield[tid] = tid == "quiet_friends" ? 60 : 0;
            }
            game.State = forgetsState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            var ledgerInvoke = screen.Q<Button>("ledger-invoke-capstone");
            Check(ledgerInvoke != null && Kit.IsVisible(ledgerInvoke.parent), "ledger shows quick-access Invoke button when world_forgets is available");
            ((Action)ledgerInvoke.userData)();
            yield return Layout();
            Check(screen.Dialogs.Kind == "invoke" && game.ClockHeld, "ledger Invoke opens confirmation dialog and holds clock");
            Check(screen.Dialogs.Q<Label>("invoke-risk-text")?.text.Contains(GameEngine.InvokeRisk(game.State, "world_forgets")) == true, "Invoke confirmation displays InvokeRisk");
            Press(screen.Dialogs, "Cancel");
            yield return Layout();
            Check(!screen.Dialogs.IsOpen && !game.State.CapstonesUsed.Contains("world_forgets"), "Cancel leaves world_forgets unspent");
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("talent-yield-martyrs")?.text == "0 Dread so far", "Martyrs displays 0 Dread so far yield");
            Check(screen.Dialogs.Q<Label>("talent-yield-quiet_friends")?.text == "60 Influence saved so far", "Quiet friends displays non-zero yield");
            var treeInvoke = screen.Dialogs.Q<Button>("invoke-world_forgets");
            Check(treeInvoke != null && treeInvoke.enabledSelf, "tree shows enabled Invoke button for world_forgets");
            ((Action)treeInvoke.userData)();
            yield return Layout();
            Check(screen.Dialogs.Kind == "invoke", "tree Invoke opens confirmation dialog");
            var confirmInvoke = screen.Dialogs.Q<Button>("confirm-invoke");
            Check(confirmInvoke != null && confirmInvoke.enabledSelf, "confirmation dialog has enabled Invoke stamp");
            ((Action)confirmInvoke.userData)();
            yield return Layout();
            Check(game.State.CapstonesUsed.Contains("world_forgets") && Math.Abs(game.State.Awareness - 45) < 0.01 && game.State.Regions["africa"].Threat == null && game.State.Regions["africa"].Scattered, "confirming world_forgets drops awareness, dissolves threats, scatters cells");
            Check(!Kit.IsVisible(screen.Q("capstone-quick")), "ledger quick-access hides once world_forgets is spent");
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("talent-state-world_forgets")?.text.Contains("SPENT") == true, "world_forgets displays SPENT state in tree");
            Check(screen.Dialogs.Q("talent-spent-world_forgets") != null && screen.Dialogs.Q("invoke-world_forgets") == null, "spent world_forgets removes Invoke button");
            Check(screen.Dialogs.Q<Label>("talent-yield-world_forgets")?.text == "Invoked: −25 world awareness", "spent world_forgets displays invoked yield");
            screen.Dialogs.Close();
            Debug.Log("UI_CHECK: capstones, yields, Procession, and world_forgets");

            // Rite halted band (Forecast.Rite blockers) and prominent scatter/sacrifice halt warnings + Dreams of R'lyeh.
            var riteState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "rite-dreams-ui"), "africa").State;
            riteState.ElapsedMonths = Balance.PreparationMonths;
            riteState.Status = "alignment";
            riteState.Influence = 260;
            riteState.Dread = 160;
            riteState.Awareness = 40;
            Derived.Recruit(riteState.Regions["africa"], 300);
            foreach (string rid in new[] { "latin_america", "middle_east", "oceania" })
            {
                Derived.Recruit(riteState.Regions[rid], Derived.SiteRequirement(riteState.Regions[rid]) + 60);
                riteState.Regions[rid].Site = new Site { Invested = true, Progress = GameEngine.SiteMonths(riteState) };
            }
            foreach (string tid in new[] { "red_altars", "scapegoats", "dreams_of_rlyeh" }) { riteState.Talents.Add(tid); riteState.TalentYield[tid] = 0; }
            riteState = GameEngine.StartAwakening(riteState).State;
            riteState.FinalProgress = 2;
            riteState.Regions["oceania"].Threat = new Threat { Kind = "occult", Severity = 1, DueMonth = riteState.ElapsedMonths + 3 };
            game.State = riteState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Check(screen.Q("threat-scatter-halts-oceania") != null, "threat clipping highlights HALTS RITE on scatter during the final rite");
            screen.Select("oceania");
            yield return Layout();
            Check(screen.Panel.Q("response-rite-halt-scatter") != null, "case file threat card highlights rite-halting scatter warning");
            screen.Panel.LayoutExplain("scatter");
            yield return Layout();
            var scatterWarn = screen.Panel.Q("detail-rite-halt-warning");
            Check(scatterWarn != null && scatterWarn.Query<Label>().ToList().Any(l => l.text.Contains("The final rite halts while this site hides")), "scatter detail displays prominent rite-halt callout");

            // Dreams of R'lyeh: sacrifice in Africa advances rite +1 mo; sacrifice in thin Oceania halts rite and does not advance.
            screen.Select("africa");
            yield return Layout();
            screen.Panel.LayoutSetGroup("RITES");
            yield return Layout();
            var sacRow = screen.Panel.Q<Button>("sacrifice");
            Check(sacRow != null && sacRow.ClassListContains("aw-dream-ready") && sacRow.Query<Label>().ToList().Any(l => l.text == "+1 RITE MONTH"), "Dreams of R'lyeh highlights sacrifice row with +1 RITE MONTH");
            screen.Panel.LayoutExplain("sacrifice");
            yield return Layout();
            var dreamBanner = screen.Panel.Q("detail-dreams-banner");
            Check(dreamBanner != null && dreamBanner.Query<Label>().ToList().Any(l => l.text.Contains("ADVANCES RITE +1 MO")), "sacrifice detail shows Dreams of R'lyeh advance banner");
            game.Act("sacrifice", "africa");
            yield return Layout();
            Check(game.State.FinalProgress == 3 && Math.Abs(game.State.Awareness - 42) < 0.01, "Dreams sacrifice advances rite progress and adds +2 awareness");
            screen.OpenDialog("talents");
            yield return Layout();
            Check(screen.Dialogs.Q<Label>("talent-yield-dreams_of_rlyeh")?.text == "+10 Madness spread · 1 rite months dreamed forward so far", "Dreams of R'lyeh yield updates after advancing sacrifice: " + screen.Dialogs.Q<Label>("talent-yield-dreams_of_rlyeh")?.text);
            screen.Dialogs.Close();

            // Dreams of R'lyeh at 30+ Madness advances the rite by 2 months.
            game.State.Regions["africa"].Madness = 40;
            game.State.Regions["africa"].Cooldowns.Remove("sacrifice");
            screen.Refresh();
            yield return Layout();
            sacRow = screen.Panel.Q<Button>("sacrifice");
            Check(sacRow != null && sacRow.Query<Label>().ToList().Any(l => l.text == "+2 RITE MONTHS"), "Dreams of R'lyeh shows +2 RITE MONTHS when Madness >= 30");
            screen.Panel.LayoutExplain("sacrifice");
            yield return Layout();
            Check(screen.Panel.Q("detail-dreams-banner")?.Query<Label>().ToList().Any(l => l.text.Contains("ADVANCES RITE +2 MO")) == true, "sacrifice detail shows +2 MO Dreams advance at 30+ Madness");

            // Thin site sacrifice: does not advance dream and highlights rite-halt warning.
            var thinOc = game.State.Regions["oceania"];
            Derived.LoseFollowers(thinOc, thinOc.Population.Followers - (Derived.SiteRequirement(thinOc) + 10));
            screen.Select("oceania");
            yield return Layout();
            screen.Panel.LayoutSetGroup("RITES");
            yield return Layout();
            screen.Panel.LayoutExplain("sacrifice");
            yield return Layout();
            Check(screen.Panel.Q("detail-dreams-banner")?.Query<Label>().ToList().Any(l => l.text.Contains("DOES NOT ADVANCE")) == true, "thin-site sacrifice detail reports Dreams does not advance");
            Check(screen.Panel.Q("detail-rite-halt-warning")?.Query<Label>().ToList().Any(l => l.text.Contains("The final rite halts until this site again holds")) == true, "thin-site sacrifice detail shows prominent rite-halt warning");
            screen.Deselect();

            // Halt the rite with scattered, unsupported, dormant, and destroyed blockers.
            game.State.Regions["latin_america"].Scattered = true;
            game.State.Regions["latin_america"].ScatteredUntil = game.State.ElapsedMonths + 5;
            Derived.LoseFollowers(game.State.Regions["middle_east"], game.State.Regions["middle_east"].Population.Followers - (Derived.SiteRequirement(game.State.Regions["middle_east"]) - 10));
            game.State.Regions["oceania"].Site.Dormant = true;
            screen.Refresh();
            yield return Layout();
            var haltedBox = screen.Q("rite-halted-box");
            Check(haltedBox != null && Kit.IsVisible(haltedBox), "rite band displays RITE HALTED box when Forecast.Rite is not advancing");
            string haltedText = screen.Q<Label>("rite-halted-reasons")?.text ?? "";
            Check(haltedText.Contains("RITE HALTED") && haltedText.Contains("South America: scattered") && haltedText.Contains("West Asia: unsupported") && haltedText.Contains("Oceania: dormant"), "rite halted box names all blocking regions and reasons: " + haltedText);
            Check(screen.Query<Label>().ToList().Any(l => l.text.Contains("RITE HALTED  3 / 12")), "ledger sites caption shows RITE HALTED Progress/Required");
            Debug.Log("UI_CHECK: rite halted, scatter/sacrifice halt warnings, and Dreams of R'lyeh");

            // Mysticism in active file, Bury the record, and Restore the site (highlight only when recoverable).
            var actionState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "bury-restore-ui"), "africa").State;
            actionState.ElapsedMonths = 90;
            actionState.Influence = 300;
            actionState.Awareness = 42;
            Derived.Recruit(actionState.Regions["africa"], 340);
            Derived.Recruit(actionState.Regions["europe"], 100);
            actionState.Regions["africa"].Site = new Site { Invested = true, Progress = 14, Recoverable = 0 };
            game.State = actionState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            screen.Select("africa");
            yield return Layout();
            Check(screen.Panel.Q<Label>("mysticism-note")?.text == Derived.MysticismNote("africa"), "active file displays Derived.MysticismNote for Africa");
            screen.Select("europe");
            yield return Layout();
            Check(screen.Panel.Q<Label>("mysticism-note")?.text == Derived.MysticismNote("europe"), "active file displays Derived.MysticismNote for Europe");
            screen.Select("africa");
            yield return Layout();
            var buryRow = screen.Panel.Q<Button>("bury");
            Check(buryRow != null && buryRow.Query<Label>().ToList().Any(l => l.text == "50 INF") && buryRow.Query<Label>().ToList().Any(l => l.text == "−5 AWARENESS"), "CULT tab shows Bury the record with price and awareness effect");
            screen.Panel.LayoutExplain("bury");
            yield return Layout();
            var buryTexts = screen.Panel.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(buryTexts.Any(t => t.Contains("−5 world awareness")) && buryTexts.Any(t => t.Contains($"next burial costs {Balance.BuryBaseCost + GameEngine.BuryStep(actionState)} Influence")), "Bury detail shows effect and rising cost risk from PreviewAction");
            game.Act("bury", "africa");
            yield return Layout();
            Check(game.State.BuryUses == 1 && Math.Abs(game.State.Awareness - 37) < 0.01, "performing Bury lowers awareness and increments BuryUses");

            screen.Panel.LayoutSetGroup("RITES");
            yield return Layout();
            var restoreRow = screen.Panel.Q<Button>("restore");
            Check(restoreRow != null && !restoreRow.ClassListContains("aw-restore-ready") && restoreRow.Query<Label>().ToList().Any(l => l.text == "NO LOST PREP"), "Restore row is muted when no recoverable months exist");
            Check(screen.Panel.Q<Button>("site-restore-stamp") == null, "site card omits restore stamp when Recoverable is 0");
            game.State.Regions["africa"].Site.Recoverable = 6;
            screen.Refresh();
            yield return Layout();
            restoreRow = screen.Panel.Q<Button>("restore");
            Check(restoreRow.ClassListContains("aw-restore-ready") && restoreRow.Query<Label>().ToList().Any(l => l.text == "90 INF") && restoreRow.Query<Label>().ToList().Any(l => l.text == "+3 MO LOST"), "Restore row highlights with cost and months when Recoverable > 0");
            Check(screen.Panel.Q<Button>("site-restore-stamp") != null, "site card shows direct restore stamp when Recoverable > 0");
            screen.Panel.LayoutExplain("restore");
            yield return Layout();
            var restoreTexts = screen.Panel.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(restoreTexts.Any(t => t.Contains("+3 preparation months recovered.")) && restoreTexts.Any(t => t.Contains("+10 Exposure.")), "Restore detail shows effect and risk from PreviewAction");
            game.Act("restore", "africa");
            yield return Layout();
            Check(game.State.Regions["africa"].Site.Progress == 17 && game.State.Regions["africa"].Site.Recoverable == 0 && !screen.Panel.Q<Button>("restore").ClassListContains("aw-restore-ready"), "performing Restore recovers months and clears highlight");
            screen.Deselect();
            Debug.Log("UI_CHECK: mysticism, bury, and restore");

            // Cornered threat: Threats.Options reasons + Threats.Cornered mark in both clipping and case file.
            var corneredState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "cornered-ui"), "africa").State;
            corneredState.ElapsedMonths = 80;
            corneredState.Influence = 200;
            corneredState.Dread = 18;
            Derived.Recruit(corneredState.Regions["africa"], 300);
            corneredState.Regions["africa"].Cooldowns["counter"] = 89;
            corneredState.Regions["africa"].Threat = new Threat { Kind = "occult", Severity = 2, DueMonth = 85 };
            Check(Threats.Cornered(corneredState, "africa"), "staged threat is cornered");
            game.State = corneredState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            var clipping = screen.Q("threat-card-africa");
            Check(clipping != null && clipping.Q("threat-cornered-africa") != null, "threat clipping shows CORNERED mark");
            var clipTexts = clipping.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(clipTexts.Any(t => t.Contains("counter rests 9 more months")) && clipTexts.Any(t => t.Contains("need 40 Dread")), "threat clipping shows Threats.Options reasons on appearance");
            screen.Select("africa");
            yield return Layout();
            Check(screen.Panel.Q("panel-threat-cornered") != null, "case file threat card shows CORNERED mark");
            var fileThreatTexts = screen.Panel.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(fileThreatTexts.Any(t => t.Contains("counter rests 9 more months")) && fileThreatTexts.Any(t => t.Contains("need 40 Dread")), "case file threat card shows Threats.Options reasons");
            screen.Deselect();
            Debug.Log("UI_CHECK: cornered threat and options");

            // Madness UI (ledger Dread/mo, region panel MadnessText + quickened site + action previews), threat grace, and Easy 20-month sites.
            var madState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("easy", "madness-ui"), "africa").State;
            madState.ElapsedMonths = 96;
            madState.Influence = 220;
            madState.Dread = 80;
            Derived.Recruit(madState.Regions["africa"], 340);
            madState.Regions["africa"].Madness = 60;
            madState.Regions["africa"].Site = new Site { Invested = true, Progress = 12 };
            madState.Regions["africa"].ThreatGraceUntil = madState.ElapsedMonths + Balance.ThreatGraceMonths;
            Derived.Recruit(madState.Regions["europe"], 140);
            madState.Regions["europe"].Madness = 20;
            game.State = madState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Check(screen.Q<Label>("dread-madness-rate")?.text == "+4/mo from Madness", "ledger shows total Madness Dread per month: " + screen.Q<Label>("dread-madness-rate")?.text);
            screen.Select("africa");
            yield return Layout();
            Check(screen.Panel.Q<Label>("stat-madness-value")?.text == "60%", "region panel shows Madness percentage");
            string expectedMadText = GameEngine.MadnessText(game.State, "africa");
            Check(screen.Panel.Q<Label>("madness-text")?.text.Contains(expectedMadText) == true, "region panel shows MadnessText: " + screen.Panel.Q<Label>("madness-text")?.text);
            Check(screen.Panel.Q<Label>("region-threat-grace")?.text == $"Quiet for {Format.Months(Balance.ThreatGraceMonths)}", "region panel shows threat grace when ThreatGraceLeft > 0");
            Check(screen.Panel.Q<Label>("site-progress-label")?.text == "12 / 20 MO", "Easy site card uses 20 site months: " + screen.Panel.Q<Label>("site-progress-label")?.text);
            Check(screen.Panel.Q<Label>("site-madness-quickened") != null, "preparing site with Madness displays quickened note");
            screen.Panel.LayoutSetGroup("RITES");
            yield return Layout();
            Check(screen.Panel.Q<Button>("manifest")?.Query<Label>().ToList().Any(l => l.text == "+20 MADNESS") == true, "manifest row shows +20 MADNESS");
            Check(screen.Panel.Q<Button>("sacrifice")?.Query<Label>().ToList().Any(l => l.text == "+10 MADNESS") == true, "sacrifice row shows +10 MADNESS");
            screen.Panel.LayoutExplain("manifest");
            yield return Layout();
            Check(screen.Panel.Query<Label>().ToList().Any(l => l.text.Contains("Madness here +20")), "manifest detail mentions Madness gain");
            screen.Panel.LayoutExplain("sacrifice");
            yield return Layout();
            Check(screen.Panel.Query<Label>().ToList().Any(l => l.text.Contains("Madness here +10")), "sacrifice detail mentions Madness gain");
            game.State.Regions["africa"].Site.Progress = 20;
            game.State.Regions["africa"].ThreatGraceUntil = 0;
            screen.Refresh();
            yield return Layout();
            Check(!Kit.IsVisible(screen.Panel.Q("region-threat-grace")), "threat grace hides when ThreatGraceLeft == 0");
            Check(screen.Panel.Query<Label>().ToList().Any(l => l.text == "Ready for the final rite."), "Easy site at 20 months reads as ready");
            screen.OpenDialog("guide");
            yield return Layout();
            var guideTexts = screen.Dialogs.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(guideTexts.Any(t => t.Contains("20 months")) && !guideTexts.Any(t => t.Contains("24 months")), "Easy guide displays 20 months, not 24");
            screen.Dialogs.Close();
            var easyLoss = madState.Clone();
            easyLoss.Status = "lost";
            easyLoss.ElapsedMonths = Balance.Deadline;
            var easyEnding = Notes.Ending(easyLoss);
            Check(easyEnding.Advice.Any(a => a.Contains("20 months")) && !easyEnding.Advice.Any(a => a.Contains("24 months")), "Easy ending advice uses 20 months");
            screen.Deselect();
            Debug.Log("UI_CHECK: madness, threat grace, and Easy site months");
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
                game.State = Awakening.Core.Dev.Playthrough.Play("early", outcome == "won" ? "measure-2" : "drowned", Balance.Deadline);
                Check(game.State.Status == outcome, outcome + " harness seed ends as " + outcome);
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

            // Conceal global cooldown stamp on the action row and detail pane.
            var concealState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "conceal-ui"), "africa").State;
            Derived.Recruit(concealState.Regions["europe"], 100);
            concealState.Awareness = 40;
            concealState.Influence = 200;
            concealState.Regions["africa"].Exposure = 30;
            concealState.Regions["europe"].Exposure = 30;
            game.State = concealState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            screen.Select("africa");
            yield return Layout();
            var concealRow = screen.Panel.Q<Button>("conceal");
            Check(concealRow != null && !concealRow.Query<Label>().ToList().Any(l => l.text.Contains("GLOBAL EFFECT IN")), "conceal row omits global cooldown stamp before concealing");
            game.Act("conceal", "africa");
            yield return Layout();
            Check(GameEngine.ConcealWorldCooldown(game.State) == Balance.ConcealWorldMonths, "concealing starts global awareness cooldown");
            screen.Select("europe");
            yield return Layout();
            concealRow = screen.Panel.Q<Button>("conceal");
            string expectedStamp = $"GLOBAL EFFECT IN {Balance.ConcealWorldMonths} MO";
            Check(concealRow.Query<Label>().ToList().Any(l => l.text == expectedStamp), "conceal row shows global cooldown stamp");
            screen.Panel.LayoutExplain("conceal");
            yield return Layout();
            var panelTexts = screen.Panel.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(panelTexts.Any(t => t == expectedStamp) && panelTexts.Any(t => t.Contains($"The world effect returns in {Balance.ConcealWorldMonths} months.")), "conceal detail shows stamp and preview copy");
            screen.Deselect();
            Debug.Log("UI_CHECK: conceal global cooldown stamp");

            // Rite margin strip: 3 sites, Dread/100, calendar dates, click-to-inspect detail, and negative-margin acceleration.
            var marginState = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "margin-ui"), "africa").State;
            marginState.ElapsedMonths = 185;
            marginState.Status = "playing";
            marginState.Dread = 85;
            marginState.Influence = 200;
            Derived.Recruit(marginState.Regions["oceania"], Derived.SiteRequirement(marginState.Regions["oceania"]) + 40);
            marginState.Regions["oceania"].Site = new Site { Invested = true, Progress = 24 };
            Derived.Recruit(marginState.Regions["middle_east"], Derived.SiteRequirement(marginState.Regions["middle_east"]) + 40);
            marginState.Regions["middle_east"].Site = new Site { Invested = true, Progress = 20 };
            marginState.Regions["middle_east"].Scattered = true;
            marginState.Regions["middle_east"].ScatteredUntil = 189;
            marginState.Regions["latin_america"].Population.Followers = 50;
            marginState.Regions["latin_america"].Site = new Site { Invested = true, Progress = 2 };
            game.State = marginState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            var riteBanner = screen.Q("rite-banner");
            Check(riteBanner != null && Kit.IsVisible(riteBanner), "rite margin strip appears when sites are funded");
            string marginLine = screen.Q<Label>("rite-margin-line").text;
            Check(marginLine.Contains(Format.Date(207)) && marginLine.Contains(Format.Date(212)) && marginLine.Contains("margin 5 months"), "rite margin line uses calendar dates and margin: " + marginLine);
            Check(screen.Q<Label>("rite-margin-dread").text == "DREAD 85/100", "rite margin strip displays Dread out of 100");
            Check(!Kit.IsVisible(screen.Q("concede-button")), "concede button stays hidden while the rite fits");
            var siteChips = Enumerable.Range(0, 3).Select(i => screen.Q<Button>("rite-site-" + i)).ToList();
            Check(siteChips.All(c => c != null), "rite margin strip renders three site chips");
            Check(siteChips.Any(c => c.text.Contains("scattered")), "site chip flags scattered flock");
            Check(siteChips.Any(c => c.text.Contains("50/")), "site chip flags unsupported site followers/required");
            ((Action)siteChips[1].userData)();
            yield return Layout();
            var siteDetail = screen.Q("rite-site-detail");
            Check(siteDetail != null && Kit.IsVisible(siteDetail), "clicking a site chip reveals site detail");
            string detailText = string.Join(" ", siteDetail.Query<Label>().ToList().Select(l => l.text));
            Check(detailText.Contains("followers") && detailText.Contains("Scattered"), "site detail reports followers and scattered status: " + detailText);
            ((Action)siteChips[1].userData)();
            yield return Layout();
            Check(!Kit.IsVisible(siteDetail), "clicking the same site chip collapses site detail");

            // Negative margin with Fits == true (eclipse available) means acceleration needed, not doomed.
            var accelState = marginState.Clone();
            accelState.ElapsedMonths = 190;
            accelState.Regions["latin_america"].Site.Progress = 0;
            accelState.Regions["latin_america"].Opportunity = new Opportunity { Kind = "eclipse", ExpiresMonth = 195 };
            var accelMargin = Forecast.Margin(accelState);
            Check(accelMargin.Margin < 0 && accelMargin.Fits && !GameEngine.CanConcede(accelState), "eclipse makes negative-margin state still fit");
            game.State = accelState;
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Check(screen.Q<Label>("rite-margin-line").text.Contains("needs acceleration"), "negative margin with Fits explains acceleration is needed");
            Check(!Kit.IsVisible(screen.Q("concede-button")), "concede button stays hidden when acceleration can save the rite");
            Debug.Log("UI_CHECK: rite margin strip and site details");

            // Doomed warning, concede button, confirmation dialog, and conceded ending with DatedFact.
            game.State = GameController.DoomedState(200);
            Check(!Forecast.Margin(game.State).Fits && GameEngine.CanConcede(game.State), "doomed harness state cannot fit and can concede");
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            screen.Dialogs.Close();
            yield return Layout();
            Check(screen.Q("rite-banner").Query<Label>().ToList().Any(l => l.text == Notes.RiteDoomedHeadline), "doomed state displays Lovecraftian warning headline");
            Check(Kit.IsVisible(screen.Q("rite-doomed-cause")) && !string.IsNullOrEmpty(screen.Q<Label>("rite-doomed-cause").text), "doomed state displays the cause");
            var concedeBtn = screen.Q<Button>("concede-button");
            Check(concedeBtn != null && Kit.IsVisible(concedeBtn), "concede button is visible when CanConcede is true");
            ((Action)concedeBtn.userData)();
            yield return Layout();
            Check(screen.Dialogs.Kind == "concede" && game.ClockHeld, "concede button opens confirmation dialog and holds clock");
            Press(screen.Dialogs, "Keep Watching");
            yield return Layout();
            Check(!screen.Dialogs.IsOpen && GameEngine.IsActive(game.State), "Keep Watching cancels conceding");
            ((Action)screen.Q<Button>("concede-button").userData)();
            yield return Layout();
            Press(screen.Dialogs, "LET THE STARS PASS");
            yield return Layout();
            Check(game.State.Status == "lost" && game.State.ResultCode == "conceded", "confirming concede ends run as conceded loss");
            Check(screen.Dialogs.Kind == "end" && game.ClockHeld, "conceding opens the ending sheet");
            var concededEnding = Notes.Ending(game.State);
            Check(concededEnding.Fact.StartsWith("You let the stars pass in " + Format.Date(game.State.ElapsedMonths), StringComparison.Ordinal), "conceded ending derives its own dated fact: " + concededEnding.Fact);
            var endLabels = screen.Dialogs.Query<Label>().ToList().Select(l => l.text).ToList();
            Check(endLabels.Any(t => t.Contains("The Stars Passed Unanswered")), "conceded ending shows its own title");
            Check(endLabels.Any(t => t.Contains(concededEnding.Fact)), "conceded ending renders its dated fact");
            var postCutoff = GameEngine.Concede(GameController.DoomedState(213));
            Check(postCutoff.ResultCode == "conceded" && Notes.DatedFact(postCutoff).Contains(Format.Date(213)), "post-cutoff conceded run derives a dated fact");
            Debug.Log("UI_CHECK: doomed warning, concede confirmation, and conceded ending");

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
            game.State = Awakening.Core.Dev.Playthrough.Play("early", "drowned", Balance.Deadline);
            game.ShowGame();
            yield return Layout();
            screen = (GameScreen)game.CurrentScreen;
            Press(screen.Dialogs, "New World");
            yield return Layout();
            Check(game.State == null && game.CurrentScreen is StartScreen && !game.ClockHeld, "New World returns to start screen");
            Debug.Log("UI_CHECKS_PASSED: start, difficulty rules, saved run, onboarding, goals, all sheets, clock, keyboard focus, participation, settings, talent tree, capstones, yields, world_forgets, rite halted, dreams, mysticism, bury, restore, cornered threat, abandon, endings, conceal stamp, rite margin, concede, retry");
        }
    }
}
#endif
