using System;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Xunit;
using static Awakening.Core.Tests.PlaytestTestSupport;

namespace Awakening.Core.Tests
{
    public class PlaytestOutboxTests
    {
        [Fact]
        public void BodyAndAttemptMustBothBeDurableBeforeSendingAndOnlyOneRequestFlies()
        {
            var r = new Rig(); r.Start(); r.Outbox.Process();
            Assert.Equal(2, r.Storage.Generation); Assert.Empty(r.Transport.Sent);
            Assert.Equal(0, N(Item(r.Storage.Writes[0], "jobs", 0), "attempts"));
            var attempted = Item(r.Storage.Writes[1], "jobs", 0);
            Assert.Equal(1, N(attempted, "attempts")); Assert.Equal(r.Unix + 10, N(attempted, "next_retry"));
            r.Storage.SavedGeneration = 1; r.Outbox.Process(); Assert.Empty(r.Transport.Sent);
            r.Storage.SavedGeneration = 2; r.Outbox.Process();
            Assert.Single(r.Transport.Sent); Assert.Equal(attempted["body"], r.Transport.Sent[0].Body);
            r.Mono += 60000; r.Unix += 60;
            r.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), r.State);
            r.Outbox.Process(); r.Outbox.Process(); Assert.Single(r.Transport.Sent);
        }
        [Fact]
        public void NotReadyOrReadOnlyStorageCannotSendOrSeal()
        {
            var r = new Rig(); r.Start(); r.Storage.Ready = false; r.Outbox.Process();
            r.Storage.Ready = true; r.Storage.Writable = false; r.Outbox.Process();
            Assert.Empty(r.Storage.Writes); Assert.Empty(r.Transport.Sent); Assert.Empty(A(r.Capture.Data, "jobs"));
        }
        [Fact]
        public void FailedPreparationNeverSends()
        {
            var r = new Rig(); r.Start(); r.Storage.Refuse = true; r.Outbox.Process(); r.Outbox.Process();
            Assert.Empty(r.Transport.Sent); Assert.False(r.Capture.Capturing());
            Assert.Contains("No playtest request", r.Capture.Warning);
        }
        [Fact]
        public void ReceiptIsConfirmedOnlyWhenItsRemovalIsDurable()
        {
            var r = new Rig(); r.Start(); r.Send(); var job = r.Capture.NextJob(r.Unix + 10);
            r.Respond(201, JsonWriter.Write(Receipt(job)));
            Assert.Equal(0, r.Capture.PendingEvents()); Assert.Equal("Saving the server receipt…", r.Outbox.Notice);
            r.Outbox.Process(); Assert.Equal("Saving the server receipt…", r.Outbox.Notice);
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process();
            Assert.Equal("Receipt confirmed and saved locally.", r.Outbox.Notice);
            Assert.NotEmpty(O(r.Storage.Writes[r.Storage.Writes.Count - 1], "receipt"));
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReceiptSaveFailureUndoesRemoval(bool asynchronous)
        {
            var r = new Rig(); r.Start(); r.Send(); var job = r.Capture.NextJob(r.Unix + 10);
            string body = (string)job["body"];
            if (!asynchronous) r.Storage.Refuse = true;
            r.Respond(200, JsonWriter.Write(Receipt(job)));
            if (asynchronous) { r.Storage.Failed = true; r.Outbox.Process(); }
            Assert.Equal(2, r.Capture.PendingEvents()); Assert.False(r.Capture.Capturing());
            Assert.Empty(O(r.Capture.Data, "receipt")); Assert.Equal(body, Item(r.Capture.Data, "jobs", 0)["body"]);
            Assert.DoesNotContain("confirmed and saved", r.Outbox.Notice);
        }
        [Theory]
        [InlineData("broken")]
        [InlineData("[]")]
        [InlineData("{}")]
        public void MalformedReceiptPostponesImmutableJob(string response)
        {
            var r = new Rig(); r.Start(); r.Send(); string body = r.Transport.Sent[0].Body;
            r.Respond(201, response);
            var job = Item(r.Capture.Data, "jobs", 0);
            Assert.Equal(body, job["body"]); Assert.Equal(2, r.Capture.PendingEvents());
            Assert.Equal(r.Unix + 1, N(job, "next_retry"));
            Assert.Contains("valid receipt", r.Outbox.Notice);
        }
        [Fact]
        public void SplitAfter413PreservesEventIdsAndDurablyPreparesNewBodies()
        {
            var r = new Rig(); r.Start(); r.Send();
            var original = Body(Item(r.Capture.Data, "jobs", 0)); r.Respond(413);
            Assert.Equal(2, A(r.Capture.Data, "jobs").Count);
            Assert.Equal(Item(original, "events", 0)["event_id"], Item(Body(Item(r.Capture.Data, "jobs", 0)), "events", 0)["event_id"]);
            Assert.Equal(Item(original, "events", 1)["event_id"], Item(Body(Item(r.Capture.Data, "jobs", 1)), "events", 0)["event_id"]);
            r.Mono = 5000; r.Outbox.Process(); Assert.Single(r.Transport.Sent);
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process(); Assert.Equal(2, r.Transport.Sent.Count);
            Assert.NotEqual(r.Transport.Sent[0].Id, r.Transport.Sent[1].Id);
        }
        [Theory]
        [InlineData(0)]
        [InlineData(429)]
        [InlineData(500)]
        [InlineData(503)]
        public void TransientFailuresRespectRetryAfterAndKeepBody(int status)
        {
            var r = new Rig(); r.Start(); r.Send(); string body = r.Transport.Sent[0].Body;
            r.Respond(status, "", "300");
            var job = Item(r.Capture.Data, "jobs", 0); Assert.Equal(body, job["body"]); Assert.Equal(r.Unix + 300, N(job, "next_retry"));
            r.Mono = 299000; r.Unix += 299; r.Outbox.Process(); Assert.Single(r.Transport.Sent);
            r.Mono = 300000; r.Unix += 1; r.Outbox.Process(); Assert.Single(r.Transport.Sent);
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process();
            Assert.Equal(2, r.Transport.Sent.Count); Assert.Equal(body, r.Transport.Sent[1].Body);
        }
        [Fact]
        public void RetryAfterParsesSecondsAndHttpDatesUsingWallTime()
        {
            double now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
            Assert.Equal(600, PlaytestOutbox.RetryAfterSeconds("Wed, 18 Nov 2026 12:10:00 GMT", now));
            Assert.Equal(0, PlaytestOutbox.RetryAfterSeconds("Wed, 18 Nov 2026 11:59:59 GMT", now));
            Assert.Equal(2.5, PlaytestOutbox.RetryAfterSeconds("2.5", now)); Assert.Equal(0, PlaytestOutbox.RetryAfterSeconds("-1", now));
            Assert.Equal(0, PlaytestOutbox.RetryAfterSeconds("Wed, 18 Nope 2026 12:00:00 GMT", now));
        }
        [Theory]
        [InlineData(400)]
        [InlineData(403)]
        [InlineData(404)]
        [InlineData(409)]
        [InlineData(410)]
        [InlineData(415)]
        [InlineData(422)]
        public void PermanentRejectionStopsTheRunAndRetainsRecords(int status)
        {
            var r = new Rig(); r.Start(); r.Send(); r.Respond(status);
            Assert.False(r.Capture.Capturing()); Assert.Empty(r.Capture.NextJob(double.MaxValue)); Assert.Equal(2, r.Capture.PendingEvents());
            Assert.True((bool)Item(r.Capture.Data, "jobs", 0)["blocked"]);
            r.Unix += 1000; r.Mono += 1000000; r.Outbox.Process(); Assert.Single(r.Transport.Sent);
        }
        [Fact]
        public void SingleEvent413IsPermanentlyRejected()
        {
            var r = new Rig(); r.Start(); r.Capture.SplitJob(r.Capture.SealBatch()); r.Send(); r.Respond(413);
            Assert.False(r.Capture.Capturing()); Assert.Empty(r.Capture.NextJob(double.MaxValue));
        }
        [Fact]
        public void FlushesAtTwentySecondsOrThirtyTwoEventsAndSpacesSendsByFiveSeconds()
        {
            var r = new Rig(); r.Start(); r.Send();
            var job = Item(r.Capture.Data, "jobs", 0); r.Respond(201, JsonWriter.Write(Receipt(job)));
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process();
            r.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), r.State);
            r.Mono = 19999; r.Outbox.Process(); Assert.Empty(A(r.Capture.Data, "jobs"));
            r.Mono = 20000; r.Send(); Assert.Equal(2, r.Transport.Sent.Count);
            job = Item(r.Capture.Data, "jobs", 0); r.Respond(200, JsonWriter.Write(Receipt(job)));
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process();
            for (int i = 0; i < 32; i++) r.Capture.EmitEvent("time_checkpoint", M("reason", "periodic"), r.State);
            r.Outbox.Process(); Assert.Single(A(r.Capture.Data, "jobs")); Assert.Equal(2, r.Transport.Sent.Count);
            r.Mono = 24999; r.Outbox.Process(); Assert.Equal(2, r.Transport.Sent.Count);
            r.Mono = 25000; r.Outbox.Process(); Assert.Equal(2, r.Transport.Sent.Count);
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process(); Assert.Equal(3, r.Transport.Sent.Count);
        }
        [Fact]
        public void UrgentTerminalEventFlushesBeforeTwentySeconds()
        {
            var r = new Rig(); r.Start(); r.Send(); r.Respond(201, JsonWriter.Write(Receipt(Item(r.Capture.Data, "jobs", 0))));
            r.Storage.SavedGeneration = r.Storage.Generation; r.Outbox.Process();
            r.Capture.EmitEvent("run_abandoned", M("reason", "explicit_abandon"), r.State, "ended"); r.Outbox.Process();
            Assert.Single(A(r.Capture.Data, "jobs")); Assert.Empty(A(r.Capture.Data, "staging"));
        }
        [Fact]
        public void OpeningWithoutRenewedConsentStillPurgesExpiredQueue()
        {
            var r = new Rig(); r.Start(); var job = r.Capture.SealBatch(); job["queued_at"] = r.Unix - PlaytestCapture.Ttl - 1;
            r.Capture.Configure(Configuration(), Saved(r.Capture, r.State)); r.Outbox.Process();
            Assert.False(r.Capture.Confirmed); Assert.Equal(0, r.Capture.PendingEvents()); Assert.Single(r.Storage.Writes);
            Assert.Empty(r.Transport.Sent); Assert.NotEmpty(r.Capture.Warning);
        }
        [Fact]
        public void StoppingParticipationCancelsAndPersistsClearedRecordsAndIgnoresLateResponse()
        {
            var r = new Rig(); r.Start(); r.Send(); var response = new PlaytestResponse { Id = r.Transport.Sent[0].Id, Status = 201, Body = JsonWriter.Write(Receipt(Item(r.Capture.Data, "jobs", 0))) };
            r.Outbox.Withdraw(); r.Transport.Responses.Add(response); r.Outbox.Process();
            Assert.Equal(1, r.Transport.Cancellations); Assert.False(r.Capture.Confirmed); Assert.Equal(0, r.Capture.PendingEvents());
            Assert.Empty(O(r.Capture.Data, "testers")); Assert.Empty(O(r.Capture.Data, "receipt"));
            Assert.Empty(A(r.Storage.Writes[r.Storage.Writes.Count - 1], "jobs")); Assert.True(r.Capture.Answered()); Assert.False(r.Capture.Restore());
        }
        [Fact]
        public void StorageFailureCancelsInFlightAndStopsCapture()
        {
            var r = new Rig(); r.Start(); r.Send(); r.Storage.Failed = true; r.Outbox.Process();
            Assert.False(r.Capture.Capturing()); Assert.Equal(1, r.Transport.Cancellations); Assert.Equal(2, r.Capture.PendingEvents());
        }
    }
}
