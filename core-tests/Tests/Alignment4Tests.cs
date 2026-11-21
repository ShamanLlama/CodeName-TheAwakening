using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Json;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Xunit;

namespace Awakening.Core.Tests
{
    // alignment-4: concealment's world effect rests between uses, the rite's margin is derived, and a run can be conceded.
    public class Alignment4Tests
    {
        static GameState TwoCells(double awareness)
        {
            var s = EngineTests.Start();
            Derived.Recruit(s.Regions["europe"], 100);
            s.Awareness = awareness; s.Influence = 200.0;
            s.Regions["africa"].Exposure = 30.0; s.Regions["europe"].Exposure = 30.0;
            return s;
        }

        [Fact]
        public void ConcealLowersWorldAwarenessThenRestsSixMonthsInEveryRegion()
        {
            var s = TwoCells(40.0);
            Assert.Equal(0, GameEngine.ConcealWorldCooldown(s));
            var ready = GameEngine.PreviewAction(s, "conceal", "africa");
            Assert.True(ready.Ok, ready.Reason); Assert.Equal(40, ready.Influence);
            Assert.Equal("−25 Exposure, −3 world awareness. The price rises with awareness.", ready.Effect);
            Assert.Equal("The world effect then rests for 6 months in every region.", ready.Risk);

            var first = GameEngine.PerformAction(s, "conceal", "africa").State;
            Assert.Equal(37.0, first.Awareness); Assert.Equal(160.0, first.Influence); Assert.Equal(5.0, first.Regions["africa"].Exposure);
            Assert.Equal(s.ElapsedMonths + Balance.ConcealWorldMonths, first.WorldConcealReadyAt);
            Assert.Equal(6, GameEngine.ConcealWorldCooldown(first));
            Assert.Equal(s.ElapsedMonths + 12, first.Regions["africa"].Cooldowns["conceal"]);

            var resting = GameEngine.PreviewAction(first, "conceal", "europe");
            Assert.True(resting.Ok, resting.Reason); Assert.Equal(20, resting.Influence); Assert.Equal(0.0, resting.Awareness);
            Assert.Equal("−25 Exposure only. The world effect returns in 6 months.", resting.Effect);
            Assert.Equal("", resting.Risk);
            var second = GameEngine.PerformAction(first, "conceal", "europe").State;
            Assert.Equal(37.0, second.Awareness); Assert.Equal(140.0, second.Influence); Assert.Equal(5.0, second.Regions["europe"].Exposure);
            Assert.Equal(first.WorldConcealReadyAt, second.WorldConcealReadyAt);

            var later = second.Clone(); later.ElapsedMonths += 5;
            Assert.Equal(1, GameEngine.ConcealWorldCooldown(later));
            later.ElapsedMonths += 1;
            Assert.Equal(0, GameEngine.ConcealWorldCooldown(later));
            Assert.Equal(20 + (int)Js.RoundHalfUp(later.Awareness * Balance.ConcealAwarenessPrice), GameEngine.ConcealCost(later));
        }

        [Fact]
        public void NothingToConcealNamesTheWorldEffectsReturn()
        {
            var s = TwoCells(40.0);
            s.Regions["africa"].Exposure = 0.0;
            Assert.True(GameEngine.PreviewAction(s, "conceal", "africa").Ok, "world effect alone is worth concealing");
            s.WorldConcealReadyAt = s.ElapsedMonths + 4;
            var preview = GameEngine.PreviewAction(s, "conceal", "africa");
            Assert.False(preview.Ok);
            Assert.Equal("Nothing to conceal here. The world effect returns in 4 months.", preview.Reason);
            Assert.False(GameEngine.PerformAction(s, "conceal", "africa").Ok);
            s.Awareness = 0.0;
            Assert.Equal("Nothing to conceal. Public rites remain visible.", GameEngine.PreviewAction(s, "conceal", "africa").Reason);
        }

        [Fact]
        public void ConcealWithoutWorldAwarenessKeepsTheWorldEffectReady()
        {
            var s = TwoCells(0.0);
            var preview = GameEngine.PreviewAction(s, "conceal", "africa");
            Assert.True(preview.Ok, preview.Reason); Assert.Equal("−25 Exposure.", preview.Effect); Assert.Equal("", preview.Risk);
            var next = GameEngine.PerformAction(s, "conceal", "africa").State;
            Assert.Equal(0, next.WorldConcealReadyAt); Assert.Equal(0, GameEngine.ConcealWorldCooldown(next));
        }

