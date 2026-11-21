using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    // Port of the capture-only cases in godot/tests/test_playtest.gd. No rules or filesystem lease.
    public class PlaytestCaptureTests
    {
        [Fact]
        public void NewEventKindsSurviveSaveAndKeepTheQueueBounded()
        {
            var r = new Rig(); r.Start();
            foreach (string kind in new[] { "threat_spawned", "threat_resolved", "month_snapshot", "speed_changed" })
                r.Capture.EmitEvent(kind, M(), r.State);
            var saved = Saved(r.Capture, r.State);
            var restored = new PlaytestCapture(); restored.Configure(Configuration(), saved);
            Assert.False(restored.Corrupt);
            Assert.Equal("awakening-3", O(restored.Active(), "context")["adapter"]);
            for (int n = 0; n < PlaytestCapture.MaxEvents; n++) r.Capture.EmitEvent("speed_changed", M("previous", 0, "new", 1), r.State);
            Assert.False(r.Capture.Capturing());
            Assert.True(r.Capture.PendingEvents() <= PlaytestCapture.MaxEvents);
            Assert.Equal("capture_gap", O(r.Capture.Active(), "last_event")["type"]);
        }

        [Fact]
        public void ConsentPrecedesIdsAndStartEvents()
        {
            var r = new Rig(false);
            r.Start();
            Assert.Empty(O(r.Capture.Data, "testers"));
            Assert.Equal(0, r.Capture.PendingEvents());
            Assert.False(r.Capture.Answered());
            Assert.True(r.Capture.Accept());
            r.Start();
            Assert.Equal(2, r.Capture.PendingEvents());
            Assert.Equal("run_started", O(Item(r.Capture.Data, "staging", 0), "event")["type"]);
            Assert.Empty(r.Transport.Sent);
        }
        [Fact]
        public void SavedRetryIsImmutableAndResumeStartsAFreshClock()
        {
            var r = new Rig(); r.Start();
            r.Capture.Tick(1200, "planning");
            r.Capture.EmitEvent("time_checkpoint", M("reason", "before_save"), r.State);
            var job = r.Capture.SealBatch();
            string body = (string)job["body"];
            r.Capture.Attempted(job); r.Capture.Postpone(job, 600);
            var saved = Saved(r.Capture, r.State);
            var restored = new PlaytestCapture(() => r.Unix);
            restored.Configure(Configuration(), saved);
            Assert.False(restored.Corrupt);
            Assert.Empty(restored.NextJob(9999999999));
            restored.Accept();
            Assert.Equal(body, restored.NextJob(9999999999)["body"]);
            string run = (string)restored.Data["active"], session = (string)restored.Active()["session_id"];
            restored.Resume(r.State);
            Assert.Equal(run, restored.Data["active"]);
            Assert.NotEqual(session, restored.Active()["session_id"]);
            EqualJson(PlaytestCapture.EmptyClock(), restored.Active()["clock"]);
            var staging = A(restored.Data, "staging");
            Assert.Equal("resume", O(O((Dictionary<string, object>)staging[0], "event"), "data")["mode"]);
            Assert.Equal("capture_gap", O((Dictionary<string, object>)staging[1], "event")["type"]);
        }
        [Fact]
        public void ClocksPartitionPlanningSimulationBackgroundAndExcluded()
        {
            var r = new Rig(); r.Start();
            r.Capture.Tick(1000, "simulation"); r.Capture.Tick(2000, "planning");
            r.Capture.Tick(3000, "background"); r.Capture.Tick(4000, "excluded");
            EqualJson(M("elapsed_ms", 10000, "simulation_ms", 1000, "planning_ms", 2000, "background_ms", 3000, "excluded_ms", 4000), r.Capture.Active()["clock"]);
            r.Capture.Withdraw(); r.Capture.Tick(1000, "simulation"); r.Start();
            Assert.Equal(0, r.Capture.PendingEvents());
            Assert.Empty(O(r.Capture.Data, "testers"));
        }
        [Fact]
        public void LegacyOrUnobservedProgressHasPartialCoverage()
        {
            var r = new Rig(); r.State.Status = "playing"; r.State.NextLogId++;
            r.Capture.Resume(r.State);
            Assert.Equal("partial", O(r.Capture.Active(), "context")["coverage"]);
            var first = O(Item(r.Capture.Data, "staging", 0), "event");
            Assert.Equal("capture_started", first["type"]); Assert.Equal("playing", first["phase"]);
            Assert.Equal(2, A(r.Capture.Data, "staging").Count);
            var saved = Saved(r.Capture, r.State); r.State.NextLogId++;
            var restored = new PlaytestCapture(); restored.Configure(Configuration(), saved); restored.Accept(); restored.Resume(r.State);
            Assert.NotEqual(saved["active"], restored.Data["active"]);
            Assert.Equal("partial", O(restored.Active(), "context")["coverage"]);
        }
        [Fact]
        public void ReceiptsSplittingAndPermanentRejection()
        {
            var r = new Rig(); r.Start(); var c = r.Capture;
            var job = c.SealBatch();
            Assert.Empty(c.ConfirmJob(job, M("batch_id", job["id"])));
            Assert.True(c.SplitJob(job));
            var first = Item(c.Data, "jobs", 0); var second = Item(c.Data, "jobs", 1);
            Assert.NotEqual(job["id"], first["id"]); Assert.NotEqual(job["id"], second["id"]); Assert.NotEqual(first["id"], second["id"]);
            EqualJson(A(Body(job), "events")[0], A(Body(first), "events")[0]);
            Assert.Equal(job["queued_at"], first["queued_at"]);
            var undo = c.ConfirmJob(first, Receipt(first, 1));
            Assert.NotEmpty(undo); Assert.Equal(1, c.PendingEvents());
            c.UndoConfirmation(undo); Assert.Equal(2, c.PendingEvents());
            c.Reject(first, "HTTP 409");
            Assert.Empty(c.NextJob(9999999999)); Assert.False(c.Capturing());
        }
        [Fact]
        public void QueueExpiryAndOverflowAreVisibleAndBounded()
        {
            var r = new Rig(); r.Start();
            for (int i = 0; i < PlaytestCapture.MaxEvents; i++) r.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), r.State);
            Assert.False(r.Capture.Capturing());
            var last = O((Dictionary<string, object>)A(r.Capture.Data, "staging").Last(), "event");
            Assert.Equal("capture_gap", last["type"]); Assert.Equal("queue_full", O(last, "data")["reason"]);
            Assert.InRange(r.Capture.PendingEvents(), 1, PlaytestCapture.MaxEvents);
            r.Capture.SealBatch();
            Assert.False(r.Capture.Expire(r.Unix + PlaytestCapture.Ttl));
            Assert.True(r.Capture.Expire(r.Unix + PlaytestCapture.Ttl + 1));
            Assert.Equal(0, r.Capture.PendingEvents()); Assert.NotEmpty(r.Capture.Warning);
            Assert.Equal(1, N(r.Capture.Data, "losses"));
        }
        [Fact]
        public void CorruptOptionalMetadataIsRetainedForExportAndCannotCapture()
        {
            var c = new PlaytestCapture();
            foreach (object malformed in new object[] { M("version", 1, "jobs", "broken"), null, true, new List<object>(), "broken" })
            {
                c.Configure(Configuration(), malformed);
                Assert.True(c.Corrupt); Assert.Same(malformed, c.Snapshot(State())); Assert.False(c.Accept());
            }
        }
        [Fact]
        public void TerminalCommentReusesEventAndSequence()
        {
            var r = new Rig(); r.Start(); var c = r.Capture;
            c.EmitEvent("run_abandoned", M("reason", "explicit_abandon"), r.State, "ended");
            var terminal = O(c.Active(), "last_event");
            Assert.True(c.QueueComment("  A synthetic observation  "));
            var job = Item(c.Data, "jobs", 0); var comment = Body(job);
            EqualJson(new List<object> { terminal }, comment["events"]);
            Assert.Equal(terminal["sequence"], c.Active()["sequence"]);
            Assert.Equal(c.Active()["last_session_id"], comment["session_id"]);
            Assert.Equal("A synthetic observation", O(comment, "comment")["text"]);
            Assert.NotEmpty(c.ConfirmJob(job, Receipt(job, 0)));
        }
        [Fact]
        public void NewRoundSeparatesTestersAndNewBuildStartsPartial()
        {
            var r = new Rig(); r.Start(); var first = O(r.Capture.Active(), "context"); var saved = Saved(r.Capture, r.State);
            var options = Configuration(); options["build_id"] = PlaytestCapture.Identifier(); options["round_id"] = PlaytestCapture.Identifier();
            var next = new PlaytestCapture(); next.Configure(options, saved); next.Accept(); next.Resume(r.State);
            Assert.NotEqual(first["tester_id"], O(next.Active(), "context")["tester_id"]);
            Assert.NotEqual(first["run_id"], O(next.Active(), "context")["run_id"]);
            Assert.Equal("partial", O(next.Active(), "context")["coverage"]);
            EqualJson(A(saved, "staging")[0], A(next.Data, "staging")[0]);
        }
        [Fact]
        public void CommentsAreBoundedAndWithdrawalCanBeRenewed()
        {
            var r = new Rig(); r.Start(); var c = r.Capture;
            Assert.False(c.QueueComment("before end"));
            c.EmitEvent("run_abandoned", M("reason", "explicit_abandon"), r.State, "ended");
            Assert.False(c.QueueComment("  ")); Assert.False(c.QueueComment(new string('x', 1001)));
            for (int i = 0; i < 10; i++) Assert.True(c.QueueComment("Synthetic comment " + i));
            Assert.False(c.QueueComment("Eleventh comment"));
            c.Withdraw(); Assert.True(c.Accept()); Assert.Empty(c.Warning);
        }
        [Fact]
        public void CommentTrimmingAndLengthsUseGodotUnicodeSemantics()
        {
            var r = new Rig(); r.Start();
            r.Capture.EmitEvent("run_abandoned", M("reason", "explicit_abandon"), r.State, "ended");
            Assert.True(r.Capture.QueueComment("\u0001 \t\n\u00a0observation\u00a0\r"));
            Assert.Equal("\u00a0observation\u00a0", O(Body(Item(r.Capture.Data, "jobs", 0)), "comment")["text"]);
            Assert.True(r.Capture.QueueComment(string.Concat(Enumerable.Repeat("🐙", 1000))));
            Assert.False(r.Capture.QueueComment(string.Concat(Enumerable.Repeat("🐙", 1001))));
        }
        [Theory]
        [InlineData(0.0, "0.0")]
        [InlineData(1.0, "1.0")]
        [InlineData(12.345678901234567, "12.3456789012346")]
        [InlineData(0.0000001, "0.0000001")]
        [InlineData(1e-20, "0.00000000000000000001")]
        [InlineData(1e-32, "0.00000000000000000000000000000001")]
        [InlineData(1e-33, "0.0")]
        [InlineData(-1e-33, "-0.0")]
        [InlineData(123456789.123456789, "123456789.123457")]
        [InlineData(1000000000.000001, "1000000000.0")]
        [InlineData(0.30000000000000004, "0.3")]
        public void SealedNumbersMatchGodotStringifyDefaultPrecision(double number, string godot)
        {
            // Expected tokens captured from Godot 4.7.2 JSON.stringify, not .NET formatting.
            var r = new Rig(); r.Start();
            r.Capture.EmitEvent("action_performed", M("spent", M("influence", number)), r.State);
            string body = (string)r.Capture.SealBatch()["body"];
            Assert.Contains("\"influence\":" + godot + "}", body);
        }
        [Fact]
        public void RetryAfterHonorsServerDelays()
        {
            Assert.Equal(300, PlaytestOutbox.RetryAfterSeconds("300")); Assert.Equal(0, PlaytestOutbox.RetryAfterSeconds("invalid"));
            var r = new Rig(); r.Start(); var job = r.Capture.SealBatch();
            r.Capture.Attempted(job); r.Capture.Postpone(job, 600);
            Assert.Equal(r.Unix + 600, N(job, "next_retry")); Assert.Empty(r.Capture.NextJob(r.Unix + 599));
        }
        [Fact]
        public void RememberedChoiceIsExplicitPerRoundAndDevice()
        {
            var r = new Rig(false); var c = r.Capture;
            Assert.False(c.Answered()); Assert.False(c.Restore()); c.Accept();
            object tester = O(c.Data, "testers")[(string)Configuration()["round_id"]];
            var reopened = new PlaytestCapture(); reopened.Configure(Configuration(), Saved(c, r.State));
            Assert.False(reopened.Confirmed); Assert.True(reopened.Answered()); Assert.True(reopened.Restore());
            Assert.Equal(tester, O(reopened.Data, "testers")[(string)Configuration()["round_id"]]);
            var no = new PlaytestCapture(); no.Configure(Configuration()); no.Decline();
            Assert.True(no.Answered()); Assert.False(no.Confirmed); Assert.Empty(O(no.Data, "testers"));
            var noAgain = new PlaytestCapture(); noAgain.Configure(Configuration(), Saved(no, r.State));
            Assert.False(noAgain.Restore()); Assert.True(noAgain.Accept());
            reopened.Withdraw(); Assert.True(reopened.Answered()); Assert.False(reopened.Restore());
            var other = Configuration(); other["round_id"] = "10000000-0000-4000-8000-000000000009";
            var nextRound = new PlaytestCapture(); nextRound.Configure(other, Saved(c, r.State));
            Assert.False(nextRound.Answered()); Assert.False(nextRound.Restore());
        }
        [Fact]
        public void IdentifiersUseRandomBytesInNetworkOrderAndVersionFourBits()
        {
            var bytes = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
            string id = PlaytestCapture.Identifier(() => bytes);
            Assert.Equal("00010203-0405-4607-8809-0a0b0c0d0e0f", id);
            Assert.Equal(6, bytes[6]); Assert.True(PlaytestCapture.ValidId(id));
            Assert.False(PlaytestCapture.ValidId(id.ToUpperInvariant())); Assert.True(PlaytestCapture.ValidId(id + "\n"));
            Assert.Throws<ArgumentException>(() => PlaytestCapture.Identifier(() => new byte[4]));
        }
        [Theory]
        [InlineData("https://example.invalid/api/playtests/v1/batches/", false, true)]
        [InlineData("http://127.0.0.1:58483/api/playtests/v1/batches/", false, false)]
        [InlineData("http://127.0.0.1:58483/api/playtests/v1/batches/", true, true)]
        [InlineData("https://user@example.invalid/api/playtests/v1/batches/", true, false)]
        [InlineData("https://example.invalid/?/api/playtests/v1/batches/", false, false)]
        [InlineData("https://example.invalid/#/api/playtests/v1/batches/", false, false)]
        [InlineData("https://example.invalid/\n/api/playtests/v1/batches/", false, false)]
        [InlineData("https://example.invalid/api/playtests/v1/batches", false, false)]
        public void EndpointValidationMatchesGodot(string endpoint, bool debug, bool valid) => Assert.Equal(valid, PlaytestCapture.ValidEndpoint(endpoint, debug));

        [Fact]
        public void CheckpointsAreObservedOnceAndSnapshotsDoNotAliasLiveState()
        {
            var r = new Rig(); r.Start();
            r.Capture.Checkpoint("tutorial_completed", null, r.State); r.Capture.Checkpoint("tutorial_completed", null, r.State);
            var saved = Saved(r.Capture, r.State); r.Capture.Tick(10, "planning");
            Assert.Equal(3, A(saved, "staging").Count);
            Assert.Equal(0, N(O(O(O(saved, "runs"), (string)saved["active"]), "clock"), "elapsed_ms"));
            Assert.Equal(0, N(O(O(Item(r.Capture.Data, "staging", 2), "event"), "clock"), "elapsed_ms"));
        }
        [Fact]
        public void SessionAndSequenceLimitsStopWithoutInventingTime()
        {
            var r = new Rig(); r.Start(); r.Capture.Tick(PlaytestCapture.MaxClockMs, "background");
            r.Capture.Tick(1, "planning"); Assert.False(r.Capture.Capturing());
            Assert.Equal(PlaytestCapture.MaxClockMs, N(O(r.Capture.Active(), "clock"), "elapsed_ms"));
            var s = new Rig(); s.Start(); s.Capture.Active()["sequence"] = 19998;
            s.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), s.State);
            Assert.Equal(19999, N(s.Capture.Active(), "sequence")); Assert.False(s.Capture.Capturing());
            Assert.Equal("capture_gap", O(s.Capture.Active(), "last_event")["type"]);
            var t = new Rig(); t.Start(); t.Capture.Active()["sessions"] = 64;
            var saved = Saved(t.Capture, t.State); var restored = new PlaytestCapture(); restored.Configure(Configuration(), saved); restored.Accept(); restored.Resume(t.State);
            Assert.NotEqual(saved["active"], restored.Data["active"]); Assert.Equal("partial", O(restored.Active(), "context")["coverage"]);
        }
        [Fact]
        public void SplitKeepsCommentOnlyOnSecondHalfAndCannotSplitOneEvent()
        {
            var r = new Rig(); r.Start(); var job = r.Capture.SealBatch(); var body = Body(job);
            body["comment"] = M("comment_id", PlaytestCapture.Identifier(), "text", "synthetic"); job["body"] = JsonWriter.Write(body);
            Assert.True(r.Capture.SplitJob(job));
            var first = Item(r.Capture.Data, "jobs", 0); var second = Item(r.Capture.Data, "jobs", 1);
            Assert.False(Body(first).ContainsKey("comment")); EqualJson(body["comment"], Body(second)["comment"]);
            Assert.False(r.Capture.SplitJob(first));
        }
        [Fact]
        public void SealingRespectsCountBytesAndSessionBoundaries()
        {
            var r = new Rig(); r.Start();
            for (int i = 0; i < 40; i++) r.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), r.State);
            Assert.Equal(32, A(Body(r.Capture.SealBatch()), "events").Count);
            Assert.Equal(10, A(Body(r.Capture.SealBatch()), "events").Count);
            for (int i = 0; i < 4; i++) r.Capture.EmitEvent("time_checkpoint", M("synthetic", new string('x', 22000)), r.State);
            var job = r.Capture.SealBatch();
            Assert.Equal(2, A(Body(job), "events").Count); Assert.InRange(System.Text.Encoding.UTF8.GetByteCount((string)job["body"]), 1, 65536);
            var saved = Saved(r.Capture, r.State); var reopened = new PlaytestCapture(); reopened.Configure(Configuration(), saved); reopened.Accept(); reopened.Resume(r.State);
            Assert.Equal(2, A(Body(reopened.SealBatch()), "events").Count);
            Assert.Equal("resume", O(Item(Body(reopened.SealBatch()), "events", 0), "data")["mode"]);
        }
        static void EqualJson(object expected, object actual) => Assert.Equivalent(JsonReader.Parse(JsonWriter.Write(expected)), JsonReader.Parse(JsonWriter.Write(actual)), strict: true);
    }
}
