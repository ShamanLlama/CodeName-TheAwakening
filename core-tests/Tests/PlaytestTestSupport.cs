using System;
using System.Collections.Generic;
using Awakening.Core.Json;
using Awakening.Core.Playtest;

namespace Awakening.Core.Tests
{
    internal static class PlaytestTestSupport
    {
        internal static Dictionary<string, object> M(params object[] pairs)
        {
            var map = new Dictionary<string, object>();
            for (int i = 0; i < pairs.Length; i += 2) map[(string)pairs[i]] = pairs[i + 1];
            return map;
        }
        internal static Dictionary<string, object> O(Dictionary<string, object> map, string key) => (Dictionary<string, object>)map[key];
        internal static List<object> A(Dictionary<string, object> map, string key) => (List<object>)map[key];
        internal static Dictionary<string, object> Item(Dictionary<string, object> map, string key, int index) => (Dictionary<string, object>)A(map, key)[index];
        internal static double N(Dictionary<string, object> map, string key) => Convert.ToDouble(map[key]);
        internal static Dictionary<string, object> Body(Dictionary<string, object> job) => (Dictionary<string, object>)JsonReader.Parse((string)job["body"]);
        internal static Dictionary<string, object> Configuration() => M("enabled", true, "endpoint", "https://playtest.example.invalid/api/playtests/v1/batches/", "collection_id", "10000000-0000-4000-8000-000000000001", "build_id", "10000000-0000-4000-8000-000000000002", "round_id", "10000000-0000-4000-8000-000000000003");
        internal static CaptureState State() => new CaptureState { Seed = "synthetic-playtest", Difficulty = "normal", Ruleset = "alignment-7", Status = "placing", NextLogId = 1 };
        internal static Dictionary<string, object> Saved(PlaytestCapture capture, CaptureState state) => (Dictionary<string, object>)JsonReader.Parse(JsonWriter.Write(capture.Snapshot(state)));
        internal static Dictionary<string, object> Receipt(Dictionary<string, object> job, int accepted = -1) => M("batch_id", job["id"], "receipt_id", PlaytestCapture.Identifier(), "accepted_events", accepted < 0 ? A(Body(job), "events").Count : accepted, "accepted_comment", false, "run_expires_at", "2027-04-30T12:00:00Z");
        internal sealed class Rig
        {
            internal double Unix = 1800000000;
            internal double Mono;
            int _id;
            internal readonly PlaytestCapture Capture;
            internal readonly CaptureState State = PlaytestTestSupport.State();
            internal readonly Storage Storage;
            internal readonly Transport Transport = new Transport();
            internal readonly PlaytestOutbox Outbox;
            internal Rig(bool joined = true)
            {
                Capture = new PlaytestCapture(() => Unix, () => { var bytes = new byte[16]; BitConverter.GetBytes(++_id).CopyTo(bytes, 0); return bytes; }, () => 0.5);
                Capture.Configure(Configuration());
                if (joined) Capture.Accept();
                Storage = new Storage(() => Saved(Capture, State));
                Outbox = new PlaytestOutbox(Capture, Storage, Transport, () => Mono, () => Unix);
            }
            internal void Start() => Capture.StartRun(State);
            internal void Send()
            {
                Outbox.Process();
                Storage.SavedGeneration = Storage.Generation;
                Outbox.Process();
            }
            internal void Respond(int status, string body = "", string retry = "")
            {
                Transport.Responses.Add(new PlaytestResponse { Id = Transport.Sent[Transport.Sent.Count - 1].Id, Status = status, Body = body, RetryAfter = retry });
                Outbox.Process();
            }
        }
        internal sealed class Storage : IPlaytestStorage
        {
            readonly Func<Dictionary<string, object>> _snapshot;
            internal Storage(Func<Dictionary<string, object>> snapshot) => _snapshot = snapshot;
            public bool Ready { get; set; } = true;
            public bool Writable { get; set; } = true;
            public bool Failed { get; set; }
            public long SavedGeneration { get; set; }
            internal long Generation;
            internal bool Refuse;
            internal readonly List<Dictionary<string, object>> Writes = new List<Dictionary<string, object>>();
            public void Poll() { }
            public long Persist()
            {
                if (Refuse) return -1;
                Writes.Add(_snapshot());
                return ++Generation;
            }
        }
        internal sealed class Transport : IPlaytestTransport
        {
            internal readonly List<PlaytestResponse> Sent = new List<PlaytestResponse>();
            internal readonly List<PlaytestResponse> Responses = new List<PlaytestResponse>();
            internal int Cancellations;
            public void Send(string id, string endpoint, string body) => Sent.Add(new PlaytestResponse { Id = id, Body = body, RetryAfter = endpoint });
            public IEnumerable<PlaytestResponse> PollResponses()
            {
                var responses = Responses.ToArray();
                Responses.Clear();
                return responses;
            }
            public void Cancel() { Cancellations++; Responses.Clear(); }
        }
    }
}
