using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Playtest;
using Awakening.Core.Rules;
using Awakening.Core.Save;

namespace Awakening.Core.Session
{
    public enum SessionScreen { Start, Game }

    /// <summary>A platform visibility observation in the same monotonic milliseconds as the clock.</summary>
    public readonly struct SessionVisibilityChange
    {
        public readonly long At;
        public readonly bool Focused, Visible;
        public SessionVisibilityChange(long at, bool focused, bool visible)
        {
            At = at;
            Focused = focused;
            Visible = visible;
        }
    }

    /// <summary>A chronicle entry or a refused command. Presentation chooses its sound and toast.</summary>
    public sealed class GameEvent
    {
        public int Id, Month;
        public string Message;
        public bool Refused;
    }

    /// <summary>
    /// Engine-free main.gd: owns commands, save envelope, consent, capture and the four clock modes.
    /// Construct after platform loading with the decoded save, config, monotonic millisecond clock
    /// and persist(envelope) callback (scheduled generation, or -1 on failure). Capture may be injected
    /// for deterministic UUID/wall time; the constructor configures it and restores consent only
    /// when writable (blocked=false). The callback must serialize the envelope during the call.
    ///
    /// Unity integration: GameController forwards its commands/settings, ShowStart/ShowGame and
    /// dialog Hold/Release here. Update polls storage/outbox, then calls Process(unscaledDeltaTime,
    /// storage.Failed). OnApplicationFocus forwards to OnApplicationFocus(focused, IsWeb); the web
    /// drains timestamped GameStorage.VisibilityChanges through visibilityChanges, mapping each
    /// observation to SessionVisibilityChange. Use Storage.Now for the injected clock, and
    /// envelope => Storage.Persist(SaveData.EncodeSave(envelope)) for persistence. Outbox's storage
    /// adapter calls Commit; cancelPendingRequests cancels its transport on withdrawal. Recovery
    /// copies, audio, rendering, download and disposing storage/outbox remain in the controller.
    /// Subscribe Changed to redraw and Events to chronicle/toasts/audio. Screen is presentation
    /// state only: swapping Unity elements must call ShowStart/ShowGame, not mutate game rules.
    /// Public State/settings mirror the controller; callers replacing State must call LabTouch
    /// before and LabSync afterwards. Normal gameplay uses the command methods instead.
    /// </summary>
    public sealed class GameSession
    {
        public event Action Changed;
        public event Action<List<GameEvent>> Events;
        public GameState State;
        public SaveSettings Settings;
        public bool TutorialComplete;
        public Dictionary<string, object> LastResult;
        public bool Blocked;
        public string StorageNotice = "";
        public PlaytestCapture Lab { get; }
        public SessionScreen Screen { get; private set; } = SessionScreen.Start;
        public int Holds => _holds;
        public bool Focused => _labFocused;
        public bool Visible => _labVisible;

        readonly Func<long> _now;
        readonly Func<SaveEnvelope, long> _persist;
        readonly Func<IEnumerable<SessionVisibilityChange>> _visibilityChanges;
        readonly Action _cancelPendingRequests;
        readonly Func<string> _newSeed;
        readonly SaveEnvelope _save;
        long _labAt, _labSavedAt, _labCheckpointAt;
        string _labMode = "excluded";
        bool _labFocused = true, _labVisible = true;
        double _elapsed;
        int _holds;

        public GameSession(SaveEnvelope save, Dictionary<string, object> options,
            Func<long> monotonicMilliseconds, Func<SaveEnvelope, long> persist,
            PlaytestCapture lab = null, bool blocked = false,
            Func<IEnumerable<SessionVisibilityChange>> visibilityChanges = null,
            Action cancelPendingRequests = null, Func<string> newSeed = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _now = monotonicMilliseconds ?? throw new ArgumentNullException(nameof(monotonicMilliseconds));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            _visibilityChanges = visibilityChanges;
            _cancelPendingRequests = cancelPendingRequests;
            _newSeed = newSeed ?? (() => Guid.NewGuid().ToString("N").Substring(0, 8));
            State = save.State;
            Settings = SaveData.WithDefaultSettings(save.Settings);
            TutorialComplete = save.TutorialComplete;
            LastResult = save.LastResult;
            Blocked = blocked;
            Lab = lab ?? new PlaytestCapture();
            Lab.Configure(options ?? new Dictionary<string, object>(), save.Playtest ?? new Dictionary<string, object>());
            if (!Blocked) Lab.Restore();
            _labAt = _labSavedAt = _labCheckpointAt = _now();
        }

