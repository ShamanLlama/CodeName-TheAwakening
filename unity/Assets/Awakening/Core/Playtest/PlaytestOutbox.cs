using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Awakening.Core.Json;
using static Awakening.Core.Playtest.PlaytestJson;

namespace Awakening.Core.Playtest
{
    /// <summary>Persists the complete save envelope, including capture.Snapshot(gameState).
    /// Generations acknowledge completed durable writes, not scheduled writes.</summary>
    public interface IPlaytestStorage
    {
        bool Ready { get; }
        bool Writable { get; }
        bool Failed { get; }
        long SavedGeneration { get; }
        void Poll();
        long Persist();
    }

    /// <summary>Platform implementations enforce a 10 s timeout, 16 KiB response cap,
    /// no redirects, JSON content type, and omitted browser credentials.</summary>
    public interface IPlaytestTransport
    {
        void Send(string id, string endpoint, string body);
        IEnumerable<PlaytestResponse> PollResponses();
        void Cancel();
    }

    public sealed class PlaytestResponse
    {
        public string Id;
        public int Status;
        public string Body = "";
        public string RetryAfter = "";
    }

    /// <summary>Transport-independent port of playtest_client.gd. Call Process every frame;
    /// monotonicMilliseconds is the platform clock (performance.now on WebGL).</summary>
    public sealed class PlaytestOutbox
    {
        readonly PlaytestCapture _capture;
        readonly IPlaytestStorage _storage;
        readonly IPlaytestTransport _transport;
        readonly Func<double> _monotonicMilliseconds;
        readonly Func<double> _unixSeconds;
        string _sending = "";
        string _prepared = "";
        long _generation = -1;
        Dictionary<string, object> _undo = Map();
        double _nextSend;
        double _flushAt;
        public string Notice { get; private set; } = "";

        public PlaytestOutbox(PlaytestCapture capture, IPlaytestStorage storage, IPlaytestTransport transport, Func<double> monotonicMilliseconds = null, Func<double> unixSeconds = null)
        {
            _capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _monotonicMilliseconds = monotonicMilliseconds ?? (() => Stopwatch.GetTimestamp() * (1000d / Stopwatch.Frequency));
            _unixSeconds = unixSeconds ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d);
            _flushAt = _monotonicMilliseconds() + 20000;
        }
        public void Process()
        {
            _storage.Poll();
            if (_storage.Failed)
            {
                if (_undo.Count != 0) { _capture.UndoConfirmation(_undo); _undo = Map(); }
                _capture.StopCapture("Storage is unavailable. Capture and sending stopped; export the local record.");
                Cancel();
                return;
            }
            if (!_storage.Ready || !_storage.Writable) return;
            if (!_capture.Corrupt && _capture.Expire()) _storage.Persist();
            if (!_capture.Confirmed) return;
            if (_undo.Count != 0)
            {
                if (_storage.SavedGeneration >= _generation)
                {
                    _undo = Map();
                    Notice = "Receipt confirmed and saved locally.";
                }
                else return;
            }
            if (_sending.Length != 0)
            {
                foreach (var response in _transport.PollResponses()) Completed(response.Id, response.Status, response.Body, response.RetryAfter);
                return;
            }
            if (_prepared.Length != 0)
            {
                if (_storage.SavedGeneration >= _generation)
                {
                    var job = _capture.FindJob(_prepared);
                    _prepared = "";
                    if (job.Count != 0) Send(job);
                }
                return;
            }
            double now = _monotonicMilliseconds();
            var staging = Arr(_capture.Data, "staging");
            if (staging.Count != 0 && (_capture.Urgent || staging.Count >= 32 || now >= _flushAt))
            {
                _capture.SealBatch();
                _generation = _storage.Persist();
                _flushAt = now + 20000;
            }
            if (now < _nextSend) return;
            var next = _capture.NextJob();
            if (next.Count == 0) return;
            _capture.Attempted(next);
            _prepared = Str(next, "id");
            _generation = _storage.Persist();
            if (_generation < 0)
            {
                _prepared = "";
                _capture.StopCapture("Saving failed. No playtest request was sent.");
            }
        }
        void Send(Dictionary<string, object> job)
        {
            _sending = Str(job, "id");
            _nextSend = _monotonicMilliseconds() + 5000;
            Notice = "Sending pending records…";
            _transport.Send(_sending, Str(job, "endpoint"), Str(job, "body"));
        }
        void Completed(string id, int status, string body, string retryAfter)
        {
            if (id != _sending) return;
            _sending = "";
            var job = _capture.FindJob(id);
            if (job.Count == 0) return;
            if (status == 200 || status == 201)
            {
                _undo = JsonReader.TryParse(body, out object receipt) && receipt is Dictionary<string, object> map ? _capture.ConfirmJob(job, map) : Map();
                if (_undo.Count == 0)
                {
                    _capture.Postpone(job);
                    Notice = "The server did not provide a valid receipt. Records remain pending.";
                }
                else Notice = "Saving the server receipt…";
            }
            else if (status == 413 && _capture.SplitJob(job)) Notice = "Splitting the pending batch; event identifiers are unchanged.";
            else if (status == 0 || status == 429 || status >= 500)
            {
                _capture.Postpone(job, RetryAfterSeconds(retryAfter, _unixSeconds()));
                Notice = "Connection unavailable or collection paused. Records are saved for retry.";
            }
            else
            {
                _capture.Reject(job, "Collection rejected this run (HTTP " + status.ToString(CultureInfo.InvariantCulture) + "). Export the local record and contact the GTC operator.");
                Notice = _capture.Warning;
            }
            _generation = _storage.Persist();
            if (_generation < 0 && _undo.Count != 0)
            {
                _capture.UndoConfirmation(_undo);
                _undo = Map();
                _capture.StopCapture("The receipt could not be saved. Records remain available for export.");
            }
        }
        public static double RetryAfterSeconds(string value, double? unixSeconds = null)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && !double.IsNaN(seconds) && !double.IsInfinity(seconds)) return Math.Max(0, seconds);
            // Same six-part HTTP date interpretation as Godot; its parser ignores weekday/zone.
            string[] parts = (value ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            if (parts.Length != 6 || Array.IndexOf(months, parts[2]) < 0) return 0;
            string date = parts[3] + "-" + (Array.IndexOf(months, parts[2]) + 1).ToString("00", CultureInfo.InvariantCulture) + "-" + parts[1].PadLeft(2, '0') + "T" + parts[4];
            if (!DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)) return 0;
            return Math.Max(0, parsed.ToUnixTimeSeconds() - (unixSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d));
        }
        public void Cancel()
        {
            _transport.Cancel();
            _sending = "";
            _prepared = "";
            _undo = Map();
        }
        /// <summary>The controller uses this when the player stops participation.</summary>
        public void Withdraw()
        {
            _capture.Withdraw();
            Cancel();
            _storage.Persist();
        }
    }
}
