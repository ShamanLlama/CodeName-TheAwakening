using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Awakening.Core.Session;
using Xunit;

namespace Awakening.Core.Tests
{
    // alignment-6: capstones that open another way to win (The Great Procession, The world forgets, Dreams of R'lyeh);
    // what each talent gave this run; the final rite as data, with the risk of halting it; a more demanding Normal.
    public class Alignment6Tests
    {
        static GameState At(int month, string difficulty = "normal", double influence = 1000.0)
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState(difficulty, "alignment-6"), "africa").State;
            s.ElapsedMonths = month; s.Influence = influence; s.Dread = 200.0;
            if (month >= Balance.PreparationMonths) s.Status = "alignment";
            Derived.Recruit(s.Regions["africa"], 300);
            return s;
        }
        static GameState With(GameState s, params string[] talents)
        {
            foreach (string id in talents) { s.Talents.Add(id); s.TalentYield[id] = 0.0; }
            return s;
        }
        static RegionState Cell(GameState s, string region, double followers)
        {
            var r = s.Regions[region];
            if (r.Population.Followers < followers) Derived.Recruit(r, followers - r.Population.Followers);
            return r;
        }
        static GameState Site(GameState s, string region, int progress = Balance.SiteMonths, double spare = 80)
        {
            var r = s.Regions[region];
            Cell(s, region, Derived.SiteRequirement(r) + spare);
            r.Site = new Site { Invested = true, Progress = progress };
            return s;
        }
        /// <summary>Three prepared, supported sites at the alignment, with Dread for the rite.</summary>
        static GameState Ready(params string[] talents)
        {
            var s = With(At(Balance.PreparationMonths), talents);
            foreach (string id in new[] { "latin_america", "middle_east", "oceania" }) Site(s, id);
            return s;
        }
        static GameState Quiet(GameState s) { s.RngState = Rng.SeedToState("quiet"); return s; }
        static GameState Begin(GameState s)
        {
            var result = GameEngine.StartAwakening(s);
            Assert.True(result.Ok, result.Message);
            return result.State;
        }
        static GameState Land(GameState s, string region, string kind, int severity)
        {
            s.Regions[region].Threat = new Threat { Kind = kind, DueMonth = s.ElapsedMonths + 1, Severity = severity };
            var next = s.Clone();
            next.ElapsedMonths++;
            Director.ResolveWorld(next);
            return next;
        }
        /// <summary>Leaves the region with this many followers; the rest are lost, so its site requirement stays put.</summary>
        static void Hold(GameState s, string region, double followers)
        {
            var r = s.Regions[region];
            Derived.LoseFollowers(r, r.Population.Followers - followers);
        }
        static void GoPublic(GameState s, params string[] regions)
        {
            foreach (string id in regions) { Cell(s, id, 80); s.Regions[id].Public = true; s.Regions[id].Revealed = true; }
        }

        // ---- The tree ----

        [Fact]
        public void EachBranchEndsInACapstoneThatOpensAnotherWayToWin()
        {
            Assert.Equal(new[] { "great_procession", "world_forgets", "dreams_of_rlyeh" },
                GameData.TalentIds.Where(id => GameData.Talents[id].Tier == 3));
            foreach (string gone in new[] { "faith_among_faiths", "bought_press", "offerings" }) Assert.False(GameData.Talents.ContainsKey(gone));
            Assert.Equal(new[] { "world_forgets" }, GameData.TalentIds.Where(GameEngine.IsInvokable));
            Assert.False(GameEngine.IsInvokable(null));
            Assert.Contains("If 5 or more regions worship in public when the final rite begins, it needs 8 months, not 12", GameData.Talents["great_procession"].Description);
            Assert.Contains("+0.3 world awareness a month", GameData.Talents["great_procession"].Description);
            Assert.Contains("−25 world awareness and every threat dissolves", GameData.Talents["world_forgets"].Description);
            Assert.Contains("for 6 months", GameData.Talents["world_forgets"].Description);
            Assert.Contains("one month forward, at +2 world awareness", GameData.Talents["dreams_of_rlyeh"].Description);
            Assert.Contains("the world stops counting their followers", GameData.Talents["open_worship"].Description);
            Assert.Equal("Requires Red altars and Mute witnesses or Scapegoats.", GameEngine.TalentReason(At(200), "dreams_of_rlyeh"));
        }

        // ---- The Great Procession ----

        [Fact]
        public void TheGreatProcessionShortensTheRiteWhenFiveRegionsWorshipInPublic()
        {
            var s = Ready("swelling_flock", "open_worship", "great_procession");
            GoPublic(s, "africa", "latin_america", "middle_east", "oceania");
            Assert.Equal(4, GameEngine.ProcessionRegions(s));
            Assert.Equal(12, GameEngine.RiteMonthsFor(s));
            Assert.Equal("4 of 5 regions worship in public; the rite would need 12 months", GameEngine.TalentYieldText(s, "great_procession"));
            GoPublic(s, "south_asia");
            Assert.Equal(8, GameEngine.RiteMonthsFor(s));
            Assert.Equal(8, Forecast.Rite(s).Required);
            Assert.False(Forecast.Rite(s).Started);

            var rite = Begin(s);
            Assert.Equal(8, rite.RiteMonthsRequired);
            Assert.Equal(new[] { "latin_america", "middle_east", "oceania" }, rite.RiteSites);
            Assert.Equal(4.0, GameEngine.TalentYieldOf(rite, "great_procession"));
            Assert.Equal("The rite needs 8 months, not 12", GameEngine.TalentYieldText(rite, "great_procession"));
            Assert.StartsWith("The Great Procession begins: 5 regions walk in the open. Maintain three supported sites for 8 months.", rite.Log.Last().Message);
            Assert.True(GameEngine.InProcession(rite));
            Assert.Equal("The Great Procession does not turn back underground.", GameEngine.PreviewAction(rite, "hide", "africa").Reason);

            // Each public region adds the procession's awareness every month of the rite.
            var one = GameEngine.AdvanceSimulation(Quiet(rite.Clone()), 1);
            var plain = rite.Clone(); plain.RiteMonthsRequired = 12;
            var without = GameEngine.AdvanceSimulation(Quiet(plain), 1);
            Assert.Equal(5 * Balance.ProcessionAwareness, one.Awareness - without.Awareness, 9);

            var won = GameEngine.AdvanceSimulation(Quiet(rite), 8);
            Assert.Equal("won", won.Status);
            Assert.Equal(Balance.PreparationMonths + 8, won.ElapsedMonths);
            Assert.Equal(8, won.FinalProgress);
        }

        [Fact]
        public void AProcessionTooSmallOrWithoutTheTalentNeedsTwelveMonths()
        {
            var small = Ready("swelling_flock", "open_worship", "great_procession");
            GoPublic(small, "africa", "latin_america", "middle_east");
            var rite = Begin(small);
            Assert.Equal(12, rite.RiteMonthsRequired);
            Assert.Equal(0.0, GameEngine.TalentYieldOf(rite, "great_procession"));
            Assert.Equal("Too few walked: the rite needs 12 months", GameEngine.TalentYieldText(rite, "great_procession"));
            Assert.False(GameEngine.InProcession(rite));
            Assert.Equal("", GameEngine.PreviewAction(rite, "hide", "africa").Reason);

            var plain = Ready("swelling_flock", "open_worship");
            GoPublic(plain, "africa", "latin_america", "middle_east", "oceania", "south_asia");
            Assert.Equal(12, Begin(plain).RiteMonthsRequired);
            Assert.StartsWith("The final rite begins. Maintain three supported sites for 12 months.", Begin(plain).Log.Last().Message);
        }

        [Fact]
        public void TheProcessionMayStillBeginWhenTwelveMonthsNoLongerFit()
        {
            var s = Ready("swelling_flock", "open_worship", "great_procession");
            s.ElapsedMonths = Balance.Deadline - 10;
            Assert.Equal("Fewer than 12 months remain. The final rite can no longer finish in time.", GameEngine.AwakeningReason(s));
            GoPublic(s, "africa", "latin_america", "middle_east", "oceania", "south_asia");
            Assert.Equal("", GameEngine.AwakeningReason(s));
            Assert.Equal(Balance.Deadline - 8, Forecast.Margin(s).LatestStart);
            s.ElapsedMonths = Balance.Deadline - 7;
            Assert.Equal("Fewer than 8 months remain. The final rite can no longer finish in time.", GameEngine.AwakeningReason(s));
        }

        // ---- The world forgets ----

        [Fact]
        public void TheWorldForgetsOnceAtThePriceOfEveryCell()
        {
            var s = With(At(150), "quiet_friends", "contingency");
            Assert.Equal("Take The world forgets first.", GameEngine.InvokeReason(s, "world_forgets"));
            Assert.Equal("This talent is not invoked; it works on its own.", GameEngine.InvokeReason(s, "great_procession"));
            With(s, "world_forgets");
            Assert.Equal("The world has nothing to forget yet.", GameEngine.InvokeReason(s, "world_forgets"));
            Assert.Same(s, GameEngine.InvokeCapstone(s, "world_forgets").State);

            s.Awareness = 40.0;
            Site(s, "latin_america", 20);
            Cell(s, "oceania", 50);
            s.Regions["africa"].Public = true;
            s.Regions["latin_america"].Threat = new Threat { Kind = "occult", Severity = 2, DueMonth = 155 };
            s.Regions["oceania"].Threat = new Threat { Kind = "investigation", Severity = 1, DueMonth = 152 };
            Assert.Equal("", GameEngine.InvokeReason(s, "world_forgets"));
            Assert.Equal("Every cell goes to ground for 6 months: no recruits, no income, no site progress. Public worship ends.", GameEngine.InvokeRisk(s, "world_forgets"));
            var result = GameEngine.InvokeCapstone(s, "world_forgets");
            Assert.True(result.Ok, result.Message);
            var f = result.State;
            Assert.Equal(15.0, f.Awareness);
            Assert.All(GameData.RegionIds, id => Assert.Null(f.Regions[id].Threat));
            foreach (string id in new[] { "africa", "latin_america", "oceania" })
            {
                Assert.True(f.Regions[id].Scattered);
                Assert.Equal(156, f.Regions[id].ScatteredUntil);
                Assert.False(f.Regions[id].Public);
            }
            Assert.False(f.Regions["europe"].Scattered);
            Assert.Equal(new[] { "world_forgets" }, f.CapstonesUsed);
            Assert.Equal(1000.0, f.Influence);
            Assert.Equal("Invoked: −25 world awareness", GameEngine.TalentYieldText(f, "world_forgets"));
            Assert.Equal("The world forgets: −25 world awareness, 2 threat(s) dissolve. Every cell goes to ground for 6 months.", f.Log.Last().Message);
            Assert.Equal("Already invoked this run.", GameEngine.InvokeReason(f, "world_forgets"));
            Assert.False(GameEngine.InvokeCapstone(f, "world_forgets").Ok);

            // No recruits, no income and no site progress while hidden; they gather after 6 months.
            Assert.Equal(0.0, Derived.InfluencePerMonth(f));
            var later = GameEngine.AdvanceSimulation(Quiet(f), 5);
            Assert.Equal(20, later.Regions["latin_america"].Site.Progress);
            Assert.True(later.Regions["africa"].Scattered);
            Assert.False(GameEngine.AdvanceSimulation(Quiet(later), 1).Regions["africa"].Scattered);

            var low = With(At(150), "quiet_friends", "contingency", "world_forgets"); low.Awareness = 10.0;
            Assert.Equal(0.0, GameEngine.InvokeCapstone(low, "world_forgets").State.Awareness);
            Assert.Equal(10.0, GameEngine.TalentYieldOf(GameEngine.InvokeCapstone(low, "world_forgets").State, "world_forgets"));
        }

        [Fact]
        public void ForgettingDuringTheRiteHaltsIt()
        {
            var s = Begin(Ready("quiet_friends", "sleeper_cells", "world_forgets"));
            s.Awareness = 70.0;
            Assert.EndsWith("The final rite halts for 6 months.", GameEngine.InvokeRisk(s, "world_forgets"));
            var f = GameEngine.InvokeCapstone(s, "world_forgets").State;
            var status = Forecast.Rite(f);
            Assert.False(status.Advancing);
            Assert.Equal(new[] { "scattered", "scattered", "scattered" }, status.Blockers.Select(b => b.Reason));
            var later = GameEngine.AdvanceSimulation(Quiet(f), 5);
            Assert.Equal(0, later.FinalProgress);
            Assert.True(Forecast.Rite(later).Advancing);
            Assert.Equal(1, GameEngine.AdvanceSimulation(Quiet(later), 1).FinalProgress);
            var over = f.Clone(); over.Status = "won";
            Assert.StartsWith("Start a run", GameEngine.InvokeReason(over, "world_forgets"));
        }

        // ---- Dreams of R'lyeh ----

        [Fact]
        public void DreamsOfRlyehCarryTheRiteWithEverySacrifice()
        {
            var s = Begin(Ready("red_altars", "scapegoats", "dreams_of_rlyeh"));
            Assert.True(GameEngine.DreamAdvances(s, "africa"));
            var preview = GameEngine.PreviewAction(s, "sacrifice", "africa");
            Assert.True(preview.Ok, preview.Reason);
            Assert.Equal(Balance.DreamsAwareness, preview.Awareness);
            Assert.EndsWith("The dream carries the final rite one month forward (1/12).", preview.Effect);
            Assert.StartsWith("+2 world awareness, +12 Exposure.", preview.Risk);
            var fed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Equal(1, fed.FinalProgress);
            Assert.Equal(s.Awareness + 2, fed.Awareness);
            Assert.Equal(s.Dread + Balance.RedAltarsSacrificeDread, fed.Dread);
            Assert.Equal("1 rite months dreamed forward so far", GameEngine.TalentYieldText(fed, "dreams_of_rlyeh"));

            // The dream never carries the rite past its end, and only while three sites stand after the knife.
            var near = s.Clone(); near.FinalProgress = 12;
            Assert.False(GameEngine.DreamAdvances(near, "africa"));
            var thin = s.Clone();
            Hold(thin, "oceania", Derived.SiteRequirement(thin.Regions["oceania"]) + 10);
            Assert.False(GameEngine.DreamAdvances(thin, "oceania"));
            var breaking = GameEngine.PreviewAction(thin, "sacrifice", "oceania");
            Assert.Equal(0, breaking.Awareness);
            Assert.Contains("The final rite halts until this site again holds 75 followers.", breaking.Risk);
            Assert.Equal(0, GameEngine.PerformAction(thin, "sacrifice", "oceania").State.FinalProgress);

            var halted = s.Clone(); halted.Regions["middle_east"].Scattered = true; halted.Regions["middle_east"].ScatteredUntil = 220;
            Assert.Contains("The dream does not carry: three supported sites must stand after the knife.", GameEngine.PreviewAction(halted, "sacrifice", "africa").Risk);

            // Outside the rite a sacrifice is only a sacrifice.
            var before = Ready("red_altars", "scapegoats", "dreams_of_rlyeh");
            Assert.False(GameEngine.DreamAdvances(before, "africa"));
            Assert.Equal(0, GameEngine.PreviewAction(before, "sacrifice", "africa").Awareness);
            Assert.Equal(0, GameEngine.PerformAction(before, "sacrifice", "africa").State.FinalProgress);
            var plain = Begin(Ready("red_altars", "scapegoats"));
            Assert.Equal(0, GameEngine.PerformAction(plain, "sacrifice", "africa").State.FinalProgress);
        }

        [Fact]
        public void ADreamtRiteEndsWhenItsLastMonthPasses()
        {
            var s = Begin(Ready("red_altars", "scapegoats", "dreams_of_rlyeh"));
            s.FinalProgress = 11;
            var fed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Equal(12, fed.FinalProgress);
            var won = GameEngine.AdvanceSimulation(Quiet(fed), 1);
            Assert.Equal("won", won.Status);
            Assert.Equal(12, won.FinalProgress);
            Assert.True(SaveData.IsGameState(won));
        }

        // ---- What each talent gave ----

        [Fact]
        public void YieldTextIsEmptyForTalentsNotTaken()
        {
            var s = At(40);
            foreach (string id in GameData.TalentIds) Assert.Equal("", GameEngine.TalentYieldText(s, id));
            Assert.Equal("", GameEngine.TalentYieldText(s, null));
            var taken = GameEngine.TakeTalent(With(At(24), new string[0]), "martyrs");
            Assert.False(taken.Ok);
            var root = GameEngine.TakeTalent(At(24), "swelling_flock").State;
            Assert.Equal(0.0, root.TalentYield["swelling_flock"]);
            Assert.Equal("+0 followers so far", GameEngine.TalentYieldText(root, "swelling_flock"));
        }

        [Fact]
        public void SwellingFlockCountsTheExtraFollowers()
        {
            var s = With(At(30), "swelling_flock");
            double recruited = GameEngine.AdvanceSimulation(Quiet(s.Clone()), 1).Regions["africa"].Population.Followers - s.Regions["africa"].Population.Followers;
            var next = GameEngine.AdvanceSimulation(Quiet(s), 1);
            Assert.Equal(recruited * Balance.SwellingFlockRecruitment / (1 + Balance.SwellingFlockRecruitment), GameEngine.TalentYieldOf(next, "swelling_flock"), 6);
            Assert.StartsWith("+", GameEngine.TalentYieldText(next, "swelling_flock"));
        }

        [Fact]
        public void OpenWorshipCountsTheAwarenessItSpares()
        {
            var s = With(At(30), "swelling_flock", "open_worship");
            var revealed = GameEngine.PerformAction(s, "reveal", "africa").State;
            Assert.Equal(Balance.RevealAwareness, GameEngine.TalentYieldOf(revealed, "open_worship"));
            Assert.Equal(0.0, revealed.Awareness);
            // A month later: the drip of a public cell and the followers the world no longer counts.
            var month = GameEngine.AdvanceSimulation(Quiet(revealed), 1);
            double uncounted = month.Regions["africa"].Population.Followers / GameData.WorldPopulation * Balance.AwarenessPerCapita;
            Assert.Equal(Balance.RevealAwareness + 0.075 + uncounted, GameEngine.TalentYieldOf(month, "open_worship"), 9);
            Assert.Equal(0.0, month.Awareness, 9);
            Assert.Equal("5 world awareness spared so far", GameEngine.TalentYieldText(month, "open_worship"));
            // Hidden followers still count.
            var hidden = With(At(30), "swelling_flock", "open_worship");
            Assert.True(GameEngine.AdvanceSimulation(Quiet(hidden), 1).Awareness > 0);
        }

        [Fact]
        public void MartyrsCountTheirDread()
        {
            var s = With(At(40), "swelling_flock", "martyrs"); s.Dread = 50.0;
            Assert.Equal("0 Dread so far", GameEngine.TalentYieldText(s, "martyrs"));
            var hit = Land(s, "africa", "raid", 2);
            Assert.Equal(hit.Dread - 50.0, GameEngine.TalentYieldOf(hit, "martyrs"));
            Assert.True(GameEngine.TalentYieldOf(hit, "martyrs") > 0);
        }

        [Fact]
        public void QuietFriendsCountTheInfluenceSavedObstructing()
        {
            var s = With(At(40), "quiet_friends");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 3, DueMonth = 45 };
            var done = GameEngine.PerformAction(s, "counter", "africa").State;
            Assert.Equal(30.0, GameEngine.TalentYieldOf(done, "quiet_friends"));
            Assert.Equal("30 Influence saved so far", GameEngine.TalentYieldText(done, "quiet_friends"));
        }

        [Fact]
        public void SleeperCellsCountTheMonthsOfHidingSaved()
        {
            var s = With(At(40), "quiet_friends", "sleeper_cells");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 1, DueMonth = 45 };
            var hidden = GameEngine.PerformAction(s, "scatter", "africa").State;
            Assert.Equal(3.0, GameEngine.TalentYieldOf(hidden, "sleeper_cells"));
            Assert.Equal("3 months of hiding saved so far", GameEngine.TalentYieldText(hidden, "sleeper_cells"));
        }

        [Fact]
        public void ContingencyCountsTheMonthsBeyondHalf()
        {
            var hit = Land(Site(With(At(60), "quiet_friends", "contingency"), "africa", 20), "africa", "occult", 1);
            var restored = GameEngine.PerformAction(hit, "restore", "africa").State;
            Assert.Equal(3.0, GameEngine.TalentYieldOf(restored, "contingency"));
            Assert.Equal("3 more preparation months restored so far", GameEngine.TalentYieldText(restored, "contingency"));
        }

        [Fact]
        public void RedAltarsCountDreadGainedAndSaved()
        {
            var s = With(At(40), "red_altars"); s.Dread = 100.0;
            var fed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Equal(10.0, GameEngine.TalentYieldOf(fed, "red_altars"));
            var shown = GameEngine.PerformAction(fed, "manifest", "africa").State;
            Assert.Equal(15.0, GameEngine.TalentYieldOf(shown, "red_altars"));
            shown.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 2, DueMonth = 45 };
            var silenced = GameEngine.PerformAction(shown, "silence", "africa").State;
            Assert.Equal(29.0, GameEngine.TalentYieldOf(silenced, "red_altars"));
            Assert.Equal("29 Dread gained or saved so far", GameEngine.TalentYieldText(silenced, "red_altars"));
            // Near the cap only the Dread actually gained counts.
            var full = With(At(40), "red_altars"); full.Dread = Balance.DreadCap - 30;
            Assert.Equal(5.0, GameEngine.TalentYieldOf(GameEngine.PerformAction(full, "sacrifice", "africa").State, "red_altars"));
        }

        [Fact]
        public void MuteWitnessesCountTheAwarenessSpared()
        {
            var s = With(At(40), "red_altars", "mute_witnesses");
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", Severity = 3, DueMonth = 50 };
            var done = GameEngine.PerformAction(s, "silence", "africa").State;
            Assert.Equal(3.0, GameEngine.TalentYieldOf(done, "mute_witnesses"));
            Assert.Equal(0.0, GameEngine.TalentYieldOf(done, "red_altars") - 21.0);
            Assert.Equal("3 world awareness spared so far", GameEngine.TalentYieldText(done, "mute_witnesses"));
        }

        [Fact]
        public void ScapegoatsCountTheAwarenessLifted()
        {
            var s = With(At(40), "red_altars", "scapegoats"); s.Awareness = 30.0;
            var hit = Land(s, "africa", "investigation", 3);
            Assert.Equal(3.0, GameEngine.TalentYieldOf(hit, "scapegoats"));
            Assert.Equal("3 world awareness lifted so far", GameEngine.TalentYieldText(hit, "scapegoats"));
            var calm = With(At(40), "red_altars", "scapegoats"); calm.Awareness = 1.0;
            Assert.Equal(1.0, GameEngine.TalentYieldOf(Land(calm, "africa", "investigation", 3), "scapegoats"));
        }

        [Fact]
        public void EveryTalentHasAYieldLine()
        {
            var s = At(60);
            s.Talents.AddRange(GameData.TalentIds);
            foreach (string id in GameData.TalentIds) Assert.NotEqual("", GameEngine.TalentYieldText(s, id));
            Assert.Equal("Not yet invoked", GameEngine.TalentYieldText(s, "world_forgets"));
            Assert.Equal("0 rite months dreamed forward so far", GameEngine.TalentYieldText(s, "dreams_of_rlyeh"));
        }

        // ---- The final rite as data ----

        [Fact]
        public void RiteStatusBeforeAndDuringTheRite()
        {
            var s = Ready();
            var before = Forecast.Rite(s);
            Assert.False(before.Started); Assert.False(before.Advancing); Assert.Empty(before.Blockers);
            Assert.Equal(0, before.Progress); Assert.Equal(12, before.Required);

            var rite = Begin(s);
            var status = Forecast.Rite(rite);
            Assert.True(status.Started); Assert.True(status.Advancing); Assert.Empty(status.Blockers);
            Assert.Equal(12, status.Required);
            var later = GameEngine.AdvanceSimulation(Quiet(rite), 3);
            Assert.Equal(3, Forecast.Rite(later).Progress);
        }

        [Fact]
        public void RiteBlockersNameEachSiteAndWhy()
        {
            var rite = Begin(Ready());
            Site(rite, "south_asia", 20);
            var r = rite.Clone();
            r.Regions["latin_america"].Scattered = true; r.Regions["latin_america"].ScatteredUntil = r.ElapsedMonths + 5;
            Hold(r, "middle_east", Derived.SiteRequirement(r.Regions["middle_east"]) - 1);
            r.Regions["oceania"].Site.Dormant = true;
            var status = Forecast.Rite(r);
            Assert.False(status.Advancing);
            // The fourth, unprepared site is not one the rite stands on and is not listed.
            Assert.Equal(new[] { "latin_america:scattered", "middle_east:unsupported", "oceania:dormant" }, status.Blockers.Select(b => b.Region + ":" + b.Reason));

            var hit = rite.Clone();
            hit.Regions["oceania"].Site.Progress = 18;
            Derived.LoseFollowers(hit.Regions["middle_east"], hit.Regions["middle_east"].Population.Followers);
            Assert.Equal(new[] { "middle_east:destroyed", "oceania:unprepared" }, Forecast.Rite(hit).Blockers.Select(b => b.Region + ":" + b.Reason));

            // A flock that gathers this month counts again; a fourth prepared site can carry the rite.
            var soon = rite.Clone();
            soon.Regions["latin_america"].Scattered = true; soon.Regions["latin_america"].ScatteredUntil = soon.ElapsedMonths + 1;
            Assert.True(Forecast.Rite(soon).Advancing);
            var spare = rite.Clone();
            Site(spare, "europe");
            spare.Regions["oceania"].Site.Dormant = true;
            Assert.True(Forecast.Rite(spare).Advancing);
            Assert.Empty(Forecast.Rite(spare).Blockers);
        }

        [Fact]
        public void ScatteringASiteTheRiteNeedsSaysItHalts()
        {
            var rite = Begin(Ready());
            rite.Regions["oceania"].Threat = new Threat { Kind = "occult", Severity = 1, DueMonth = rite.ElapsedMonths + 3 };
            var risk = GameEngine.PreviewAction(rite, "scatter", "oceania").Risk;
            Assert.EndsWith("Its ritual site stalls and cannot carry the final rite. The final rite halts while this site hides: 8 months.", risk);
            var sleeper = rite.Clone(); With(sleeper, "quiet_friends", "sleeper_cells");
            Assert.EndsWith("The final rite halts while this site hides: 5 months.", GameEngine.PreviewAction(sleeper, "scatter", "oceania").Risk);
            // With a fourth prepared site the rite goes on.
            var spare = rite.Clone(); Site(spare, "europe");
            Assert.DoesNotContain("The final rite halts", GameEngine.PreviewAction(spare, "scatter", "oceania").Risk);
            // Before the rite there is nothing to halt.
            var before = Ready(); before.Regions["oceania"].Threat = new Threat { Kind = "occult", Severity = 1, DueMonth = 205 };
            Assert.DoesNotContain("The final rite halts", GameEngine.PreviewAction(before, "scatter", "oceania").Risk);
        }

        [Fact]
        public void SacrificingASitesSupportSaysTheRiteHalts()
        {
            var rite = Begin(Ready());
            var thin = rite.Clone();
            Hold(thin, "oceania", Derived.SiteRequirement(thin.Regions["oceania"]) + 10);
            Assert.EndsWith("The final rite halts until this site again holds 75 followers.", GameEngine.PreviewAction(thin, "sacrifice", "oceania").Risk);
            Hold(rite, "oceania", Derived.SiteRequirement(rite.Regions["oceania"]) + 60);
            Assert.DoesNotContain("halts", GameEngine.PreviewAction(rite, "sacrifice", "oceania").Risk);
            Assert.DoesNotContain("halts", GameEngine.PreviewAction(rite, "sacrifice", "africa").Risk);
        }

        // ---- A more demanding Normal ----

        [Fact]
        public void ThreatsWeighMoreFromMonthOneHundredAndAtTheHunt()
        {
            var s = At(99); s.Regions["africa"].Exposure = 0.0;
            double before = Director.ThreatPressure(s, "africa");
            s.ElapsedMonths = 100;
            double after = Director.ThreatPressure(s, "africa");
            Assert.Equal(Balance.MidgamePressure * (0.6 + GameData.Regions["africa"].InstitutionalResistance / 150.0), after - before, 9);
            Site(s, "africa", 10);
            double site = Director.ThreatPressure(s, "africa");
            s.ElapsedMonths = 200;
            Assert.Equal(0.6 * (0.6 + 35 / 150.0), Director.ThreatPressure(s, "africa") - site, 9);
            Assert.Equal(35, GameData.Difficulties["normal"].BuryStep);
            Assert.Equal(1.0, GameData.Difficulties["easy"].Pressure);
            Assert.Equal(5, GameData.Difficulties["easy"].OccultPreparationLoss);
        }

        // ---- Saves ----

        static string SaveOf(GameState s)
        {
            var save = SaveData.DefaultSave(); save.State = s;
            return SaveData.EncodeSave(save);
        }

        [Fact]
        public void SavesKeepYieldsCapstonesAndTheRite()
        {
            var s = Begin(Ready("quiet_friends", "sleeper_cells", "world_forgets"));
            s.Awareness = 50.0;
            s = GameEngine.InvokeCapstone(s, "world_forgets").State;
            s.TalentYield["quiet_friends"] = 42.5;
            var decoded = SaveData.DecodeSave(SaveOf(s));
            Assert.NotNull(decoded?.State);
            Assert.Equal(42.5, decoded.State.TalentYield["quiet_friends"]);
            Assert.Equal(new[] { "world_forgets" }, decoded.State.CapstonesUsed);
            Assert.Equal(new[] { "latin_america", "middle_east", "oceania" }, decoded.State.RiteSites);
            Assert.Equal(12, decoded.State.RiteMonthsRequired);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(s), GameStateJson.ToJson(decoded.State));

            var procession = Ready("swelling_flock", "open_worship", "great_procession");
            GoPublic(procession, "africa", "latin_america", "middle_east", "oceania", "south_asia");
            var walking = GameEngine.AdvanceSimulation(Quiet(Begin(procession)), 2);
            Assert.Equal(8, SaveData.DecodeSave(SaveOf(walking)).State.RiteMonthsRequired);
        }

        [Theory]
        [InlineData("yield-untaken")]
        [InlineData("yield-negative")]
        [InlineData("capstone-untaken")]
        [InlineData("capstone-passive")]
        [InlineData("capstone-twice")]
        [InlineData("rite-months")]
        [InlineData("rite-overrun")]
        [InlineData("rite-sites-early")]
        [InlineData("rite-sites-unknown")]
        [InlineData("procession-early")]
        [InlineData("missing")]
        public void SavesRejectImpossibleCapstonesAndRites(string forgery)
        {
            var s = Begin(Ready("quiet_friends", "sleeper_cells", "world_forgets"));
            s.FinalProgress = 3;
            var json = GameStateJson.ToJson(s);
            Assert.True(SaveData.IsGameState(json));
            var alignment = GameStateJson.ToJson(Ready("quiet_friends"));
            Assert.True(SaveData.IsGameState(alignment));
            switch (forgery)
            {
                case "yield-untaken": json["talentYield"] = new Dictionary<string, object> { ["martyrs"] = 3.0 }; break;
                case "yield-negative": json["talentYield"] = new Dictionary<string, object> { ["quiet_friends"] = -1.0 }; break;
                case "capstone-untaken": json["talents"] = new List<object> { "quiet_friends", "sleeper_cells" }; json["talentYield"] = new Dictionary<string, object>(); json["capstonesUsed"] = new List<object> { "world_forgets" }; break;
                case "capstone-passive": json["capstonesUsed"] = new List<object> { "sleeper_cells" }; break;
                case "capstone-twice": json["capstonesUsed"] = new List<object> { "world_forgets", "world_forgets" }; break;
                case "rite-months": json["riteMonthsRequired"] = 10; break;
                case "rite-overrun": json["riteMonthsRequired"] = 8; json["finalProgress"] = 9; break;
                case "rite-sites-early": json = alignment; json["riteSites"] = new List<object> { "europe" }; break;
                case "rite-sites-unknown": json["riteSites"] = new List<object> { "atlantis", "europe", "africa" }; break;
                case "procession-early": json = alignment; json["riteMonthsRequired"] = 8; break;
                case "missing": json.Remove("talentYield"); break;
            }
            Assert.False(SaveData.IsGameState(json));
        }

        [Fact]
        public void Alignment5RunsRetireWithANoticeAndKeepSettings()
        {
            // The alignment-5 shape: talents and burials, no yields, capstones or rite record.
            var json = GameStateJson.ToJson(At(60));
            json["ruleset"] = "alignment-5";
            foreach (string key in new[] { "talentYield", "capstonesUsed", "riteMonthsRequired", "riteSites" }) json.Remove(key);
            json["talents"] = new List<object> { "red_altars", "scapegoats", "offerings" };
            var raw = JsonWriter.Write(new Dictionary<string, object>
            {
                ["version"] = 2, ["state"] = json, ["settings"] = new Dictionary<string, object> { ["muted"] = false, ["volume"] = 70.0, ["uiScale"] = 1.25 },
                ["tutorialComplete"] = true, ["lastResult"] = null
            });
            var decoded = SaveData.DecodeSave(raw);
            Assert.NotNull(decoded); Assert.Null(decoded.State);
            Assert.Equal(70.0, decoded.Settings.Volume); Assert.Equal(1.25, decoded.Settings.UiScale); Assert.True(decoded.TutorialComplete);
            Assert.Equal("Your unfinished run used older rules (alignment-5) and cannot continue under alignment-6. Settings and playtest records are kept; start a new run.",
                SaveData.TakeRetiredNotice(decoded));
            Assert.Contains("alignment-5", SaveData.RetiredRulesets);
        }

        // ---- Session ----

        [Fact]
        public void TheSessionInvokesACapstoneSavesAndReportsIt()
        {
            int writes = 0;
            var game = new GameSession(SaveData.DefaultSave(), new Dictionary<string, object>(), () => 0, envelope => ++writes);
            var events = new List<GameEvent>();
            game.Events += list => events.AddRange(list);
            game.Begin("normal", "invoke");
            game.Place("africa");
            game.InvokeCapstone("world_forgets");
            Assert.True(events.Last().Refused);
            Assert.Equal("Take The world forgets first.", events.Last().Message);

            var s = With(game.State.Clone(), "quiet_friends", "sleeper_cells", "world_forgets");
            s.Awareness = 30.0;
            game.State = s;
            int before = writes;
            game.InvokeCapstone("world_forgets");
            Assert.False(events.Last().Refused);
            Assert.StartsWith("The world forgets: −25 world awareness", events.Last().Message);
            Assert.Equal(5.0, game.State.Awareness);
            Assert.True(writes > before);
            Assert.Equal(new[] { "world_forgets" }, SaveData.DecodeSave(SaveData.EncodeSave(game.Envelope())).State.CapstonesUsed);
        }

        // ---- The bot ----

        [Fact]
        public void EachBotStrategyReachesItsCapstoneAndUsesIt()
        {
            var capstones = new Dictionary<string, string> { ["early"] = "great_procession", ["late"] = "world_forgets", ["terror"] = "dreams_of_rlyeh" };
            foreach (string strategy in Playthrough.Strategies)
            {
                Assert.Equal(capstones[strategy], Playthrough.TalentPlans[strategy][2]);
                int used = 0, won = 0;
                for (int i = 0; i < 8; i++)
                {
                    var end = Playthrough.Play(strategy, "measure-" + i);
                    bool usedHere = strategy == "early" ? end.RiteMonthsRequired == Balance.ProcessionRiteMonths
                        : strategy == "late" ? end.CapstonesUsed.Contains("world_forgets")
                        : GameEngine.TalentYieldOf(end, "dreams_of_rlyeh") > 0;
                    if (usedHere) { used++; if (end.Status == "won") won++; }
                }
                Assert.True(used >= 3, strategy + " used its capstone in " + used + " of 8 runs");
                Assert.True(won >= 1, strategy + " never won with its capstone");
            }
        }
    }
}
