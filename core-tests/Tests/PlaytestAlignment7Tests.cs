using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Awakening.Core.Session;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class PlaytestAlignment7Tests
    {
        static GameSession Session(string difficulty = "normal")
        {
            var game = new GameSession(SaveData.DefaultSave(), Configuration(), () => 0, _ => 1);
            game.SetParticipation(true);
            game.Begin(difficulty, "telemetry-alignment-7"); game.Place("africa");
            return game;
        }
        static List<Dictionary<string, object>> Events(GameSession game) => A(game.Lab.Data, "staging").Cast<Dictionary<string, object>>().Select(e => O(e, "event")).ToList();
        static RegionState Cell(GameSession game, string id, int progress = -1, double madness = 0)
        {
            var r = game.State.Regions[id];
            Derived.Recruit(r, Math.Max(0, Derived.SiteRequirement(r) + 200 - r.Population.Followers));
            r.Madness = madness;
            if (progress >= 0) r.Site = new Site { Invested = true, Progress = progress };
            return r;
        }

        [Fact]
        public void FreshAndPartialCapturesUseTheNewAdapterAndRuleset()
        {
            var game = Session();
            Assert.Equal("alignment-7", O(Events(game).First(), "data")["ruleset"]);
            Assert.Equal("awakening-6", O(game.Lab.Active(), "context")["adapter"]);
            game.SetParticipation(false); game.SetParticipation(true);
            var partial = Events(game).First();
            Assert.Equal("capture_started", partial["type"]);
            Assert.Equal("alignment-7", O(partial, "data")["ruleset"]);
            Assert.True(PlaytestCapture.ValidEvent(partial));
            O(partial, "data")["ruleset"] = "alignment-6";
            Assert.False(PlaytestCapture.ValidEvent(partial));
        }

        [Fact]
        public void PendingV5BatchesStayImmutableWhenV6BeginsPartialCapture()
        {
            var rig = new Rig(); rig.Start();
            O(rig.Capture.Active(), "context")["adapter"] = "awakening-5";
            var job = rig.Capture.SealBatch();
            var oldBody = Body(job);
            O(Item(oldBody, "events", 0), "data")["ruleset"] = "alignment-6";
            job["body"] = JsonWriter.Write(oldBody);
            string original = (string)job["body"];
            var restored = new PlaytestCapture(() => rig.Unix);
            restored.Configure(Configuration(), Saved(rig.Capture, rig.State));
            Assert.False(restored.Corrupt);
            Assert.True(restored.Restore()); restored.Resume(rig.State);
            Assert.Equal(original, Item(restored.Data, "jobs", 0)["body"]);
            Assert.Equal("awakening-6", O(restored.Active(), "context")["adapter"]);
            var start = O(Item(restored.Data, "staging", 0), "event");
            Assert.Equal("capture_started", start["type"]);
            Assert.True(PlaytestCapture.ValidEvent(start));
            Assert.True(PlaytestCapture.ValidStore(restored.Snapshot(rig.State)));
        }

        [Fact]
        public void AnnualSnapshotObservesCultMadnessAndOnlyUnfinishedQuickenedSites()
        {
            var game = Session();
            game.State.ElapsedMonths = 11; game.State.Influence = 1000;
            Cell(game, "africa", 3, 60);
            Cell(game, "europe", GameEngine.SiteMonths(game.State), 80); // Ready sites do not quicken.
            Cell(game, "latin_america"); // Include a cult even when Madness is zero.
            game.State.Regions["oceania"].Madness = 90; // A region without a cult is absent.
            game.Advance(1);
            var data = O(Assert.Single(Events(game), e => Equals(e["type"], "month_snapshot")), "data");
            var expected = game.State.Regions.Where(p => p.Value.Population.Followers > 0).ToDictionary(p => p.Key, p => (object)p.Value.Madness);
            Assert.Equal(JsonWriter.Write(expected), JsonWriter.Write(O(data, "madness")));
            Assert.Equal(0, N(O(data, "madness"), "latin_america"));
            Assert.False(O(data, "madness").ContainsKey("oceania"));
            Assert.Equal(GameEngine.MadnessDreadTotal(game.State), N(data, "madness_dread"), 9);
            Assert.Equal(1, N(data, "quickened_sites"));
            Assert.Equal(GameData.RegionIds.Count(id => GameEngine.MadnessQuickens(game.State, id)), N(data, "quickened_sites"));
        }

        [Theory]
        [InlineData("playing")]
        [InlineData("alignment")]
        [InlineData("awakening")]
        public void ManifestAndSacrificeReportMadnessAfterTheAcceptedAction(string phase)
        {
            var game = Session();
            game.State.Status = phase;
            game.State.ElapsedMonths = phase == "playing" ? 40 : Balance.PreparationMonths;
            game.State.Influence = 1000; game.State.Dread = 200;
            Cell(game, "africa", madness: 85);
            foreach (string action in new[] { "manifest", "sacrifice" })
            {
                Assert.True(GameEngine.PreviewAction(game.State, action, "africa").Ok);
                game.Act(action, "africa");
                var entry = Events(game).Last(e => Equals(e["type"], "action_performed"));
                Assert.Equal(game.State.Regions["africa"].Madness, N(O(entry, "data"), "madness_after"));
                Assert.Equal(100, N(O(entry, "data"), "madness_after"));
                Assert.Equal(action == "sacrifice" && phase == "awakening", O(entry, "data").ContainsKey("rite_advanced"));
                Assert.True(PlaytestCapture.ValidEvent(entry));
            }
            game.Act("recruit", "africa");
            Assert.False(O(Events(game).Last(e => Equals(e["type"], "action_performed")), "data").ContainsKey("madness_after"));
            int count = Events(game).Count;
            game.Act("manifest", "africa"); // Still on cooldown: refused commands emit nothing.
            Assert.Equal(count, Events(game).Count);
        }

        [Theory]
        [InlineData("counter")]
        [InlineData("silence")]
        [InlineData("scatter")]
        [InlineData("ignored")]
        [InlineData("forgotten")]
        public void ResolutionReportsOnlyTheGraceGrantedByThisResponse(string response)
        {
            var game = Session();
            game.State.ElapsedMonths = 40; game.State.Influence = 1000; game.State.Dread = 200;
            var r = Cell(game, "africa");
            r.Threat = new Threat { Kind = "occult", Severity = 1, DueMonth = 41 };
            r.ThreatGraceUntil = 44; // An unrelated existing grace must not leak into other resolutions.
            if (response == "ignored") game.Advance(1);
            else if (response == "forgotten")
            {
                game.State.Talents.Add("world_forgets"); game.State.TalentYield["world_forgets"] = 0;
                game.InvokeCapstone("world_forgets");
            }
            else game.Act(response, "africa");
            var entry = Assert.Single(Events(game), e => Equals(e["type"], "threat_resolved"));
            var data = O(entry, "data");
            Assert.Equal(response, data["response"]);
            int expected = response == "counter" || response == "silence" ? GameEngine.ThreatGraceLeft(game.State, "africa") : 0;
            Assert.Equal(expected, N(data, "grace_months"));
            if (expected > 0) Assert.Equal(Balance.ThreatGraceMonths, expected);
            Assert.True(PlaytestCapture.ValidEvent(entry));
            Assert.DoesNotContain(Events(game), e => Equals(e["type"], "threat_grace"));
        }

        [Theory]
        [InlineData("easy", 20)]
        [InlineData("normal", 24)]
        public void FirstSiteReadyFiresAtTheDifficultysPreparationBoundary(string difficulty, int months)
        {
            var game = Session(difficulty);
            game.State.Influence = 1000;
            Cell(game, "africa", months - 2);
            game.Advance(1);
            Assert.DoesNotContain(Events(game), e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "first_site_ready"));
            game.Advance(1);
            Assert.Equal(months, game.State.Regions["africa"].Site.Progress);
            var ready = Assert.Single(Events(game), e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "first_site_ready"));
            Assert.Equal("africa", O(ready, "data")["region"]);
            Assert.Equal(2, N(ready, "game_month"));
            game.Advance(1);
            Assert.Single(Events(game), e => Equals(e["type"], "checkpoint_reached") && Equals(O(e, "data")["checkpoint"], "first_site_ready"));
        }

        static Dictionary<string, object> Observation(string kind, Dictionary<string, object> data) => M(
            "event_id", PlaytestCapture.Identifier(), "sequence", 4, "type", kind, "phase", "playing", "game_month", 12, "clock", PlaytestCapture.EmptyClock(), "data", data);

        [Fact]
        public void ValidatorEnforcesAllNewRequiredFieldsRangesAndForbiddenFields()
        {
            var snapshot = Observation("month_snapshot", M("awareness", 0, "influence", 0, "dread", 0, "followers", 100, "cells", 1, "funded_sites", 0, "ready_sites", 0, "talents", 0, "unused_slots", 0, "bury_uses", 0, "yields", M(), "madness", M("africa", 0, "europe", 100), "madness_dread", 0, "quickened_sites", 8));
            void Reject(Dictionary<string, object> source, Action<Dictionary<string, object>> mutate)
            {
                Assert.True(PlaytestCapture.ValidEvent(source));
                var copy = (Dictionary<string, object>)JsonReader.Parse(JsonWriter.Write(source));
                mutate(O(copy, "data"));
                Assert.False(PlaytestCapture.ValidEvent(copy));
            }
            foreach (string field in new[] { "madness", "madness_dread", "quickened_sites" }) Reject(snapshot, d => d.Remove(field));
            foreach (object invalid in new object[] { -1, 101, "50", true, null, double.NaN, double.PositiveInfinity }) Reject(snapshot, d => d["madness"] = M("africa", invalid));
            Reject(snapshot, d => d["madness"] = M("moon", 0));
            Reject(snapshot, d => d["madness"] = new List<object>());
            foreach (object invalid in new object[] { -1, "2", null, double.NaN, double.PositiveInfinity }) Reject(snapshot, d => d["madness_dread"] = invalid);
            foreach (object invalid in new object[] { -1, 9, 1.5, true, null }) Reject(snapshot, d => d["quickened_sites"] = invalid);
            var empty = O(snapshot, "data"); empty["madness"] = M(); empty["quickened_sites"] = 0;
            Assert.True(PlaytestCapture.ValidEvent(snapshot));

            foreach (string action in new[] { "manifest", "sacrifice" })
            {
                var performed = Observation("action_performed", M("action", action, "region", "africa", "spent", M("influence", 0, "dread", 0, "followers", 0), "madness_after", 100));
                Reject(performed, d => d.Remove("madness_after"));
                foreach (object invalid in new object[] { -1, 101, "20", null, double.NaN }) Reject(performed, d => d["madness_after"] = invalid);
                Reject(performed, d => d["action"] = "recruit");
                Reject(performed, d => d["rite_advanced"] = false);
            }
            foreach (string response in new[] { "counter", "silence", "scatter", "ignored", "forgotten" })
            {
                var resolved = Observation("threat_resolved", M("region", "africa", "kind", "occult", "severity", 1, "due_month", 12, "response", response, "legal_responses", new List<object> { "counter", "scatter", "silence" }, "losses", M("followers", 0, "preparation_months", 0, "income_per_month", 0, "cells", 0, "sites", 0), "grace_months", 0));
                Reject(resolved, d => d.Remove("grace_months"));
                foreach (object invalid in new object[] { -1, 25, 1.5, "5", null }) Reject(resolved, d => d["grace_months"] = invalid);
                if (response == "counter" || response == "silence")
                {
                    O(resolved, "data")["grace_months"] = 24;
                    Assert.True(PlaytestCapture.ValidEvent(resolved));
                }
                else Reject(resolved, d => d["grace_months"] = 1);
            }
        }

        [Theory]
        [InlineData("normal", "measure-0")]
        [InlineData("easy", "dreams")]
        public void RealMadnessPoliciesExportNormalAndEasyBatches(string difficulty, string seed)
        {
            // Replay accepted bot commands via GameSession; never grant resources or mutate game state.
            var game = PlaytestAlignment6Tests.Play("terror", seed, difficulty: difficulty, madnessPolicy: true);
            var events = Events(game);
            Assert.Equal("run_finished", events.Last()["type"]);
            Assert.Contains(events, e => Equals(e["type"], "month_snapshot") && N(O(e, "data"), "quickened_sites") > 0);
            Assert.Contains(events, e => Equals(e["type"], "month_snapshot") && N(O(e, "data"), "madness_dread") > 0);
            Assert.Contains(events, e => Equals(e["type"], "threat_resolved") && N(O(e, "data"), "grace_months") > 0);
            Assert.Contains(events, e => Equals(e["type"], "action_performed") && Equals(O(e, "data")["action"], "sacrifice") && O(e, "data").ContainsKey("madness_after"));
            Assert.Contains(events, e => Equals(e["type"], "rite_month"));
            Assert.True(string.IsNullOrEmpty(game.Lab.Warning), game.Lab.Warning);
            while (A(game.Lab.Data, "staging").Count > 0) game.Lab.SealBatch();
            int index = 0;
            foreach (Dictionary<string, object> job in A(game.Lab.Data, "jobs"))
            {
                Assert.Equal("awakening-6", Body(job)["adapter"]);
                Assert.All(A(Body(job), "events"), e => Assert.True(PlaytestCapture.ValidEvent(e), JsonWriter.Write(e)));
                string output = Environment.GetEnvironmentVariable("AWAKENING_PLAYTEST_BATCH_OUTPUT");
                if (string.IsNullOrEmpty(output)) continue;
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, $"batch-alignment7-{difficulty}-{++index:00}.json"), (string)job["body"]);
            }
        }
    }
}
