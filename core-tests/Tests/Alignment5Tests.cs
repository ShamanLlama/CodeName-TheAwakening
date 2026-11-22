using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Json;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Xunit;

namespace Awakening.Core.Tests
{
    // alignment-5: the in-run talent tree replaces the doctrine; Bury the record and Restore the site; the answers to a
    // threat as it appears; Mysticism shapes manifestations; each difficulty changes rules, not only multipliers.
    // Kept under alignment-6 with its capstones and values (Alignment6Tests covers what changed).
    public class Alignment5Tests
    {
        static GameState At(int month, string difficulty = "normal", double influence = 1000.0)
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState(difficulty, "alignment-5"), "africa").State;
            s.ElapsedMonths = month; s.Influence = influence; s.Dread = 200.0;
            Derived.Recruit(s.Regions["africa"], 300);
            return s;
        }
        static GameState With(GameState s, params string[] talents) { s.Talents.AddRange(talents); return s; }
        static GameState Take(GameState s, string id)
        {
            var result = GameEngine.TakeTalent(s, id);
            Assert.True(result.Ok, result.Message);
            return result.State;
        }
        static GameState Site(GameState s, string region, int progress)
        {
            var r = s.Regions[region];
            if (r.Population.Followers < Derived.SiteRequirement(r) + 60) Derived.Recruit(r, Derived.SiteRequirement(r) + 60 - r.Population.Followers);
            r.Site = new Site { Invested = true, Progress = progress };
            return s;
        }
        static GameState Land(GameState s, string region, string kind, int severity)
        {
            s.Regions[region].Threat = new Threat { Kind = kind, DueMonth = s.ElapsedMonths + 1, Severity = severity };
            var next = s.Clone();
            next.ElapsedMonths++;
            Director.ResolveWorld(next);
            return next;
        }

        // ---- Tree structure ----

        [Fact]
        public void TheTreeHasThreeBranchesOfARootTwoExclusiveMiddlesAndACapstone()
        {
            Assert.Equal(new[] { "congregation", "hidden_hand", "harbinger" }, GameData.BranchIds);
            Assert.Equal(12, GameData.TalentIds.Length);
            Assert.Equal(GameData.TalentIds.OrderBy(x => x), GameData.Talents.Keys.OrderBy(x => x));
            foreach (string branch in GameData.BranchIds)
            {
                var nodes = GameData.TalentIds.Select(id => GameData.Talents[id]).Where(t => t.Branch == branch).ToList();
                Assert.Equal(new[] { 1, 2, 2, 3 }, nodes.Select(t => t.Tier));
                var root = nodes[0]; var a = nodes[1]; var b = nodes[2]; var cap = nodes[3];
                Assert.Empty(root.Requires); Assert.Null(root.Excludes);
                Assert.Equal(b.Id, a.Excludes); Assert.Equal(a.Id, b.Excludes);
                Assert.Equal(new[] { root.Id }, a.Requires); Assert.Equal(new[] { root.Id }, b.Requires);
                Assert.Equal(new[] { root.Id, a.Id, b.Id }, cap.Requires); Assert.Null(cap.Excludes);
                Assert.Equal(GameData.Branches[branch].Id, branch);
            }
            // Descriptions carry the numbers from Balance.
            Assert.Contains("+35%", GameData.Talents["swelling_flock"].Description);
            Assert.Contains("after 5 months, not 8", GameData.Talents["sleeper_cells"].Description);
            Assert.Contains("2 world awareness per severity, not 3", GameData.Talents["mute_witnesses"].Description);
            Assert.Contains("needs 8 months, not 12", GameData.Talents["great_procession"].Description);
            Assert.Contains("1 Dread for every 3", GameData.Talents["martyrs"].Description);
        }

        [Fact]
        public void SealsOpenWithTimeByDifficultyAndNeverCompleteTheTree()
        {
            Assert.Equal(new[] { 24, 64, 104, 144, 176 }, GameData.Difficulties["normal"].TalentSlotMonths);
            Assert.Equal(6, GameData.Difficulties["easy"].TalentSlots);
            Assert.Equal(5, GameData.Difficulties["normal"].TalentSlots);
            Assert.Equal(4, GameData.Difficulties["hard"].TalentSlots);
            foreach (var d in GameData.Difficulties.Values) Assert.True(d.TalentSlots < GameData.TalentIds.Length - GameData.BranchIds.Length);

            var s = At(0);
            Assert.Equal(0, GameEngine.TalentSlotsOpen(s)); Assert.Equal(24, GameEngine.NextTalentSlotMonth(s));
            Assert.Equal("The next seal opens in 24 months.", GameEngine.TalentReason(s, "swelling_flock"));
            s.ElapsedMonths = 24;
            Assert.Equal(1, GameEngine.TalentSlotsOpen(s)); Assert.Equal(1, GameEngine.TalentSlotsUnused(s)); Assert.Equal(64, GameEngine.NextTalentSlotMonth(s));
            s = Take(s, "swelling_flock");
            Assert.Equal(0, GameEngine.TalentSlotsUnused(s));
            Assert.Equal("The next seal opens in 40 months.", GameEngine.TalentReason(s, "open_worship"));
            // An open seal waits; it does not accrue anything.
            s.ElapsedMonths = 150;
            Assert.Equal(4, GameEngine.TalentSlotsOpen(s)); Assert.Equal(3, GameEngine.TalentSlotsUnused(s)); Assert.Equal(176, GameEngine.NextTalentSlotMonth(s));
            s.ElapsedMonths = 199;
            Assert.Equal(-1, GameEngine.NextTalentSlotMonth(s));
            foreach (string id in new[] { "open_worship", "great_procession", "quiet_friends", "contingency" }) s = Take(s, id);
            Assert.Equal("Every seal of this run is spent.", GameEngine.TalentReason(s, "world_forgets"));

            Assert.Equal(1, GameEngine.TalentSlotsOpen(At(24, "easy"))); Assert.Equal(2, GameEngine.TalentSlotsOpen(At(44, "easy")));
            Assert.Equal(1, GameEngine.TalentSlotsOpen(At(71, "hard"))); Assert.Equal(4, GameEngine.TalentSlotsOpen(At(168, "hard")));
        }

        [Fact]
        public void EachTalentCostsMoreAndIsPaidInInfluence()
        {
            var s = At(200);
            s.Status = "alignment";
            var costs = new List<int>();
            foreach (string id in new[] { "quiet_friends", "sleeper_cells", "world_forgets", "red_altars", "scapegoats" })
            {
                costs.Add(GameEngine.TalentCost(s));
                double before = s.Influence;
                var next = Take(s, id);
                Assert.Equal(before - costs.Last(), next.Influence);
                Assert.Equal(s.Telemetry.InfluenceSpent + costs.Last(), next.Telemetry.InfluenceSpent);
                Assert.DoesNotContain(id, s.Talents);
                s = next;
            }
            Assert.Equal(new[] { 40, 70, 100, 130, 160 }, costs);
            Assert.Equal(new[] { "quiet_friends", "sleeper_cells", "world_forgets", "red_altars", "scapegoats" }, s.Talents);
            var poor = At(24, influence: 39.0);
            Assert.Equal("Need 40 Influence.", GameEngine.TalentReason(poor, "red_altars"));
            Assert.Same(poor, GameEngine.TakeTalent(poor, "red_altars").State);
        }

        [Fact]
        public void MiddlesExcludeEachOtherAndCapstonesNeedTheRootAndAMiddle()
        {
            var s = At(200); s.Status = "alignment";
            Assert.Equal("Requires Swelling flock.", GameEngine.TalentReason(s, "open_worship"));
            Assert.Equal("Requires Swelling flock and Open worship or Martyrs.", GameEngine.TalentReason(s, "great_procession"));
            s = Take(s, "swelling_flock");
            Assert.Equal("Requires Open worship or Martyrs.", GameEngine.TalentReason(s, "great_procession"));
            s = Take(s, "martyrs");
            Assert.Equal("Closed for this run: the cult chose Martyrs.", GameEngine.TalentReason(s, "open_worship"));
            Assert.Equal("", GameEngine.TalentReason(s, "great_procession"));
            Assert.Equal("Already taken.", GameEngine.TalentReason(s, "martyrs"));
            Assert.Equal("Unknown talent.", GameEngine.TalentReason(s, "recruitment"));
            Assert.Equal("Unknown talent.", GameEngine.TalentReason(s, null));
            var taken = GameEngine.TakeTalent(s, "great_procession").State;
            Assert.Contains("The Great Procession (Congregation)", taken.Log.Last().Message);
            Assert.Contains("Open worship is closed for this run.", Take(Take(At(64), "swelling_flock"), "martyrs").Log.Last().Message);
            // No respecialization: there is no command that removes a talent, and completed runs refuse new ones.
            var over = taken.Clone(); over.Status = "won";
            Assert.StartsWith("Start a run", GameEngine.TalentReason(over, "quiet_friends"));
        }

        // ---- Each node ----

        [Fact]
        public void SwellingFlockKeepsTheCongregationDoctrine()
        {
            var plain = At(30); var flock = With(At(30), "swelling_flock");
            Assert.Equal(Derived.RecruitmentPerMonth(plain, plain.Regions["africa"]) * 1.35, Derived.RecruitmentPerMonth(flock, flock.Regions["africa"]), 9);
        }

        [Fact]
        public void OpenWorshipMakesPublicWorshipGrowFastAndQuietly()
        {
            var plain = At(30); plain.Awareness = 60.0; plain.Regions["africa"].Public = true;
            var open = With(plain.Clone(), "swelling_flock", "open_worship");
            var flock = With(plain.Clone(), "swelling_flock");
            double drag = Derived.AwarenessDrag(plain);
            Assert.Equal(Derived.RecruitmentPerMonth(flock, flock.Regions["africa"]) / drag * 2.5 / 2.0, Derived.RecruitmentPerMonth(open, open.Regions["africa"]), 9);
            // Hidden cells are unchanged.
            open.Regions["africa"].Public = false; flock.Regions["africa"].Public = false;
            Assert.Equal(Derived.RecruitmentPerMonth(flock, flock.Regions["africa"]), Derived.RecruitmentPerMonth(open, open.Regions["africa"]), 9);
            // Going public no longer alarms the world, and a public cell adds no monthly awareness of its own.
            var reveal = GameEngine.PreviewAction(open, "reveal", "africa");
            Assert.True(reveal.Ok, reveal.Reason); Assert.Equal(0, reveal.Awareness);
            var loud = plain.Clone(); var quiet = open.Clone(); quiet.Regions["africa"].Public = true;
            double share = Derived.CultShare(loud) * Balance.AwarenessPerCapita;
            Assert.Equal(60.0 + 0.075 + share, GameEngine.AdvanceSimulation(Clear(loud), 1).Awareness, 1);
            Assert.True(GameEngine.AdvanceSimulation(Clear(quiet), 1).Awareness < GameEngine.AdvanceSimulation(Clear(loud), 1).Awareness);
        }
        static GameState Clear(GameState s) { s.RngState = Rng.SeedToState("quiet"); return s; }

        [Fact]
        public void MartyrsTurnFollowersLostToAThreatIntoDread()
        {
            var s = With(At(40), "swelling_flock", "martyrs"); s.Dread = 50.0;
            var hit = Land(s, "africa", "raid", 2);
            double lost = hit.Telemetry.RaidLosses;
            Assert.True(lost > 0);
            Assert.Equal(50.0 + Js.RoundHalfUp(lost / 3.0), hit.Dread);
            Assert.Contains("The martyrs feed the dark", hit.Log.Last().Message);
            var plain = At(40); plain.Dread = 50.0;
            Assert.Equal(50.0, Land(plain, "africa", "raid", 2).Dread);
        }

        [Fact]
        public void QuietFriendsKeepTheHiddenHandDoctrine()
        {
            var s = With(At(40), "quiet_friends");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 3, DueMonth = 50 };
            Assert.Equal(60, GameEngine.CounterCost(s, s.Regions["africa"]));
            s.Regions["africa"].Threat = null;
            Director.ScheduleThreat(s, "africa");
            Assert.Equal(40 + Balance.ThreatWarningMonths + Balance.QuietFriendsWarning, s.Regions["africa"].Threat.DueMonth);
        }

        [Fact]
        public void SleeperCellsGatherAgainSooner()
        {
            var s = With(At(40), "quiet_friends", "sleeper_cells");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 1, DueMonth = 45 };
            Assert.Contains("For 5 months", GameEngine.PreviewAction(s, "scatter", "africa").Risk);
            var hidden = GameEngine.PerformAction(s, "scatter", "africa").State;
            Assert.Equal(45, hidden.Regions["africa"].ScatteredUntil);
            Assert.False(GameEngine.AdvanceSimulation(hidden, 5).Regions["africa"].Scattered);
            Assert.Equal("5 months scattered", Threats.Options(s, "africa")[1].Cost);
        }

        [Fact]
        public void RedAltarsKeepTheHarbingerDoctrine()
        {
            var s = With(At(40), "red_altars");
            Assert.Equal(200.0 + 35, GameEngine.PerformAction(s, "sacrifice", "africa").State.Dread);
            Assert.Equal(15, GameEngine.PreviewAction(s, "manifest", "africa").Dread);
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 2, DueMonth = 50 };
            Assert.Equal(26, GameEngine.PreviewAction(s, "silence", "africa").Dread);
        }

        [Fact]
        public void MuteWitnessesSilenceForLessAwareness()
        {
            var s = With(At(40), "red_altars", "mute_witnesses");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 3, DueMonth = 50 };
            Assert.Equal(6.0, GameEngine.PreviewAction(s, "silence", "africa").Awareness);
            Assert.Equal(6.0, GameEngine.PerformAction(s, "silence", "africa").State.Awareness);
            Assert.Equal("39 Dread · +6 awareness", Threats.Options(s, "africa")[2].Cost);
        }

        [Fact]
        public void ScapegoatsLowerAwarenessWhenAThreatLands()
        {
            var s = With(At(40), "red_altars", "scapegoats"); s.Awareness = 30.0;
            var hit = Land(s, "africa", "investigation", 3);
            Assert.Equal(30.0 - 3 * Balance.ScapegoatSeverityAwareness, hit.Awareness);
            Assert.Contains("Scapegoats take the blame", hit.Log.Last().Message);
            var plain = With(At(40), "red_altars"); plain.Awareness = 30.0;
            Assert.Equal(30.0, Land(plain, "africa", "investigation", 3).Awareness);
            var clean = With(At(40), "red_altars", "scapegoats"); clean.Awareness = 0.0;
            Assert.Equal(0.0, Land(clean, "africa", "investigation", 3).Awareness);
        }

        // ---- Bury the record ----

        [Fact]
        public void BuryingTheRecordBuysAwarenessAtARisingPrice()
        {
            var s = At(40); s.Awareness = 40.0; s.Regions["africa"].Exposure = 10.0;
            Derived.Recruit(s.Regions["europe"], 100);
            var preview = GameEngine.PreviewAction(s, "bury", "africa");
            Assert.True(preview.Ok, preview.Reason);
            Assert.Equal(50, preview.Influence); Assert.Equal(0, preview.Awareness);
            Assert.Equal("−5 world awareness. Concealment's own world effect is not spent.", preview.Effect);
            Assert.Equal("+8 Exposure. The next burial costs 85 Influence.", preview.Risk);
            var once = GameEngine.PerformAction(s, "bury", "africa").State;
            Assert.Equal(35.0, once.Awareness); Assert.Equal(950.0, once.Influence); Assert.Equal(18.0, once.Regions["africa"].Exposure);
            Assert.Equal(1, once.BuryUses); Assert.Equal(0, once.WorldConcealReadyAt);
            Assert.StartsWith("Available in 6 months", GameEngine.PreviewAction(once, "bury", "africa").Reason);
            Assert.Equal(85, GameEngine.PreviewAction(once, "bury", "europe").Influence);
            var twice = GameEngine.PerformAction(once, "bury", "europe").State;
            Assert.Equal(120, GameEngine.BuryCost(twice));
            Assert.Equal(0, GameEngine.ConcealWorldCooldown(twice));

            Assert.Equal("Nothing to bury: the world has not noticed you.", GameEngine.PreviewAction(At(40), "bury", "africa").Reason);
            Assert.Equal("Establish a cult here first.", GameEngine.PreviewAction(s, "bury", "oceania").Reason);
            var poor = At(40, influence: 49.0); poor.Awareness = 10.0;
            Assert.Equal("Need 50 Influence.", GameEngine.PreviewAction(poor, "bury", "africa").Reason);
        }

        [Fact]
        public void BuryStepsByDifficulty()
        {
            foreach (var (difficulty, step) in new[] { ("easy", 15), ("normal", 35), ("hard", 40) })
            {
                var s = At(40, difficulty); s.BuryUses = 2;
                Assert.Equal(50 + 2 * step, GameEngine.BuryCost(s));
            }
            var low = At(40); low.Awareness = 3.0;
            Assert.Equal(0.0, GameEngine.PerformAction(low, "bury", "africa").State.Awareness);
        }

        // ---- Restore the site ----

        [Fact]
        public void AnIgnoredOccultThreatLeavesMonthsToRestore()
        {
            var s = Site(At(60), "africa", 20);
            var hit = Land(s, "africa", "occult", 1);
            Assert.Equal(14, hit.Regions["africa"].Site.Progress);
            Assert.Equal(6, hit.Regions["africa"].Site.Recoverable);
            Assert.Equal(3, GameEngine.RestoreMonths(hit, "africa")); Assert.Equal(90, GameEngine.RestoreCost(hit, "africa"));
            var preview = GameEngine.PreviewAction(hit, "restore", "africa");
            Assert.True(preview.Ok, preview.Reason);
            Assert.Equal(90, preview.Influence); Assert.Equal("+3 preparation months recovered.", preview.Effect); Assert.Equal("+10 Exposure.", preview.Risk);
            double exposure = hit.Regions["africa"].Exposure;
            var restored = GameEngine.PerformAction(hit, "restore", "africa").State;
            Assert.Equal(17, restored.Regions["africa"].Site.Progress); Assert.Equal(0, restored.Regions["africa"].Site.Recoverable);
            Assert.Equal(exposure + 10, restored.Regions["africa"].Exposure); Assert.Equal(hit.Influence - 90, restored.Influence);
            // Once per loss.
            Assert.Equal("No lost preparation to restore here.", GameEngine.PreviewAction(restored, "restore", "africa").Reason);
            // A new loss replaces the old one: 5 of 5 months taken, 3 recoverable.
            var again = Land(Site(At(60), "africa", 5), "africa", "occult", 1);
            Assert.Equal(5, again.Regions["africa"].Site.Recoverable); Assert.Equal(3, GameEngine.RestoreMonths(again, "africa"));
            var poor = hit.Clone(); poor.Influence = 89.0;
            Assert.Equal("Need 90 Influence.", GameEngine.PreviewAction(poor, "restore", "africa").Reason);
            Assert.Equal("No lost preparation to restore here.", GameEngine.PreviewAction(Site(At(60), "africa", 20), "restore", "africa").Reason);
        }

        [Fact]
        public void ContingencyRestoresEveryMonthWithoutATrace()
        {
            var hit = Land(With(Site(At(60), "africa", 20), "quiet_friends", "contingency"), "africa", "occult", 1);
            Assert.Equal(6, GameEngine.RestoreMonths(hit, "africa"));
            var preview = GameEngine.PreviewAction(hit, "restore", "africa");
            Assert.Equal(180, preview.Influence); Assert.Equal("", preview.Risk);
            var restored = GameEngine.PerformAction(hit, "restore", "africa").State;
            Assert.Equal(20, restored.Regions["africa"].Site.Progress);
            Assert.Equal(hit.Regions["africa"].Exposure, restored.Regions["africa"].Exposure);
        }

        [Fact]
        public void EasyOccultSocietiesTakeFiveMonths()
        {
            Assert.Equal(5, GameData.Difficulties["easy"].OccultPreparationLoss);
            var hit = Land(Site(At(60, "easy"), "africa", 20), "africa", "occult", 1);
            Assert.Equal(15, hit.Regions["africa"].Site.Progress); Assert.Equal(5, hit.Regions["africa"].Site.Recoverable);
            var s = Site(At(60, "easy"), "africa", 20); s.Regions["africa"].Threat = new Threat { Kind = "occult", Severity = 1, DueMonth = 70 };
            Assert.Contains("ritual −5 preparation months", Threats.Describe(s, s.Regions["africa"]));
        }

        // ---- Threat options ----

        [Fact]
        public void ThreatOptionsListEachAnswerWithItsReasonAndPrice()
        {
            var s = At(40); s.Influence = 100.0; s.Dread = 100.0;
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 2, DueMonth = 45 };
            var options = Threats.Options(s, "africa");
            Assert.Equal(new[] { "counter", "scatter", "silence" }, options.Select(o => o.Response));
            Assert.All(options, o => { Assert.True(o.Ok, o.Reason); Assert.Equal("", o.Reason); });
            Assert.Equal("60 Influence", options[0].Cost); Assert.Equal("8 months scattered", options[1].Cost); Assert.Equal("40 Dread · +6 awareness", options[2].Cost);
            Assert.False(Threats.Cornered(s, "africa"));

            s.Regions["africa"].Cooldowns["counter"] = 49; s.Dread = 39.0;
            options = Threats.Options(s, "africa");
            Assert.Equal("Available in 9 months.", options[0].Reason);
            Assert.True(options[1].Ok);
            Assert.Equal("Need 40 Dread.", options[2].Reason);
            Assert.True(Threats.Cornered(s, "africa"));
            s.Dread = 100.0; s.Influence = 59.0; s.Regions["africa"].Cooldowns.Remove("counter");
            Assert.Equal("Need 60 Influence.", Threats.Options(s, "africa")[0].Reason);
            Assert.False(Threats.Cornered(s, "africa"));

            Assert.False(Threats.Cornered(At(40), "africa"));
            Assert.All(Threats.Options(At(40), "africa"), o => Assert.Equal("No approaching threat in this region.", o.Reason));
        }

        // ---- Mysticism ----

        [Fact]
        public void MysticismShapesManifestationsRegionByRegion()
        {
            var expected = new Dictionary<string, (double awareness, int deniers, int unaware)>
            {
                ["africa"] = (6, 78, 60), ["latin_america"] = (7, 75, 58), ["south_asia"] = (7, 75, 58), ["middle_east"] = (7, 72, 55),
                ["oceania"] = (8, 68, 53), ["north_america"] = (9, 55, 43), ["east_asia"] = (9, 55, 43), ["europe"] = (10, 52, 40),
            };
            foreach (var pair in expected)
            {
                Assert.Equal(pair.Value.awareness, Derived.ManifestAwareness(pair.Key));
                Assert.Equal(pair.Value.deniers, Derived.ManifestDeniers(pair.Key));
                Assert.Equal(pair.Value.unaware, Derived.ManifestUnaware(pair.Key));
            }
            Assert.Equal("Mystical: a manifestation here draws 6 world awareness, not 8, and turns up to 78 deniers and 60 unaware into believers.", Derived.MysticismNote("africa"));
            Assert.Equal("Skeptical: a manifestation here draws 10 world awareness, not 8, and turns up to 52 deniers and 40 unaware into believers.", Derived.MysticismNote("europe"));
            Assert.Equal("Mystical: a manifestation here draws 8 world awareness and turns up to 68 deniers and 53 unaware into believers.", Derived.MysticismNote("oceania"));

            var s = At(40);
            var preview = GameEngine.PreviewAction(s, "manifest", "africa");
            Assert.Equal(6, preview.Awareness);
            Assert.Equal("Up to 78 deniers and 60 unaware people become believers.", preview.Effect);
            Assert.Equal("+15 Exposure and +6 awareness. New believers can strengthen resistance.", preview.Risk);
            var before = s.Regions["africa"].Population;
            var done = GameEngine.PerformAction(s, "manifest", "africa").State.Regions["africa"].Population;
            Assert.Equal(before.Deniers - 78, done.Deniers); Assert.Equal(before.Believers + 78 + 60, done.Believers);
        }

        // ---- Difficulty rules ----

        [Fact]
        public void HardRestsConcealmentsWorldEffectNineMonths()
        {
            var s = At(40, "hard"); s.Awareness = 30.0; s.Regions["africa"].Exposure = 30.0;
            Assert.Equal("The world effect then rests for 9 months in every region.", GameEngine.PreviewAction(s, "conceal", "africa").Risk);
            Assert.Equal(49, GameEngine.PerformAction(s, "conceal", "africa").State.WorldConcealReadyAt);
            var normal = At(40); normal.Awareness = 30.0; normal.Regions["africa"].Exposure = 30.0;
            Assert.Equal(46, GameEngine.PerformAction(normal, "conceal", "africa").State.WorldConcealReadyAt);
        }

        [Fact]
        public void EachDifficultyStatesTheRulesItChanges()
        {
            Assert.Equal("5 talent seals. An ignored occult society costs a site 6 months. Each burial costs 35 Influence more. Concealment's world effect rests 6 months.", GameData.Difficulties["normal"].Rule);
            Assert.StartsWith("6 talent seals. An ignored occult society costs a site 5 months. Each burial costs 15", GameData.Difficulties["easy"].Rule);
            Assert.EndsWith("Each burial costs 40 Influence more. Concealment's world effect rests 9 months.", GameData.Difficulties["hard"].Rule);
            Assert.Equal(new[] { 1.2, 1.0, 0.85 }, GameData.DifficultyIds.Select(d => GameData.Difficulties[d].Recruitment));
            Assert.Equal(new[] { 1.0, 1.0, 1.25 }, GameData.DifficultyIds.Select(d => GameData.Difficulties[d].Pressure));
        }

        // ---- Saves ----

        static string SaveOf(GameState s)
        {
            var save = SaveData.DefaultSave(); save.State = s;
            return SaveData.EncodeSave(save);
        }

        [Fact]
        public void SavesKeepTalentsBurialsAndRecoverableMonths()
        {
            var s = Land(Site(With(At(60), "quiet_friends", "contingency"), "africa", 20), "africa", "occult", 1);
            s.BuryUses = 3;
            var decoded = SaveData.DecodeSave(SaveOf(s));
            Assert.NotNull(decoded?.State);
            Assert.Equal(new[] { "quiet_friends", "contingency" }, decoded.State.Talents);
            Assert.Equal(3, decoded.State.BuryUses); Assert.Equal(6, decoded.State.Regions["africa"].Site.Recoverable);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(s), GameStateJson.ToJson(decoded.State));
        }

        [Theory]
        [InlineData("doctrine")]
        [InlineData("unknown")]
        [InlineData("duplicate")]
        [InlineData("siblings")]
        [InlineData("orphan")]
        [InlineData("capstone")]
        [InlineData("too-many")]
        [InlineData("recoverable")]
        [InlineData("bury")]
        public void SavesRejectImpossibleTrees(string forgery)
        {
            var s = At(200); s.Status = "alignment";
            var json = GameStateJson.ToJson(s);
            switch (forgery)
            {
                case "doctrine": json.Remove("talents"); json["specialization"] = "terror"; break;
                case "unknown": json["talents"] = new List<object> { "recruitment" }; break;
                case "duplicate": json["talents"] = new List<object> { "red_altars", "red_altars" }; break;
                case "siblings": json["talents"] = new List<object> { "red_altars", "mute_witnesses", "scapegoats" }; break;
                case "orphan": json["talents"] = new List<object> { "mute_witnesses" }; break;
                case "capstone": json["talents"] = new List<object> { "red_altars", "dreams_of_rlyeh" }; break;
                case "too-many": json["talents"] = new List<object> { "red_altars", "mute_witnesses", "dreams_of_rlyeh", "quiet_friends", "sleeper_cells", "world_forgets" }; break;
                case "recoverable": ((Dictionary<string, object>)((Dictionary<string, object>)((Dictionary<string, object>)json["regions"])["europe"])["site"])["recoverable"] = 3; break;
                case "bury": json["buryUses"] = -1; break;
            }
            Assert.False(SaveData.IsGameState(json));
            json["talents"] = new List<object> { "red_altars", "mute_witnesses", "dreams_of_rlyeh", "quiet_friends", "sleeper_cells" };
            if (forgery != "recoverable" && forgery != "bury") Assert.True(SaveData.IsGameState(json));
        }

        [Fact]
        public void Alignment4RunsRetireWithANoticeAndKeepSettings()
        {
            // The alignment-4 shape: a doctrine instead of talents, no burials and no recoverable months.
            var json = GameStateJson.ToJson(At(60));
            json["ruleset"] = "alignment-4"; json.Remove("talents"); json.Remove("buryUses"); json["specialization"] = "infiltration";
            foreach (Dictionary<string, object> region in ((Dictionary<string, object>)json["regions"]).Values) ((Dictionary<string, object>)region["site"]).Remove("recoverable");
            var raw = JsonWriter.Write(new Dictionary<string, object>
            {
                ["version"] = 2, ["state"] = json, ["settings"] = new Dictionary<string, object> { ["muted"] = false, ["volume"] = 55.0, ["hints"] = new List<object> { "first-site" } },
                ["tutorialComplete"] = true, ["lastResult"] = null
            });
            var decoded = SaveData.DecodeSave(raw);
            Assert.NotNull(decoded); Assert.Null(decoded.State);
            Assert.Equal(55.0, decoded.Settings.Volume); Assert.Equal(new[] { "first-site" }, decoded.Settings.Hints); Assert.True(decoded.TutorialComplete);
            Assert.Equal("Your unfinished run used older rules (alignment-4) and cannot continue under alignment-6. Settings and playtest records are kept; start a new run.",
                SaveData.TakeRetiredNotice(decoded));
        }

        // ---- The bot ----

        [Fact]
        public void EveryBotStrategyFollowsItsBranchAndBuysTalents()
        {
            Assert.Equal(new[] { "early", "late", "terror" }, Playthrough.Strategies);
            var roots = new Dictionary<string, string> { ["early"] = "congregation", ["late"] = "hidden_hand", ["terror"] = "harbinger" };
            foreach (string strategy in Playthrough.Strategies)
            {
                var plan = Playthrough.TalentPlans[strategy];
                Assert.Equal(roots[strategy], GameData.Talents[plan[0]].Branch);
                Assert.Equal(3, plan.Take(3).Count(t => GameData.Talents[t].Branch == roots[strategy]));
                var end = Playthrough.Play(strategy, "measure-0");
                Assert.True(end.Talents.Count >= 2, strategy + " bought " + end.Talents.Count);
                Assert.Equal(plan.Take(end.Talents.Count), end.Talents);
            }
            Assert.Contains("open_worship", Playthrough.TalentPlans["early"]);
        }
    }
}