        /// <summary>Run once per frame after polling storage/outbox. Frame simulation caps at 0.5 s.</summary>
        public void Process(double delta, bool storageFailed = false)
        {
            if (storageFailed && !Blocked)
            {
                Blocked = true;
                StorageNotice = "Saving is unavailable. This session continues in memory.";
                Lab.StopCapture("Storage is unavailable. Capture and sending stopped; export the local record.");
                Changed?.Invoke();
            }
            LabTouch();
            if (Lab.Capturing())
            {
                long now = _now();
                if (now - _labCheckpointAt >= 20000) LabCheckpoint("periodic");
                if (now - _labSavedAt >= 5000) Commit();
            }
            if (State == null || _holds > 0 || Screen != SessionScreen.Game || !GameEngine.IsActive(State) || State.Speed == 0) return;
            _elapsed += Math.Min(delta, 0.5) * State.Speed / Balance.SecondsPerMonth;
            int months = (int)Math.Floor(_elapsed);
            if (months > 0)
            {
                _elapsed -= months;
                Advance(months);
            }
        }

        public void OnApplicationFocus(bool focused, bool web = false)
        {
            if (!web)
            {
                LabTouch();
                _labFocused = focused;
                LabSync();
                LabCheckpoint("focus_changed");
            }
            if (!focused && State != null && GameEngine.IsActive(State) && State.Speed != 0) SetSpeed(0);
        }

        public void ShowStart()
        {
            LabTouch();
            _holds = 0;
            Screen = SessionScreen.Start;
            LabSync();
        }

        public void ShowGame()
        {
            LabTouch();
            if (State != null) Lab.Resume(Capture(State));
            _holds = 0;
            _elapsed = 0;
            Screen = SessionScreen.Game;
            LabSync();
            Commit();
        }

        public void Hold()
        {
            LabTouch();
            _holds++;
            LabSync();
            LabCheckpoint("menu_changed");
        }

        public void Release()
        {
            LabTouch();
            _holds = Math.Max(0, _holds - 1);
            LabSync();
            LabCheckpoint("menu_changed");
        }

        public void Begin(string difficulty, string seed)
        {
            LabTouch();
            if (State != null && Lab.Capturing())
                Lab.EmitEvent("run_abandoned", new Dictionary<string, object> { ["reason"] = "replaced_by_new_run" }, Capture(State), "ended");
            // Godot strip_edges removes controls/ASCII spaces, but preserves Unicode seed characters.
            string chosen = PlaytestJson.StripEdges(seed ?? "");
            if (chosen.Length == 0) chosen = _newSeed();
            State = GameEngine.CreateGameState(difficulty, chosen);
            if (!Blocked) Lab.StartRun(Capture(State));
            _labAt = _now();
            LabSync();
            Commit();
            ShowGame();
        }

        public void Abandon()
        {
            LabTouch();
            if (State != null) Lab.EmitEvent("run_abandoned", new Dictionary<string, object> { ["reason"] = "explicit_abandon" }, Capture(State), "ended");
            State = null;
            Commit();
            ShowStart();
        }

        public void Place(string regionId)
        {
            LabTouch();
            Apply(GameEngine.PlaceStartingFoothold(State, regionId), "starting_region_selected", new Dictionary<string, object> { ["region"] = regionId });
        }

        public void Act(string actionId, string regionId)
        {
            LabTouch();
            Apply(GameEngine.PerformAction(State, actionId, regionId), "action_performed", new Dictionary<string, object> { ["action"] = actionId, ["region"] = regionId });
        }

        /// <summary>Take a talent of the in-run tree (alignment-5); replaces the doctrine.</summary>
        public void TakeTalent(string id)
        {
            LabTouch();
            var talent = id != null && GameData.Talents.TryGetValue(id, out TalentDefinition found) ? found : null;
            int slot = State == null ? 0 : State.Talents.Count + 1;
            Apply(GameEngine.TakeTalent(State, id), "talent_taken", new Dictionary<string, object>
            {
                ["talent"] = id, ["branch"] = talent?.Branch, ["tier"] = talent?.Tier ?? 0, ["slot"] = slot,
            });
        }

        public void Awaken()
        {
            LabTouch();
            Apply(GameEngine.StartAwakening(State), "awakening_started", new Dictionary<string, object>());
        }

