using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Awakening.Core.Session;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class PlaytestAlignment5Tests
    {
        static GameSession Session()
        {
            var game = new GameSession(SaveData.DefaultSave(), Configuration(), () => 0, _ => 1);
            game.SetParticipation(true);
            game.Begin("normal", "telemetry-alignment-5");
            game.Place("africa");
            return game;
        }
        static List<Dictionary<string, object>> Events(GameSession game) => A(game.Lab.Data, "staging").Cast<Dictionary<string, object>>().Select(e => O(e, "event")).ToList();
        static void Export(GameSession game, string name)
        {
            int index = 0;
            while (A(game.Lab.Data, "staging").Count != 0) game.Lab.SealBatch();
            foreach (Dictionary<string, object> job in A(game.Lab.Data, "jobs"))
            {
                Assert.Equal("awakening-6", Body(job)["adapter"]);
                Assert.All(A(Body(job), "events"), e => Assert.True(PlaytestCapture.ValidEvent(e)));
                string output = Environment.GetEnvironmentVariable("AWAKENING_PLAYTEST_BATCH_OUTPUT");
                if (!string.IsNullOrEmpty(output))
                {
                    Directory.CreateDirectory(output);
                    File.WriteAllText(Path.Combine(output, $"batch-{name}-{++index:00}.json"), (string)job["body"]);
                }
            }
        }

        [Fact]
        public void LocalValidatorRejectsRetiredAndMalformedV4Payloads()
        {
            var game = Session();
            game.Advance(24);
            game.State.Influence = 100;
            game.TakeTalent("red_altars");
            var talent = Events(game).Single(e => Equals(e["type"], "talent_taken"));
            Assert.True(PlaytestCapture.ValidEvent(talent));
            var mutations = new Action<Dictionary<string, object>>[]
            {
                e => e["type"] = "doctrine_selected",
                e => O(e, "data").Remove("spent"),
                e => O(e, "data")["branch"] = "unknown",
                e => O(e, "data")["tier"] = 4,
                e => O(e, "data")["slot"] = 0,
                e => O(e, "data")["slot"] = 7,
                e => O(e, "data")["talent"] = "Not an ID",
                e => O(O(e, "data"), "spent")["influence"] = -1,
                e => O(e, "data")["extra"] = 1,
                e => e["phase"] = "placing",
            };
            foreach (var mutate in mutations)
            {
                var changed = (Dictionary<string, object>)Awakening.Core.Json.JsonReader.Parse(Awakening.Core.Json.JsonWriter.Write(talent));
                mutate(changed);
                Assert.False(PlaytestCapture.ValidEvent(changed));
            }
            var start = Events(game).First();
            O(start, "data")["ruleset"] = "alignment-4";
            Assert.False(PlaytestCapture.ValidEvent(start));
            var snapshot = Events(game).First(e => Equals(e["type"], "month_snapshot"));
            O(snapshot, "data").Remove("unused_slots");
            Assert.False(PlaytestCapture.ValidEvent(snapshot));
        }

        [Fact]
        public void RealSessionOrdersEmitTalentsBurialsAndRestoration()
        {
            // A small manual policy uses only accepted game commands, with no resource/state grants.
            var game = Session();
            bool restored = false;
            foreach (int month in Enumerable.Range(0, Balance.Deadline))
            {
                if (GameEngine.IsOver(game.State)) break;
                Action<string> act = action => { if (GameEngine.PreviewAction(game.State, action, "africa").Ok) game.Act(action, "africa"); };
                var region = game.State.Regions["africa"];
                if (region.Threat != null)
                {
                    // Let an occult threat land once so restore has a real loss to repair.
                    if (restored || region.Threat.Kind != "occult" || region.Site.Progress < 6) act("counter");
                }
                foreach (string talent in new[] { "red_altars", "scapegoats", "dreams_of_rlyeh" })
                {
                    if (GameEngine.HasTalent(game.State, talent)) continue;
                    if (GameEngine.TalentReason(game.State, talent) == "") game.TakeTalent(talent);
                    break;
                }
                if (game.State.Regions["africa"].Site.Recoverable > 0 && GameEngine.PreviewAction(game.State, "restore", "africa").Ok)
                {
                    game.Act("restore", "africa"); restored = true;
                }
                if (game.State.Regions["africa"].Population.Followers < 700) act("recruit");
                if (game.State.Regions["africa"].Population.Followers > 350 && game.State.Dread < 100) act("sacrifice");
                act("prepare");
                if (game.State.Awareness > 0 && game.State.Influence > 200) act("bury");
                act("conceal");
                game.Advance(1);
            }
            var events = Events(game);
            Assert.Contains(events, e => Equals(e["type"], "talent_taken") && Equals(O(e, "data")["talent"], "dreams_of_rlyeh"));
            foreach (string action in new[] { "bury", "restore" })
                Assert.True(events.Any(e => Equals(e["type"], "action_performed") && Equals(O(e, "data")["action"], action)), "Missing action: " + action);
            Assert.DoesNotContain(events, e => Equals(e["type"], "doctrine_selected"));
            Assert.Equal(game.State.Telemetry.Sacrificed, events.Where(e => Equals(e["type"], "action_performed")).Sum(e => N(O(O(e, "data"), "spent"), "followers")), 8);
            var snapshot = O(events.Last(e => Equals(e["type"], "month_snapshot")), "data");
            Assert.Equal(3, N(snapshot, "talents"));
            Assert.True(N(snapshot, "bury_uses") > 0);
            Export(game, "manual-alignment5");
        }

        [Fact]
        public void CorneredSpawnUsesTheStateAfterFundingAndPartialCaptureUsesAlignment5()
        {
            var game = Session();
            // Controlled boundary: funding spends the last Influence/Dread, creating a cornered threat.
            var region = game.State.Regions["africa"];
            region.Population.Followers = Derived.SiteRequirement(region) + 100;
            game.State.Influence = GameData.Actions["prepare"].Influence;
            game.State.Dread = GameEngine.SiteDreadCost(game.State);
            game.Act("prepare", "africa");
            var spawn = O(Assert.Single(Events(game), e => Equals(e["type"], "threat_spawned")), "data");
            Assert.True((bool)spawn["cornered"]);
            Assert.Equal(new[] { "scatter" }, A(spawn, "legal_responses").Cast<string>());
            game.Advance(12);
            Export(game, "cornered-alignment5");
            game.SetParticipation(false);
            game.SetParticipation(true);
            var start = Assert.Single(Events(game), e => Equals(e["type"], "capture_started"));
            Assert.Equal(Balance.Ruleset, O(start, "data")["ruleset"]);
            Export(game, "partial-alignment5");
        }
    }
}