        [Fact]
        public void EveryEndingRecordsItsResultCode()
        {
            var won = EngineTests.Ready(); won = GameEngine.StartAwakening(won).State;
            Assert.Null(won.ResultCode);
            won.FinalProgress = Balance.FinalMonths - 1;
            won = GameEngine.AdvanceSimulation(won, 1);
            Assert.Equal("won", won.Status); Assert.Equal("awakened", won.ResultCode);

            var aware = EngineTests.Start(); aware.Awareness = 99.99; aware.Regions["africa"].Public = true;
            aware = GameEngine.AdvanceSimulation(aware, 1);
            Assert.Equal("lost", aware.Status); Assert.Equal("world_awareness", aware.ResultCode);

            var late = EngineTests.Start(); late.ElapsedMonths = Balance.Deadline - 1; late.Status = "alignment";
            late = GameEngine.AdvanceSimulation(late, 1);
            Assert.Equal("lost", late.Status); Assert.Equal("deadline", late.ResultCode);
        }

        static GameState Sites(int month, params int[] progress)
        {
            var s = EngineTests.Start();
            s.ElapsedMonths = month; s.Status = month >= Balance.PreparationMonths ? "alignment" : "playing"; s.Dread = 200.0; s.Influence = 200.0;
            string[] regions = { "africa", "europe", "latin_america", "oceania" };
            for (int i = 0; i < progress.Length; i++)
            {
                var r = s.Regions[regions[i]];
                Derived.Recruit(r, Derived.SiteRequirement(r) + 60);
                r.Site = new Site { Invested = true, Progress = progress[i] };
            }
            return s;
        }

        [Fact]
        public void MarginComesFromTheThirdSlowestFundedSite()
        {
            var none = Sites(0);
            var open = Forecast.Margin(none);
            Assert.Equal(Balance.LatestRiteStart, open.LatestStart); Assert.Equal(-1, open.EarliestStart);
            Assert.True(open.Fits); Assert.False(open.Started); Assert.Empty(open.Sites);

            var m = Forecast.Margin(Sites(185, 0, 20, 10, 2));
            Assert.Equal(185 + 22, m.EarliestStart); Assert.Equal(Balance.LatestRiteStart - 207, m.Margin); Assert.True(m.Fits);
            Assert.Equal(new[] { "europe", "latin_america", "oceania", "africa" }, m.Sites.Select(x => x.Region));
            Assert.Equal(new[] { 4, 14, 22, 24 }, m.Sites.Select(x => x.MonthsLeft));
            var europe = m.Sites[0];
            Assert.True(europe.Supported); Assert.False(europe.Scattered);
            Assert.Equal((int)Derived.SiteRequirement(EngineTests.Start().Regions["europe"]), europe.Required);
            Assert.True(europe.Followers >= europe.Required);

            var ready = Forecast.Margin(EngineTests.Ready());
            Assert.Equal(Balance.PreparationMonths, ready.EarliestStart); Assert.Equal(12, ready.Margin); Assert.True(ready.Fits);
        }

        [Fact]
        public void LateSitesFitOnlyWithAccelerationAvailableNow()
        {
            var s = Sites(190, 24, 24, 0);
            var late = Forecast.Margin(s);
            Assert.Equal(214, late.EarliestStart); Assert.Equal(-2, late.Margin); Assert.False(late.Fits);
            Assert.True(GameEngine.CanConcede(s));

            s.Regions["latin_america"].Opportunity = new Opportunity { Kind = "eclipse", ExpiresMonth = 195 };
            var eclipse = Forecast.Margin(s);
            Assert.Equal(-2, eclipse.Margin); Assert.True(eclipse.Fits);
            Assert.False(GameEngine.CanConcede(s));

            // An unfunded region still counts if a new site there could finish in time.
            var two = Sites(180, 24, 24);
            Assert.True(Forecast.Margin(two).Fits); Assert.Equal(-1, Forecast.Margin(two).EarliestStart);
            two.ElapsedMonths = 189;
            Assert.False(Forecast.Margin(two).Fits);
            two.Regions["oceania"].Figure = new Figure { Kind = "celebrity", Revealed = false };
            Assert.True(Forecast.Margin(two).Fits);
        }

