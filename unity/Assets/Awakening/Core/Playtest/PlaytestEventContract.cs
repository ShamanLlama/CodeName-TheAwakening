using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Awakening.Core.Playtest.PlaytestJson;

namespace Awakening.Core.Playtest
{
    // Event-level awakening-6 schema and semantics. Batch/stream ordering is checked by GTC.
    internal static class PlaytestEventContract
    {
        static readonly string[] Regions = { "north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania" };
        static readonly string[] Actions = { "recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "scatter", "silence", "exploit", "convert", "reveal_figure", "bury", "restore" };
        static readonly string[] Responses = { "counter", "scatter", "silence" };
        static readonly Regex TalentId = new Regex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant);
        static bool In(object value, params string[] options) => value is string text && Array.IndexOf(options, text) >= 0;
        static bool Keys(Dictionary<string, object> map, params string[] keys) => map.Count == keys.Length && keys.All(map.ContainsKey);
        static bool PositiveInteger(object value, int max) => Integer(value, max) && Convert.ToDouble(value) >= 1;
        static bool Spent(object value) => value is Dictionary<string, object> map && Keys(map, "influence", "dread", "followers") && map.Values.All(v => Number(v, 1e9));
        static bool Legal(object value) => value is List<object> list && list.Count <= 3 && list.Distinct().Count() == list.Count && list.All(v => In(v, Responses));
        static bool Threat(Dictionary<string, object> data) => In(Get(data, "region"), Regions) && In(Get(data, "kind"), "raid", "investigation", "occult") && PositiveInteger(Get(data, "severity"), 3) && Integer(Get(data, "due_month"), 240) && Legal(Get(data, "legal_responses"));

        static bool Required(object value) => Integer(value, 12) && (Convert.ToDouble(value) == 8 || Convert.ToDouble(value) == 12);
        static bool Blocker(object value) => value is Dictionary<string, object> map && Keys(map, "region", "reason") && In(Get(map, "region"), Regions) && In(Get(map, "reason"), "scattered", "unsupported", "dormant", "destroyed", "unprepared");

