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
using static Awakening.Core.Dev.Playthrough;

namespace Awakening.Core.Tests
{
    public class PlaytestAlignment6Tests
    {
        // Mirror the production bot's orders through GameSession, with no state/resource grants.
        internal static GameSession Play(string strategy, string seed, Action<GameSession> onMonth = null, int stopMonth = Balance.Deadline, string difficulty = "normal", bool madnessPolicy = false)
        {
            var game = new GameSession(SaveData.DefaultSave(), Configuration(), () => 0, _ => 1);
            game.SetParticipation(true);
            game.Begin(difficulty, seed); game.Place(Farm);
            var s = game.State;
            GameState Command(Action command) { command(); return game.State; }
            Action<string, string> act = (id, region) =>
            {
                if (GameEngine.PreviewAction(s, id, region).Ok) s = Command(() => game.Act(id, region));
            };
            Action<string, string> spare = (id, region) =>
            {
                var preview = GameEngine.PreviewAction(s, id, region);
                if (preview.Ok && s.Influence - preview.Influence >= Reserve(s) + Saving(s, strategy)) s = Command(() => game.Act(id, region));
            };
            Action spareBury = () =>
            {
                // Bury where the extra Exposure hurts least: the cell with the least of it.
                string best = null;
                foreach (string id in GameData.RegionIds)
                    if (s.Regions[id].Population.Followers > 0 && GameEngine.PreviewAction(s, "bury", id).Ok && (best == null || s.Regions[id].Exposure < s.Regions[best].Exposure)) best = id;
                if (best != null) spare("bury", best);
            };
            for (int step = 0; step < stopMonth; step++)
            {
                if (GameEngine.IsOver(s)) break;
                foreach (string id in GameData.RegionIds) if (s.Regions[id].Threat != null) {
                    var target = Respond(s, id);
                    if (!ReferenceEquals(target, s))
                    {
                        string action = target.Telemetry.DreadSpent > s.Telemetry.DreadSpent ? "silence" : target.Telemetry.InfluenceSpent > s.Telemetry.InfluenceSpent ? "counter" : "scatter";
                        s = Command(() => game.Act(action, id));
                    }
                }
                foreach (string id in Targets)
                {
                    var r = s.Regions[id];
                    if (r.Opportunity != null && (r.Opportunity.Kind == "nightmare" || r.Opportunity.Kind == "eclipse" && r.Site.Invested)) act("exploit", id);
                }
                foreach (string id in Targets) if (s.Regions[id].Population.Followers <= 0) act("expand", id);
                foreach (string talent in TalentPlans[strategy])
                {
                    if (GameEngine.HasTalent(s, talent)) continue;
                    if (GameEngine.TalentReason(s, talent).Length == 0)
                        s = Command(() => game.TakeTalent(talent));
                    break;
                }
                foreach (string id in Targets)
                    if (s.Regions[id].Site.Recoverable > 0) spare("restore", id);
                foreach (string id in Targets) if (!s.Regions[id].Site.Invested) act("prepare", id);
                int owed = DreadOwed(s);
                // Keep these pre-action references, just like the Godot policy's dictionaries.
                var farm = s.Regions[Farm];
                if (farm.Population.Followers < 700) spare("recruit", Farm);
                if (s.Dread < owed && farm.Population.Followers - Balance.SacrificeFollowers >= 260) act("sacrifice", Farm);
                string farmNeed = ConcealNeed(s, farm);
                if (farmNeed == "evidence") act("conceal", Farm);
                else if (farmNeed == "awareness") spare("conceal", Farm);
                foreach (string id in Targets)
                {
                    var r = s.Regions[id];
                    if (r.Population.Followers <= 0) continue;
                    double need = Derived.SiteRequirement(r);
                    double buffer = need + 160;
                    if (r.Population.Followers < buffer) spare("recruit", id);
                    int revealMonth = strategy == "late" ? 160 : 8;
                    bool allowsPublic = s.ElapsedMonths >= revealMonth && (strategy != "terror" || id == Farm)
                        && (strategy != "early" || GameEngine.HasTalent(s, "open_worship") || s.ElapsedMonths >= WorshipDeadline);
                    if (allowsPublic && !r.Public && r.Population.Followers < need) spare("reveal", id);
                    // Open worship takes away most of the reason to go back underground.
                    bool stayPublic = GameEngine.HasTalent(s, "open_worship");
                    if (r.Public && (!allowsPublic || r.Population.Followers >= buffer && !stayPublic)) act("hide", id);
                    string needConceal = ConcealNeed(s, r);
                    if (needConceal == "evidence") act("conceal", id);
                    else if (needConceal == "awareness") spare("conceal", id);
                    if (s.Dread < owed && r.Population.Followers - Balance.SacrificeFollowers >= need + 120) act("sacrifice", id);
                }
                if (madnessPolicy && strategy == "terror")
                {
                    foreach (string id in Targets)
                    {
                        var r = s.Regions[id];
                        if (r.Population.Followers <= 0 || !r.Site.Invested || r.Site.Progress >= GameEngine.SiteMonths(s) || !Derived.Supported(r)) continue;
                        bool late = GameEngine.SiteMonths(s) - r.Site.Progress > Balance.PreparationMonths - s.ElapsedMonths;
                        if (late && r.Madness < MadnessTarget && s.Dread - GameEngine.PreviewAction(s, "manifest", id).Dread >= MadnessDreadFloor)
                            act("manifest", id);
                        if (r.Madness < MadnessTarget && r.Population.Followers - Balance.SacrificeFollowers >= Derived.SiteRequirement(r) + MadnessKeep)
                            act("sacrifice", id);
                    }
                }
                if (GameEngine.HasTalent(s, "great_procession"))
                {
                    // The procession needs five public regions: the farm and one more cell walk with the three sites.
                    if (s.Regions[Marcher].Population.Followers <= 0) act("expand", Marcher);
                    if (s.ElapsedMonths >= ProcessionReveal)
                        foreach (string id in new[] { Farm, Marcher }.Concat(Targets)) if (!s.Regions[id].Public) act("reveal", id);
                }
                if (s.Status == "awakening" && GameEngine.HasTalent(s, "dreams_of_rlyeh"))
                    foreach (string id in new[] { Farm }.Concat(Targets))
                    {
                        var r = s.Regions[id];
                        double keep = r.Site.Invested ? Derived.SiteRequirement(r) + 20 : 60;
                        if (GameEngine.DreamAdvances(s, id) && s.Awareness + Balance.DreamsAwareness <= DreamCeiling && r.Population.Followers - Balance.SacrificeFollowers >= keep)
                            act("sacrifice", id);
                    }
                if (ShouldForget(s)) s = Command(() => game.InvokeCapstone("world_forgets"));
                if (s.Awareness >= BuryAt || GameEngine.InProcession(s) && s.Awareness >= BuryAt - 15) spareBury();
                bool walk = !GameEngine.HasTalent(s, "great_procession") || GameEngine.ProcessionRegions(s) >= Balance.ProcessionPublicRegions || s.ElapsedMonths >= ProcessionWait;
                if (s.Status == "alignment" && Derived.ReadySites(s) >= 3 && walk) s = Command(game.Awaken);
                // Optional test interventions still go through accepted session commands.
                onMonth?.Invoke(game);
                s = game.State;
                s = Command(() => game.Advance(1));
            }
            return game;
        }

