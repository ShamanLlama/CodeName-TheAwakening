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
    // alignment-7: Madness as a regional resource (Dread, faster sites, and its price in occultists and awareness);
    // Dreams of R'lyeh acting before the rite; a grace after obstructing or silencing; Easy sites and talents.
    public class Alignment7Tests
    {
        static GameState At(int month, string difficulty = "normal")
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState(difficulty, "alignment-7"), "africa").State;
            s.ElapsedMonths = month; s.Influence = 1000.0; s.Dread = 200.0;
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
        static RegionState Site(GameState s, string region, int progress)
        {
            var r = Cell(s, region, Derived.SiteRequirement(s.Regions[region]) + 80);
            r.Site = new Site { Invested = true, Progress = progress };
            return r;
        }
        static GameState Ready(params string[] talents)
        {
            var s = With(At(Balance.PreparationMonths), talents);
            foreach (string id in new[] { "latin_america", "middle_east", "oceania" }) Site(s, id, GameEngine.SiteMonths(s));
            return s;
        }

        // ---- Madness: sources and decay ----

        [Fact]
        public void ManifestationsAndSacrificesLeaveMadnessThatFadesAMonthAtATime()
        {
            var s = At(41);
            var manifested = GameEngine.PerformAction(s, "manifest", "africa").State;
            Assert.Equal(Balance.ManifestMadness, manifested.Regions["africa"].Madness);
            var sacrificed = GameEngine.PerformAction(manifested, "sacrifice", "africa").State;
            Assert.Equal(Balance.ManifestMadness + Balance.SacrificeMadness, sacrificed.Regions["africa"].Madness);
            Assert.Contains("Madness here +10 (30).", GameEngine.PreviewAction(manifested, "sacrifice", "africa").Effect);
            Assert.Contains("Madness here +20 (20)", GameEngine.PreviewAction(s, "manifest", "africa").Effect);
            Assert.Contains("Madness draws occultists.", GameEngine.PreviewAction(s, "manifest", "africa").Risk);
            var later = GameEngine.AdvanceSimulation(sacrificed, 1);
            Assert.Equal(30 - Balance.MadnessDecay, later.Regions["africa"].Madness, 9);
            // Madness never passes 100 nor falls under 0.
            var full = At(41); full.Regions["africa"].Madness = 95;
            Assert.Equal(100, GameEngine.PerformAction(full, "manifest", "africa").State.Regions["africa"].Madness);
            var faint = At(41); faint.Regions["africa"].Madness = 0.4;
            Assert.Equal(0, GameEngine.AdvanceSimulation(faint, 1).Regions["africa"].Madness);
            Assert.Equal(1.0, Balance.MadnessDecay);
        }

        // ---- Madness: what it gives ----

        [Fact]
        public void MadnessYieldsDreadEveryMonthWithinTheCap()
        {
            var s = At(41); s.Dread = 100; s.Regions["africa"].Madness = 40;
            Assert.Equal(2.0, GameEngine.MadnessDread(s.Regions["africa"]));
            Cell(s, "europe", 50).Madness = 10;
            s.Regions["oceania"].Madness = 60; // no cell: no Dread
            Assert.Equal(0.0, GameEngine.MadnessDread(s.Regions["oceania"]));
            Assert.Equal(2.5, GameEngine.MadnessDreadTotal(s), 9);
            var next = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal(102.5, next.Dread, 9);
            Assert.Equal(2.5, next.Telemetry.MadnessDread, 9);
            Assert.Equal(s.Telemetry.DreadEarned + 2.5, next.Telemetry.DreadEarned, 9);

            var capped = At(41); capped.Dread = Balance.DreadCap - 0.5; capped.Regions["africa"].Madness = 40;
            var top = GameEngine.AdvanceSimulation(capped, 1);
            Assert.Equal(Balance.DreadCap, top.Dread);
            Assert.Equal(0.5, top.Telemetry.MadnessDread, 9);
        }

        [Fact]
        public void MadnessQuickensAPreparingSiteInProportion()
        {
            var s = At(40);
            var r = Site(s, "africa", 0);
            r.Madness = 60;
            Assert.True(GameEngine.MadnessQuickens(r)); Assert.True(GameEngine.MadnessQuickens(s, "africa"));
            Assert.Equal(0.6, GameEngine.MadnessHaste(r), 9);
            var one = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal(1, one.Regions["africa"].Site.Progress);
            Assert.Equal(0.6, one.Regions["africa"].Site.Quickening, 9);
            var two = GameEngine.AdvanceSimulation(one, 1);
            // 0.6 + 0.59: the second month brings a whole extra month.
            Assert.Equal(3, two.Regions["africa"].Site.Progress);
            Assert.Equal(0.19, two.Regions["africa"].Site.Quickening, 9);
            Assert.Equal(1, two.Telemetry.QuickenedMonths);

            // A dormant, unsupported or calm site gains nothing.
            var calm = At(40); Site(calm, "africa", 0);
            Assert.False(GameEngine.MadnessQuickens(calm.Regions["africa"]));
            Assert.Equal(0.0, GameEngine.AdvanceSimulation(calm, 1).Regions["africa"].Site.Quickening);
            var dormant = At(40); Site(dormant, "africa", 0).Madness = 60; dormant.Regions["africa"].Site.Dormant = true; dormant.Influence = 0;
            Assert.False(GameEngine.MadnessQuickens(dormant.Regions["africa"]));
            var thin = At(40); var t = Site(thin, "africa", 0); t.Madness = 60;
            Derived.LoseFollowers(t, t.Population.Followers - 10);
            Assert.False(GameEngine.MadnessQuickens(thin.Regions["africa"]));
            // A prepared site stops: no extra month past the end, and MadnessQuickens(state, region) says so.
            var done = At(40); Site(done, "africa", Balance.SiteMonths).Madness = 80;
            Assert.False(GameEngine.MadnessQuickens(done, "africa"));
            Assert.Equal(Balance.SiteMonths, GameEngine.AdvanceSimulation(done, 3).Regions["africa"].Site.Progress);
        }

        [Fact]
        public void MadnessTextSaysWhatTheRegionsMadnessDoes()
        {
            var s = At(40);
            Assert.Equal("", GameEngine.MadnessText(s, "africa"));
            s.Regions["africa"].Madness = 40;
            Assert.Equal("+2 Dread/month · draws occultists", GameEngine.MadnessText(s, "africa"));
            s.Regions["africa"].Madness = 61;
            Assert.Equal("+3.1 Dread/month · draws occultists · +0.05 world awareness/month", GameEngine.MadnessText(s, "africa"));
            Site(s, "africa", 4);
            Assert.Equal("+3.1 Dread/month · site prepares 61% faster · draws occultists · +0.05 world awareness/month", GameEngine.MadnessText(s, "africa"));
            s.Regions["oceania"].Madness = 50; // no cell
            Assert.Equal("", GameEngine.MadnessText(s, "oceania"));
        }

        // ---- Madness: what it costs ----

        [Fact]
        public void MadnessDrawsOccultistsAndTheWorldNoticesHighMadness()
        {
            var s = At(41);
            double calm = Director.ThreatPressure(s, "africa");
            s.Regions["africa"].Madness = 40;
            double factor = GameData.Difficulties["normal"].Pressure * (0.6 + GameData.Regions["africa"].InstitutionalResistance / 150.0);
            Assert.Equal(40 / Balance.MadnessPressureDivisor * factor, Director.ThreatPressure(s, "africa") - calm, 9);

            // From 50 a hidden cell without a site draws an occult society; under 50, investigators.
            var low = At(41); low.Regions["africa"].Madness = 49;
            Director.ScheduleThreat(low, "africa");
            Assert.Equal("investigation", low.Regions["africa"].Threat.Kind);
            var high = At(41); high.Regions["africa"].Madness = 50;
            Director.ScheduleThreat(high, "africa");
            Assert.Equal("occult", high.Regions["africa"].Threat.Kind);
            var open = At(41); open.Regions["africa"].Madness = 80; open.Regions["africa"].Public = true;
            Director.ScheduleThreat(open, "africa");
            Assert.Equal("raid", open.Regions["africa"].Threat.Kind);

            // Each region of high Madness with a cell adds world awareness every month.
            var under = At(41); under.Regions["africa"].Madness = 49.5;
            var over = At(41); over.Regions["africa"].Madness = 50;
            double gap = GameEngine.AdvanceSimulation(over, 1).Awareness - GameEngine.AdvanceSimulation(under, 1).Awareness;
            Assert.Equal(Balance.MadnessAwareness, gap, 4);
            var empty = At(41); empty.Regions["oceania"].Madness = 90;
            Assert.False(GameEngine.MadnessHigh(empty.Regions["oceania"]));
        }

        // ---- Dreams of R'lyeh ----

        [Fact]
        public void DreamsOfRlyehDoubleTheMadnessOfSacrificesAndSpreadIt()
        {
            var s = With(At(41), "red_altars", "scapegoats", "dreams_of_rlyeh");
            Assert.Equal(20, GameEngine.SacrificeMadness(s));
            var fed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Equal(20, fed.Regions["africa"].Madness);
            Assert.Equal(10, GameEngine.TalentYieldOf(fed, "dreams_of_rlyeh"));
            Assert.Equal("+10 Madness spread so far", GameEngine.TalentYieldText(fed, "dreams_of_rlyeh"));

            // After the month's fading, each region still at 30+ gives each neighbouring cell +2 (net +1 a month for one
            // that already had Madness); regions without a cell receive nothing.
            var d = With(At(41), "red_altars", "scapegoats", "dreams_of_rlyeh");
            d.Regions["africa"].Madness = 31;
            Cell(d, "latin_america", 60).Madness = 10; Cell(d, "middle_east", 60);
            var next = GameEngine.AdvanceSimulation(d, 1);
            Assert.Equal(11, next.Regions["latin_america"].Madness, 9);
            Assert.Equal(2, next.Regions["middle_east"].Madness, 9);
            Assert.Equal(0, next.Regions["europe"].Madness);
            Assert.Equal(30, next.Regions["africa"].Madness, 9);
            Assert.Equal(4, GameEngine.TalentYieldOf(next, "dreams_of_rlyeh"), 9);
            // Under 30 once faded, or without the talent, nothing spreads.
            var faint = With(At(41), "red_altars", "scapegoats", "dreams_of_rlyeh"); faint.Regions["africa"].Madness = 30.5; Cell(faint, "latin_america", 60);
            Assert.Equal(0, GameEngine.AdvanceSimulation(faint, 1).Regions["latin_america"].Madness);
            var plain = With(At(41), "red_altars", "scapegoats"); plain.Regions["africa"].Madness = 60; Cell(plain, "latin_america", 60);
            Assert.Equal(0, GameEngine.AdvanceSimulation(plain, 1).Regions["latin_america"].Madness);
            Assert.Equal(10, GameEngine.SacrificeMadness(plain));
        }

        [Fact]
        public void ADreamSacrificeWhereMadnessRunsHighCarriesTheRiteTwoMonths()
        {
            var s = GameEngine.StartAwakening(Ready("red_altars", "scapegoats", "dreams_of_rlyeh")).State;
            Assert.Equal(1, GameEngine.DreamMonths(s, "africa"));
            s.Regions["africa"].Madness = 30;
            Assert.Equal(2, GameEngine.DreamMonths(s, "africa"));
            var preview = GameEngine.PreviewAction(s, "sacrifice", "africa");
            Assert.EndsWith("The dream carries the final rite 2 months forward (2/12).", preview.Effect);
            var fed = GameEngine.PerformAction(s, "sacrifice", "africa").State;
            Assert.Equal(2, fed.FinalProgress); Assert.Equal(2, fed.DreamRiteMonths);
            Assert.Equal(s.Awareness + Balance.DreamsAwareness, fed.Awareness);
            Assert.Equal("+10 Madness spread · 2 rite months dreamed forward so far", GameEngine.TalentYieldText(fed, "dreams_of_rlyeh"));
            Assert.True(SaveData.IsGameState(fed));
            // Never past the rite's end.
            var near = s.Clone(); near.FinalProgress = 11;
            Assert.Equal(1, GameEngine.DreamMonths(near, "africa"));
            Assert.Equal(12, GameEngine.PerformAction(near, "sacrifice", "africa").State.FinalProgress);
            // Before the rite nothing is carried.
            Assert.Equal(0, GameEngine.DreamMonths(Ready("red_altars", "scapegoats", "dreams_of_rlyeh"), "africa"));
        }

        // ---- The grace after an answer ----

        static GameState Hunted(string response)
        {
            // From month 200 a site under the hunt draws a threat at every roll: its pressure passes 1.
            var s = At(203);
            var r = Site(s, "africa", 10);
            r.Exposure = 100; r.Madness = 100; s.Awareness = 90;
            Assert.True(Director.ThreatPressure(s, "africa") >= 1.0);
            r.Threat = new Threat { Kind = "occult", DueMonth = 207, Severity = 3 };
            if (response == null) { r.Threat = null; return s; }
            var answered = GameEngine.PerformAction(s, response, "africa");
            Assert.True(answered.Ok, answered.Message);
            return answered.State;
        }

        [Theory]
        [InlineData("counter")]
        [InlineData("silence")]
        public void ObstructingOrSilencingLeavesTheRegionQuietForAWhile(string response)
        {
            var s = Hunted(response);
            Assert.Equal(Balance.ThreatGraceMonths, GameEngine.ThreatGraceLeft(s, "africa"));
            Assert.Equal(208, s.Regions["africa"].ThreatGraceUntil);
            // Month 204 rolls: the region is passed over, though its pressure passes 1.
            var rolled = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal(204, rolled.ElapsedMonths);
            Assert.Null(rolled.Regions["africa"].Threat);
            Assert.Equal(4, GameEngine.ThreatGraceLeft(rolled, "africa"));
            // Month 208 rolls again and the grace is over.
            var after = GameEngine.AdvanceSimulation(rolled, 4);
            Assert.Equal(0, GameEngine.ThreatGraceLeft(after, "africa"));
            Assert.NotNull(after.Regions["africa"].Threat);
            Assert.True(SaveData.IsGameState(s));
            Assert.Equal(s.Regions["africa"].ThreatGraceUntil, GameStateJson.FromJson(GameStateJson.ToJson(s)).Regions["africa"].ThreatGraceUntil);
            // Without an answer the same roll draws a threat.
            Assert.NotNull(GameEngine.AdvanceSimulation(Hunted(null), 1).Regions["africa"].Threat);
        }

        [Fact]
        public void AnswersSayHowLongTheRegionStaysQuiet()
        {
            var s = At(41);
            s.Regions["africa"].Threat = new Threat { Kind = "investigation", DueMonth = 45, Severity = 1 };
            Assert.Equal("No further obstruction here for 12 months. No new threat here for 5 months.", GameEngine.PreviewAction(s, "counter", "africa").Risk);
            Assert.Equal("The threat ends tonight. No new threat here for 5 months.", GameEngine.PreviewAction(s, "silence", "africa").Effect);
            Assert.Equal(0, GameEngine.ThreatGraceLeft(s, "africa"));
            // Scattering is no grace: the region is simply out of the roll while it hides.
            var hidden = GameEngine.PerformAction(s, "scatter", "africa").State;
            Assert.Equal(0, GameEngine.ThreatGraceLeft(hidden, "africa"));
            Assert.Equal(5, Balance.ThreatGraceMonths);
        }

        // ---- Easy: sites and talents ----

        [Fact]
        public void EasySitesPrepareInTwentyMonthsAndEveryTextSaysSo()
        {
            Assert.Equal(new[] { 20, 24, 24 }, GameData.DifficultyIds.Select(d => GameData.Difficulties[d].SiteMonths));
            var s = At(40, "easy");
            Assert.Equal(20, GameEngine.SiteMonths(s));
            Assert.Equal(Balance.Deadline - Balance.FinalMonths - 20, GameEngine.LatestSiteStart(s));
            Assert.Contains("Allow 20 months of preparation plus 12 for the final rite.", Forecast.Ritual(s).Message);
            Assert.Contains("Allow 24 months of preparation plus 12 for the final rite.", Forecast.Ritual(At(40)).Message);
            Cell(s, "africa", 600);
            Assert.StartsWith("Commit 60 Dread and 40 Influence, then pay 1 Influence/month and keep local support for 20 months.", GameEngine.PreviewAction(s, "prepare", "africa").Effect);
            Assert.Contains("keep local support for 24 months (20 on Easy)", GameData.Actions["prepare"].Description);

            var r = Site(s, "africa", 10);
            Assert.Equal("Preparing · 10/20 months", Derived.SiteLabel(s, r));
            r.Site.Progress = 19;
            var ready = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal(20, ready.Regions["africa"].Site.Progress);
            Assert.Equal("Ritual prepared", Derived.SiteLabel(ready, ready.Regions["africa"]));
            Assert.Equal(1, Derived.ReadySites(ready));
            Assert.Contains(ready.Log, e => e.Message == "The ritual site in Africa is prepared.");
            Assert.Equal(20, GameEngine.AdvanceSimulation(ready, 2).Regions["africa"].Site.Progress);
            Assert.True(SaveData.IsGameState(ready));

            // The last safe start moves too: on Easy a site funded at month 192 still finishes before a 12-month rite.
            var late = At(192, "easy"); Cell(late, "latin_america", 400);
            Assert.True(GameEngine.PreviewAction(late, "prepare", "latin_america").Ok);
            late.ElapsedMonths = 193;
            Assert.Equal("Too late: preparation needs 20 more months and the final rite needs 12, but only 31 remain.", GameEngine.PreviewAction(late, "prepare", "latin_america").Reason);
            var normal = At(192); Cell(normal, "latin_america", 400);
            Assert.StartsWith("Too late: preparation needs 24 more months", GameEngine.PreviewAction(normal, "prepare", "latin_america").Reason);
        }

        [Fact]
        public void EasyTalentsAreCheaperAndNormalsStepIsTwentyFive()
        {
            var easy = At(176, "easy");
            var costs = new List<int>();
            foreach (string id in new[] { "quiet_friends", "sleeper_cells", "world_forgets" })
            {
                costs.Add(GameEngine.TalentCost(easy));
                easy = GameEngine.TakeTalent(easy, id).State;
            }
            Assert.Equal(new[] { 30, 50, 70 }, costs);
            Assert.Equal(40, GameEngine.TalentCost(At(176)));
            Assert.Equal(65, GameEngine.TalentCost(GameEngine.TakeTalent(At(176), "quiet_friends").State));
            Assert.Equal(65, GameEngine.TalentCost(GameEngine.TakeTalent(At(176, "hard"), "quiet_friends").State));
            Assert.StartsWith("Sites prepare in 20 months. 6 talent seals; talents cost 30 Influence, 20 more for each one taken.", GameData.Difficulties["easy"].Rule);
        }

        // ---- Saves ----

        [Fact]
        public void Alignment6RunsRetireWithANoticeAndKeepSettings()
        {
            // The alignment-6 shape: no Madness telemetry, grace, dream months or site quickening.
            var json = GameStateJson.ToJson(At(60));
            json["ruleset"] = "alignment-6";
            json.Remove("dreamRiteMonths");
            var raw = JsonWriter.Write(new Dictionary<string, object>
            {
                ["version"] = 2, ["state"] = json, ["settings"] = new Dictionary<string, object> { ["muted"] = false, ["volume"] = 55.0, ["hideGoals"] = true },
                ["tutorialComplete"] = true, ["lastResult"] = null
            });
            var decoded = SaveData.DecodeSave(raw);
            Assert.NotNull(decoded); Assert.Null(decoded.State);
            Assert.Equal(55.0, decoded.Settings.Volume); Assert.True(decoded.Settings.HideGoals); Assert.True(decoded.TutorialComplete);
            Assert.Equal("Your unfinished run used older rules (alignment-6) and cannot continue under alignment-7. Settings and playtest records are kept; start a new run.",
                SaveData.TakeRetiredNotice(decoded));
            Assert.Contains("alignment-6", SaveData.RetiredRulesets);
            Assert.Equal("alignment-7", Balance.Ruleset);
        }

        [Theory]
        [InlineData("quickening-over")]
        [InlineData("quickening-without-site")]
        [InlineData("grace-past")]
        [InlineData("dreams-past-progress")]
        [InlineData("easy-progress-over")]
        [InlineData("missing-madness-dread")]
        [InlineData("missing-quickened")]
        [InlineData("missing-grace")]
        public void SavesRejectImpossibleAlignment7Values(string mutation)
        {
            var s = At(60, mutation.StartsWith("easy") ? "easy" : "normal");
            Site(s, "africa", 5);
            var json = GameStateJson.ToJson(s);
            Assert.True(SaveData.IsGameState(json));
            var regions = (Dictionary<string, object>)json["regions"];
            var africa = (Dictionary<string, object>)regions["africa"];
            var europe = (Dictionary<string, object>)regions["europe"];
            var telemetry = (Dictionary<string, object>)json["telemetry"];
            switch (mutation)
            {
                case "quickening-over": ((Dictionary<string, object>)africa["site"])["quickening"] = 1.5; break;
                case "quickening-without-site": ((Dictionary<string, object>)europe["site"])["quickening"] = 0.5; break;
                case "grace-past": africa["threatGraceUntil"] = Balance.Deadline + Balance.ThreatGraceMonths + 1; break;
                case "dreams-past-progress": json["dreamRiteMonths"] = 1; break;
                case "easy-progress-over": ((Dictionary<string, object>)africa["site"])["progress"] = 21; break;
                case "missing-madness-dread": telemetry.Remove("madnessDread"); break;
                case "missing-quickened": telemetry.Remove("quickenedMonths"); break;
                case "missing-grace": africa.Remove("threatGraceUntil"); break;
            }
            Assert.False(SaveData.IsGameState(json));
        }

        [Fact]
        public void Alignment7FieldsSurviveASave()
        {
            var s = At(60);
            var r = Site(s, "africa", 5);
            r.Madness = 42.5; r.Site.Quickening = 0.25; r.ThreatGraceUntil = 63;
            s.Telemetry.MadnessDread = 12.5; s.Telemetry.QuickenedMonths = 3;
            var back = GameStateJson.FromJson(GameStateJson.ToJson(s));
            Assert.NotNull(back);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(s), GameStateJson.ToJson(back));
            Assert.Equal(0.25, back.Regions["africa"].Site.Quickening);
            Assert.Equal(3, back.Telemetry.QuickenedMonths);
        }

        // ---- The bot ----

        [Fact]
        public void TheHarbingerPolicyRaisesMadnessWhereItsSitesPrepare()
        {
            int quickened = 0, runs = 0;
            for (int i = 0; i < 6; i++)
            {
                var end = Playthrough.Play("terror", "measure-" + i);
                quickened += end.Telemetry.QuickenedMonths;
                if (end.Telemetry.MadnessDread > 0) runs++;
            }
            Assert.True(quickened > 0, "terror quickened no site month in 6 runs");
            Assert.Equal(6, runs);
        }
    }
}