        internal static bool Valid(Dictionary<string, object> entry)
        {
            if (!Keys(entry, "event_id", "sequence", "type", "phase", "game_month", "clock", "data")) return false;
            try { if (Bytes(entry) > 2048) return false; }
            catch (ArgumentException) { return false; } // Non-finite numbers cannot be JSON.
            var data = Obj(entry, "data");
            var clock = Obj(entry, "clock");
            if (!Keys(clock, PlaytestCapture.ClockKeys)) return false;
            string kind = Str(entry, "type"), phase = Str(entry, "phase");
            double month = Num(entry, "game_month");
            bool playing = In(phase, "playing", "alignment", "awakening");
            switch (kind)
            {
                case "run_started": case "capture_started":
                    return Keys(data, "difficulty", "seed", "ruleset") && In(Get(data, "difficulty"), "easy", "normal", "hard") && Get(data, "seed") is string seed && Characters(seed) >= 1 && Characters(seed) <= 64 && Equals(Get(data, "ruleset"), "alignment-7")
                        && Num(entry, "sequence") == 1 && Num(clock, "elapsed_ms") == 0 && (kind != "run_started" || phase == "placing" && month == 0);
                case "session_started":
                    return Keys(data, "mode") && In(Get(data, "mode"), "new", "resume", "partial") && Num(clock, "elapsed_ms") == 0;
                case "starting_region_selected":
                    return Keys(data, "region") && In(Get(data, "region"), Regions) && phase == "playing" && month == 0;
                case "action_performed":
                    bool sacrifice = phase == "awakening" && Equals(Get(data, "action"), "sacrifice");
                    bool madnessAction = In(Get(data, "action"), "manifest", "sacrifice");
                    var actionKeys = new List<string> { "action", "region", "spent" };
                    if (sacrifice) actionKeys.Add("rite_advanced");
                    if (madnessAction) actionKeys.Add("madness_after");
                    return playing && Keys(data, actionKeys.ToArray()) && (!sacrifice || Get(data, "rite_advanced") is bool) && (!madnessAction || Number(Get(data, "madness_after"), 100)) && In(Get(data, "action"), Actions) && In(Get(data, "region"), Regions) && Spent(Get(data, "spent"));
                case "talent_taken":
                    return playing && Keys(data, "talent", "branch", "tier", "slot", "spent") && Get(data, "talent") is string talent && TalentId.IsMatch(talent) && In(Get(data, "branch"), "congregation", "hidden_hand", "harbinger") && PositiveInteger(Get(data, "tier"), 3) && PositiveInteger(Get(data, "slot"), 6) && Spent(Get(data, "spent"));
                case "awakening_started":
                    return phase == "awakening" && Keys(data, "spent", "required") && Required(Get(data, "required")) && Spent(Get(data, "spent"));
                case "phase_changed":
                    return phase == "alignment" && Keys(data, "from_phase") && Equals(Get(data, "from_phase"), "playing");
                case "checkpoint_reached":
                    return Keys(data, "checkpoint", "region") && In(Get(data, "checkpoint"), "tutorial_completed", "first_site_ready") && (Equals(Get(data, "checkpoint"), "first_site_ready") ? In(Get(data, "region"), Regions) : Get(data, "region") == null);
                case "time_checkpoint":
                    return Keys(data, "reason") && In(Get(data, "reason"), "periodic", "focus_changed", "visibility_changed", "menu_changed", "before_save");
                case "capture_gap":
                    return Keys(data, "reason", "missing_events") && In(Get(data, "reason"), "queue_full", "storage_unavailable", "session_conflict", "unknown_time", "expired_queue") && (Get(data, "missing_events") == null || Integer(Get(data, "missing_events"), 20000));
                case "run_finished":
                    return phase == "ended" && Keys(data, "outcome", "reason") && In(Get(data, "outcome"), "won", "lost") && In(Get(data, "reason"), "awakened", "world_awareness", "deadline", "conceded") && (Equals(Get(data, "outcome"), "won") == Equals(Get(data, "reason"), "awakened")) && (!Equals(Get(data, "reason"), "deadline") || month == 224);
                case "run_abandoned":
                    return phase == "ended" && Keys(data, "reason") && In(Get(data, "reason"), "explicit_abandon", "replaced_by_new_run");
                case "threat_spawned":
                    return playing && Keys(data, "region", "kind", "severity", "due_month", "legal_responses", "cornered") && Threat(data) && Num(data, "due_month") > month && Get(data, "cornered") is bool cornered && cornered == !Arr(data, "legal_responses").Any(v => In(v, "counter", "silence"));
                case "threat_resolved":
                    if (!playing || !Keys(data, "region", "kind", "severity", "due_month", "legal_responses", "response", "losses", "grace_months") || !Threat(data) || !In(Get(data, "response"), "counter", "scatter", "silence", "forgotten", "ignored") || !(Get(data, "losses") is Dictionary<string, object> losses)) return false;
                    if (!Integer(Get(data, "grace_months"), 24) || (!In(data["response"], "counter", "silence") && Num(data, "grace_months") != 0)) return false;
                    if (!Keys(losses, "followers", "preparation_months", "income_per_month", "cells", "sites") || !Number(Get(losses, "followers"), 1e9) || !Number(Get(losses, "income_per_month"), 1e9) || !Integer(Get(losses, "preparation_months"), 48) || !Integer(Get(losses, "cells"), 1) || !Integer(Get(losses, "sites"), 1)) return false;
                    return Equals(data["response"], "ignored") ? Num(data, "due_month") <= month : (Equals(data["response"], "forgotten") || Arr(data, "legal_responses").Contains(data["response"])) && losses.Values.All(v => Convert.ToDouble(v) == 0);
                case "month_snapshot":
                    return playing && month > 0 && month % 12 == 0 && Keys(data, "awareness", "influence", "dread", "followers", "cells", "funded_sites", "ready_sites", "talents", "unused_slots", "bury_uses", "yields", "madness", "madness_dread", "quickened_sites") && Number(Get(data, "awareness"), 100) && new[] { "influence", "dread", "followers" }.All(k => Number(Get(data, k), 1e9)) && new[] { "cells", "funded_sites", "ready_sites" }.All(k => Integer(Get(data, k), 8)) && Num(data, "ready_sites") <= Num(data, "funded_sites") && Num(data, "funded_sites") <= Num(data, "cells") && Integer(Get(data, "talents"), 6) && Integer(Get(data, "unused_slots"), 6) && Num(data, "talents") + Num(data, "unused_slots") <= 6 && Integer(Get(data, "bury_uses"), 20000) && Get(data, "yields") is Dictionary<string, object> yields && yields.Count == Num(data, "talents") && yields.All(pair => TalentId.IsMatch(pair.Key) && Number(pair.Value, 1e9))
                        && Get(data, "madness") is Dictionary<string, object> madness && madness.All(pair => In(pair.Key, Regions) && Number(pair.Value, 100)) && Number(Get(data, "madness_dread"), double.MaxValue) && Integer(Get(data, "quickened_sites"), 8);
                case "capstone_invoked":
                    return playing && Keys(data, "talent", "awareness_before", "awareness_after", "threats_cancelled") && Get(data, "talent") is string capstone && TalentId.IsMatch(capstone) && Number(Get(data, "awareness_before"), 100) && Number(Get(data, "awareness_after"), 100) && Integer(Get(data, "threats_cancelled"), 8);
                case "rite_month":
                    return phase == "awakening" && month > 0 && Keys(data, "progress", "required", "advancing", "blockers") && Required(Get(data, "required")) && Integer(Get(data, "progress"), 12) && Num(data, "progress") <= Num(data, "required") && Get(data, "advancing") is bool && Get(data, "blockers") is List<object> blockers && blockers.Count <= 8 && blockers.All(Blocker) && blockers.Cast<Dictionary<string, object>>().Select(b => b["region"]).Distinct().Count() == blockers.Count;
                case "speed_changed":
                    return Keys(data, "previous", "new") && Integer(Get(data, "previous"), 2) && Integer(Get(data, "new"), 2) && Num(data, "previous") != Num(data, "new");
                default: return false;
            }
        }
    }
}