        /// <summary>End the run as a loss once the final rite can no longer fit (GameEngine.CanConcede).</summary>
        public void Concede()
        {
            LabTouch();
            if (State == null || !GameEngine.CanConcede(State)) return;
            GameState before = State;
            State = GameEngine.Concede(State);
            LabTransition(before);
            LabSync();
            LastResult = SaveData.ResultFor(State);
            Commit();
            LogEntry last = State.Log[State.Log.Count - 1];
            Events?.Invoke(new List<GameEvent> { new GameEvent { Id = last.Id, Month = last.Month, Message = last.Message } });
        }

        public void SetSpeed(int speed)
        {
            LabTouch();
            GameState next = GameEngine.SetGameSpeed(State, speed);
            // Godot compares dictionaries by value: choosing the current speed is a no-op.
            if (ReferenceEquals(next, State) || next.Speed == State.Speed) return;
            GameState before = State;
            State = next;
            LabSpeedChanged(before);
            _elapsed = 0;
            LabSync();
            LabCheckpoint("menu_changed");
            Commit();
        }

        /// <summary>Playtest control: advances time without granting anything.</summary>
        public void DebugAdvance(int months)
        {
            LabTouch();
            if (State != null) Lab.EmitEvent("capture_gap", Gap(), Capture(State));
            Advance(months);
        }

        static Dictionary<string, object> Gap() => new Dictionary<string, object> { ["reason"] = "unknown_time", ["missing_events"] = null };

        public void Advance(int months)
        {
            LabTouch();
            GameState before = State;
            // Observe each month: a long advance can both spawn and expire a threat.
            // Preserve the engine's stop at alignment even though it is still active.
            for (int month = 0; month < Math.Min(months, Balance.Deadline); month++)
            {
                GameState previous = State;
                State = GameEngine.AdvanceSimulation(State, 1);
                if (ReferenceEquals(State, previous)) break;
                LabTransition(previous);
                if (GameEngine.IsOver(State) || previous.Status == "playing" && State.Status == "alignment") break;
            }
            if (ReferenceEquals(State, before)) return;
            LabSync();
            var fresh = State.Log.Where(e => e.Id >= before.NextLogId).Select(e => new GameEvent { Id = e.Id, Month = e.Month, Message = e.Message }).ToList();
            if (before.Status != State.Status && GameEngine.IsOver(State))
            {
                LastResult = SaveData.ResultFor(State);
            }
            Commit();
            Events?.Invoke(fresh);
        }

        void Apply(CommandResult result, string kind, Dictionary<string, object> payload)
        {
            if (result.Ok)
            {
                GameState before = State;
                State = result.State;
                if (kind != "starting_region_selected") payload["spent"] = Spent(before, State);
                Lab.EmitEvent(kind, payload, Capture(State));
                LabTransition(before, kind == "action_performed" ? (string)payload["action"] : null);
                LabSync();
                Commit();
                LogEntry last = State.Log[State.Log.Count - 1];
                Events?.Invoke(new List<GameEvent> { new GameEvent { Id = last.Id, Month = last.Month, Message = last.Message } });
            }
            else
            {
                Events?.Invoke(new List<GameEvent> { new GameEvent { Id = -1, Month = State.ElapsedMonths, Message = result.Message, Refused = true } });
            }
        }

        public void SetAudio(bool muted, double volume)
        {
            Settings.Muted = muted;
            Settings.Volume = volume;
            Commit();
        }

        public void SetUiScale(double scale) { Settings.UiScale = scale; Commit(); }
        public void SetReducedMotion(bool reduced) { Settings.ReducedMotion = reduced; Commit(); }
        public bool HintSeen(string id) => Settings.Hints.Contains(id);
        public void MarkHint(string id)
        {
            if (HintSeen(id)) return;
            Settings.Hints.Add(id);
            Commit();
        }

        public void CompleteTutorial()
        {
            LabTouch();
            if (State != null && !TutorialComplete) Lab.Checkpoint("tutorial_completed", null, Capture(State));
            TutorialComplete = true;
            Commit();
        }

        /// <summary>Persist the entire envelope; a failed write stops capture and further writes.</summary>
        public long Commit()
        {
            long generation = -1;
            if (!Blocked)
            {
                generation = _persist(Envelope());
                _labSavedAt = _now();
                if (generation < 0)
                {
                    Blocked = true;
                    StorageNotice = "Saving is unavailable. This session continues in memory.";
                    Lab.StopCapture("Saving is unavailable. Capture stopped; export the local record.");
                }
            }
            Changed?.Invoke();
            return generation;
        }

