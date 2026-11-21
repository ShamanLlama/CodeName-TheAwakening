using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Json;
using Awakening.Core.Rules;

namespace Awakening.Core.Save
{
    public sealed class SaveSettings
    {
        public bool Muted = true;
        public double Volume = 40;
        public double UiScale = 1.0;
        public bool ReducedMotion;
        public bool HideGoals;
        public List<string> Hints = new List<string>();
        internal Dictionary<string, object> Extra = new Dictionary<string, object>();
    }
    public sealed class SaveEnvelope
    {
        public int Version = 2;
        public GameState State;
        public SaveSettings Settings = new SaveSettings();
        public bool TutorialComplete;
        public Dictionary<string, object> LastResult;
        // Opaque capture-owned JSON: no schema conversion or filtering here.
        public object Playtest;
        public string RetiredRuleset;
        internal Dictionary<string, object> Extra = new Dictionary<string, object>();
        internal string RawPlaytest, PlaytestAtDecode;
    }

    /// <summary>Pure save-v2 codec and validation, ported from godot/core/save_data.gd.</summary>
    public static class SaveData
    {
        public const int SaveVersion = 2;
        public static readonly string[] RetiredRulesets = { "alignment-1", "alignment-2", "alignment-3" };
        public static readonly string[] ResultCodes = { "awakened", "world_awareness", "deadline", "conceded" };
        public const string RetiredNotice = "Your unfinished run used older rules (%s) and cannot continue under %s. Settings and playtest records are kept; start a new run.";
        public static SaveEnvelope DefaultSave() => new SaveEnvelope();
        static Dictionary<string, object> Record(object value) => value as Dictionary<string, object>;
        static object Get(Dictionary<string, object> value, string key) => value != null && value.TryGetValue(key, out var entry) ? entry : null;
        static double N(object value) => Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        static bool Number(object value, double maximum = 9007199254740991.0)
        {
            if (!(value is double || value is int || value is uint || value is long)) return false;
            double n = N(value);
            return !double.IsNaN(n) && !double.IsInfinity(n) && n >= 0 && n <= maximum;
        }
        static bool Integer(object value, double maximum = 9007199254740991.0) => Number(value, maximum) && N(value) == Math.Floor(N(value));
        static bool OneOf(object value, string[] choices) => value is string s && choices.Contains(s);
        static bool SettingsValid(object value)
        {
            var v = Record(value);
            if (v == null || !(Get(v, "muted") is bool) || !Number(Get(v, "volume"), 100)) return false;
            if (v.ContainsKey("uiScale") && !Number(v["uiScale"], 4)) return false;
            if (v.ContainsKey("reducedMotion") && !(v["reducedMotion"] is bool)) return false;
            if (v.ContainsKey("hideGoals") && !(v["hideGoals"] is bool)) return false;
            if (v.ContainsKey("hints") && (!(v["hints"] is List<object> hints) || hints.Any(h => !(h is string)))) return false;
            return true;
        }
        public static SaveSettings WithDefaultSettings(Dictionary<string, object> value)
        {
            return new SaveSettings
            {
                Muted = (bool)value["muted"], Volume = N(value["volume"]),
                UiScale = value.ContainsKey("uiScale") ? N(value["uiScale"]) : 1.0,
                ReducedMotion = value.ContainsKey("reducedMotion") && (bool)value["reducedMotion"],
                HideGoals = value.ContainsKey("hideGoals") && (bool)value["hideGoals"],
                Hints = value.ContainsKey("hints") ? ((List<object>)value["hints"]).Cast<string>().ToList() : new List<string>(),
                Extra = new Dictionary<string, object>(value)
            };
        }
        public static SaveSettings WithDefaultSettings(SaveSettings value) => new SaveSettings
        {
            Muted = value.Muted, Volume = value.Volume, UiScale = value.UiScale, ReducedMotion = value.ReducedMotion,
            HideGoals = value.HideGoals,
            Hints = new List<string>(value.Hints), Extra = new Dictionary<string, object>(value.Extra)
        };
        public static bool IsGameState(object value)
        {
            if (value is GameState state) value = GameStateJson.ToJson(state);
            var v = Record(value);
            if (v == null || !Number(Get(v, "version"), 2) || N(v["version"]) != 2 || !Equals(Get(v, "ruleset"), Balance.Ruleset) || !(Get(v, "seed") is string)
                || !Integer(Get(v, "rngState"), 4294967295) || !Integer(Get(v, "elapsedMonths"), Balance.Deadline)
                || !OneOf(Get(v, "status"), GameData.Statuses) || !OneOf(Get(v, "difficulty"), GameData.DifficultyIds)
                || !Integer(Get(v, "speed"), 2) || !Number(Get(v, "influence"), Balance.InfluenceCap)
                || !Number(Get(v, "dread"), Balance.DreadCap) || !Number(Get(v, "awareness"), 100)
                || Get(v, "specialization") != null && !OneOf(v["specialization"], GameData.SpecializationIds)
                || !Integer(Get(v, "finalProgress"), Balance.FinalMonths) || !Integer(Get(v, "nextLogId"))
                || Get(v, "resultReason") != null && !(v["resultReason"] is string) || Record(Get(v, "regions")) == null
                || Get(v, "worldConcealReadyAt") != null && !Integer(v["worldConcealReadyAt"], Balance.Deadline + Balance.ConcealWorldMonths)
                || Get(v, "resultCode") != null && !OneOf(v["resultCode"], ResultCodes)) return false;
            string status = (string)v["status"];
            double month = N(v["elapsedMonths"]);
            object code = Get(v, "resultCode");
            if (code != null && (status == "won") != Equals(code, "awakened") || code != null && status != "won" && status != "lost") return false;
            bool conceded = Equals(code, "conceded");
            if (status == "placing" && month != 0 || status == "playing" && month >= Balance.PreparationMonths
                || new[] { "alignment", "awakening", "won" }.Contains(status) && month < Balance.PreparationMonths
                || status == "won" && N(v["finalProgress"]) != Balance.FinalMonths
                || status == "lost" && month != Balance.Deadline && N(v["awareness"]) < 100 && !conceded
                || new[] { "won", "lost" }.Contains(status) && N(v["speed"]) != 0) return false;
            foreach (string id in GameData.RegionIds)
            {
                var r = Record(Get(Record(v["regions"]), id));
                var site = Record(Get(r, "site"));
                if (r == null || Record(Get(r, "population")) == null || !(Get(r, "public") is bool) || !Number(Get(r, "exposure"), 100)
                    || !(Get(r, "revealed") is bool) || !(Get(r, "scattered") is bool) || !Integer(Get(r, "scatteredUntil"), Balance.Deadline + Balance.ScatterMonths)
                    || (bool)r["scattered"] != (month < N(r["scatteredUntil"]))
                    || !Number(Get(r, "madness"), 100) || !Integer(Get(r, "backlashUntil")) || Record(Get(r, "cooldowns")) == null || site == null
                    || !(Get(site, "invested") is bool) || !Integer(Get(site, "progress"), Balance.SiteMonths)
                    || !(Get(site, "dormant") is bool) || !(bool)site["invested"] && (N(site["progress"]) != 0 || (bool)site["dormant"])) return false;
                var population = Record(r["population"]);
                double sum = 0.0;
                foreach (string key in new[] { "unaware", "deniers", "believers", "followers", "losses" })
                {
                    if (!Number(Get(population, key))) return false;
                    sum += N(population[key]);
                }
                if (Math.Abs(sum - GameData.Regions[id].PopulationWeight * Balance.PopulationScale) > 0.00001) return false;
                foreach (var entry in Record(r["cooldowns"]))
                    if (!GameData.ActionIds.Contains(entry.Key) || !Integer(entry.Value)) return false;
                object threat = Get(r, "threat");
                var t = Record(threat);
                if (threat != null && (t == null || !OneOf(Get(t, "kind"), GameData.ThreatKinds)
                    || !Integer(Get(t, "dueMonth")) || N(t["dueMonth"]) <= month || !Integer(Get(t, "severity"), 3) || N(t["severity"]) < 1)) return false;
                object opportunity = Get(r, "opportunity");
                var o = Record(opportunity);
                if (opportunity != null && (o == null || !OneOf(Get(o, "kind"), GameData.OpportunityKinds)
                    || !Integer(Get(o, "expiresMonth")) || N(o["expiresMonth"]) <= month)) return false;
                object figure = Get(r, "figure");
                var f = Record(figure);
                if (figure != null && (f == null || !OneOf(Get(f, "kind"), new[] { "celebrity", "politician" }) || !(Get(f, "revealed") is bool))) return false;
            }
            if (!(Get(v, "log") is List<object> log) || log.Count > 100) return false;
            double lastId = 0.0;
            foreach (object item in log)
            {
                var entry = Record(item);
                if (entry == null || !Integer(Get(entry, "id")) || N(entry["id"]) <= lastId || N(entry["id"]) >= N(v["nextLogId"])
                    || !Integer(Get(entry, "month"), month) || !(Get(entry, "message") is string)) return false;
                lastId = N(entry["id"]);
            }
            var telemetry = Record(Get(v, "telemetry"));
            if (telemetry == null || Record(Get(telemetry, "actions")) == null || Record(Get(telemetry, "revelations")) == null) return false;
            foreach (string key in new[] { "sacrificed", "raidLosses", "influenceEarned", "influenceSpent", "dreadEarned", "dreadSpent", "upkeepPaid", "threatsCountered" })
                if (!Number(Get(telemetry, key))) return false;
            foreach (var entry in Record(telemetry["actions"]))
                if (!GameData.ActionIds.Contains(entry.Key) || !Integer(entry.Value)) return false;
            foreach (var entry in Record(telemetry["revelations"]))
                if (!GameData.RegionIds.Contains(entry.Key) || !Integer(entry.Value, month)) return false;
            return true;
        }
        public static SaveEnvelope DecodeSave(string raw)
        {
            if (!JsonReader.TryParse(raw, out object parsed)) return null;
            var v = Record(parsed);
            string retired = "";
            var state = Record(Get(v, "state"));
            if (state != null && OneOf(Get(state, "ruleset"), RetiredRulesets))
            {
                retired = (string)state["ruleset"];
                v["state"] = null;
            }
            if (v == null || !Number(Get(v, "version"), 2) || N(v["version"]) != 2 || !SettingsValid(Get(v, "settings")) || !(Get(v, "tutorialComplete") is bool)
                || Get(v, "state") != null && !IsGameState(v["state"])) return null;
            object lastResult = Get(v, "lastResult");
            var r = Record(lastResult);
            if (lastResult != null && (r == null || !(Get(r, "won") is bool) || !Integer(Get(r, "month"), Balance.Deadline)
                || !Number(Get(r, "followers"), 10000) || !Integer(Get(r, "sites"), 8) || !(Get(r, "seed") is string))) return null;
            return new SaveEnvelope
            {
                Version = 2, State = Get(v, "state") == null ? null : GameStateJson.FromJson(v["state"]),
                Settings = WithDefaultSettings(Record(v["settings"])), TutorialComplete = (bool)v["tutorialComplete"],
                LastResult = r, Playtest = Get(v, "playtest"), RetiredRuleset = retired.Length != 0 ? retired : Get(v, "retiredRuleset") as string,
                Extra = new Dictionary<string, object>(v), RawPlaytest = JsonReader.RawMember(raw, "playtest"), PlaytestAtDecode = JsonWriter.Write(Get(v, "playtest"))
            };
        }
        public static string EncodeSave(SaveEnvelope save)
        {
            var settings = new Dictionary<string, object>(save.Settings.Extra)
            {
                ["muted"] = save.Settings.Muted, ["volume"] = save.Settings.Volume, ["uiScale"] = save.Settings.UiScale,
                ["reducedMotion"] = save.Settings.ReducedMotion, ["hideGoals"] = save.Settings.HideGoals, ["hints"] = save.Settings.Hints
            };
            var envelope = new Dictionary<string, object>(save.Extra)
            {
                ["version"] = save.Version, ["state"] = GameStateJson.ToJson(save.State), ["settings"] = settings,
                ["tutorialComplete"] = save.TutorialComplete, ["lastResult"] = save.LastResult
            };
            if (save.Playtest != null || envelope.ContainsKey("playtest"))
                envelope["playtest"] = save.RawPlaytest != null && JsonWriter.Write(save.Playtest) == save.PlaytestAtDecode
                    ? (object)new RawJson(save.RawPlaytest) : save.Playtest;
            envelope.Remove("retiredRuleset");
            if (!string.IsNullOrEmpty(save.RetiredRuleset)) envelope["retiredRuleset"] = save.RetiredRuleset;
            return JsonWriter.Write(envelope);
        }
        public static string TakeRetiredNotice(SaveEnvelope decoded)
        {
            if (string.IsNullOrEmpty(decoded.RetiredRuleset)) return "";
            string ruleset = decoded.RetiredRuleset;
            decoded.RetiredRuleset = null;
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                RetiredNotice.Replace("(%s)", "({0})").Replace("under %s", "under {1}"), ruleset, Balance.Ruleset);
        }
        public static Dictionary<string, object> ResultFor(GameState state) => new Dictionary<string, object>
        {
            ["won"] = state.Status == "won", ["month"] = state.ElapsedMonths, ["followers"] = Derived.TotalFollowers(state),
            ["sites"] = Derived.ReadySites(state), ["seed"] = state.Seed
        };
    }
}
