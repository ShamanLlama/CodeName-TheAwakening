using System;
using System.Collections.Generic;
using System.IO;
using Awakening.Core.Dev;
using Awakening.Core.Json;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Xunit;

namespace Awakening.Core.Tests
{
    public class SaveTests
    {
        static string Envelope(GameState state)
        {
            var save = SaveData.DefaultSave(); save.State = state;
            return SaveData.EncodeSave(save);
        }
        [Fact]
        public void AwarenessLossSurvivesReload()
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "x"), "africa").State; s.Awareness = 99.99;
            var lost = GameEngine.AdvanceSimulation(s, 1);
            Assert.Equal("lost", lost.Status); Assert.True(lost.ElapsedMonths < Balance.Deadline);
            var decoded = SaveData.DecodeSave(Envelope(lost)); Assert.NotNull(decoded);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(lost), GameStateJson.ToJson(decoded.State));
        }
        [Fact]
        public void RejectsCorruptAndImpossibleSaves()
        {
            Assert.Null(SaveData.DecodeSave("{not json")); Assert.Null(SaveData.DecodeSave("[]"));
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "x"), "africa").State;
            var created = s.Clone(); created.Regions["africa"].Population.Followers++;
            Assert.Null(SaveData.DecodeSave(Envelope(created)));
            var earlyLoss = s.Clone(); earlyLoss.Status = "lost"; earlyLoss.Speed = 0;
            Assert.Null(SaveData.DecodeSave(Envelope(earlyLoss)));
        }
        [Fact]
        public void DecodedIntegersSupportMonthArithmetic()
        {
            var s = GameEngine.AdvanceSimulation(GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("hard", "ints"), "europe").State, 30);
            var decoded = SaveData.DecodeSave(Envelope(s)); Assert.NotNull(decoded);
            Assert.IsType<int>(decoded.State.ElapsedMonths);
            GoldenTraces.RequireEqual(GameStateJson.ToJson(GameEngine.AdvanceSimulation(s, 40)), GameStateJson.ToJson(GameEngine.AdvanceSimulation(decoded.State, 40)));
        }
        [Fact]
        public void FilePayloadRoundTripsAndKeepsARecoveryCopy()
        {
            // File replacement/recovery belongs to Platform. Exercise its codec boundary with
            // real bytes here; Core must neither discard nor rewrite an unreadable payload.
            string path = Path.Combine(Path.GetTempPath(), "awakening-save-" + Guid.NewGuid().ToString("N") + ".json");
            string recovery = path + ".recovery";
            try
            {
                var save = SaveData.DefaultSave(); save.State = GameEngine.CreateGameState("easy", "file");
                File.WriteAllText(path, SaveData.EncodeSave(save));
                var loaded = SaveData.DecodeSave(File.ReadAllText(path)); Assert.NotNull(loaded);
                GoldenTraces.RequireEqual(GameStateJson.ToJson(save.State), GameStateJson.ToJson(loaded.State));
                File.WriteAllText(path, "{broken");
                Assert.Null(SaveData.DecodeSave(File.ReadAllText(path)));
                File.Copy(path, recovery); Assert.Equal("{broken", File.ReadAllText(recovery));
                File.Delete(path); Assert.False(File.Exists(path)); Assert.Null(SaveData.DefaultSave().State);
            }
            finally { File.Delete(path); File.Delete(recovery); }
        }
        [Fact]
        public void RetiredRulesetRunIsDroppedButTheSaveIsKept()
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "old"), "africa").State;
            s.Ruleset = "alignment-1";
            var save = SaveData.DefaultSave(); save.State = s; save.Settings.Volume = 70; save.TutorialComplete = true;
            var decoded = SaveData.DecodeSave(SaveData.EncodeSave(save)); Assert.NotNull(decoded); Assert.Null(decoded.State);
            Assert.Equal(70, decoded.Settings.Volume); Assert.True(decoded.TutorialComplete);
            string notice = SaveData.TakeRetiredNotice(decoded);
            Assert.Contains("alignment-1", notice); Assert.Contains(Balance.Ruleset, notice);
            Assert.Null(decoded.RetiredRuleset); Assert.Equal("", SaveData.TakeRetiredNotice(decoded));
            Assert.DoesNotContain("retiredRuleset", SaveData.EncodeSave(decoded));
            foreach (string older in new[] { "alignment-2", "alignment-3", "alignment-4" })
            {
                s.Ruleset = older;
                var previous = SaveData.DecodeSave(SaveData.EncodeSave(save)); Assert.NotNull(previous); Assert.Null(previous.State);
                Assert.Contains(older, SaveData.TakeRetiredNotice(previous));
            }
            s.Ruleset = "unknown-9";
            Assert.Null(SaveData.DecodeSave(SaveData.EncodeSave(save)));
        }
        [Fact]
        public void RealGodotAlignment3SaveRetiresItsRunAndKeepsSettings()
        {
            // Emitted by Godot 4.7.2 under alignment-3: Playthrough.play("early", "resume", 60), saved with
            // JSON.stringify(SaveData.default_save()). alignment-5 cannot continue it, like alignment-2 runs before it.
            string path = Path.Combine(GoldenTests.RepoRoot, "core-tests", "Tests", "Fixtures", "godot-save-continuation.json");
            var fixture = (Dictionary<string, object>)JsonReader.Parse(File.ReadAllText(path));
            var decoded = SaveData.DecodeSave((string)fixture["save"]);
            Assert.NotNull(decoded);
            Assert.Null(decoded.State);
            Assert.Equal(33.5, decoded.Settings.Volume);
            Assert.Equal(1.3, decoded.Settings.UiScale);
            Assert.True(decoded.Settings.ReducedMotion);
            Assert.Equal(new[] { "first-site" }, decoded.Settings.Hints);
            Assert.Equal("Your unfinished run used older rules (alignment-3) and cannot continue under alignment-5. Settings and playtest records are kept; start a new run.",
                SaveData.TakeRetiredNotice(decoded));
        }
        [Fact]
        public void OlderSettingsGainIndependentInterfaceDefaults()
        {
            const string old = "{\"version\":2,\"state\":null,\"settings\":{\"muted\":true,\"volume\":40},\"tutorialComplete\":false,\"lastResult\":null}";
            var save = SaveData.DecodeSave(old); Assert.NotNull(save);
            Assert.Equal(1.0, save.Settings.UiScale); Assert.False(save.Settings.ReducedMotion); Assert.Empty(save.Settings.Hints);
            var copy = SaveData.WithDefaultSettings(save.Settings); copy.Hints.Add("first-site"); Assert.Empty(save.Settings.Hints);
            Assert.NotNull(SaveData.DecodeSave(SaveData.EncodeSave(save)));
        }
        [Fact]
        public void CaptureObjectRemainsVerbatimThroughRetirementAndResaving()
        {
            const string capture = "{ \"adapter\": \"awakening-2\",\"events\": [1.00, 2e-4, {\"text\":\"ñ \\u0061\"}],\"future\":true }";
            string raw = "{\"version\":2,\"state\":{\"ruleset\":\"alignment-2\"},\"settings\":{\"muted\":false,\"volume\":70},\"tutorialComplete\":true,\"playtest\":" + capture + "}";
            var save = SaveData.DecodeSave(raw); Assert.NotNull(save); Assert.IsType<Dictionary<string, object>>(save.Playtest);
            SaveData.TakeRetiredNotice(save); save.Settings.Volume = 25;
            string encoded = SaveData.EncodeSave(save); Assert.Contains("\"playtest\":" + capture, encoded);
            var restored = SaveData.DecodeSave(encoded); Assert.NotNull(restored);
            GoldenTraces.RequireEqual(save.Playtest, restored.Playtest);
            ((Dictionary<string, object>)save.Playtest)["future"] = false;
            Assert.False((bool)((Dictionary<string, object>)SaveData.DecodeSave(SaveData.EncodeSave(save)).Playtest)["future"]);
        }
    }
}