        /// <summary>Preserve unknown save fields. Serialize immediately; this is the owned envelope.</summary>
        public SaveEnvelope Envelope()
        {
            _save.Version = 2;
            _save.State = State;
            _save.Settings = Settings;
            _save.TutorialComplete = TutorialComplete;
            _save.LastResult = LastResult;
            _save.Playtest = Lab.Snapshot(State == null ? null : Capture(State));
            return _save;
        }

        public bool SubmitPlaytestComment(string text)
        {
            if (!Lab.QueueComment(text)) return false;
            return Commit() >= 0;
        }

        public static CaptureState Capture(GameState s) => new CaptureState
        {
            Seed = s.Seed, Difficulty = s.Difficulty, Ruleset = s.Ruleset, Status = s.Status,
            ElapsedMonths = s.ElapsedMonths, NextLogId = s.NextLogId, IsOver = GameEngine.IsOver(s),
        };

        public void SetParticipation(bool accepted)
        {
            LabTouch();
            if (accepted)
            {
                if (Blocked || !Lab.Accept()) return;
                if (State != null && Screen == SessionScreen.Game) Lab.Resume(Capture(State));
            }
            else
            {
                _cancelPendingRequests?.Invoke();
                Lab.Withdraw();
            }
            _labAt = _now();
            LabSync();
            Commit();
        }

        /// <summary>The start screen asks once per round before the first run on this device.</summary>
        public bool NeedsParticipationChoice() => Lab.Configured() && !Blocked && !Lab.Corrupt && !Lab.Confirmed && !Lab.Answered();

        public void DeclineParticipation()
        {
            Lab.Decline();
            Commit();
        }

        public string PlaytestStatus(string outboxNotice = "")
        {
            if (!string.IsNullOrEmpty(Lab.Warning)) return Lab.Warning;
            if (!Lab.Configured()) return "Playtest collection is not configured in this build.";
            if (Blocked) return StorageNotice;
            if (!Lab.Confirmed) return "Not sharing. Participation is optional; any pending records stay on this device until you join.";
            if (Lab.PendingEvents() > 0) return $"{Lab.PendingEvents()} records pending. {outboxNotice}";
            return Lab.Data.TryGetValue("receipt", out object receipt) && receipt is Dictionary<string, object> confirmed && confirmed.Count > 0
                ? "Receipt confirmed and saved locally." : "Participation enabled. No receipt yet.";
        }

        public static Dictionary<string, object> Spent(GameState before, GameState after) => new Dictionary<string, object>
        {
            ["influence"] = Math.Max(0, after.Telemetry.InfluenceSpent - before.Telemetry.InfluenceSpent),
            ["dread"] = Math.Max(0, after.Telemetry.DreadSpent - before.Telemetry.DreadSpent),
            ["followers"] = Math.Max(0, after.Telemetry.Sacrificed - before.Telemetry.Sacrificed),
        };

        public string LabCurrentMode()
        {
            if (!_labVisible || !_labFocused) return "background";
            if (State == null || Screen != SessionScreen.Game || GameEngine.IsOver(State)) return "excluded";
            return _holds == 0 && GameEngine.IsActive(State) && State.Speed != 0 ? "simulation" : "planning";
        }

        public void LabSync() => _labMode = LabCurrentMode();

        void LabAccount(long at)
        {
            if (at < _labAt) return;
            long milliseconds = at - _labAt;
            if (milliseconds > 30000 && _labMode != "background" && Lab.Capturing())
            {
                Lab.Tick((int)Math.Min(int.MaxValue, milliseconds), "excluded");
                Lab.EmitEvent("capture_gap", Gap(), Capture(State));
            }
            else
            {
                Lab.Tick((int)Math.Min(int.MaxValue, milliseconds), _labMode);
            }
            _labAt = at;
        }

        /// <summary>Account observed time before changing a command, screen, hold or focus.</summary>
        public void LabTouch()
        {
            if (_visibilityChanges != null)
                foreach (SessionVisibilityChange entry in _visibilityChanges())
                {
                    LabAccount(entry.At);
                    bool visibilityChanged = _labVisible != entry.Visible;
                    _labFocused = entry.Focused;
                    _labVisible = entry.Visible;
                    LabSync();
                    if (State != null)
                        Lab.EmitEvent("time_checkpoint", new Dictionary<string, object> { ["reason"] = visibilityChanged ? "visibility_changed" : "focus_changed" }, Capture(State));
                }
            LabAccount(_now());
            LabSync();
        }

        void LabCheckpoint(string reason)
        {
            if (State == null || !Lab.Capturing()) return;
            Lab.EmitEvent("time_checkpoint", new Dictionary<string, object> { ["reason"] = reason }, Capture(State));
            _labCheckpointAt = _now();
            Commit();
        }