        static List<Dictionary<string, object>> Events(GameSession game) => A(game.Lab.Data, "staging").Cast<Dictionary<string, object>>().Select(e => O(e, "event")).ToList();
        static void Export(GameSession game, string name)
        {
            Assert.True(string.IsNullOrEmpty(game.Lab.Warning), game.Lab.Warning);
            while (A(game.Lab.Data, "staging").Count != 0) game.Lab.SealBatch();
            int index = 0;
            foreach (Dictionary<string, object> job in A(game.Lab.Data, "jobs"))
            {
                Assert.Equal("awakening-6", Body(job)["adapter"]);
                Assert.All(A(Body(job), "events"), e => Assert.True(PlaytestCapture.ValidEvent(e), JsonWriter.Write(e)));
                string output = Environment.GetEnvironmentVariable("AWAKENING_PLAYTEST_BATCH_OUTPUT");
                if (string.IsNullOrEmpty(output)) continue;
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, $"batch-{name}-{++index:00}.json"), (string)job["body"]);
            }
        }

        [Theory]
        [InlineData("early", "procession")]
        [InlineData("late", "forget")]
        [InlineData("terror", "dreams")]
        public void RealPoliciesEmitCapstonesYieldsAndEveryRiteTick(string strategy, string seed)
        {
            var game = Play(strategy, seed);
            var events = Events(game);
            var rite = events.Where(e => Equals(e["type"], "rite_month")).ToList();
            Assert.NotEmpty(rite);
            Assert.Equal(rite.Count, rite.Select(e => e["game_month"]).Distinct().Count());
            Assert.True(rite.Count <= 24);
            var startMonth = N(events.Single(e => Equals(e["type"], "awakening_started")), "game_month");
            Assert.Equal(Enumerable.Range((int)startMonth + 1, rite.Count), rite.Select(e => (int)N(e, "game_month")));
            var terminal = events.Last();
            Assert.Equal("run_finished", terminal["type"]);
            Assert.Equal(terminal["game_month"], rite.Last()["game_month"]);
            foreach (var snapshot in events.Where(e => Equals(e["type"], "month_snapshot")))
            {
                var data = O(snapshot, "data");
                Assert.Equal(N(data, "talents"), O(data, "yields").Count);
                var owned = events.Where(e => N(e, "sequence") < N(snapshot, "sequence") && Equals(e["type"], "talent_taken")).Select(e => (string)O(e, "data")["talent"]);
                Assert.Equal(owned.OrderBy(id => id), O(data, "yields").Keys.OrderBy(id => id));
            }
            if (strategy == "early")
            {
                Assert.Equal(8, N(O(events.Single(e => Equals(e["type"], "awakening_started")), "data"), "required"));
                Assert.Equal("won", game.State.Status);
                Assert.DoesNotContain(events, e => Equals(e["type"], "capstone_invoked"));
            }
            if (strategy == "late")
            {
                var invocation = O(Assert.Single(events, e => Equals(e["type"], "capstone_invoked")), "data");
                Assert.Equal("world_forgets", invocation["talent"]);
                var forgotten = events.Where(e => Equals(e["type"], "threat_resolved") && Equals(O(e, "data")["response"], "forgotten")).ToList();
                Assert.NotEmpty(forgotten);
                Assert.Equal(N(invocation, "threats_cancelled"), forgotten.Count);
                int count = events.Count;
                game.InvokeCapstone("world_forgets");
                Assert.Equal(count, Events(game).Count);
                Assert.Contains(events, e => Equals(e["type"], "month_snapshot") && O(O(e, "data"), "yields").TryGetValue("world_forgets", out var value) && Convert.ToDouble(value) == 0);
                Assert.All(forgotten, e => Assert.All(O(O(e, "data"), "losses").Values, v => Assert.Equal(0, Convert.ToDouble(v))));
            }
            if (strategy == "terror")
                Assert.Contains(events, e => Equals(e["type"], "action_performed") && Equals(O(e, "data").GetValueOrDefault("rite_advanced"), true));
            Export(game, "alignment6-" + seed);
        }
        [Fact]
        public void RealScatteringHaltsTheRiteAndDreamSacrificesReportBothOutcomes()
        {
            bool scattered = false, sacrificed = false;
            var game = Play("terror", "dreams", g =>
            {
                if (scattered || g.State.Status != "awakening") return;
                Assert.True(GameEngine.PreviewAction(g.State, "scatter", Targets[0]).Ok);
                g.Act("scatter", Targets[0]); scattered = true;
                foreach (string id in GameData.RegionIds)
                    if (GameEngine.PreviewAction(g.State, "sacrifice", id).Ok)
                    {
                        g.Act("sacrifice", id); sacrificed = true; break;
                    }
            });
            Assert.True(scattered && sacrificed);
            var events = Events(game);
            Assert.Contains(events, e => Equals(e["type"], "action_performed") && Equals(O(e, "data").GetValueOrDefault("rite_advanced"), false));
            Assert.Contains(events, e => Equals(e["type"], "action_performed") && Equals(O(e, "data").GetValueOrDefault("rite_advanced"), true));
            var halted = events.Where(e => Equals(e["type"], "rite_month") && Equals(O(e, "data")["advancing"], false));
            Assert.Contains(halted, e => A(O(e, "data"), "blockers").Cast<Dictionary<string, object>>().Any(b => Equals(b["reason"], "scattered")));
            Export(game, "alignment6-halted-dreams");
        }

        [Fact]
        public void AnOccultThreatLandingInsideTheTickReportsAnUnpreparedBlocker()
        {
            bool landed = false;
            var game = Play("early", "procession", g =>
            {
                if (landed || g.State.Status != "awakening") return;
                // Controlled threat boundary after a real command-only buildup. The next tick
                // must inspect its consequences, even though Forecast.Rite(before) can advance.
                g.State.Regions[Targets[0]].Threat = new Threat { Kind = "occult", Severity = 3, DueMonth = g.State.ElapsedMonths + 1 };
                Assert.True(Forecast.Rite(g.State).Advancing);
                landed = true;
            });
            Assert.True(landed);
            var halted = Events(game).Where(e => Equals(e["type"], "rite_month") && Equals(O(e, "data")["advancing"], false));
            Assert.Contains(halted, e => A(O(e, "data"), "blockers").Cast<Dictionary<string, object>>().Any(b => Equals(b["reason"], "unprepared")));
            Export(game, "alignment6-unprepared");
        }

        [Fact]
        public void ARealRiteResumesPartialCaptureWithoutInventingItsStartOrTalents()
        {
            bool resumed = false;
            var game = Play("terror", "dreams", g =>
            {
                if (resumed || g.State.Status != "awakening") return;
                g.SetParticipation(false); g.SetParticipation(true);
                resumed = true;
            });
            Assert.True(resumed);
            var events = Events(game);
            var start = events.First();
            Assert.Equal("capture_started", start["type"]);
            Assert.Equal(Balance.Ruleset, O(start, "data")["ruleset"]);
            Assert.DoesNotContain(events, e => Equals(e["type"], "awakening_started"));
            Assert.Contains(events, e => Equals(e["type"], "rite_month"));
            Export(game, "alignment6-partial-rite");
        }

        [Fact]
        public void V5ValidatorRejectsMalformedObservationsAndRetiredResponses()
        {
            var game = Play("terror", "dreams");
            var events = Events(game);
            void Reject(string kind, Action<Dictionary<string, object>> mutate)
            {
                var entry = (Dictionary<string, object>)JsonReader.Parse(JsonWriter.Write(events.First(e => Equals(e["type"], kind))));
                Assert.True(PlaytestCapture.ValidEvent(entry));
                mutate(entry);
                Assert.False(PlaytestCapture.ValidEvent(entry));
            }
            Reject("rite_month", e => O(e, "data")["progress"] = 13);
            Reject("rite_month", e => O(e, "data")["required"] = 9);
            Reject("rite_month", e => O(e, "data")["blockers"] = new List<object> { M("region", "africa", "reason", "unknown") });
            Reject("rite_month", e => O(e, "data")["blockers"] = new List<object> { M("region", "africa", "reason", "scattered"), M("region", "africa", "reason", "dormant") });
            Reject("awakening_started", e => O(e, "data").Remove("required"));
            Reject("month_snapshot", e => O(e, "data")["yields"] = M("bad-id", 0));
            Reject("threat_resolved", e => O(e, "data")["response"] = "offering");
            var sacrifice = events.First(e => Equals(e["type"], "action_performed") && O(e, "data").ContainsKey("rite_advanced"));
            O(sacrifice, "data").Remove("rite_advanced");
            Assert.False(PlaytestCapture.ValidEvent(sacrifice));
        }

        [Fact]
        public void RiteObservationLimitSurvivesSaveAndAcknowledgedBatchRemoval()
        {
            var rig = new Rig(); rig.Start(); rig.State.Status = "awakening";
            for (int month = 200; month < 223; month++)
            {
                rig.State.ElapsedMonths = month;
                rig.Capture.RiteMonth(M("progress", 0, "required", 12, "advancing", false, "blockers", new List<object>()), rig.State);
            }
            while (A(rig.Capture.Data, "staging").Count > 0) rig.Capture.SealBatch();
            A(rig.Capture.Data, "jobs").Clear(); // The receiver acknowledged earlier observations.
            var restored = new PlaytestCapture(() => rig.Unix);
            restored.Configure(Configuration(), Saved(rig.Capture, rig.State));
            Assert.False(restored.Corrupt);
            Assert.True(restored.Restore()); restored.Resume(rig.State);
            restored.RiteMonth(M("progress", 0, "required", 12, "advancing", false, "blockers", new List<object>()), rig.State); // duplicate 222
            rig.State.ElapsedMonths = 223;
            restored.RiteMonth(M("progress", 0, "required", 12, "advancing", false, "blockers", new List<object>()), rig.State);
            rig.State.ElapsedMonths = 224;
            restored.RiteMonth(M("progress", 0, "required", 12, "advancing", false, "blockers", new List<object>()), rig.State);
            Assert.Single(A(restored.Data, "staging").Cast<Dictionary<string, object>>(), e => Equals(O(e, "event")["type"], "rite_month"));
        }

    }
}
