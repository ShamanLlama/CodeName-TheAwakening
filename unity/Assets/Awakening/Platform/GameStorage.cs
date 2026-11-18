using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Awakening.Core.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Awakening.Platform
{
    /// <summary>A browser focus or visibility change, stamped with the monotonic clock.</summary>
    public readonly struct VisibilityChange
    {
        public readonly long At;
        public readonly bool Focused;
        public readonly bool Visible;

        public VisibilityChange(long at, bool focused, bool visible)
        {
            At = at;
            Focused = focused;
            Visible = visible;
        }
    }

    /// <summary>The answer to one playtest POST. Status 0 means the request never got an answer.</summary>
    public readonly struct TransportResponse
    {
        public readonly string Id;
        public readonly int Status;
        public readonly string Body;
        public readonly string RetryAfter;

        public TransportResponse(string id, int status, string body, string retryAfter)
        {
            Id = id;
            Status = status;
            Body = body ?? "";
            RetryAfter = retryAfter ?? "";
        }
    }

    /// <summary>
    /// One writer per save, and the playtest transport. Port of godot/core/playtest_platform.gd.
    /// WebGL: the whole save envelope goes into one IndexedDB transaction (shared with the Godot
    /// build: database lucid-awakening-save-v1, key = save path) and a Web Lock keeps a second
    /// window from writing or sending. Native: a temporary file, flush and atomic rename, and a
    /// lock directory next to the save. A generation number tells the outbox when a save that
    /// holds a batch is durable, so nothing is sent before it is saved.
    /// </summary>
    public sealed class GameStorage
    {
        /// <summary>The Godot build's save path, which is also the IndexedDB key on the web.</summary>
        public const string SaveKey = "user://save-v2.json";
        public const int ResponseLimit = 16384;

        public bool Ready { get; private set; }
        public bool Writable { get; private set; }
        public bool Failed { get; private set; }
        public int SavedGeneration { get; private set; }
        public int Generation { get; private set; }
        /// <summary>Native save file; empty on the web.</summary>
        public string FilePath { get; private set; } = "";

        static readonly Stopwatch Clock = Stopwatch.StartNew();
        string _token = "";
        string _lockPath = "";
        bool _web;
        readonly Dictionary<string, UnityWebRequest> _requests = new Dictionary<string, UnityWebRequest>();

        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        public void Initialize(string nativeDirectory = null)
        {
            _web = IsWeb;
            if (_web)
            {
                Browser.Init(SaveKey);
                return;
            }
            string directory = nativeDirectory ?? Application.persistentDataPath;
            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, "save-v2.json");
            _lockPath = FilePath + ".lock";
            string ownerFile = Path.Combine(_lockPath, "owner.json");
            if (Directory.Exists(_lockPath) && IsStale(ownerFile))
            {
                TryDelete(ownerFile);
                try { Directory.Delete(_lockPath); } catch (IOException) { }
            }
            try
            {
                if (!Directory.Exists(_lockPath))
                {
                    Directory.CreateDirectory(_lockPath);
                    _token = Guid.NewGuid().ToString();
                    File.WriteAllText(ownerFile, JsonWriter.Write(new Dictionary<string, object>
                    {
                        ["pid"] = (double)Process.GetCurrentProcess().Id, ["token"] = _token,
                    }));
                    Writable = true;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Writable = false;
            }
            Ready = true;
        }

        /// <summary>A lock whose process is gone (checked through /proc on Linux) can be taken over.</summary>
        static bool IsStale(string ownerFile)
        {
            if (Application.platform != RuntimePlatform.LinuxPlayer && Application.platform != RuntimePlatform.LinuxEditor) return false;
            try
            {
                if (!(JsonReader.Parse(File.ReadAllText(ownerFile)) is Dictionary<string, object> owner) || !(owner.TryGetValue("pid", out object pid) && pid is double id))
                    return false;
                return !Directory.Exists("/proc/" + (long)id);
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        public void Poll()
        {
            if (!_web) return;
            if (!(JsonReader.Parse(Browser.Status()) is Dictionary<string, object> status)) return;
            Ready = status["ready"] is bool ready && ready;
            Writable = status["writable"] is bool writable && writable;
            Failed = status["failed"] is bool failed && failed;
            SavedGeneration = status["saved"] is double saved ? (int)saved : SavedGeneration;
        }

        /// <summary>The stored save text, or null when there is none. Throws IOException when unreadable.</summary>
        public string LoadRaw()
        {
            if (_web) return Browser.Load();
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }

        /// <summary>Copies an unreadable native save aside before a new run replaces it.</summary>
        public bool RetainCopy(string suffix, string raw)
        {
            if (_web || string.IsNullOrEmpty(FilePath)) return true;
            try
            {
                File.WriteAllText(FilePath + suffix, raw);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>Writes the save. Returns its generation, or -1 when storage cannot write.</summary>
        public int Persist(string envelope)
        {
            if (!Ready || !Writable || Failed) return -1;
            Generation++;
            if (_web)
            {
                Browser.Save(envelope, Generation);
                return Generation;
            }
            string temporary = FilePath + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(envelope);
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
                SavedGeneration = Generation;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Failed = true;
                Writable = false;
                return -1;
            }
            return Generation;
        }

        /// <summary>Monotonic milliseconds: performance.now() on the web, a stopwatch natively.</summary>
        public long Now() => _web ? (long)Browser.Now() : Clock.ElapsedMilliseconds;

        public List<VisibilityChange> VisibilityChanges()
        {
            var changes = new List<VisibilityChange>();
            if (!_web) return changes;
            if (!(JsonReader.Parse(Browser.TakeVisibility()) is List<object> entries)) return changes;
            foreach (object entry in entries)
                if (entry is Dictionary<string, object> e)
                    changes.Add(new VisibilityChange((long)(double)e["at"], (bool)e["focused"], (bool)e["visible"]));
            return changes;
        }

        public void Send(string id, string endpoint, string body)
        {
            if (_web)
            {
                Browser.Send(id, endpoint, body);
                return;
            }
            var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10,
                redirectLimit = 0,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest();
            _requests[id] = request;
        }

        public List<TransportResponse> Responses()
        {
            var responses = new List<TransportResponse>();
            if (_web)
            {
                if (JsonReader.Parse(Browser.Poll()) is List<object> entries)
                    foreach (object entry in entries)
                        if (entry is Dictionary<string, object> e)
                            responses.Add(new TransportResponse((string)e["id"], (int)(double)e["status"], e["body"] as string, e["retry_after"] as string));
                return responses;
            }
            var done = new List<string>();
            foreach (KeyValuePair<string, UnityWebRequest> pair in _requests)
            {
                UnityWebRequest request = pair.Value;
                if (!request.isDone) continue;
                done.Add(pair.Key);
                bool answered = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
                byte[] data = request.downloadHandler?.data;
                if (!answered || data != null && data.Length > ResponseLimit)
                    responses.Add(new TransportResponse(pair.Key, 0, "", ""));
                else
                    responses.Add(new TransportResponse(pair.Key, (int)request.responseCode, data == null ? "" : System.Text.Encoding.UTF8.GetString(data),
                        request.GetResponseHeader("Retry-After")));
                request.Dispose();
            }
            foreach (string id in done) _requests.Remove(id);
            return responses;
        }

        public void Cancel()
        {
            if (_web)
            {
                Browser.Cancel();
                return;
            }
            foreach (UnityWebRequest request in _requests.Values)
            {
                request.Abort();
                request.Dispose();
            }
            _requests.Clear();
        }

        public void Dispose()
        {
            Cancel();
            if (_web) Browser.Dispose();
            else if (!string.IsNullOrEmpty(_token))
            {
                string ownerFile = Path.Combine(_lockPath, "owner.json");
                try
                {
                    if (JsonReader.Parse(File.ReadAllText(ownerFile)) is Dictionary<string, object> owner && owner.TryGetValue("token", out object token) && (string)token == _token)
                    {
                        File.Delete(ownerFile);
                        Directory.Delete(_lockPath);
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is FormatException || exception is UnauthorizedAccessException) { }
            }
            Writable = false;
        }

        /// <summary>Hands the player a file: a browser download, or a file under the save folder.</summary>
        public string Download(string text, string filename)
        {
            if (_web)
            {
                Browser.Download(text, filename, "application/json");
                return filename;
            }
            string directory = Path.Combine(Path.GetDirectoryName(FilePath) ?? Application.persistentDataPath, "reports");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, filename);
            File.WriteAllText(path, text);
            Application.OpenURL("file://" + directory);
            return path;
        }

        public static bool TouchScreen()
        {
            if (IsWeb) return Browser.TouchScreen();
            // Desktop players (Linux, Steam) get the mouse layout: Unity reports touch support on
            // desktops that merely have a touch-capable input stack, which Godot did not.
            return Application.isMobilePlatform && Input.touchSupported;
        }

        static void TryDelete(string file)
        {
            try { File.Delete(file); } catch (IOException) { }
        }

        /// <summary>Calls into AwakeningLab.jslib. Outside a WebGL player these are never reached.</summary>
        static class Browser
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            [DllImport("__Internal")] static extern void AwakeningLabInit(string key);
            [DllImport("__Internal")] static extern string AwakeningLabStatus();
            [DllImport("__Internal")] static extern string AwakeningLabLoad();
            [DllImport("__Internal")] static extern void AwakeningLabSave(string raw, int generation);
            [DllImport("__Internal")] static extern double AwakeningLabNow();
            [DllImport("__Internal")] static extern string AwakeningLabTakeVisibility();
            [DllImport("__Internal")] static extern void AwakeningLabSend(string id, string url, string body);
            [DllImport("__Internal")] static extern string AwakeningLabPoll();
            [DllImport("__Internal")] static extern void AwakeningLabCancel();
            [DllImport("__Internal")] static extern void AwakeningLabDispose();
            [DllImport("__Internal")] static extern void AwakeningLabDownload(string text, string filename, string type);
            [DllImport("__Internal")] static extern int AwakeningLabTouchScreen();

            public static void Init(string key) => AwakeningLabInit(key);
            public static string Status() => AwakeningLabStatus();
            public static string Load() => AwakeningLabLoad();
            public static void Save(string raw, int generation) => AwakeningLabSave(raw, generation);
            public static double Now() => AwakeningLabNow();
            public static string TakeVisibility() => AwakeningLabTakeVisibility();
            public static void Send(string id, string url, string body) => AwakeningLabSend(id, url, body);
            public static string Poll() => AwakeningLabPoll();
            public static void Cancel() => AwakeningLabCancel();
            public static void Dispose() => AwakeningLabDispose();
            public static void Download(string text, string filename, string type) => AwakeningLabDownload(text, filename, type);
            public static bool TouchScreen() => AwakeningLabTouchScreen() != 0;
#else
            static Exception NotWeb() => new PlatformNotSupportedException("The browser bridge exists only in a WebGL player.");
            public static void Init(string key) => throw NotWeb();
            public static string Status() => throw NotWeb();
            public static string Load() => throw NotWeb();
            public static void Save(string raw, int generation) => throw NotWeb();
            public static double Now() => throw NotWeb();
            public static string TakeVisibility() => throw NotWeb();
            public static void Send(string id, string url, string body) => throw NotWeb();
            public static string Poll() => throw NotWeb();
            public static void Cancel() => throw NotWeb();
            public static void Dispose() => throw NotWeb();
            public static void Download(string text, string filename, string type) => throw NotWeb();
            public static bool TouchScreen() => throw NotWeb();
#endif
        }
    }
}
