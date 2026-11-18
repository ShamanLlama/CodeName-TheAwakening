using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Json;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;
using Awakening.Core.Session;
using Awakening.Platform;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// Root of the game in Unity: storage, screens, sound, interface size, keys and the screenshot
    /// harness. Port of godot/main.gd; the run, its commands, the save envelope, consent, playtest
    /// emission and the clock partition live in the engine-free <see cref="GameSession"/>, which is
    /// tested without Unity. Screens read <see cref="State"/> and call the command methods; every
    /// change raises <see cref="Changed"/>.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        /// <summary>Interface sizes the player can pick. The canvas is designed at 1600×900.</summary>
        public static readonly float[] UiScales = { 1f, 1.3f, 1.6f, 2f };
        /// <summary>A layout narrower or shorter than this, in canvas units, switches to the compact layout.</summary>
        static readonly Vector2 CompactSize = new Vector2(1240, 700);
        /// <summary>The smallest canvas the compact layout fits.</summary>
        const float MinCanvasHeight = 430f;
        static readonly Vector2 BaseCanvas = new Vector2(1600, 900);

        public event Action Changed;
        public event Action<List<GameEvent>> Events;

        /// <summary>The run and its playtest capture. Null only before storage has loaded.</summary>
        public GameSession Session { get; private set; }
        public GameState State { get => Session.State; set => Session.State = value; }
        public SaveSettings Settings => Session.Settings;
        public bool TutorialComplete { get => Session.TutorialComplete; set => Session.TutorialComplete = value; }
        public Dictionary<string, object> LastResult { get => Session.LastResult; set => Session.LastResult = value; }
        public bool Blocked { get => Session.Blocked; set => Session.Blocked = value; }
        public string StorageNotice { get => Session.StorageNotice; set => Session.StorageNotice = value; }
        public PlaytestCapture Lab => Session.Lab;
        public Sfx Sfx;
        public readonly GameStorage Storage = new GameStorage();
        PlaytestOutbox _outbox;
        bool _commanding;
        string _lastStatus = "";

        VisualElement _screen;
        VisualElement _root;
        VisualElement _rotate;
        PanelSettings _panelSettings;
        Tooltips _tooltips;
        Dictionary<string, string> _shot = new Dictionary<string, string>();
        bool _ready;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal VisualElement CurrentScreen => _screen;
        internal bool ClockHeld => Session.Holds > 0;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var host = new GameObject("The Awakening");
            DontDestroyOnLoad(host);
            host.AddComponent<GameController>();
        }

        void Start()
        {
            Application.targetFrameRate = 60;
            // Development players would otherwise cover the ledger with Unity's error console.
            Debug.developerConsoleEnabled = false;
            _shot = ShotArgs();
            if (_shot.Count > 0) Application.runInBackground = true;
            Kit.Touch = GameStorage.TouchScreen() || _shot.ContainsKey("touch");
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Hex("07080b");
            camera.cullingMask = 0;
            camera.gameObject.AddComponent<AudioListener>();
            Sfx = gameObject.AddComponent<Sfx>();
            BuildPanel();
            StartCoroutine(Initialize());
        }

        void BuildPanel()
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/AwakeningTheme");
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panelSettings.clearColor = false;
            var holder = new GameObject("Interface");
            holder.transform.SetParent(transform);
            holder.SetActive(false);
            var document = holder.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            holder.SetActive(true);
            _root = document.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Palette.Desk;
            _root.EnableInClassList("aw-touch", Kit.Touch);
            _tooltips = new Tooltips(_root);
        }

        System.Collections.IEnumerator Initialize()
        {
            var lab = new PlaytestCapture(debugBuild: Debug.isDebugBuild);
            if (_shot.Count > 0)
            {
                // Screenshots never touch the real save or send anything. Like main.gd, which loads no
                // save then, they start from its in-memory settings: sound on.
                SaveEnvelope blank = SaveData.DefaultSave();
                blank.Settings.Muted = false;
                Session = new GameSession(blank, new Dictionary<string, object>(), () => Storage.Now(), _ => -1, lab, blocked: true);
            }
            else
            {
                Storage.Initialize();
                float deadline = Time.realtimeSinceStartup + 5f;
                while (!Storage.Ready && Time.realtimeSinceStartup < deadline)
                {
                    Storage.Poll();
                    yield return null;
                }
                (SaveEnvelope save, bool blocked, string notice) = LoadSave();
                if (!Storage.Writable || !Storage.Ready)
                {
                    blocked = true;
                    notice = "Another window owns this save, or storage is unavailable. This window plays in memory; saving and playtest capture are disabled.";
                }
                var config = Resources.Load<TextAsset>("Data/playtest-config");
                var options = config != null && JsonReader.TryParse(config.text, out object parsed) && parsed is Dictionary<string, object> found
                    ? found : new Dictionary<string, object>();
                Session = new GameSession(save, options, () => Storage.Now(), envelope => Storage.Persist(SaveData.EncodeSave(envelope)), lab, blocked,
                    () => Storage.VisibilityChanges().Select(v => new SessionVisibilityChange(v.At, v.Focused, v.Visible)),
                    () => _outbox?.Cancel(), NewSeed);
                Session.StorageNotice = notice;
                _outbox = new PlaytestOutbox(Session.Lab, new PlaytestStorageAdapter(Storage, () => Session.Commit()), new PlaytestTransportAdapter(Storage), () => Storage.Now());
            }
            Session.Changed += () => Changed?.Invoke();
            Session.Events += OnSessionEvents;
            _lastStatus = State?.Status ?? "";
            ApplyAudio();
            if (_shot.TryGetValue("scale", out string scale)) Settings.UiScale = double.Parse(scale, System.Globalization.CultureInfo.InvariantCulture);
            if (_shot.TryGetValue("motion", out string motion)) Settings.ReducedMotion = motion == "reduced";
            Kit.ReducedMotion = Settings.ReducedMotion;
            FitWindow();
            _ready = true;
            if (_shot.Count > 0) StartCoroutine(StageShot());
            else ShowStart();
        }

        /// <summary>Godot's "%08x" % randi(): an 8-digit hex seed when the player leaves it empty.</summary>
        static string NewSeed() => ((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue)).ToString("x8");

        (SaveEnvelope save, bool blocked, string notice) LoadSave()
        {
            string raw;
            try
            {
                raw = Storage.LoadRaw();
            }
            catch (Exception exception) when (exception is System.IO.IOException || exception is UnauthorizedAccessException)
            {
                return (SaveData.DefaultSave(), true, "Storage is unavailable. You can play, but this session will not save.");
            }
            if (raw == null) return (SaveData.DefaultSave(), false, "");
            SaveEnvelope decoded = SaveData.DecodeSave(raw);
            if (decoded == null)
            {
                // The web keeps an unreadable save and plays in memory; native builds keep a recovery
                // copy when a new run starts.
                return GameStorage.IsWeb
                    ? (SaveData.DefaultSave(), true, "The browser save is unreadable. It has been retained; this session continues in memory.")
                    : (SaveData.DefaultSave(), false, "This save could not be read. Starting fresh will retain a recovery copy.");
            }
            string notice = SaveData.TakeRetiredNotice(decoded);
            // Keep the retired run aside before the next save replaces it.
            if (notice != "") Storage.RetainCopy(".retired.json", raw);
            return (decoded, false, notice);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!_ready || _shot.Count > 0) return;
            Session.OnApplicationFocus(focused, GameStorage.IsWeb);
        }

        void OnApplicationQuit()
        {
            _outbox?.Cancel();
            Storage.Dispose();
        }

        void Update()
        {
            if (!_ready) return;
            HandleKeys();
            if (_lastWidth != Screen.width || _lastHeight != Screen.height)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
                if (FitWindow()) RebuildScreen();
            }
            if (_shot.Count > 0) return;
            Storage.Poll();
            _outbox?.Process();
            Session.Process(Time.unscaledDeltaTime, Storage.Failed);
        }

        int _lastWidth, _lastHeight;

        void HandleKeys()
        {
            if (!Input.anyKeyDown) return;
            // Typing in a field must not trigger shortcuts.
            var focused = _root.panel?.focusController?.focusedElement as VisualElement;
            bool typing = focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null;
            if (typing && !Input.GetKeyDown(KeyCode.Tab) && !Input.GetKeyDown(KeyCode.Escape)) return;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            foreach (KeyCode key in WatchedKeys)
            {
                if (!Input.GetKeyDown(key)) continue;
                if (_screen is GameScreen game)
                {
                    if (Session.Holds == 0 && !game.Dialogs.IsOpen && State != null)
                    {
                        if (key == KeyCode.Space)
                        {
                            SetSpeed(State.Speed != 0 ? 0 : 1);
                            continue;
                        }
                        if (key == KeyCode.Alpha1 || key == KeyCode.Keypad1)
                        {
                            SetSpeed(1);
                            continue;
                        }
                        if (key == KeyCode.Alpha2 || key == KeyCode.Keypad2)
                        {
                            SetSpeed(2);
                            continue;
                        }
                    }
                    game.HandleKey(key, shift);
                }
                else if (_screen is StartScreen start)
                {
                    start.HandleKey(key);
                }
            }
        }

        static readonly KeyCode[] WatchedKeys =
        {
            KeyCode.Space, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Escape, KeyCode.Tab, KeyCode.C,
            KeyCode.D, KeyCode.H, KeyCode.R, KeyCode.N, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Return, KeyCode.KeypadEnter,
        };

        // --- Screens ---------------------------------------------------------------------

        public void ShowStart()
        {
            Detach();
            Session.ShowStart();
            Swap(new StartScreen(this));
        }

        public void ShowGame()
        {
            Detach();
            Session.ShowGame();
            Swap(new GameScreen(this));
        }

        /// <summary>
        /// Takes the current screen down before the session changes screens. main.gd swaps the screen
        /// before it saves; here the save's Changed would otherwise reach the old screen, which could
        /// open a dialog and hold the clock on its way out.
        /// </summary>
        void Detach()
        {
            _tooltips.Hide();
            _screen?.RemoveFromHierarchy();
            _screen = null;
        }

        void Swap(VisualElement screen)
        {
            _tooltips.Hide();
            _screen?.RemoveFromHierarchy();
            _screen = screen;
            _root.Insert(0, screen);
            if (_rotate != null && Kit.IsVisible(_rotate)) _rotate.BringToFront();
        }

        // --- Interface size and orientation ----------------------------------------------

        /// <summary>The canvas size, in layout units, that a scale gives in the current window.</summary>
        Vector2 CanvasAt(float scale)
        {
            var pixels = new Vector2(Screen.width, Screen.height);
            if (pixels.x <= 0 || pixels.y <= 0) return BaseCanvas / scale;
            // Godot's canvas_items with "expand": the base fits the window, then the longer side grows.
            float fit = Mathf.Min(pixels.x / BaseCanvas.x, pixels.y / BaseCanvas.y) * scale;
            return pixels / fit;
        }

        /// <summary>
        /// Applies the chosen interface size and decides whether the canvas needs the compact layout.
        /// Returns true when the layout changed, so the caller can rebuild the screen.
        /// </summary>
        bool FitWindow()
        {
            float scale = Mathf.Clamp((float)Settings.UiScale, 1f, UiScales[UiScales.Length - 1]);
            // Never shrink the canvas below what the compact layout fits.
            scale = Mathf.Min(scale, Mathf.Max(1f, CanvasAt(1f).y / MinCanvasHeight));
            var pixels = new Vector2(Screen.width, Screen.height);
            _panelSettings.scale = Mathf.Min(pixels.x / BaseCanvas.x, pixels.y / BaseCanvas.y) * scale;
            Vector2 canvas = CanvasAt(scale);
            bool was = Kit.Compact;
            Kit.Compact = canvas.x < CompactSize.x || canvas.y < CompactSize.y;
            ShowRotateHint();
            return was != Kit.Compact;
        }

        /// <summary>The interface sizes that still leave room for the compact layout in this window.</summary>
        public float[] AvailableUiScales()
        {
            float largest = Mathf.Max(1f, CanvasAt(1f).y / MinCanvasHeight);
            return UiScales.Where(value => value <= largest + 0.001f).ToArray();
        }

        public void SetUiScale(float scale)
        {
            Session.SetUiScale(scale);
            FitWindow();
            RebuildScreen();
        }

        public void SetReducedMotion(bool reduced)
        {
            Session.SetReducedMotion(reduced);
            Kit.ReducedMotion = reduced;
        }

        /// <summary>Sizes are fixed when a screen is built, so a new layout rebuilds the current screen.</summary>
        void RebuildScreen()
        {
            if (_screen == null) return;
            if (_screen is GameScreen game)
            {
                string region = game.Selected;
                string dialog = game.Dialogs.Kind;
                // The open dialog's hold on the clock goes with the old screen; the new one reopens it.
                game.Dialogs.Close();
                var next = new GameScreen(this);
                Swap(next);
                next.schedule.Execute(() =>
                {
                    if (region != "") next.Select(region);
                    if (!string.IsNullOrEmpty(dialog)) next.OpenDialog(dialog);
                });
            }
            else
            {
                ShowStart();
            }
        }

        /// <summary>
        /// A phone held upright cannot show the chart at a readable size; ask for landscape. The
        /// upright canvas is very tall, so the hint is sized to the real screen width.
        /// </summary>
        void ShowRotateHint()
        {
            bool upright = Kit.Touch && Screen.width < Screen.height;
            if (!upright)
            {
                if (_rotate != null) Kit.SetVisible(_rotate, false);
                return;
            }
            if (_rotate == null)
            {
                _rotate = new VisualElement { name = "rotate" };
                Kit.Fill(_rotate);
                _rotate.style.backgroundColor = Palette.Desk;
                _rotate.style.justifyContent = Justify.Center;
                _rotate.style.alignItems = Align.Center;
                _root.Add(_rotate);
            }
            _rotate.Clear();
            float factor = Mathf.Max(1f, CanvasAt(_panelSettings.scale).x / 480f);
            Stack column = Kit.VBox(Mathf.RoundToInt(18 * factor));
            column.style.alignItems = Align.Center;
            var glyph = new RotateGlyph();
            glyph.style.width = glyph.style.height = 120 * factor;
            column.Add(glyph);
            column.Add(Kit.Label("Turn your phone sideways", Mathf.RoundToInt(34 * factor), Palette.Lamp, Fonts.Display));
            column.Add(Kit.Label("The chart needs the long side of the screen.", Mathf.RoundToInt(19 * factor), Palette.LampDim, Fonts.DisplayItalic));
            _rotate.Add(column);
            Kit.SetVisible(_rotate, true);
            _rotate.BringToFront();
            if (State != null && GameEngine.IsActive(State) && State.Speed != 0) SetSpeed(0);
        }

        /// <summary>A phone outline turning a quarter.</summary>
        sealed class RotateGlyph : VisualElement
        {
            public RotateGlyph()
            {
                generateVisualContent += context =>
                {
                    Painter2D p = context.painter2D;
                    float k = contentRect.width / 120f;
                    Vector2 c = contentRect.size * 0.5f;
                    Color ink = Palette.Brass;
                    var upright = new Rect(c.x - 22 * k, c.y - 38 * k, 44 * k, 76 * k);
                    Draw2D.StrokeRect(p, upright, Palette.Alpha(ink, 0.35f), 2f);
                    var turned = new Rect(c.x - 38 * k, c.y - 22 * k, 76 * k, 44 * k);
                    Draw2D.StrokeRect(p, turned, ink, 2.5f);
                    Draw2D.Arc(p, c, 54 * k, -Mathf.PI * 0.85f, -Mathf.PI * 0.6f, ink, 2f);
                    Vector2 tip = c + Draw2D.FromAngle(-Mathf.PI * 0.6f) * 54 * k;
                    Draw2D.Line(p, tip, tip + new Vector2(-9, -2) * k, ink, 2f);
                    Draw2D.Line(p, tip, tip + new Vector2(-2, 9) * k, ink, 2f);
                };
            }
        }

        /// <summary>Dialogs and the guide hold the clock while open.</summary>
        public void Hold() => Session.Hold();

        public void Release() => Session.Release();

        // --- Commands --------------------------------------------------------------------

        public void Begin(string difficulty, string seed)
        {
            if (_shot.Count == 0 && !Blocked && !GameStorage.IsWeb)
            {
                // Copies an unreadable save aside before a new run overwrites it.
                string raw = null;
                try { raw = Storage.LoadRaw(); } catch (System.IO.IOException) { }
                if (raw != null && SaveData.DecodeSave(raw) == null) Blocked = !Storage.RetainCopy(".recovery", raw);
            }
            Detach();
            Session.Begin(difficulty, seed);
            _lastStatus = State.Status;
            Swap(new GameScreen(this));
        }

        public void Abandon()
        {
            Detach();
            Session.Abandon();
            Swap(new StartScreen(this));
        }

        public void Place(string regionId) => Command(() => Session.Place(regionId));

        public void Act(string actionId, string regionId) => Command(() => Session.Act(actionId, regionId));

        public void ChooseDoctrine(string choice) => Command(() => Session.ChooseDoctrine(choice));

        public void Awaken() => Command(() => Session.Awaken());

        /// <summary>A player command: the act sound when the run changed, the refusal sound when it did not.</summary>
        void Command(Action run)
        {
            GameState before = State;
            _commanding = true;
            try
            {
                run();
            }
            finally
            {
                _commanding = false;
            }
            Sfx.Play(ReferenceEquals(before, State) ? "refuse" : "act");
            _lastStatus = State?.Status ?? "";
        }

        public void SetSpeed(int speed)
        {
            GameState before = State;
            Session.SetSpeed(speed);
            if (!ReferenceEquals(before, State)) Sfx.Play("click");
        }

        /// <summary>Playtest control: advances time without granting anything.</summary>
        public void DebugAdvance(int months) => Session.DebugAdvance(months);

        /// <summary>The month's news decides the sound: the ending, a threat, a ready site, an omen.</summary>
        void OnSessionEvents(List<GameEvent> entries)
        {
            if (!_commanding && State != null)
            {
                if (_lastStatus != State.Status && GameEngine.IsOver(State)) Sfx.Play("end");
                else if (entries.Any(e => Classify(e.Message) == "threat")) Sfx.Play("threat");
                else if (entries.Any(e => Classify(e.Message) == "site")) Sfx.Play("site");
                else if (entries.Any(e => Classify(e.Message) == "omen")) Sfx.Play("omen");
                _lastStatus = State.Status;
            }
            Events?.Invoke(entries);
        }

        public void SetAudio(bool muted, float volume)
        {
            Session.SetAudio(muted, volume);
            ApplyAudio();
        }

        /// <summary>Contextual tips are shown once per device.</summary>
        public bool HintSeen(string id) => Session.HintSeen(id);

        public void MarkHint(string id) => Session.MarkHint(id);

        public void CompleteTutorial() => Session.CompleteTutorial();

        void ApplyAudio()
        {
            Sfx.Muted = Settings.Muted;
            Sfx.Volume = (float)(Settings.Volume / 100.0);
        }

        // --- Playtest ---------------------------------------------------------------------

        public static CaptureState Capture(GameState s) => GameSession.Capture(s);

        public void SetParticipation(bool accepted) => Session.SetParticipation(accepted);

        /// <summary>The start screen asks once per round before the first run on this device.</summary>
        public bool NeedsParticipationChoice() => Session.NeedsParticipationChoice();

        public void DeclineParticipation() => Session.DeclineParticipation();

        public string PlaytestStatus() => Session.PlaytestStatus(_outbox?.Notice ?? "");

        public bool SubmitPlaytestComment(string text) => Session.SubmitPlaytestComment(text);

        public void ExportPlaytestRecord()
        {
            Session.LabTouch();
            Storage.Download(SaveData.EncodeSave(Session.Envelope()), "awakening-local-playtest.json");
        }

        public void ExportReport()
        {
            if (State == null) return;
            Storage.Download(ExportTelemetry(State), $"cthulhu-playtest-{State.Seed}-m{State.ElapsedMonths}.json");
        }

        /// <summary>The run summary export of godot/core/save_data.gd export_telemetry.</summary>
        static string ExportTelemetry(GameState state)
        {
            Dictionary<string, object> json = GameStateJson.ToJson(state);
            var regions = new Dictionary<string, object>();
            foreach (string id in GameData.RegionIds)
            {
                var r = (Dictionary<string, object>)((Dictionary<string, object>)json["regions"])[id];
                regions[id] = new Dictionary<string, object>
                {
                    ["population"] = r["population"], ["public"] = r["public"], ["exposure"] = r["exposure"], ["site"] = r["site"], ["threat"] = r["threat"],
                };
            }
            var report = new Dictionary<string, object>
            {
                ["ruleset"] = state.Ruleset, ["seed"] = state.Seed, ["difficulty"] = state.Difficulty, ["month"] = state.ElapsedMonths,
                ["status"] = state.Status, ["specialization"] = state.Specialization, ["resultReason"] = state.ResultReason,
                ["followers"] = Derived.TotalFollowers(state), ["readySites"] = Derived.ReadySites(state), ["finalProgress"] = state.FinalProgress,
                ["resources"] = new Dictionary<string, object> { ["influence"] = state.Influence, ["dread"] = state.Dread }, ["awareness"] = state.Awareness,
            };
            foreach (var pair in (Dictionary<string, object>)json["telemetry"]) report[pair.Key] = pair.Value;
            var log = (List<object>)json["log"];
            report["logCoverage"] = new Dictionary<string, object>
            {
                ["retained"] = log.Count, ["total"] = state.NextLogId - 1,
                ["firstMonth"] = state.Log.Count > 0 ? (object)state.Log[0].Month : null,
                ["lastMonth"] = state.Log.Count > 0 ? (object)state.Log[state.Log.Count - 1].Month : null,
                ["truncated"] = state.NextLogId - 1 > state.Log.Count,
            };
            report["regions"] = regions;
            report["log"] = log;
            return JsonWriter.Write(report);
        }

        static Dictionary<string, object> Spent(GameState before, GameState after) => new Dictionary<string, object>
        {
            ["influence"] = Math.Max(0, after.Telemetry.InfluenceSpent - before.Telemetry.InfluenceSpent),
            ["dread"] = Math.Max(0, after.Telemetry.DreadSpent - before.Telemetry.DreadSpent),
            ["followers"] = Math.Max(0, after.Telemetry.Sacrificed - before.Telemetry.Sacrificed),
        };

        /// <summary>Sorts world-event log lines for toasts and sound. Matches the engine's fixed message text.</summary>
        public static string Classify(string message)
        {
            foreach (string name in GameData.ThreatNames.Values)
                if (message.StartsWith(name, StringComparison.Ordinal)) return "threat";
            if (message.Contains(" loses ") && message.Contains(" followers to ")) return "loss";
            foreach (string name in GameData.OpportunityNames.Values)
                if (message.StartsWith(name, StringComparison.Ordinal)) return message.Contains(" passes in ") ? "faded" : "omen";
            if (message.Contains("is prepared")) return "site";
            if (message.Contains("lies dormant") || message.StartsWith("The world senses the coming alignment", StringComparison.Ordinal)) return "threat";
            if (message.StartsWith("The stars align", StringComparison.Ordinal) || message.StartsWith("The final rite begins", StringComparison.Ordinal)) return "phase";
            if (message.StartsWith("Three surviving", StringComparison.Ordinal) || message.StartsWith("The world knows", StringComparison.Ordinal)
                || message.StartsWith("The alignment has passed", StringComparison.Ordinal)) return "end";
            return "info";
        }

        // --- Screenshot harness (development builds) -------------------------------------
        // the-awakening.x86_64 -shot /tmp/a.png -scenario mid [-select europe] [-dialog menu]

        static Dictionary<string, string> ShotArgs()
        {
            var parsed = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i].StartsWith("--", StringComparison.Ordinal) && args[i].Length > 2) parsed[args[i].Substring(2)] = args[i + 1];
            return parsed.ContainsKey("shot") && (Debug.isDebugBuild || Application.isEditor) ? parsed : new Dictionary<string, string>();
        }

        internal static Dictionary<string, object> ShotLabOptions() => new Dictionary<string, object>
        {
            ["enabled"] = true, ["endpoint"] = "https://playtest.example.invalid/api/playtests/v1/batches/",
            ["collection_id"] = "10000000-0000-4000-8000-000000000001", ["build_id"] = "10000000-0000-4000-8000-000000000002",
            ["round_id"] = "10000000-0000-4000-8000-000000000003",
        };

        System.Collections.IEnumerator StageShot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_shot.ContainsKey("check-ui"))
            {
                yield return UiChecks.Run(this);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(_shot["shot"]);
                yield return null;
                yield return null;
                Application.Quit();
                yield break;
            }