        [Fact]
        public void ScatteredSitesCountTheMonthsTheyStall()
        {
            var s = Sites(100, 24, 24, 22);
            var r = s.Regions["latin_america"];
            r.Scattered = true; r.ScatteredUntil = 103;
            var site = Forecast.Margin(s).Sites.Single(x => x.Region == "latin_america");
            Assert.True(site.Scattered); Assert.False(site.Supported); Assert.Equal(4, site.MonthsLeft);
            Assert.Equal(23, GameEngine.AdvanceSimulation(s, 3).Regions["latin_america"].Site.Progress);
            Assert.Equal(24, GameEngine.AdvanceSimulation(s, 4).Regions["latin_america"].Site.Progress);
        }

        [Fact]
        public void StartedRiteFitsWhileItsMonthsRemain()
        {
            var s = GameEngine.StartAwakening(EngineTests.Ready(206)).State;
            var m = Forecast.Margin(s);
            Assert.True(m.Started); Assert.True(m.Fits);
            s.ElapsedMonths = 215; s.FinalProgress = 2;
            Assert.False(Forecast.Margin(s).Fits);
            Assert.False(GameEngine.CanConcede(s));
        }

        [Fact]
        public void ConcedingEndsADoomedRunAsALoss()
        {
            var start = EngineTests.Start();
            Assert.False(GameEngine.CanConcede(start));
            Assert.Same(start, GameEngine.Concede(start));

            var waiting = EngineTests.Ready(Balance.LatestRiteStart);
            Assert.True(Forecast.Margin(waiting).Fits); Assert.False(GameEngine.CanConcede(waiting));
            var doomed = EngineTests.Ready(Balance.LatestRiteStart + 1);
            Assert.False(Forecast.Margin(doomed).Fits); Assert.True(GameEngine.CanConcede(doomed));
            var snapshot = doomed.Clone();
            var over = GameEngine.Concede(doomed);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(snapshot), GameStateJson.ToJson(doomed));
            Assert.Equal("lost", over.Status); Assert.Equal("conceded", over.ResultCode); Assert.Equal(0, over.Speed);
            Assert.Equal(GameEngine.ConcededReason, over.ResultReason);
            Assert.Equal(GameEngine.ConcededReason, over.Log.Last().Message);
            Assert.False(GameEngine.CanConcede(over)); Assert.Same(over, GameEngine.Concede(over));
            Assert.False(Forecast.Margin(over).Fits);
            Assert.Same(over, GameEngine.AdvanceSimulation(over, 1));
        }

        static string SaveOf(GameState s)
        {
            var save = SaveData.DefaultSave(); save.State = s;
            return SaveData.EncodeSave(save);
        }

        [Fact]
        public void SavesKeepTheWorldCooldownAndTheEndingCode()
        {
            var s = GameEngine.PerformAction(TwoCells(40.0), "conceal", "africa").State;
            var decoded = SaveData.DecodeSave(SaveOf(s));
            Assert.Equal(s.WorldConcealReadyAt, decoded.State.WorldConcealReadyAt);

            var conceded = GameEngine.Concede(EngineTests.Ready(Balance.LatestRiteStart + 1));
            var restored = SaveData.DecodeSave(SaveOf(conceded));
            Assert.NotNull(restored?.State);
            Assert.Equal("conceded", restored.State.ResultCode); Assert.Equal("lost", restored.State.Status);

            var forged = conceded.Clone(); forged.ResultCode = null;
            Assert.Null(SaveData.DecodeSave(SaveOf(forged)));
            forged.ResultCode = "abandoned";
            Assert.Null(SaveData.DecodeSave(SaveOf(forged)));
            var active = EngineTests.Start(); active.ResultCode = "deadline";
            Assert.Null(SaveData.DecodeSave(SaveOf(active)));
            var mislabeled = conceded.Clone(); mislabeled.ResultCode = "awakened";
            Assert.Null(SaveData.DecodeSave(SaveOf(mislabeled)));
        }

        [Fact]
        public void SavesWithoutTheNewKeysLoadWithDefaults()
        {
            var json = GameStateJson.ToJson(EngineTests.Start());
            json.Remove("worldConcealReadyAt"); json.Remove("resultCode");
            var save = new Dictionary<string, object>
            {
                ["version"] = 2, ["state"] = json, ["settings"] = new Dictionary<string, object> { ["muted"] = true, ["volume"] = 40.0 },
                ["tutorialComplete"] = false, ["lastResult"] = null
            };
            var decoded = SaveData.DecodeSave(JsonWriter.Write(save));
            Assert.NotNull(decoded?.State);
            Assert.Equal(0, decoded.State.WorldConcealReadyAt); Assert.Null(decoded.State.ResultCode);
        }
    }
}
