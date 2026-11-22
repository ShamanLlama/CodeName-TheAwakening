using System;
using System.Collections.Generic;
using System.IO;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class PlaytestStoreTests
    {
        [Fact]
        public void RealGodotRecordKeepsPendingBatchesAndStartsANewAdapterCapture()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "godot-playtest.json");
            var stored = (Dictionary<string, object>)JsonReader.Parse(File.ReadAllText(path));
            var c = new PlaytestCapture(() => 1800000000);
            c.Configure(Configuration(), stored);
            Assert.False(c.Corrupt); Assert.False(c.Confirmed);
            Assert.Equal(JsonWriter.Write(stored), JsonWriter.Write(c.Snapshot()));
            Assert.NotSame(stored, c.Data);
            string body = (string)Item(stored, "jobs", 0)["body"];
            var state = State(); state.Seed = "godot-stored-fixture"; state.Ruleset = "alignment-3";
            Assert.Equal(stored["signature"], PlaytestCapture.Signature(state));
            state.Ruleset = "alignment-5";
            Assert.True(c.Restore()); c.Resume(state);
            Assert.NotEqual(stored["active"], c.Data["active"]);
            Assert.Equal("awakening-4", O(c.Active(), "context")["adapter"]);
            Assert.Equal("partial", O(c.Active(), "context")["coverage"]);
            Assert.Equal(body, Item(c.Data, "jobs", 0)["body"]);
            Assert.Equal(0, N(O(c.Active(), "clock"), "elapsed_ms"));
        }
        [Fact]
        public void PreviousAdapterQueueRetainsItsDoctrineAndImmutableBody()
        {
            var r = new Rig(); r.Start();
            r.State.Status = "playing";
            r.Capture.EmitEvent("doctrine_selected", M("doctrine", "terror", "spent", M("influence", 50, "dread", 0, "followers", 0)), r.State);
            O(r.Capture.Active(), "context")["adapter"] = "awakening-3";
            var job = r.Capture.SealBatch();
            var body = Body(job);
            O(Item(body, "events", 0), "data")["ruleset"] = "alignment-4";
            job["body"] = JsonWriter.Write(body);
            string original = (string)job["body"];
            var restored = new PlaytestCapture(() => r.Unix);
            restored.Configure(Configuration(), Saved(r.Capture, r.State));
            Assert.False(restored.Corrupt);
            restored.Accept(); restored.Resume(r.State);
            Assert.Equal(original, Item(restored.Data, "jobs", 0)["body"]);
            Assert.Equal("awakening-4", O(restored.Active(), "context")["adapter"]);
            Assert.Equal("capture_started", O(Item(restored.Data, "staging", 0), "event")["type"]);
            Assert.True(PlaytestCapture.ValidStore(restored.Snapshot(r.State)));
        }

        [Fact]
        public void StoreValidationRejectsMalformedRecordsWithoutThrowingOrReplacingRawData()
        {
            var r = new Rig(); r.Start(); r.Capture.SealBatch();
            // Each case corrupts a different persisted boundary. Payload validation belongs to GTC.
            var mutations = new Action<Dictionary<string, object>>[]
            {
                s => s["version"] = 2,
                s => s["choices"] = new List<object>(),
                s => O(s, "choices")[(string)Configuration()["round_id"]] = 1,
                s => O(s, "testers")[(string)Configuration()["round_id"]] = "not-an-id",
                s => s["receipt"] = null,
                s => s["jobs"] = "broken",
                s => s["staging"] = false,
                s => s["active"] = "missing",
                s => s["signature"] = 0,
                s => s["losses"] = -1,
                s => Run(s)["sequence"] = 20001,
                s => Run(s)["sessions"] = 0,
                s => Run(s)["sessions"] = 65,
                s => Run(s)["comments"] = 11,
                s => Run(s)["closed"] = 1,
                s => Run(s)["stopped"] = null,
                s => Run(s)["seen"] = "broken",
                s => Run(s)["last_event"] = false,
                s => Run(s)["last_session_id"] = 0,
                s => Run(s)["session_id"] = "invalid",
                s => Run(s)["endpoint"] = "http://example.invalid/api/playtests/v1/batches/",
                s => O(Run(s), "context")["run_id"] = PlaytestCapture.Identifier(),
                s => O(Run(s), "context")["schema_version"] = "2.0",
                s => O(Run(s), "context")["adapter"] = "awakening-99",
                s => O(Run(s), "context")["participation_version"] = "other",
                s => O(Run(s), "context")["coverage"] = "unknown",
                s => O(Run(s), "context")["tester_id"] = false,
                s => O(Run(s), "clock")["elapsed_ms"] = 1,
                s => O(Run(s), "clock")["background_ms"] = -1,
                s => O(Run(s), "clock")["planning_ms"] = 0.5,
                s => O(Run(s), "clock")["elapsed_ms"] = 604800001,
                s => Run(s)["closed"] = true, // session_started is not terminal
                s => Item(s, "jobs", 0)["id"] = PlaytestCapture.Identifier(),
                s => Item(s, "jobs", 0)["round_id"] = "invalid",
                s => Item(s, "jobs", 0)["run_id"] = "missing",
                s => Item(s, "jobs", 0)["body"] = "{",
                s => Item(s, "jobs", 0)["queued_at"] = double.PositiveInfinity,
                s => Item(s, "jobs", 0)["attempts"] = 1.5,
                s => Item(s, "jobs", 0)["next_retry"] = -1,
                s => Item(s, "jobs", 0)["blocked"] = "yes",
                s => ModifyBody(s, b => b["events"] = new List<object>()),
                s => ModifyBody(s, b => b["run_id"] = PlaytestCapture.Identifier()),
                s => ModifyBody(s, b => b["session_id"] = null),
                s => ModifyBody(s, b => Item(b, "events", 0)["sequence"] = 0),
                s => ModifyBody(s, b => Item(b, "events", 0)["game_month"] = 225),
                s => ModifyBody(s, b => Item(b, "events", 0)["phase"] = "won"),
                s => ModifyBody(s, b => Item(b, "events", 0)["type"] = "made_up"),
                s => ModifyBody(s, b => Item(b, "events", 0)["data"] = new List<object>()),
                s => ModifyBody(s, b => O(Item(b, "events", 0), "clock")["elapsed_ms"] = 1),
                s => A(s, "staging").Add(M("run_id", s["active"], "session_id", PlaytestCapture.Identifier(), "event", O(Run(s), "last_event"), "created_at", -1)),
                s => A(s, "staging").Add(M("run_id", "missing")),
                s => A(s, "staging").Add(true),
            };
            foreach (var mutate in mutations)
            {
                var saved = Saved(r.Capture, r.State); mutate(saved);
                var c = new PlaytestCapture(); c.Configure(Configuration(), saved);
                Assert.True(c.Corrupt); Assert.Same(saved, c.Snapshot()); Assert.False(c.Accept());
            }
        }
        [Fact]
        public void StoredLegacyAdapterAndUnknownFieldsArePreserved()
        {
            var r = new Rig(); r.Start(); r.Capture.SealBatch(); var saved = Saved(r.Capture, r.State);
            O(Run(saved), "context")["adapter"] = "awakening-1";
            ModifyBody(saved, b => b["adapter"] = "awakening-1");
            saved["future_metadata"] = M("keep", true);
            var c = new PlaytestCapture(); c.Configure(Configuration(), saved);
            Assert.False(c.Corrupt); Assert.Equal(JsonWriter.Write(saved), JsonWriter.Write(c.Snapshot()));
        }
        [Fact]
        public void RoundAndRunLimitsRemainBounded()
        {
            var r = new Rig(); r.Start();
            for (int i = 0; i < 64; i++)
            {
                string id = PlaytestCapture.Identifier(); O(r.Capture.Data, "testers")[id] = PlaytestCapture.Identifier();
            }
            Assert.True(O(r.Capture.Data, "testers").Count > 64);
            r.Capture.StartRun(r.State); Assert.False(r.Capture.Capturing());
            var c = new PlaytestCapture(); var stored = PlaytestCapture.Fresh();
            for (int i = 0; i < 64; i++) O(stored, "testers")[PlaytestCapture.Identifier()] = PlaytestCapture.Identifier();
            c.Configure(Configuration(), stored); Assert.False(c.Accept()); Assert.Contains("round limit", c.Warning);
        }
        [Fact]
        public void ReceiptFieldsMustAllValidateBeforeRemoval()
        {
            var r = new Rig(); r.Start(); var job = r.Capture.SealBatch();
            foreach (var pair in new[] { M("batch_id", PlaytestCapture.Identifier()), M("receipt_id", "bad"), M("accepted_comment", 0), M("accepted_events", 3), M("accepted_events", -1), M("accepted_events", 0.5), M("run_expires_at", "2027-04-30T12:00:00+00:00"), M("run_expires_at", "badZ"), M("run_expires_at", "1960-01-01T00:00:00Z") })
            {
                var receipt = Receipt(job); foreach (var field in pair) receipt[field.Key] = field.Value;
                Assert.Empty(r.Capture.ConfirmJob(job, receipt)); Assert.Equal(2, r.Capture.PendingEvents());
            }
        }
        [Theory]
        [InlineData("2027-04-30T12:00:00Z")]
        [InlineData("2027-04-30 12:00:00Z")]
        [InlineData("2027-04-30T12:00:00.1Z")]
        [InlineData("2027-04-30Z")]
        public void ReceiptExpiryKeepsGodotAcceptedDateForms(string expiry)
        {
            var r = new Rig(); r.Start(); var job = r.Capture.SealBatch();
            var receipt = Receipt(job); receipt["run_expires_at"] = expiry;
            Assert.NotEmpty(r.Capture.ConfirmJob(job, receipt));
        }
        static Dictionary<string, object> Run(Dictionary<string, object> s) => O(O(s, "runs"), (string)s["active"]);
        static void ModifyBody(Dictionary<string, object> s, Action<Dictionary<string, object>> modify)
        {
            var job = Item(s, "jobs", 0); var body = Body(job); modify(body); job["body"] = JsonWriter.Write(body);
        }
    }
}