        string ObservationPhase(GameState before) => GameEngine.IsOver(State) ? before.Status : State.Status;

        void LabSpeedChanged(GameState before)
        {
            if (before.Speed != State.Speed)
                Lab.EmitEvent("speed_changed", new Dictionary<string, object> { ["previous"] = before.Speed, ["new"] = State.Speed }, Capture(State), ObservationPhase(before));
        }

        static bool SameThreat(Threat a, Threat b) => a == null ? b == null : b != null && a.Kind == b.Kind && a.Severity == b.Severity && a.DueMonth == b.DueMonth;

        void LabThreats(GameState before, string response)
        {
            foreach (string id in GameData.RegionIds)
            {
                RegionState previous = before.Regions[id], current = State.Regions[id];
                if (SameThreat(previous.Threat, current.Threat)) continue;
                if (previous.Threat != null)
                {
                    string answer = response == "counter" || response == "scatter" || response == "silence" ? response : "ignored";
                    var legal = new List<object>();
                    foreach (string action in new[] { "counter", "scatter", "silence" })
                        if (GameEngine.PreviewAction(before, action, id).Ok) legal.Add(action);
                    var consequences = Threats.Consequences(before, previous);
                    bool ignored = answer == "ignored";
                    var data = ThreatData(id, previous.Threat);
                    data["response"] = answer;
                    data["legal_responses"] = legal;
                    // Loss counters exclude recruitment in this tick; preparation uses the
                    // consequence because upkeep/progress also changes during the month.
                    data["losses"] = new Dictionary<string, object>
                    {
                        ["followers"] = ignored ? Math.Max(0, current.Population.Losses - previous.Population.Losses) : 0,
                        ["preparation_months"] = ignored ? (consequences.DestroysCell && previous.Site.Invested ? previous.Site.Progress : consequences.PreparationLost) : 0,
                        ["income_per_month"] = ignored ? consequences.IncomeLost : 0,
                        ["cells"] = ignored && consequences.DestroysCell ? 1 : 0,
                        ["sites"] = ignored && consequences.DestroysCell && previous.Site.Invested ? 1 : 0,
                    };
                    Lab.EmitEvent("threat_resolved", data, Capture(State), ObservationPhase(before));
                }
                if (current.Threat != null)
                    Lab.EmitEvent("threat_spawned", ThreatData(id, current.Threat), Capture(State), ObservationPhase(before));
            }
        }

        static Dictionary<string, object> ThreatData(string region, Threat threat) => new Dictionary<string, object>
        {
            ["region"] = region, ["kind"] = threat.Kind, ["severity"] = threat.Severity, ["due_month"] = threat.DueMonth,
        };

        void LabTransition(GameState before, string response = null)
        {
            if (before.Status == "playing" && State.Status == "alignment")
                Lab.EmitEvent("phase_changed", new Dictionary<string, object> { ["from_phase"] = "playing" }, Capture(State));
            LabThreats(before, response);
            if (State.ElapsedMonths != before.ElapsedMonths && State.ElapsedMonths % 12 == 0)
                Lab.EmitEvent("month_snapshot", new Dictionary<string, object>
                {
                    ["awareness"] = State.Awareness, ["influence"] = State.Influence, ["dread"] = State.Dread,
                    ["followers"] = Derived.TotalFollowers(State),
                    ["cells"] = State.Regions.Values.Count(r => r.Population.Followers > 0),
                    ["funded_sites"] = State.Regions.Values.Count(r => r.Site.Invested),
                    ["ready_sites"] = Derived.ReadySites(State),
                }, Capture(State), ObservationPhase(before));
            LabSpeedChanged(before);
            if (GameEngine.IsOver(State))
            {
                Lab.EmitEvent("run_finished", new Dictionary<string, object>
                {
                    ["outcome"] = State.Status,
                    ["reason"] = State.ResultCode ?? (State.Status == "won" ? "awakened" : State.Awareness >= 100 ? "world_awareness" : "deadline"),
                }, Capture(State));
                return;
            }
            foreach (string id in GameData.RegionIds)
            {
                RegionState site = State.Regions[id];
                RegionState previous = before.Regions[id];
                if (site.Site.Progress >= Balance.SiteMonths && Derived.Supported(site) && (previous.Site.Progress < Balance.SiteMonths || !Derived.Supported(previous)))
                    Lab.Checkpoint("first_site_ready", id, Capture(State));
            }
        }
    }
}
