using System;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Xunit;

namespace Awakening.Core.Tests
{
    // One-for-one checks from godot/tests/test_engine.gd.
    public class EngineTests
    {
        internal static GameState Start() => GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "engine"), "africa").State;
        internal static GameState Ready(int month = Balance.PreparationMonths)
        {
            var s = Start();
            s.ElapsedMonths = month; s.Status = "alignment"; s.Dread = 200.0;
            foreach (string id in new[] { "africa", "europe", "latin_america" })
            {
                Derived.Recruit(s.Regions[id], Derived.SiteRequirement(s.Regions[id]) + 60);
                s.Regions[id].Site = new Site { Invested = true, Progress = 24, Dormant = false };
            }
            return s;
        }
        static void EqualState(GameState expected, GameState actual) => GoldenTraces.RequireEqual(GameStateJson.ToJson(expected), GameStateJson.ToJson(actual));
        [Fact]
        public void StartsWithOneCellAndThreeResources()
        {
            var s = Start();
            Assert.Equal(100, Derived.TotalFollowers(s)); Assert.Equal(35, s.Influence); Assert.Equal(20, s.Dread);
        }
        [Fact]
        public void SacrificeConsumesFollowersWithoutMutatingInput()
        {
            var s = Start(); var snapshot = s.Clone(); double before = Derived.InfluencePerMonth(s);
            var result = GameEngine.PerformAction(s, "sacrifice", "africa");
            Assert.True(result.Ok); EqualState(snapshot, s);
            Assert.Equal(60, result.State.Regions["africa"].Population.Followers);
            Assert.Equal(40, result.State.Regions["africa"].Population.Losses);
            Assert.Equal(45, result.State.Dread);
            Assert.Equal(before - Balance.SacrificeFollowers * Balance.InfluencePerFollower, Derived.InfluencePerMonth(result.State), 12);
        }
        [Fact]
        public void RefusalsReturnTheSameState()
        {
            var s = Start();
            Assert.Same(s, GameEngine.PerformAction(s, "prepare", "africa").State);
            Assert.False(GameEngine.PerformAction(s, "expand", "oceania").Ok);
            var sacrificed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Same(sacrificed, GameEngine.PerformAction(sacrificed, "sacrifice", "africa").State);
        }
        [Fact]
        public void WaitingGrantsNoDreadAndIgnoresBadMonthCounts()
        {
            var s = Start();
            Assert.Equal(20, GameEngine.AdvanceSimulation(s, 1).Dread);
            Assert.Equal(12, GameEngine.AdvanceSimulation(GameEngine.SetGameSpeed(s, 0), 12).ElapsedMonths);
            Assert.Same(s, GameEngine.AdvanceSimulation(s, double.PositiveInfinity));
            Assert.Same(s, GameEngine.AdvanceSimulation(s, 0.5));
        }
        [Fact]
        public void AlignmentPausesLongAdvancesAndTheDeadlineEndsTheRun()
        {
            var s = GameEngine.AdvanceSimulation(Start(), Balance.Deadline);
            Assert.Equal(Balance.PreparationMonths, s.ElapsedMonths); Assert.Equal(0, s.Speed); Assert.Equal("alignment", s.Status);
            var lost = GameEngine.AdvanceSimulation(s, Balance.AlignmentMonths);
            Assert.Equal("lost", lost.Status); Assert.Equal(Balance.Deadline, lost.ElapsedMonths);
        }
        [Fact]
        public void FinalRiteWinsAfterTwelveSupportedMonths()
        {
            var started = GameEngine.StartAwakening(Ready(Balance.PreparationMonths + Balance.FinalMonths));
            Assert.True(started.Ok); var run = started.State;
            for (int month = 0; month < Balance.FinalMonths; month++)
            {
                foreach (string id in GameData.RegionIds) run.Regions[id].Threat = null;
                run = GameEngine.AdvanceSimulation(run, 1);
            }
            Assert.Equal("won", run.Status);
        }
        [Fact]
        public void LastFollowerLostDestroysTheSiteAndConservesPopulation()
        {
            var s = Ready(); var r = s.Regions["europe"];
            r.Population.Unaware += r.Population.Followers - 40; r.Population.Followers = 40.0;
            var sacrificed = GameEngine.PerformAction(s, "sacrifice", "europe").State;
            Assert.False(sacrificed.Regions["europe"].Site.Invested); Assert.Equal(2, Derived.ReadySites(sacrificed));
            foreach (string id in GameData.RegionIds) Assert.Equal(Derived.Capacity(s.Regions[id]), Derived.Capacity(sacrificed.Regions[id]), 10);
        }
        [Fact]
        public void PopulationAdjacencyAndCalendar()
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "population"), "oceania").State;
            double total = 0.0;
            foreach (string id in GameData.RegionIds)
            {
                total += Derived.LivePopulation(s.Regions[id]);
                foreach (string other in GameData.Regions[id].Adjacent) Assert.Contains(id, GameData.Regions[other].Adjacent);
            }
            Assert.Equal(10000, total); Assert.Equal("Year 1 · Month 1", Derived.Calendar(0)); Assert.Equal("Year 2 · Month 1", Derived.Calendar(12));
        }
        [Fact]
        public void JavascriptNumberFormatting()
        {
            Assert.Equal("0.13", Js.Fixed(0.125, 2)); Assert.Equal("3", Js.Fixed(2.5, 0));
            Assert.Equal("0.56", Js.Fixed(0.56, 2)); Assert.Equal("7.00", Js.Fixed(7.0, 2));
            Assert.Equal("75", Js.Num(75.0)); Assert.Equal("2.700000000000003", Js.Num(100.0 - 97.3));
            Assert.Equal(Rng.Next(Rng.SeedToState("same")), Rng.Next(Rng.SeedToState("same")));
        }
        [Fact]
        public void SiteUpkeepIsPaidMonthlyAndUnpaidSitesLieDormant()
        {
            var s = Ready(Balance.PreparationMonths - 30); s.Status = "playing";
            foreach (string id in new[] { "africa", "europe", "latin_america" }) s.Regions[id].Site.Progress = 10;
            s.Influence = 100.0;
            var paid = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal(3 * Balance.SiteUpkeep, paid.Telemetry.UpkeepPaid); Assert.Equal(11, paid.Regions["africa"].Site.Progress);
            Assert.Equal(Derived.InfluencePerMonth(paid) - 3 * Balance.SiteUpkeep, Derived.NetInfluencePerMonth(paid));
            var broke = paid.Clone(); broke.Influence = 0.0;
            foreach (string id in GameData.RegionIds)
            {
                var p = broke.Regions[id].Population;
                if (p.Followers > 10) { p.Unaware += p.Followers - 10; p.Followers = 10.0; }
            }
            var stalled = GameEngine.AdvanceSimulation(broke, 1);
            Assert.True(stalled.Regions["africa"].Site.Dormant);
            Assert.Equal("Dormant · upkeep unpaid", Derived.SiteLabel(stalled.Regions["africa"]));
            Assert.Contains(stalled.Log, e => e.Message.Contains("lies dormant"));
            stalled.Influence = 50.0;
            Assert.False(GameEngine.AdvanceSimulation(stalled, 1).Regions["africa"].Site.Dormant);
        }
        [Fact]
        public void DormantSitesCannotStartOrCarryTheFinalRite()
        {
            var s = Ready(); s.Regions["europe"].Site.Dormant = true;
            Assert.Equal(2, Derived.ReadySites(s)); Assert.False(GameEngine.StartAwakening(s).Ok);
            s.Regions["europe"].Site.Dormant = false;
            Assert.True(GameEngine.StartAwakening(s).Ok);
        }
        [Fact]
        public void ConcealPriceRisesWithAwarenessAndLowersIt()
        {
            var s = Start(); s.Awareness = 40.0; s.Influence = 100.0; s.Regions["africa"].Exposure = 0.0;
            var preview = GameEngine.PreviewAction(s, "conceal", "africa");
            Assert.True(preview.Ok, preview.Reason); Assert.Equal(40, preview.Influence);
            var result = GameEngine.PerformAction(s, "conceal", "africa");
            Assert.Equal(40.0 - Balance.ConcealAwareness, result.State.Awareness); Assert.Equal(s.Influence - 40, result.State.Influence);
            var calm = Start(); calm.Regions["africa"].Exposure = 0.0;
            Assert.False(GameEngine.PreviewAction(calm, "conceal", "africa").Ok); Assert.Equal(20, GameEngine.ConcealCost(calm));
        }
        [Fact]
        public void AlignmentHuntIsAnnouncedInAdvance()
        {
            var s = GameEngine.AdvanceSimulation(Start(), Balance.PreparationMonths - Balance.FinaleWarningMonths);
            Assert.Contains(s.Log, e => e.Message.StartsWith("The world senses the coming alignment", StringComparison.Ordinal));
            Assert.False(Director.SiteHunt(s));
            Assert.Contains("Threats against ritual sites intensify in 24 months", Forecast.Ritual(s).Message);
            var aligned = Ready(); Assert.True(Director.SiteHunt(aligned)); Assert.Contains("The world hunts ritual sites", Forecast.Ritual(aligned).Message);
        }
        [Fact]
        public void NewStateRecordsTheCurrentRuleset() => Assert.Equal(Balance.Ruleset, Start().Ruleset);
        static GameState Threatened(int severity = 1)
        {
            var s = Start(); s.ElapsedMonths = 20;
            Derived.Recruit(s.Regions["africa"], 300);
            s.Influence = 100.0; s.Dread = 100.0;
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", DueMonth = 25, Severity = severity };
            return s;
        }
        [Fact]
        public void ScatterEndsTheThreatAndCostsTheCellItsMonths()
        {
            var s = Threatened(); s.Regions["africa"].Public = true; s.Regions["africa"].Exposure = 30.0;
            var preview = GameEngine.PreviewAction(s, "scatter", "africa");
            Assert.True(preview.Ok);
            Assert.Equal(0, preview.Influence); Assert.Equal(0, preview.Dread); Assert.Equal(0, preview.Followers); Assert.Equal(0, preview.Awareness);
            var hidden = GameEngine.PerformAction(s, "scatter", "africa").State; var cell = hidden.Regions["africa"];
            Assert.Null(cell.Threat); Assert.True(cell.Scattered); Assert.False(cell.Public); Assert.Equal(20, cell.Exposure);
            Assert.Equal(0, Derived.InfluencePerMonth(hidden)); Assert.Equal(0, Derived.RecruitmentPerMonth(hidden, cell));
            Assert.Equal("The flock is scattered for 8 more months.", GameEngine.PreviewAction(hidden, "recruit", "africa").Reason);
            Assert.True(SaveData.IsGameState(hidden));
            var later = GameEngine.AdvanceSimulation(hidden, Balance.ScatterMonths - 1);
            Assert.True(later.Regions["africa"].Scattered); Assert.Equal(400, later.Regions["africa"].Population.Followers);
            var back = GameEngine.AdvanceSimulation(later, 1);
            Assert.False(back.Regions["africa"].Scattered); Assert.True(Derived.InfluencePerMonth(back) > 0);
            back.Regions["africa"].Threat = new Threat { Kind = "investigation", DueMonth = back.ElapsedMonths + 5, Severity = 1 };
            Assert.StartsWith("Available in", GameEngine.PreviewAction(back, "scatter", "africa").Reason);
            Assert.False(GameEngine.PreviewAction(Start(), "scatter", "africa").Ok);
        }
        [Fact]
        public void NobodyHuntsAScatteredCell()
        {
            var s = Threatened(); s.Regions["africa"].Threat = null; s.Regions["africa"].Exposure = 100.0; s.Awareness = 90.0;
            s.Regions["africa"].Scattered = true; s.Regions["africa"].ScatteredUntil = s.ElapsedMonths + Balance.ScatterMonths;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                s.RngState = Rng.SeedToState("hunt-" + attempt.ToString(System.Globalization.CultureInfo.InvariantCulture));
                Director.Tick(s); Assert.Null(s.Regions["africa"].Threat);
            }
            s.Regions["africa"].Scattered = false;
            bool found = false;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                s.RngState = Rng.SeedToState("hunt-" + attempt.ToString(System.Globalization.CultureInfo.InvariantCulture));
                Director.Tick(s); found |= s.Regions["africa"].Threat != null;
            }
            Assert.True(found);
        }
        [Fact]
        public void AScatteredSiteStallsAndCannotCarryTheRite()
        {
            var s = Ready(); s.Regions["europe"].Threat = new Threat { Kind = "occult", DueMonth = s.ElapsedMonths + 3, Severity = 1 };
            Assert.Equal(3, Derived.ReadySites(s));
            var hidden = GameEngine.PerformAction(s, "scatter", "europe").State;
            Assert.Equal(2, Derived.ReadySites(hidden)); Assert.NotEmpty(GameEngine.AwakeningReason(hidden));
            Assert.Equal("Stalled · the flock is scattered", Derived.SiteLabel(hidden.Regions["europe"]));
            Assert.Equal(40, hidden.Regions["europe"].Exposure);
        }
        [Fact]
        public void SilenceSpendsDreadBySeverityAndTheWorldNotices()
        {
            var s = Threatened(2); var preview = GameEngine.PreviewAction(s, "silence", "africa");
            Assert.Equal(2 * Balance.SilenceSeverityDread, preview.Dread); Assert.Equal(2 * Balance.SilenceSeverityAwareness, preview.Awareness);
            var done = GameEngine.PerformAction(s, "silence", "africa").State;
            Assert.Null(done.Regions["africa"].Threat);
            Assert.Equal(100.0 - 2 * Balance.SilenceSeverityDread, done.Dread); Assert.Equal(2 * Balance.SilenceSeverityAwareness, done.Awareness);
            Assert.Equal(2 * Balance.SilenceSeverityDread, done.Telemetry.DreadSpent); Assert.Equal(400, done.Regions["africa"].Population.Followers);
            done.Regions["africa"].Threat = new Threat { Kind = "investigation", DueMonth = 30, Severity = 1 };
            Assert.StartsWith("Available in", GameEngine.PreviewAction(done, "silence", "africa").Reason);
            Assert.True(GameEngine.PreviewAction(done, "counter", "africa").Ok);
            var harbinger = Threatened(2); harbinger.Specialization = "terror";
            Assert.Equal(2 * Balance.SilenceSeverityDreadTerror, GameEngine.PreviewAction(harbinger, "silence", "africa").Dread);
            Assert.False(GameEngine.PreviewAction(Start(), "silence", "africa").Ok);
        }
        [Fact]
        public void GoingPublicAlarmsTheWorldOnlyTheFirstTime()
        {
            var s = Threatened(); s.Regions["africa"].Threat = null;
            Assert.Equal(Balance.RevealAwareness, GameEngine.PreviewAction(s, "reveal", "africa").Awareness);
            var visible = GameEngine.PerformAction(s, "reveal", "africa").State;
            Assert.Equal(Balance.RevealAwareness, visible.Awareness);
            var hidden = GameEngine.PerformAction(visible, "hide", "africa").State; hidden.ElapsedMonths += 6;
            var again = GameEngine.PreviewAction(hidden, "reveal", "africa");
            Assert.True(again.Ok); Assert.Equal(0, again.Awareness); Assert.DoesNotContain("awareness", again.Risk);
            Assert.Equal(Balance.RevealAwareness, GameEngine.PerformAction(hidden, "reveal", "africa").State.Awareness);
        }
        [Fact]
        public void EveryConspicuousActionStatesItsAwarenessPrice()
        {
            var s = Threatened(); s.Regions["africa"].Threat = null; s.Dread = 200.0;
            Assert.Equal(Balance.ManifestAwareness, GameEngine.PreviewAction(s, "manifest", "africa").Awareness);
            Assert.Equal(Balance.SiteAwareness, GameEngine.PreviewAction(s, "prepare", "africa").Awareness);
            foreach (string id in new[] { "recruit", "sacrifice", "conceal", "counter", "scatter" }) Assert.Equal(0, GameEngine.PreviewAction(s, id, "africa").Awareness);
            Assert.Equal(Balance.ManifestAwareness, GameEngine.PerformAction(s, "manifest", "africa").State.Awareness);
            Assert.Equal(Balance.SiteAwareness, GameEngine.PerformAction(s, "prepare", "africa").State.Awareness);
        }
    }
}
