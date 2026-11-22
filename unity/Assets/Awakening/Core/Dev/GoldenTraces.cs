using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Awakening.Core.Json;
using Awakening.Core.Rules;
using Awakening.Core.Save;

namespace Awakening.Core.Dev
{
    /// <summary>Godot golden oracle: replay every command, snapshot, text and save round-trip.</summary>
    public static class GoldenTraces
    {
        static Dictionary<string, object> Map(object value) => (Dictionary<string, object>)value;
        static List<object> List(object value) => (List<object>)value;
        static string Str(Dictionary<string, object> value, string key) => (string)value[key];
        public static Dictionary<string, object> LoadTrace(string path)
        {
            using (var file = File.OpenRead(path))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, Encoding.UTF8))
                return Map(JsonReader.Parse(reader.ReadToEnd()));
        }
        public static Dictionary<string, object> PreviewJson(ActionPreview p) => new Dictionary<string, object>
        {
            ["ok"] = p.Ok, ["reason"] = p.Reason, ["influence"] = p.Influence, ["dread"] = p.Dread,
            ["followers"] = p.Followers, ["awareness"] = p.Awareness, ["effect"] = p.Effect, ["risk"] = p.Risk
        };
        public static Dictionary<string, object> ConsequencesJson(ThreatConsequences c) => new Dictionary<string, object>
        {
            ["followers"] = c.Followers, ["remaining"] = c.Remaining, ["destroysCell"] = c.DestroysCell, ["preparationLost"] = c.PreparationLost,
            ["incomeLost"] = c.IncomeLost, ["exposureAdded"] = c.ExposureAdded, ["interruptsSite"] = c.InterruptsSite
        };
        public static Dictionary<string, object> DerivedValues(GameState state)
        {
            var regions = new Dictionary<string, object>();
            foreach (string id in GameData.RegionIds)
            {
                var r = state.Regions[id];
                regions[id] = new Dictionary<string, object>
                {
                    ["recruitment"] = Derived.RecruitmentPerMonth(state, r), ["income"] = Derived.RegionIncome(r),
                    ["siteRequirement"] = Derived.SiteRequirement(r), ["siteLabel"] = Derived.SiteLabel(r), ["threat"] = Threats.Describe(state, r),
                    ["consequences"] = r.Threat != null ? ConsequencesJson(Threats.Consequences(state, r)) : null
                };
            }
            var forecast = Forecast.Ritual(state);
            return new Dictionary<string, object>
            {
                ["totalFollowers"] = Derived.TotalFollowers(state), ["influencePerMonth"] = Derived.InfluencePerMonth(state),
                ["cultShare"] = Derived.CultShare(state), ["awarenessDrag"] = Derived.AwarenessDrag(state), ["readySites"] = Derived.ReadySites(state),
                ["forecast"] = new Dictionary<string, object> { ["urgent"] = forecast.Urgent, ["message"] = forecast.Message },
                ["awakeningReason"] = GameEngine.AwakeningReason(state), ["calendar"] = Derived.Calendar(state.ElapsedMonths), ["regions"] = regions
            };
        }
        public static Dictionary<string, object> Previews(GameState state)
        {
            var all = new Dictionary<string, object>();
            foreach (string id in GameData.RegionIds)
            {
                var region = new Dictionary<string, object>();
                foreach (string action in GameData.ActionIds) region[action] = PreviewJson(GameEngine.PreviewAction(state, action, id));
                all[id] = region;
            }
            return all;
        }
        public static Dictionary<string, object> PlainState(GameState state)
        {
            var plain = GameStateJson.ToJson(state);
            plain.Remove("log");
            return plain;
        }
        public static Dictionary<string, object> Snapshot(GameState state, int logFrom, bool withPreviews)
        {
            var snapshot = new Dictionary<string, object>
            {
                ["state"] = PlainState(state), ["newLog"] = state.Log.Where(e => e.Id >= logFrom).Select(e => (object)GameStateJson.ToJson(e)).ToList(),
                ["derived"] = DerivedValues(state)
            };
            if (withPreviews) snapshot["previews"] = Previews(state);
            return snapshot;
        }
        /// <summary>The root talent that carries a retired doctrine's effect.</summary>
        public static string DoctrineRoot(string doctrine) => doctrine == "recruitment" ? "swelling_flock" : doctrine == "infiltration" ? "quiet_friends" : doctrine == "terror" ? "red_altars" : doctrine;
        static CommandResult Command(GameState state, Dictionary<string, object> command)
        {
            switch (Str(command, "op"))
            {
                case "place": return GameEngine.PlaceStartingFoothold(state, Str(command, "region"));
                case "action": return GameEngine.PerformAction(state, Str(command, "id"), Str(command, "region"));
                // The Godot scripts chose a doctrine; alignment-5 takes the root talent that keeps its effect.
                case "doctrine": return GameEngine.TakeTalent(state, DoctrineRoot(Str(command, "choice")));
                case "talent": return GameEngine.TakeTalent(state, Str(command, "id"));
                case "awaken": return GameEngine.StartAwakening(state);
                default: throw new InvalidDataException("Unknown golden command: " + command["op"]);
            }
        }
        static void CheckSnapshot(Dictionary<string, object> expected, GameState state, int logFrom, string where)
        {
            RequireEqual(expected, Snapshot(state, logFrom, expected.ContainsKey("previews")), where);
            var save = SaveData.DefaultSave();
            save.State = state;
            var decoded = SaveData.DecodeSave(SaveData.EncodeSave(save));
            if (decoded == null) throw new InvalidDataException(where + " save was rejected on reload");
            RequireEqual(GameStateJson.ToJson(state), GameStateJson.ToJson(decoded.State), where + " reloaded save");
        }
        public static GameState Replay(Dictionary<string, object> trace)
        {
            string name = Str(trace, "name");
            RequireEqual(Balance.Ruleset, trace["ruleset"], name + " ruleset");
            var state = GameEngine.CreateGameState(Str(trace, "difficulty"), Str(trace, "seed"));
            CheckSnapshot(Map(trace["initial"]), state, 1, name + " initial");
            int logFrom = state.NextLogId, index = 0;
            foreach (var stepValue in List(trace["steps"]))
            {
                var step = Map(stepValue);
                string where = string.Format(CultureInfo.InvariantCulture, "{0} step {1} (month {2})", name, index++, state.ElapsedMonths);
                foreach (var commandValue in List(step["commands"]))
                {
                    var command = Map(commandValue);
                    if (Str(command, "op") == "action")
                        RequireEqual(command["preview"], PreviewJson(GameEngine.PreviewAction(state, Str(command, "id"), Str(command, "region"))), where + " preview " + command["id"] + "@" + command["region"]);
                    var result = Command(state, command);
                    var outcome = new Dictionary<string, object> { ["ok"] = result.Ok };
                    if (!result.Ok) outcome["message"] = result.Message;
                    var expected = new Dictionary<string, object> { ["ok"] = command["ok"] };
                    if (command.ContainsKey("message")) expected["message"] = command["message"];
                    RequireEqual(expected, outcome, where + " " + command["op"]);
                    state = result.State;
                }
                state = GameEngine.AdvanceSimulation(state, Convert.ToDouble(step["months"], CultureInfo.InvariantCulture));
                CheckSnapshot(Map(step["after"]), state, logFrom, where);
                logFrom = state.NextLogId;
            }
            RequireEqual(trace["final"], state.Status, name + " final");
            return state;
        }
        public static void RequireEqual(object expected, object actual, string where = "")
        {
            var differences = new List<string>();
            Compare(expected, actual, "", differences);
            if (differences.Count != 0) throw new InvalidDataException(where + ": " + string.Join("; ", differences));
        }
        static bool Numeric(object v) => v is double || v is int || v is uint || v is long;
        static void Compare(object expected, object actual, string path, List<string> differences)
        {
            if (differences.Count >= 5) return;
            if (Numeric(expected) && Numeric(actual))
            {
                double e = Convert.ToDouble(expected, CultureInfo.InvariantCulture), a = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
                if (Math.Abs(e - a) > 1e-9 * Math.Max(1.0, Math.Abs(e))) differences.Add(path + ": expected " + JsonWriter.Write(e) + ", got " + JsonWriter.Write(a));
            }
            else if (expected is IDictionary<string, object> em && actual is IDictionary<string, object> am)
            {
                foreach (var pair in em)
                {
                    if (!am.TryGetValue(pair.Key, out object value)) differences.Add(path + "." + pair.Key + ": missing");
                    else Compare(pair.Value, value, path + "." + pair.Key, differences);
                }
                foreach (var pair in am) if (!em.ContainsKey(pair.Key)) differences.Add(path + "." + pair.Key + ": unexpected");
            }
            else if (expected is IList el && actual is IList al)
            {
                if (el.Count != al.Count) differences.Add(path + ": item counts differ");
                for (int i = 0; i < Math.Min(el.Count, al.Count); i++) Compare(el[i], al[i], path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", differences);
            }
            else if (!Equals(expected, actual)) differences.Add(path + ": expected " + JsonWriter.Write(expected) + ", got " + JsonWriter.Write(actual));
        }
        public static Dictionary<string, object> RegenerateTrace(Dictionary<string, object> old)
        {
            var state = GameEngine.CreateGameState(Str(old, "difficulty"), Str(old, "seed"));
            var steps = new List<object>();
            var trace = new Dictionary<string, object>
            {
                ["name"] = old["name"], ["seed"] = old["seed"], ["difficulty"] = old["difficulty"], ["ruleset"] = Balance.Ruleset,
                ["initial"] = Snapshot(state, 1, Map(old["initial"]).ContainsKey("previews")), ["steps"] = steps
            };
            int logFrom = state.NextLogId;
            foreach (var stepValue in List(old["steps"]))
            {
                var step = Map(stepValue);
                var commands = new List<object>();
                foreach (var commandValue in List(step["commands"]))
                {
                    var command = Map(commandValue);
                    var entry = new Dictionary<string, object> { ["op"] = command["op"] };
                    foreach (string key in new[] { "region", "id", "choice" }) if (command.ContainsKey(key)) entry[key] = command[key];
                    if (Str(command, "op") == "action") entry["preview"] = PreviewJson(GameEngine.PreviewAction(state, Str(command, "id"), Str(command, "region")));
                    var result = Command(state, command);
                    entry["ok"] = result.Ok;
                    if (!result.Ok) entry["message"] = result.Message;
                    commands.Add(entry);
                    state = result.State;
                }
                state = GameEngine.AdvanceSimulation(state, Convert.ToDouble(step["months"], CultureInfo.InvariantCulture));
                steps.Add(new Dictionary<string, object> { ["commands"] = commands, ["months"] = step["months"], ["after"] = Snapshot(state, logFrom, Map(step["after"]).ContainsKey("previews")) });
                logFrom = state.NextLogId;
            }
            trace["final"] = state.Status;
            return trace;
        }
        public static void Regenerate(string directory, TextWriter output = null)
        {
            foreach (string path in Directory.GetFiles(directory, "*.json.gz").OrderBy(p => p, StringComparer.Ordinal))
            {
                if (Path.GetFileName(path) == "policy-endings.json.gz") continue;
                Write(path, RegenerateTrace(LoadTrace(path)));
                output?.WriteLine("rewrote " + Path.GetFileName(path));
            }
            var runs = new List<object>();
            foreach (var pair in new[] { new[] { "early", "rlyeh" }, new[] { "late", "dagon" }, new[] { "terror", "the-deep" }, new[] { "early", "resume" } })
                runs.Add(new Dictionary<string, object> { ["strategy"] = pair[0], ["seed"] = pair[1], ["state"] = PlainState(Playthrough.Play(pair[0], pair[1])) });
            Write(Path.Combine(directory, "policy-endings.json.gz"), new Dictionary<string, object> { ["ruleset"] = Balance.Ruleset, ["runs"] = runs });
            output?.WriteLine("rewrote policy-endings.json.gz");
        }
        static void Write(string path, object value)
        {
            using (var file = File.Create(path))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false))) writer.Write(JsonWriter.Write(value));
        }
    }
}