#endif
            string scenario = _shot.TryGetValue("scenario", out string s) ? s : "start";
            TutorialComplete = scenario != "guide";
            Settings.Hints = _shot.ContainsKey("hints") ? new List<string>() : new List<string> { "first_cell", "spread", "threat", "omen", "sites", "doctrine", "awareness", "unsupported", "sacrifice" };
            switch (scenario)
            {
                case "start":
                    ShowStart();
                    break;
                case "consent":
                    Lab.Configure(ShotLabOptions());
                    Blocked = false;
                    ShowStart();
                    ((StartScreen)_screen).schedule.Execute(() => ((StartScreen)_screen).Begin());
                    break;
                case "continue":
                    State = Awakening.Core.Dev.Playthrough.Play("early", "rlyeh", 96);
                    ShowStart();
                    break;
                case "retired":
                    var retired = SaveData.DefaultSave();
                    retired.State = GameEngine.CreateGameState("normal", "retired");
                    retired.State.Ruleset = "alignment-2";
                    var decoded = SaveData.DecodeSave(SaveData.EncodeSave(retired));
                    StorageNotice = SaveData.TakeRetiredNotice(decoded);
                    State = decoded.State;
                    LastResult = decoded.LastResult;
                    ShowStart();
                    break;
                case "placing":
                case "guide":
                    State = GameEngine.CreateGameState("normal", "omen");
                    ShowGame();
                    break;
                case "early":
                    State = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", "omen"), "africa").State;
                    State = GameEngine.PerformAction(State, "recruit", "africa").State;
                    State = GameEngine.AdvanceSimulation(State, 14);
                    ShowGame();
                    break;
                case "awakening":
                    // This seed's run reaches the final rite; month 206 is six months into it.
                    State = Awakening.Core.Dev.Playthrough.Play("early", "resume", 206);
                    ShowGame();
                    break;
                default:
                    var months = new Dictionary<string, int> { ["mid"] = 96, ["late"] = 170, ["alignment"] = 200, ["won"] = Balance.Deadline, ["lost"] = Balance.Deadline };
                    // With this strategy the "resume" seed awakens Cthulhu and "rlyeh" runs out of time.
                    State = Awakening.Core.Dev.Playthrough.Play("early", scenario == "won" ? "resume" : "rlyeh", months.TryGetValue(scenario, out int m) ? m : 96);
                    ShowGame();
                    break;
            }
            if (_shot.ContainsKey("comment") && State != null && GameEngine.IsOver(State))
            {
                Lab.Configure(ShotLabOptions());
                Lab.Accept();
                Lab.StartRun(Capture(GameEngine.CreateGameState("normal", State.Seed)));
                Lab.EmitEvent("run_finished", new Dictionary<string, object>
                {
                    ["outcome"] = State.Status, ["reason"] = State.Status == "won" ? "awakened" : "deadline",
                }, Capture(State), "ended");
                ((GameScreen)_screen).OpenDialog("end");
            }
            yield return null;
            if (_screen is GameScreen game)
            {
                if (_shot.TryGetValue("select", out string region)) game.Select(region);
                if (_shot.TryGetValue("dialog", out string dialog))
                {
                    // --dialog none shows the screen without the dialog a phase opens by itself.
                    if (dialog == "none") game.Dialogs.Close();
                    else game.OpenDialog(dialog);
                }
            }
            for (int frame = 0; frame < 20; frame++) yield return null;
            if (_shot.ContainsKey("layout"))
                _screen.Query<Label>().ForEach(label => Debug.Log($"UI_LAYOUT {label.text} rect={label.worldBound} font={label.resolvedStyle.fontSize}"));
            float wait = _shot.TryGetValue("wait", out string w) ? float.Parse(w, System.Globalization.CultureInfo.InvariantCulture) : 0.6f;
            yield return new WaitForSecondsRealtime(wait);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(_shot["shot"]);
            yield return null;
            yield return null;
            Application.Quit();
        }
    }
}
