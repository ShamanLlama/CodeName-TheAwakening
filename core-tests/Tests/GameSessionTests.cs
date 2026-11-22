using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Awakening.Core.Session;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class GameSessionTests
    {
        sealed class SessionRig
        {
            public long Now;
            public long Generation;
            public bool Refuse;
            public int Cancellations;
            public readonly List<string> Writes = new List<string>();
            public readonly Queue<SessionVisibilityChange> Visibility = new Queue<SessionVisibilityChange>();
            public readonly GameSession Game;
            public SessionRig(bool joined = true, SaveEnvelope save = null, Dictionary<string, object> options = null, bool blocked = false)
            {
                int id = 0;
                var capture = new PlaytestCapture(() => 1800000000, () =>
                {
                    var bytes = new byte[16]; BitConverter.GetBytes(++id).CopyTo(bytes, 0); return bytes;
                });
                Game = new GameSession(save ?? SaveData.DefaultSave(), options ?? Configuration(), () => Now, envelope =>
                {
                    if (Refuse) return -1;
                    Writes.Add(SaveData.EncodeSave(envelope));
                    return ++Generation;
                }, capture, blocked, () =>
                {
                    var entries = Visibility.ToArray(); Visibility.Clear(); return entries;
                }, () => Cancellations++, () => "01234567");
                if (joined) Game.SetParticipation(true);
            }
            public void Start() { Game.Begin("normal", "clock-menu"); Game.Place("africa"); }
            public void Touch(long milliseconds) { Now += milliseconds; Game.LabTouch(); }
            public Dictionary<string, object> Clock => O(Game.Lab.Active(), "clock");
            public List<Dictionary<string, object>> Events => A(Game.Lab.Data, "staging").Cast<Dictionary<string, object>>().Select(e => O(e, "event")).ToList();
        }

        [Fact]
        public void ConcedingEndsTheRunOnlyWhenTheRiteNoLongerFits()
        {
            var rig = new SessionRig(); var game = rig.Game;
            rig.Start();
            var early = game.State;
            game.Concede();
            Assert.Same(early, game.State);
            Assert.DoesNotContain(rig.Events, e => Equals(e["type"], "run_finished"));

            game.Advance(Balance.LatestRiteStart + 1 - game.State.ElapsedMonths);
            Assert.False(GameEngine.IsOver(game.State));
            Assert.True(GameEngine.CanConcede(game.State));
            game.Concede();
            Assert.Equal("lost", game.State.Status);
            Assert.Equal("conceded", game.State.ResultCode);
            var terminal = Assert.Single(rig.Events, e => Equals(e["type"], "run_finished"));
            Assert.Same(rig.Events.Last(), terminal);
            Assert.Equal("lost", O(terminal, "data")["outcome"]);
            Assert.Equal("conceded", O(terminal, "data")["reason"]);
            GoldenTraces.RequireEqual(SaveData.ResultFor(game.State), game.LastResult);
        }

        [Fact]
        public void TakingATalentSavesLogsAndRefusesWithTheEnginesReason()
        {
            var rig = new SessionRig(); var game = rig.Game;
            var chronicle = new List<GameEvent>();
            game.Events += entries => chronicle.AddRange(entries);
            rig.Start();
            game.TakeTalent("swelling_flock");
            Assert.True(chronicle.Last().Refused);
            Assert.Equal("The next seal opens in 24 months.", chronicle.Last().Message);
            Assert.Empty(game.State.Talents);

            game.Advance(24);
            game.State.Influence = 100.0;
            int writes = rig.Writes.Count;
            game.TakeTalent("swelling_flock");
            Assert.Equal(new[] { "swelling_flock" }, game.State.Talents);
            Assert.Equal(60.0, game.State.Influence);
            Assert.False(chronicle.Last().Refused);
            Assert.Equal(game.State.Log.Last().Message, chronicle.Last().Message);
            Assert.True(rig.Writes.Count > writes);
            Assert.Contains("\"talents\":[\"swelling_flock\"]", rig.Writes.Last());
            var emitted = rig.Events.Last(e => Equals(e["type"], "talent_taken"));
            var data = O(emitted, "data");
            Assert.Equal("swelling_flock", data["talent"]); Assert.Equal("congregation", data["branch"]);
            Assert.Equal(1, N(data, "tier")); Assert.Equal(1, N(data, "slot")); Assert.Equal(40, N(O(data, "spent"), "influence"));

            game.TakeTalent("open_worship");
            Assert.True(chronicle.Last().Refused);
            Assert.Equal("The next seal opens in 40 months.", chronicle.Last().Message);
        }

        [Fact]
        public void BuryAndRestoreRunThroughTheSessionAsActions()
        {
            var rig = new SessionRig(); var game = rig.Game;
            rig.Start();
            game.State.Awareness = 20.0; game.State.Influence = 200.0;
            game.Act("bury", "africa");
            Assert.Equal(15.0, game.State.Awareness); Assert.Equal(1, game.State.BuryUses);
            var emitted = rig.Events.Last(e => Equals(e["type"], "action_performed"));
            Assert.Equal("bury", O(emitted, "data")["action"]);
            Assert.Equal(50, N(O(O(emitted, "data"), "spent"), "influence"));
        }

        // Protects actual command emission, including dynamic costs and refused decisions; capture-only tests supply costs by hand.
        [Theory]
        [MemberData(nameof(GoldenTests.Traces), MemberType = typeof(GoldenTests))]
        public void GameCommandsEmitActualCostsAndPreserveGoldenOutcomes(string file)
        {
            var trace = GoldenTraces.LoadTrace(Path.Combine(GoldenTests.GoldenDirectory, file));
            var rig = new SessionRig(); var game = rig.Game;
            var chronicle = new List<GameEvent>();
            game.Events += entries => chronicle.AddRange(entries);
            game.Begin((string)trace["difficulty"], (string)trace["seed"]);
            GoldenTraces.RequireEqual(trace["initial"], GoldenTraces.Snapshot(game.State, 1, O(trace, "initial").ContainsKey("previews")), file + " initial");
            int logFrom = game.State.NextLogId;
            double influence = 0, dread = 0, followers = 0;
            bool firstSiteSeen = false;
            foreach (Dictionary<string, object> step in A(trace, "steps"))
            {
                foreach (Dictionary<string, object> command in A(step, "commands"))
                {
                    var before = game.State;
                    int count = rig.Events.Count, entryCount = chronicle.Count;
                    switch ((string)command["op"])
                    {
                        case "place": game.Place((string)command["region"]); break;
                        case "action":
                            GoldenTraces.RequireEqual(command["preview"], GoldenTraces.PreviewJson(GameEngine.PreviewAction(before, (string)command["id"], (string)command["region"])));
                            game.Act((string)command["id"], (string)command["region"]); break;
                        case "doctrine": game.TakeTalent(GoldenTraces.DoctrineRoot((string)command["choice"])); break;
                        case "awaken": game.Awaken(); break;
                        default: throw new InvalidDataException("Unknown command");
                    }
                    Assert.Equal(entryCount + 1, chronicle.Count);
                    Assert.Equal(!(bool)command["ok"], chronicle.Last().Refused);
                    if (!(bool)command["ok"])
                    {
                        Assert.Same(before, game.State);
                        Assert.Equal(command["message"], chronicle.Last().Message);
                        Assert.Equal(count, rig.Events.Count);
                        continue;
                    }
                    Assert.Equal(game.State.Log.Last().Message, chronicle.Last().Message);
                    AssertTransitions(before, game.State, rig.Events.Skip(count).ToList(), ref firstSiteSeen);
                    var emitted = rig.Events.Skip(count).First();
                    string kind = (string)command["op"];
                    Assert.Equal(kind == "place" ? "starting_region_selected" : kind == "action" ? "action_performed" : kind == "doctrine" ? "talent_taken" : "awakening_started", emitted["type"]);
                    var data = O(emitted, "data");
                    if (kind == "place") { Assert.False(data.ContainsKey("spent")); continue; }
                    var spent = O(data, "spent");
                    // Independent engine counters, not GameSession.Spent: catches previews or resource-balance deltas used as costs.
                    Assert.Equal(game.State.Telemetry.InfluenceSpent - before.Telemetry.InfluenceSpent, N(spent, "influence"));
                    Assert.Equal(game.State.Telemetry.DreadSpent - before.Telemetry.DreadSpent, N(spent, "dread"));
                    Assert.Equal(game.State.Telemetry.Sacrificed - before.Telemetry.Sacrificed, N(spent, "followers"));
                    influence += N(spent, "influence"); dread += N(spent, "dread"); followers += N(spent, "followers");
                }
                var beforeAdvance = game.State;
                int advanceEvents = rig.Events.Count;
                game.Advance(Convert.ToInt32(step["months"]));
                if (!ReferenceEquals(beforeAdvance, game.State)) AssertTransitions(beforeAdvance, game.State, rig.Events.Skip(advanceEvents).ToList(), ref firstSiteSeen);
                GoldenTraces.RequireEqual(step["after"], GoldenTraces.Snapshot(game.State, logFrom, O(step, "after").ContainsKey("previews")), file);
                logFrom = game.State.NextLogId;
            }
            Assert.Equal(trace["final"], game.State.Status);
            Assert.Equal(game.State.Telemetry.InfluenceSpent, influence, 8);
            Assert.Equal(game.State.Telemetry.DreadSpent, dread, 8);
            Assert.Equal(game.State.Telemetry.Sacrificed, followers, 8);
            Assert.Equal(game.State.ElapsedMonths >= Balance.PreparationMonths ? 1 : 0, rig.Events.Count(e => Equals(e["type"], "phase_changed")));
            var checkpoints = rig.Events.Where(e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "first_site_ready")).ToList();
            Assert.True(checkpoints.Count <= 1);
            if (GameEngine.IsOver(game.State))
            {
                var terminal = Assert.Single(rig.Events, e => Equals(e["type"], "run_finished"));
                Assert.Same(rig.Events.Last(), terminal);
                Assert.Equal(game.State.Status, O(terminal, "data")["outcome"]);
                GoldenTraces.RequireEqual(SaveData.ResultFor(game.State), game.LastResult);
                Assert.True(game.SubmitPlaytestComment("Synthetic session golden run"));
            }
            while (A(game.Lab.Data, "staging").Count != 0) Assert.NotEmpty(game.Lab.SealBatch());
            var jobs = A(game.Lab.Data, "jobs").Cast<Dictionary<string, object>>().ToList();
            Assert.NotEmpty(jobs);
            Assert.All(jobs, job =>
            {
                var body = Body(job);
                Assert.Equal("awakening-5", body["adapter"]);
                Assert.All(A(body, "events"), e => Assert.True(PlaytestCapture.ValidEvent(e)));
            });
            string output = Environment.GetEnvironmentVariable("AWAKENING_PLAYTEST_BATCH_OUTPUT");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                for (int i = 0; i < jobs.Count; i++) File.WriteAllText(Path.Combine(output, $"batch-session-{file.Replace(".json.gz", "")}-{i + 1:00}.json"), (string)jobs[i]["body"]);
            }
        }

        [Theory]
        [InlineData("counter")]
        [InlineData("scatter")]
        [InlineData("silence")]
        public void ThreatResponsesReportTheLegalChoicesBeforeSpending(string response)
        {
            var r = new SessionRig(); r.Start(); var g = r.Game;
            g.State.Influence = 200; g.State.Dread = 200;
            g.State.Regions["africa"].Population.Followers = Derived.SiteRequirement(g.State.Regions["africa"]) + 100;
            g.Act("prepare", "africa");
            var spawn = O(Assert.Single(r.Events, e => Equals(e["type"], "threat_spawned")), "data");
            Assert.Equal("africa", spawn["region"]); Assert.Equal("occult", spawn["kind"]);
            Assert.Equal(g.State.Regions["africa"].Threat.DueMonth, N(spawn, "due_month"));
            var expected = new[] { "counter", "scatter", "silence" }.Where(a => GameEngine.PreviewAction(g.State, a, "africa").Ok).ToList();
            Assert.Equal(expected, A(spawn, "legal_responses").Cast<string>());
            Assert.Equal(Threats.Cornered(g.State, "africa"), spawn["cornered"]);
            g.Act(response, "africa");
            var resolved = O(Assert.Single(r.Events, e => Equals(e["type"], "threat_resolved")), "data");
            Assert.Equal(response, resolved["response"]);
            Assert.Equal(expected, A(resolved, "legal_responses").Cast<string>());
            Assert.All(O(resolved, "losses").Values, v => Assert.Equal(0, Convert.ToDouble(v)));
            Assert.Null(g.State.Regions["africa"].Threat);
        }

        [Fact]
        public void ExpiredThreatRecordsActualLossesDespiteSameMonthGrowthAndNoAffordableCounter()
        {
            var r = new SessionRig(); r.Start(); var g = r.Game;
            var region = g.State.Regions["africa"];
            region.Population.Followers = 150; region.Site.Invested = true; region.Site.Progress = 20;
            region.Threat = new Threat { Kind = "occult", Severity = 2, DueMonth = 1 };
            g.State.Influence = 0; g.State.Dread = 0;
            var expected = Threats.Consequences(g.State, region);
            g.Advance(1);
            var data = O(Assert.Single(r.Events, e => Equals(e["type"], "threat_resolved")), "data");
            Assert.Equal("ignored", data["response"]);
            Assert.Equal(new[] { "scatter" }, A(data, "legal_responses").Cast<string>());
            Assert.Equal(expected.Followers, N(O(data, "losses"), "followers"), 8);
            Assert.Equal(expected.PreparationLost, N(O(data, "losses"), "preparation_months"));
            Assert.Equal(expected.IncomeLost, N(O(data, "losses"), "income_per_month"), 8);
        }

        [Fact]
        public void IgnoredThreatReportsTheWholeSiteWhenItsCellIsDestroyed()
        {
            var r = new SessionRig(); r.Start(); var g = r.Game;
            var region = g.State.Regions["africa"];
            region.Population.Followers = 1; region.Site.Invested = true; region.Site.Progress = 30;
            region.Threat = new Threat { Kind = "raid", Severity = 3, DueMonth = 1 };
            g.Advance(1);
            var losses = O(O(Assert.Single(r.Events, e => Equals(e["type"], "threat_resolved")), "data"), "losses");
            Assert.Equal(1, N(losses, "followers")); Assert.Equal(1, N(losses, "cells"));
            Assert.Equal(1, N(losses, "sites")); Assert.Equal(30, N(losses, "preparation_months"));
        }

        [Fact]
        public void LongAdvancesKeepEveryAnnualSnapshotAndThreatLifecycleWithoutChangingTheEngine()
        {
            var r = new SessionRig(); r.Start(); var g = r.Game;
            var expected = GameEngine.AdvanceSimulation(g.State, 48);
            g.Advance(48);
            GoldenTraces.RequireEqual(GoldenTraces.Snapshot(expected, 1, true), GoldenTraces.Snapshot(g.State, 1, true));
            var snapshots = r.Events.Where(e => Equals(e["type"], "month_snapshot")).ToList();
            Assert.Equal(new[] { 12, 24, 36, 48 }, snapshots.Select(e => (int)N(e, "game_month")));
            Assert.Equal(g.State.Awareness, N(O(snapshots.Last(), "data"), "awareness"));
            Assert.Equal(g.State.Talents.Count, N(O(snapshots.Last(), "data"), "talents"));
            Assert.Equal(GameEngine.TalentSlotsUnused(g.State), N(O(snapshots.Last(), "data"), "unused_slots"));
            Assert.Equal(g.State.BuryUses, N(O(snapshots.Last(), "data"), "bury_uses"));
            Assert.Equal(Derived.TotalFollowers(g.State), N(O(snapshots.Last(), "data"), "followers"));
            var spawns = r.Events.Where(e => Equals(e["type"], "threat_spawned")).ToList();
            var resolutions = r.Events.Where(e => Equals(e["type"], "threat_resolved")).ToList();
            Assert.NotEmpty(spawns); Assert.NotEmpty(resolutions);
            Assert.All(resolutions, e => Assert.Contains(spawns, s => N(s, "sequence") < N(e, "sequence") && Equals(O(s, "data")["due_month"], O(e, "data")["due_month"])));
        }

        [Fact]
        public void SpeedEventsIncludeAutomaticPausesAndTerminalIsAlwaysLast()
        {
            var r = new SessionRig(); r.Start(); var g = r.Game;
            g.SetSpeed(1); g.SetSpeed(1); g.SetSpeed(2);
            g.State.ElapsedMonths = 199;
            g.Advance(3);
            Assert.Equal(200, g.State.ElapsedMonths);
            var speeds = r.Events.Where(e => Equals(e["type"], "speed_changed")).ToList();
            Assert.Equal(2, speeds.Count);
            Assert.Equal(2, N(O(speeds.Last(), "data"), "previous"));
            Assert.Equal(0, N(O(speeds.Last(), "data"), "new"));
            Assert.True(r.Events.FindIndex(e => Equals(e["type"], "phase_changed")) < r.Events.IndexOf(speeds.Last()));
            g.SetSpeed(1); g.State.ElapsedMonths = 215; g.State.Awareness = 100;
            g.Advance(1);
            Assert.Equal("run_finished", r.Events.Last()["type"]);
            Assert.Equal("month_snapshot", r.Events[r.Events.Count - 3]["type"]);
            Assert.Equal("alignment", r.Events[r.Events.Count - 3]["phase"]);
            Assert.Equal("world_awareness", O(r.Events.Last(), "data")["reason"]);
        }

        static void AssertTransitions(GameState before, GameState after, List<Dictionary<string, object>> events, ref bool firstSiteSeen)
        {
            var phases = events.Where(e => Equals(e["type"], "phase_changed")).ToList();
            Assert.Equal(before.Status == "playing" && after.Status == "alignment" ? 1 : 0, phases.Count);
            if (phases.Count != 0) Assert.Equal("playing", O(phases.Single(), "data")["from_phase"]);
            var terminals = events.Where(e => Equals(e["type"], "run_finished")).ToList();
            Assert.Equal(GameEngine.IsOver(after) ? 1 : 0, terminals.Count);
            if (terminals.Count != 0)
            {
                Assert.Equal(after.Status, O(terminals.Single(), "data")["outcome"]);
                Assert.Equal(after.Status == "won" ? "awakened" : after.Awareness >= 100 ? "world_awareness" : "deadline", O(terminals.Single(), "data")["reason"]);
                return;
            }
            string firstReady = GameData.RegionIds.FirstOrDefault(id => after.Regions[id].Site.Progress >= Balance.SiteMonths && Derived.Supported(after.Regions[id])
                && (before.Regions[id].Site.Progress < Balance.SiteMonths || !Derived.Supported(before.Regions[id])));
            var checkpoints = events.Where(e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "first_site_ready")).ToList();
            Assert.Equal(firstReady != null && !firstSiteSeen ? 1 : 0, checkpoints.Count);
            if (checkpoints.Count != 0)
            {
                Assert.Equal(firstReady, O(checkpoints.Single(), "data")["region"]);
                firstSiteSeen = true;
            }
        }

        [Fact]
        public void OpenGameMenuCountsPlanningNotSimulationOrExcluded()
        {
            var rig = new SessionRig(); var game = rig.Game; rig.Start();
            game.SetSpeed(1); game.Hold();
            Assert.Equal("planning", game.LabCurrentMode());
            rig.Touch(1234);
            Assert.Equal(1234, N(rig.Clock, "planning_ms"));
            Assert.Equal(0, N(rig.Clock, "simulation_ms"));
            Assert.Equal(0, N(rig.Clock, "excluded_ms"));
            game.OnApplicationFocus(false);
            Assert.Equal("background", game.LabCurrentMode());
            rig.Touch(2345);
            Assert.Equal(2345, N(rig.Clock, "background_ms"));
        }

        [Fact]
        public void StartScreenAsksOnlyWhenAWritableRoundHasNoAnswer()
        {
            var r = new SessionRig(joined: false); var g = r.Game;
            Assert.Equal(SessionScreen.Start, g.Screen);
            Assert.True(g.NeedsParticipationChoice());
            g.Blocked = true; Assert.False(g.NeedsParticipationChoice());
            g.SetParticipation(true); Assert.False(g.Lab.Confirmed);
            g.Blocked = false; g.DeclineParticipation();
            Assert.False(g.NeedsParticipationChoice()); Assert.Empty(O(g.Lab.Data, "testers"));
            var saved = SaveData.DecodeSave(r.Writes.Last());
            Assert.False(new SessionRig(false, saved).Game.NeedsParticipationChoice());
            var options = Configuration(); options["round_id"] = "10000000-0000-4000-8000-000000000009";
            Assert.True(new SessionRig(false, saved, options).Game.NeedsParticipationChoice());
            Assert.False(new SessionRig(false, options: M()).Game.NeedsParticipationChoice());
            Assert.False(new SessionRig(false, new SaveEnvelope { Playtest = M("version", 1, "jobs", "broken") }).Game.NeedsParticipationChoice());
            g.SetParticipation(true); Assert.False(g.NeedsParticipationChoice());
            var yes = SaveData.DecodeSave(r.Writes.Last());
            Assert.True(new SessionRig(false, yes).Game.Lab.Confirmed);
            var readOnly = new SessionRig(false, yes, blocked: true).Game;
            Assert.False(readOnly.Lab.Confirmed); Assert.False(readOnly.NeedsParticipationChoice());
        }

        [Fact]
        public void ClockPartitionsScreensPauseDialogsRunningAndBackgroundWithTimestampedVisibility()
        {
            var r = new SessionRig(); var g = r.Game; r.Start();
            g.ShowStart(); r.Touch(1000);
            g.ShowGame(); g.SetSpeed(0); r.Touch(2000);
            g.SetSpeed(1); r.Touch(3000);
            g.Hold(); g.Hold(); r.Touch(4000);
            g.Release(); r.Touch(5000);
            Assert.Equal(1, g.Holds);
            g.Release(); g.Release(); Assert.Equal(0, g.Holds);
            // A suspended frame drains browser observations at their actual times, not at frame time.
            r.Visibility.Enqueue(new SessionVisibilityChange(r.Now + 1000, true, false));
            r.Visibility.Enqueue(new SessionVisibilityChange(r.Now + 41000, false, true));
            r.Visibility.Enqueue(new SessionVisibilityChange(r.Now + 43000, true, true));
            r.Touch(45000);
            Assert.Equal(6000, N(r.Clock, "simulation_ms"));
            Assert.Equal(11000, N(r.Clock, "planning_ms"));
            Assert.Equal(42000, N(r.Clock, "background_ms"));
            Assert.Equal(1000, N(r.Clock, "excluded_ms"));
            Assert.Equal(60000, N(r.Clock, "elapsed_ms"));
            Assert.DoesNotContain(r.Events, e => Equals(e["type"], "capture_gap"));
            Assert.Equal(new[] { "visibility_changed", "visibility_changed", "focus_changed" }, r.Events.Where(e => Equals(e["type"], "time_checkpoint")).Select(e => (string)O(e, "data")["reason"]).TakeLast(3));
            g.Advance(Balance.Deadline); // Engine pauses at alignment before the deadline.
            g.Advance(Balance.Deadline);
            Assert.Equal("excluded", g.LabCurrentMode());
            var terminalClock = JsonWriter.Write(r.Clock);
            r.Touch(2000); g.Process(0);
            // Terminal events close capture: post-end time has excluded mode but cannot extend its closed clock.
            Assert.Equal(terminalClock, JsonWriter.Write(r.Clock));
        }

        [Theory]
        [InlineData(30000, false)]
        [InlineData(30001, true)]
        public void ForegroundStallOverThirtySecondsIsExcludedWithACaptureGap(long milliseconds, bool gap)
        {
            var r = new SessionRig(); r.Start(); r.Game.SetSpeed(1);
            r.Touch(milliseconds);
            Assert.Equal(gap ? 0 : milliseconds, N(r.Clock, "simulation_ms"));
            Assert.Equal(gap ? milliseconds : 0, N(r.Clock, "excluded_ms"));
            var gaps = r.Events.Where(e => Equals(e["type"], "capture_gap")).ToList();
            Assert.Equal(gap ? 1 : 0, gaps.Count);
            if (gap) GoldenTraces.RequireEqual(M("reason", "unknown_time", "missing_events", null), O(gaps.Single(), "data"));
        }

        [Fact]
        public void ProcessPersistsAtFiveSecondsAndCheckpointsAtTwentySeconds()
        {
            var r = new SessionRig(); r.Start(); r.Game.SetSpeed(0);
            long initial = r.Generation;
            r.Now = 4999; r.Game.Process(0); Assert.Equal(initial, r.Generation);
            r.Now = 5000; r.Game.Process(0); Assert.Equal(initial + 1, r.Generation);
            r.Now = 19999; r.Game.Process(0); Assert.Equal(initial + 2, r.Generation);
            int checkpoints = r.Events.Count;
            r.Now = 20000; r.Game.Process(0);
            Assert.Equal(initial + 3, r.Generation);
            Assert.Equal(checkpoints + 1, r.Events.Count);
            Assert.Equal("periodic", O(r.Events.Last(), "data")["reason"]);
            Assert.Equal(20000, N(O(r.Events.Last(), "clock"), "planning_ms"));
            r.Refuse = true; r.Now = 25000; r.Game.Process(0);
            Assert.True(r.Game.Blocked); Assert.False(r.Game.Lab.Capturing());
            int writes = r.Writes.Count; r.Game.Act("recruit", "africa");
            Assert.Equal(writes, r.Writes.Count);
        }

        [Fact]
        public void SelectingTheCurrentSpeedPreservesFractionalSimulationAndDoesNotEmitOrPersist()
        {
            var r = new SessionRig(); var g = r.Game; r.Start(); g.SetSpeed(1);
            g.Process(0.5); g.Process(0.5); g.Process(0.5);
            long writes = r.Generation; int events = r.Events.Count; var state = g.State;
            g.SetSpeed(1);
            Assert.Same(state, g.State); Assert.Equal(writes, r.Generation); Assert.Equal(events, r.Events.Count);
            g.Process(0.5); Assert.Equal(1, g.State.ElapsedMonths);
        }

        [Theory]
        [InlineData("  seed  ", "seed")]
        [InlineData("\u001fseed\u001f", "seed")]
        [InlineData("\u00a0seed\u00a0", "\u00a0seed\u00a0")]
        public void BeginPreservesGodotSeedTrimming(string input, string expected)
        {
            var r = new SessionRig(); r.Game.Begin("normal", input);
            Assert.Equal(expected, r.Game.State.Seed);
            Assert.Equal(expected, O(r.Events.First(), "data")["seed"]);
        }

        [Fact]
        public void ProcessCapsFrameSimulationAndHonorsHoldsScreenAndPause()
        {
            var r = new SessionRig(); var g = r.Game; r.Start(); g.SetSpeed(2);
            g.Process(10); Assert.Equal(0, g.State.ElapsedMonths);
            g.Hold(); g.Process(10); g.Release(); Assert.Equal(0, g.State.ElapsedMonths);
            g.Process(10); Assert.Equal(1, g.State.ElapsedMonths);
            g.SetSpeed(0); g.Process(10); Assert.Equal(1, g.State.ElapsedMonths);
            g.SetSpeed(1); g.ShowStart(); g.Process(10); Assert.Equal(1, g.State.ElapsedMonths);
            g.ShowGame(); g.DebugAdvance(2); Assert.Equal(3, g.State.ElapsedMonths);
            Assert.Single(r.Events, e => Equals(e["type"], "capture_gap"));
        }

        [Fact]
        public void ReopenedRunResumesWithAFreshClockAndStorageFailureStopsCapture()
        {
            var r = new SessionRig(); r.Start(); r.Touch(1200); r.Game.Commit();
            string run = (string)r.Game.Lab.Data["active"];
            var loaded = new SessionRig(false, SaveData.DecodeSave(r.Writes.Last()));
            Assert.True(loaded.Game.Lab.Confirmed); Assert.False(loaded.Game.NeedsParticipationChoice());
            loaded.Game.ShowGame();
            Assert.Equal(run, loaded.Game.Lab.Data["active"]); Assert.Equal(0, N(loaded.Clock, "elapsed_ms"));
            Assert.Equal("session_started", loaded.Events[loaded.Events.Count - 2]["type"]);
            Assert.Equal("resume", O(loaded.Events[loaded.Events.Count - 2], "data")["mode"]);
            Assert.Equal("capture_gap", loaded.Events.Last()["type"]);
            loaded.Game.Process(0, storageFailed: true);
            Assert.True(loaded.Game.Blocked); Assert.False(loaded.Game.Lab.Capturing());
        }

        [Fact]
        public void CommandsPersistEnvelopeTutorialAndWithdrawalAndReportChanges()
        {
            var r = new SessionRig(); var g = r.Game; int changed = 0; g.Changed += () => changed++;
            g.Begin("normal", "  "); Assert.Equal("01234567", g.State.Seed);
            g.CompleteTutorial(); g.CompleteTutorial();
            Assert.Single(r.Events, e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "tutorial_completed"));
            g.SetAudio(false, 73); g.SetUiScale(1.6); g.SetReducedMotion(true); g.MarkHint("first"); g.MarkHint("first");
            var saved = SaveData.DecodeSave(r.Writes.Last());
            Assert.True(saved.TutorialComplete); Assert.False(saved.Settings.Muted); Assert.Equal(73, saved.Settings.Volume);
            Assert.Equal(1.6, saved.Settings.UiScale); Assert.True(saved.Settings.ReducedMotion); Assert.Single(saved.Settings.Hints);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(g.State), GameStateJson.ToJson(saved.State));
            Assert.True(changed > 0);
            g.SetParticipation(false); Assert.Equal(1, r.Cancellations);
            Assert.False(g.Lab.Capturing()); Assert.True(g.Lab.Answered()); Assert.Equal(0, g.Lab.PendingEvents());
            g.SetParticipation(true); Assert.True(g.Lab.Capturing());
            g.Abandon(); Assert.Null(g.State); Assert.Equal(SessionScreen.Start, g.Screen);
            Assert.Equal("run_abandoned", O(g.Lab.Active(), "last_event")["type"]);
            Assert.Null(SaveData.DecodeSave(r.Writes.Last()).State);
        }
    }
}
