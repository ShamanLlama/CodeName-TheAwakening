using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Awakening.Core.Json;
using static Awakening.Core.Playtest.PlaytestJson;

namespace Awakening.Core.Playtest
{
    /// <summary>Consent-aware capture and durable outbox. Performs no I/O and changes no game rules.</summary>
    public sealed class PlaytestCapture
    {
        public const int MaxEvents = 5000;
        public const int MaxBytes = 8388608;
        public const int Ttl = 604800;
        public const int MaxClockMs = 604800000;
        public const int MaxSequence = 20000;
        public const int MaxSessions = 64;
        public const int MaxBodyBytes = 65536;
        public const int BatchEvents = 32;
        public const string Adapter = "awakening-3";
        public static readonly string[] StoredAdapters = { "awakening-1", "awakening-2", "awakening-3" };
        public static readonly string[] ClockKeys = { "elapsed_ms", "simulation_ms", "planning_ms", "background_ms", "excluded_ms" };
        static readonly Regex IdPattern = new Regex("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.CultureInvariant);
        readonly Func<double> _unixSeconds;
        readonly Func<byte[]> _randomBytes;
        readonly Func<double> _jitter;
        readonly bool _debugBuild;
        int _count;
        int _bytes;
        public Dictionary<string, object> Config { get; private set; } = Map();
        public Dictionary<string, object> Data { get; private set; } = Fresh();
        public bool Confirmed { get; private set; }
        public bool SessionLive { get; private set; }
        public string Warning { get; private set; } = "";
        public bool Corrupt { get; private set; }
        public object RawStored { get; private set; }
        public bool Urgent { get; private set; }

        public PlaytestCapture(Func<double> unixSeconds = null, Func<byte[]> randomBytes = null, Func<double> jitter = null, bool debugBuild = false)
        {
            _unixSeconds = unixSeconds ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            _randomBytes = randomBytes ?? RandomBytes;
            var random = new Random();
            _jitter = jitter ?? random.NextDouble;
            _debugBuild = debugBuild;
        }
        static byte[] RandomBytes()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return bytes;
        }
        public static string Identifier(Func<byte[]> randomBytes = null)
        {
            byte[] source = (randomBytes ?? RandomBytes)();
            if (source == null || source.Length != 16) throw new ArgumentException("An identifier needs 16 random bytes", nameof(randomBytes));
            var bytes = (byte[])source.Clone();
            bytes[6] = (byte)((bytes[6] & 15) | 64);
            bytes[8] = (byte)((bytes[8] & 63) | 128);
            var s = new StringBuilder(36);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i == 4 || i == 6 || i == 8 || i == 10) s.Append('-');
                s.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return s.ToString();
        }
        string NewId() => Identifier(_randomBytes);
        public static bool ValidId(object value) => value is string text && IdPattern.IsMatch(text);
        public static Dictionary<string, object> Fresh() => Map("version", 1, "choices", Map(), "testers", Map(), "runs", Map(), "active", "", "signature", "", "staging", new List<object>(), "jobs", new List<object>(), "losses", 0, "receipt", Map());
        public static Dictionary<string, object> EmptyClock() => Map("elapsed_ms", 0, "simulation_ms", 0, "planning_ms", 0, "background_ms", 0, "excluded_ms", 0);
        public static string Signature(CaptureState state) => state == null ? "" : JsonWriter.Write(new List<object> { state.Seed, state.Difficulty, state.ElapsedMonths, state.NextLogId, state.Status });
        public static bool ValidEndpoint(object value, bool debugBuild = false)
        {
            if (!(value is string text) || !text.EndsWith("/api/playtests/v1/batches/", StringComparison.Ordinal)) return false;
            foreach (string forbidden in new[] { "@", "?", "#", "\n", "\r" }) if (text.Contains(forbidden)) return false;
            return text.StartsWith("https://", StringComparison.Ordinal) || (debugBuild && text.StartsWith("http://127.0.0.1:", StringComparison.Ordinal));
        }
        public void Configure(Dictionary<string, object> options) => Configure(options, Map());
        public void Configure(Dictionary<string, object> options, object stored)
        {
            Config = Clone(options);
            Confirmed = false;
            SessionLive = false;
            Corrupt = !ValidStore(stored, _debugBuild);
            RawStored = stored;
            Data = Corrupt || (stored is Dictionary<string, object> map && map.Count == 0) ? Fresh() : Clone((Dictionary<string, object>)stored);
            if (Corrupt) Warning = "The saved playtest record is unreadable. Gameplay is available; export or clear the local record.";
            Recount();
            if (_count > MaxEvents || _bytes > MaxBytes)
            {
                Corrupt = true;
                Warning = "The saved playtest queue exceeds its limit. Export or clear the local record.";
            }
        }
        public bool Configured() => Flag(Config, "enabled") && ValidEndpoint(Get(Config, "endpoint"), _debugBuild) && ValidId(Get(Config, "collection_id")) && ValidId(Get(Config, "build_id")) && ValidId(Get(Config, "round_id"));
        public bool Accept()
        {
            if (!Configured() || Corrupt) return false;
            Expire();
            var testers = Obj(Data, "testers");
            string round = Str(Config, "round_id");
            if (!testers.ContainsKey(round) && testers.Count >= 64)
            {
                Warning = "The local record has reached its round limit. Export and clear it before joining another round.";
                return false;
            }
            Confirmed = true;
            if (Str(Data, "active").Length == 0) Warning = "";
            Obj(Data, "choices")[round] = true;
            if (!testers.ContainsKey(round)) testers[round] = NewId();
            return true;
        }
        public bool Restore() => Configured() && !Corrupt && Flag(Obj(Data, "choices"), Str(Config, "round_id")) && Obj(Data, "testers").ContainsKey(Str(Config, "round_id")) && Accept();
        public bool Answered() => Configured() && Obj(Data, "choices").ContainsKey(Str(Config, "round_id"));
        public void Decline()
        {
            if (!Configured() || Corrupt || Confirmed) return;
            Obj(Data, "choices")[Str(Config, "round_id")] = false;
        }
        public void Withdraw()
        {
            Data = Fresh();
            if (Configured()) Obj(Data, "choices")[Str(Config, "round_id")] = false;
            Confirmed = false;
            SessionLive = false;
            Corrupt = false;
            RawStored = null;
            Urgent = false;
            Warning = "Participation stopped. Unsent records were cleared. Records already received are managed by the GTC operator.";
            Recount();
        }
        public object Snapshot(CaptureState state = null)
        {
            if (Corrupt) return RawStored;
            if (SessionLive && Confirmed && !FlagStopped(Active())) Data["signature"] = Signature(state);
            return Clone(Data);
        }
        static bool FlagStopped(Dictionary<string, object> run) => !run.TryGetValue("stopped", out object stopped) || (bool)stopped;
        public Dictionary<string, object> Active() => Get(Obj(Data, "runs"), Str(Data, "active")) as Dictionary<string, object> ?? Map();
        public bool Capturing()
        {
            var run = Active();
            return Confirmed && SessionLive && !Corrupt && run.Count != 0 && !Flag(run, "closed") && !Flag(run, "stopped");
        }
        public void StartRun(CaptureState state, bool fromStart = true)
        {
            if (!Confirmed || !Configured() || Corrupt || state.IsOver) return;
            Expire();
            PruneRuns();
            if (Obj(Data, "runs").Count >= 64 || Obj(Data, "testers").Count > 64 || _count >= MaxEvents - 4 || _bytes > MaxBytes - 8192)
            {
                Warning = "Playtest storage is full. Export the local record; gameplay continues without capture.";
                SessionLive = false;
                return;
            }
            if (Characters(state.Seed) > 64)
            {
                Warning = "This seed is too long for playtest capture. Gameplay is available.";
                return;
            }
            string id = NewId();
            var context = Map("schema_version", "1.0", "adapter", Adapter, "collection_id", Config["collection_id"], "build_id", Config["build_id"], "round_id", Config["round_id"], "tester_id", Obj(Data, "testers")[Str(Config, "round_id")], "run_id", id, "participation_version", "playtests-v1", "coverage", fromStart ? "from_start" : "partial");
            Obj(Data, "runs")[id] = Map("context", context, "endpoint", Config["endpoint"], "sequence", 0, "session_id", NewId(), "clock", EmptyClock(), "sessions", 1, "comments", 0, "closed", false, "stopped", false, "seen", Map(), "last_event", Map(), "last_session_id", "");
            Data["active"] = id;
            SessionLive = true;
            EmitEvent(fromStart ? "run_started" : "capture_started", Map("difficulty", state.Difficulty, "seed", state.Seed, "ruleset", state.Ruleset), state);
            EmitEvent("session_started", Map("mode", fromStart ? "new" : "partial"), state);
            Data["signature"] = Signature(state);
            Urgent = true;
        }
        public void Resume(CaptureState state)
        {
            if (!Confirmed || !Configured() || Corrupt || SessionLive || state.IsOver) return;
            var run = Active();
            if (run.Count == 0 || Flag(run, "closed") || Flag(run, "stopped") || Str(Obj(run, "context"), "adapter") != Adapter || !Equals(Obj(run, "context")["round_id"], Config["round_id"]) || !Equals(Obj(run, "context")["build_id"], Config["build_id"]) || Str(Data, "signature") != Signature(state))
            {
                StartRun(state, false);
                return;
            }
            if (Num(run, "sessions") >= MaxSessions)
            {
                run["stopped"] = true;
                StartRun(state, false);
                return;
            }
            run["sessions"] = (int)Num(run, "sessions") + 1;
            run["session_id"] = NewId();
            run["clock"] = EmptyClock();
            SessionLive = true;
            EmitEvent("session_started", Map("mode", "resume"), state);
            EmitEvent("capture_gap", Map("reason", "unknown_time", "missing_events", null), state);
            Urgent = true;
        }
        public void Tick(int milliseconds, string mode)
        {
            if (!Capturing() || milliseconds <= 0) return;
            var clock = Obj(Active(), "clock");
            if (Num(clock, "elapsed_ms") + milliseconds > MaxClockMs)
            {
                StopCapture("A capture session reached seven days. Reopen the game to start a new session.");
                return;
            }
            clock["elapsed_ms"] = (int)Num(clock, "elapsed_ms") + milliseconds;
            clock[mode + "_ms"] = (int)Num(clock, mode + "_ms") + milliseconds;
        }
        public void EmitEvent(string kind, Dictionary<string, object> payload, CaptureState state, string phase = "", bool reserve = false)
        {
            if (!Capturing()) return;
            var run = Active();
            var entry = Map("event_id", NewId(), "sequence", (int)Num(run, "sequence") + 1, "type", kind, "phase", phase.Length != 0 ? phase : state.IsOver ? "ended" : state.Status, "game_month", state.ElapsedMonths, "clock", Clone(Obj(run, "clock")), "data", Clone(payload));
            int size = Bytes(entry);
            if (!reserve && (_count >= MaxEvents - 2 || _bytes + size > MaxBytes - 4096 || Num(run, "sequence") >= 19998))
            {
                EmitEvent("capture_gap", Map("reason", "queue_full", "missing_events", null), state, (string)Get(Obj(run, "last_event"), "phase", state.Status), true);
                StopCapture("Playtest storage is full. Capture stopped; export the local record. Gameplay continues.");
                return;
            }
            Arr(Data, "staging").Add(Map("run_id", Data["active"], "session_id", run["session_id"], "event", entry, "created_at", _unixSeconds()));
            run["sequence"] = entry["sequence"];
            run["last_event"] = Clone(entry);
            run["last_session_id"] = run["session_id"];
            _count++;
            _bytes += size;
            if (kind == "run_finished" || kind == "run_abandoned")
            {
                run["closed"] = true;
                SessionLive = false;
                Urgent = true;
            }
        }
        public void Checkpoint(string name, object region, CaptureState state)
        {
            if (!Capturing() || Obj(Active(), "seen").ContainsKey(name)) return;
            Obj(Active(), "seen")[name] = true;
            EmitEvent("checkpoint_reached", Map("checkpoint", name, "region", region), state);
        }
        public void StopCapture(string message)
        {
            if (Active().Count != 0) Active()["stopped"] = true;
            SessionLive = false;
            Warning = message;
            Urgent = true;
        }
        public Dictionary<string, object> SealBatch()
        {
            var staging = Arr(Data, "staging");
            if (staging.Count == 0 || Corrupt) return Map();
            var first = (Dictionary<string, object>)staging[0];
            var run = Obj(Obj(Data, "runs"), Str(first, "run_id"));
            var body = Clone(Obj(run, "context"));
            body["session_id"] = first["session_id"];
            body["batch_id"] = NewId();
            var events = new List<object>();
            body["events"] = events;
            int count = 0;
            double queued = Num(first, "created_at");
            foreach (Dictionary<string, object> entry in staging)
            {
                if (!Equals(entry["run_id"], first["run_id"]) || !Equals(entry["session_id"], first["session_id"]) || count >= BatchEvents) break;
                events.Add(entry["event"]);
                if (Bytes(body) > MaxBodyBytes) { events.RemoveAt(events.Count - 1); break; }
                queued = Math.Min(queued, Num(entry, "created_at"));
                count++;
            }
            if (count == 0) return Map();
            var job = Job(body, Str(run, "endpoint"), queued);
            Arr(Data, "jobs").Add(job);
            staging.RemoveRange(0, count);
            Urgent = staging.Count != 0;
            Recount();
            return job;
        }
        public bool QueueComment(string text)
        {
            var run = Active();
            text = StripEdges(text);
            if (!Confirmed || Corrupt || run.Count == 0 || !Flag(run, "closed") || Flag(run, "stopped") || Num(run, "comments") >= 10 || text.Length == 0 || Characters(text) > 1000 || _count >= MaxEvents - 2 || _bytes > MaxBytes - 16384) return false;
            var body = Clone(Obj(run, "context"));
            body["session_id"] = run["last_session_id"];
            body["batch_id"] = NewId();
            body["events"] = new List<object> { Clone(Obj(run, "last_event")) };
            body["comment"] = Map("comment_id", NewId(), "text", text);
            run["comments"] = (int)Num(run, "comments") + 1;
            Arr(Data, "jobs").Add(Job(body, Str(run, "endpoint"), _unixSeconds()));
            Urgent = true;
            Recount();
            return true;
        }
        static Dictionary<string, object> Job(Dictionary<string, object> body, string endpoint, double queued) => Map("id", body["batch_id"], "run_id", body["run_id"], "round_id", body["round_id"], "endpoint", endpoint, "body", Stringify(body), "queued_at", queued, "attempts", 0, "next_retry", 0, "blocked", false);
        public Dictionary<string, object> NextJob(double? now = null)
        {
            if (!Confirmed || !Configured() || Corrupt) return Map();
            foreach (Dictionary<string, object> job in Arr(Data, "jobs"))
                if (!Flag(job, "blocked") && Equals(job["endpoint"], Config["endpoint"]) && Flag(Obj(Data, "choices"), Str(job, "round_id")) && (now ?? _unixSeconds()) >= Num(job, "next_retry")) return job;
            return Map();
        }
        public Dictionary<string, object> FindJob(string id)
        {
            foreach (Dictionary<string, object> job in Arr(Data, "jobs")) if (Str(job, "id") == id) return job;
            return Map();
        }
        public void Attempted(Dictionary<string, object> job)
        {
            job["attempts"] = (long)Num(job, "attempts") + 1;
            job["next_retry"] = _unixSeconds() + 10;
        }
        public void Postpone(Dictionary<string, object> job, double retryAfter = 0)
        {
            double delay = Math.Min(120, Math.Pow(2, Math.Min(Num(job, "attempts") - 1, 7))) * (0.8 + _jitter() * 0.4);
            job["next_retry"] = _unixSeconds() + Math.Max(delay, retryAfter);
        }
        public void Reject(Dictionary<string, object> job, string message)
        {
            foreach (Dictionary<string, object> entry in Arr(Data, "jobs")) if (Equals(entry["run_id"], job["run_id"])) entry["blocked"] = true;
            Obj(Obj(Data, "runs"), Str(job, "run_id"))["stopped"] = true;
            if (Equals(Data["active"], job["run_id"])) SessionLive = false;
            Warning = message;
        }
        public bool SplitJob(Dictionary<string, object> job)
        {
            var body = (Dictionary<string, object>)JsonReader.Parse(Str(job, "body"));
            var events = Arr(body, "events");
            if (events.Count < 2) return false;
            int middle = events.Count / 2;
            var jobs = Arr(Data, "jobs");
            int index = jobs.IndexOf(job);
            jobs.RemoveAt(index);
            foreach (bool first in new[] { true, false })
            {
                var part = Clone(body);
                part["batch_id"] = NewId();
                part["events"] = events.GetRange(first ? 0 : middle, first ? middle : events.Count - middle);
                if (first) part.Remove("comment");
                jobs.Insert(index++, Job(part, Str(job, "endpoint"), Num(job, "queued_at")));
            }
            Recount();
            return true;
        }
        public Dictionary<string, object> ConfirmJob(Dictionary<string, object> job, Dictionary<string, object> receipt)
        {
            var body = (Dictionary<string, object>)JsonReader.Parse(Str(job, "body"));
            if (!Equals(Get(receipt, "batch_id"), job["id"]) || !ValidId(Get(receipt, "receipt_id")) || !(Get(receipt, "accepted_comment") is bool) || !Integer(Get(receipt, "accepted_events"), Arr(body, "events").Count) || !(Get(receipt, "run_expires_at") is string expires)) return Map();
            if (!expires.EndsWith("Z", StringComparison.Ordinal) || !DateTimeOffset.TryParse(expires, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) || date.ToUnixTimeSeconds() <= 0) return Map();
            var undo = Map("job", Clone(job), "receipt", Clone(Obj(Data, "receipt")));
            Arr(Data, "jobs").Remove(job);
            Data["receipt"] = Clone(receipt);
            Recount();
            return undo;
        }
        public void UndoConfirmation(Dictionary<string, object> undo)
        {
            Arr(Data, "jobs").Insert(0, undo["job"]);
            Data["receipt"] = undo["receipt"];
            Recount();
        }
        public bool Expire(double? now = null)
        {
            var jobs = Arr(Data, "jobs");
            var staging = Arr(Data, "staging");
            int old = jobs.Count + staging.Count;
            double time = now ?? _unixSeconds();
            jobs.RemoveAll(j => time - Num((Dictionary<string, object>)j, "queued_at") > Ttl);
            staging.RemoveAll(e => time - Num((Dictionary<string, object>)e, "created_at") > Ttl);
            if (old == jobs.Count + staging.Count) return false;
            Data["losses"] = (long)Num(Data, "losses") + 1;
            StopCapture("Pending playtest records expired after seven days. Coverage is incomplete; gameplay continues.");
            Recount();
            return true;
        }
        public int PendingEvents() => _count;
        void Recount()
        {
            _count = Arr(Data, "staging").Count;
            _bytes = 0;
            foreach (Dictionary<string, object> entry in Arr(Data, "staging")) _bytes += Bytes(entry["event"]);
            foreach (Dictionary<string, object> job in Arr(Data, "jobs"))
            {
                _bytes += Encoding.UTF8.GetByteCount(Str(job, "body"));
                _count += Arr((Dictionary<string, object>)JsonReader.Parse(Str(job, "body")), "events").Count;
            }
        }
        void PruneRuns()
        {
            var used = new HashSet<string> { Str(Data, "active") };
            foreach (Dictionary<string, object> entry in Arr(Data, "staging")) used.Add(Str(entry, "run_id"));
            foreach (Dictionary<string, object> job in Arr(Data, "jobs")) used.Add(Str(job, "run_id"));
            var runs = Obj(Data, "runs");
            foreach (string id in new List<string>(runs.Keys)) if (!used.Contains(id)) runs.Remove(id);
        }
        public static bool ValidStore(object value, bool debugBuild = false)
        {
            if (value is Dictionary<string, object> empty && empty.Count == 0) return true;
            if (!(value is Dictionary<string, object> store) || !Integer(Get(store, "version"), 1) || Convert.ToDouble(Get(store, "version")) != 1) return false;
            foreach (string key in new[] { "choices", "testers", "runs", "receipt" })
                if (!(Get(store, key) is Dictionary<string, object> map) || map.Count > 64) return false;
            foreach (string key in new[] { "staging", "jobs" })
                if (!(Get(store, key) is List<object> list) || list.Count > MaxEvents) return false;
            if (!(Get(store, "active") is string active) || !(Get(store, "signature") is string) || !Integer(Get(store, "losses"))) return false;
            var runs = Obj(store, "runs");
            if (active != "" && !runs.ContainsKey(active)) return false;
            foreach (var tester in Obj(store, "testers")) if (!ValidId(tester.Key) || !ValidId(tester.Value)) return false;
            foreach (var choice in Obj(store, "choices")) if (!ValidId(choice.Key) || !(choice.Value is bool)) return false;
            foreach (var pair in runs)
            {
                if (!ValidId(pair.Key) || !(pair.Value is Dictionary<string, object> run)) return false;
                if (!(Get(run, "context") is Dictionary<string, object> context) || !(Get(run, "clock") is Dictionary<string, object> clock) || !ValidId(Get(run, "session_id")) || !Integer(Get(run, "sequence"), MaxSequence) || !Integer(Get(run, "sessions"), MaxSessions) || Num(run, "sessions") < 1 || !Integer(Get(run, "comments"), 10)) return false;
                foreach (string key in new[] { "closed", "stopped" }) if (!(Get(run, key) is bool)) return false;
                foreach (string key in new[] { "seen", "last_event" }) if (!(Get(run, key) is Dictionary<string, object>)) return false;
                if (!(Get(run, "last_session_id") is string) || !ValidEndpoint(Get(run, "endpoint"), debugBuild)) return false;
                foreach (string key in new[] { "collection_id", "build_id", "round_id", "tester_id", "run_id" }) if (!ValidId(Get(context, key))) return false;
                if (!Equals(Get(context, "run_id"), pair.Key) || !Equals(Get(context, "schema_version"), "1.0") || !In(Get(context, "adapter"), StoredAdapters) || !Equals(Get(context, "participation_version"), "playtests-v1") || !In(Get(context, "coverage"), "from_start", "partial")) return false;
                var last = Obj(run, "last_event");
                if (last.Count != 0 && !ValidEvent(last)) return false;
                if (Flag(run, "closed") && (!ValidId(run["last_session_id"]) || !In(Get(last, "type"), "run_finished", "run_abandoned"))) return false;
                if (!ValidClock(clock)) return false;
            }
            foreach (object item in Arr(store, "staging"))
            {
                if (!(item is Dictionary<string, object> entry) || !(Get(entry, "run_id") is string id) || !runs.ContainsKey(id) || !ValidId(Get(entry, "session_id")) || !ValidEvent(Get(entry, "event")) || !Number(Get(entry, "created_at"))) return false;
            }
            foreach (object item in Arr(store, "jobs"))
            {
                if (!(item is Dictionary<string, object> job) || !(Get(job, "run_id") is string id) || !runs.ContainsKey(id) || !ValidId(Get(job, "id")) || !ValidId(Get(job, "round_id")) || !ValidEndpoint(Get(job, "endpoint"), debugBuild)) return false;
                if (!(Get(job, "body") is string text) || Characters(text) > MaxBodyBytes || !Number(Get(job, "queued_at")) || !Integer(Get(job, "attempts")) || !Number(Get(job, "next_retry")) || !(Get(job, "blocked") is bool)) return false;
                if (!JsonReader.TryParse(text, out object parsed) || !(parsed is Dictionary<string, object> body) || !(Get(body, "events") is List<object> events) || events.Count == 0 || events.Count > 64 || !Equals(Get(body, "batch_id"), job["id"])) return false;
                if (!ValidId(Get(body, "session_id")) || !Equals(Get(body, "run_id"), job["run_id"])) return false;
                foreach (object entry in events) if (!ValidEvent(entry)) return false;
            }
            return true;
        }
        static bool In(object value, params string[] options) => value is string text && Array.IndexOf(options, text) >= 0;
        public static bool ValidEvent(object value)
        {
            if (!(value is Dictionary<string, object> entry) || !ValidId(Get(entry, "event_id")) || !Integer(Get(entry, "sequence"), MaxSequence) || Num(entry, "sequence") < 1 || !Integer(Get(entry, "game_month"), 224)) return false;
            if (!In(Get(entry, "phase"), "placing", "playing", "alignment", "awakening", "ended") || !(Get(entry, "data") is Dictionary<string, object>) || !(Get(entry, "clock") is Dictionary<string, object> clock)) return false;
            if (!In(Get(entry, "type"), "run_started", "capture_started", "session_started", "starting_region_selected", "action_performed", "doctrine_selected", "awakening_started", "phase_changed", "checkpoint_reached", "time_checkpoint", "capture_gap", "run_finished", "run_abandoned", "threat_spawned", "threat_resolved", "month_snapshot", "speed_changed")) return false;
            return ValidClock(clock);
        }
        static bool ValidClock(Dictionary<string, object> clock)
        {
            double sum = 0;
            foreach (string key in ClockKeys)
            {
                if (!Integer(Get(clock, key), MaxClockMs)) return false;
                if (key != "elapsed_ms") sum += Num(clock, key);
            }
            return sum == Num(clock, "elapsed_ms");
        }
    }
}
