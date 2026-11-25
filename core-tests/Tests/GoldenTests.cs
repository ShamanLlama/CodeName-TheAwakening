using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Awakening.Core.Dev;
using Awakening.Core.Rules;
using Xunit;

namespace Awakening.Core.Tests
{
    public class GoldenTests
    {
        public static string RepoRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "core-tests", "golden"))) directory = directory.Parent;
                return directory == null ? throw new DirectoryNotFoundException("Golden fixtures not found") : directory.FullName;
            }
        }
        /// <summary>Traces of the current ruleset, regenerated from the Godot command scripts with GoldenTraces.Regenerate.</summary>
        public static string GoldenDirectory => Path.Combine(RepoRoot, "core-tests", "golden");
        /// <summary>The original Godot oracle, recorded with alignment-3. Kept unchanged as the reference for the port.</summary>
        public static string GodotGoldenDirectory => Path.Combine(RepoRoot, "godot", "tests", "golden");
        public static IEnumerable<object[]> GodotTraces() => Directory.GetFiles(GodotGoldenDirectory, "*.json.gz")
            .Where(p => Path.GetFileName(p) != "policy-endings.json.gz").OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { Path.GetFileName(p) });
        public static IEnumerable<object[]> Traces() => Directory.GetFiles(GoldenDirectory, "*.json.gz")
            .Where(p => Path.GetFileName(p) != "policy-endings.json.gz").OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { Path.GetFileName(p) });
        [Theory]
        [MemberData(nameof(Traces))]
        public void EveryTraceMatchesTheRecordedEngine(string file) => GoldenTraces.Replay(GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, file)));
        [Fact]
        public void GoldenCorpusIncludesNineTraces() => Assert.True(Traces().Count() >= 9);
        [Fact]
        public void GoldenCorpusReplaysTheGodotCommandScripts()
        {
            Assert.Equal(GodotTraces().Select(t => (string)t[0]), Traces().Select(t => (string)t[0]));
            foreach (object[] t in Traces())
            {
                var current = GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, (string)t[0]));
                var godot = GoldenTraces.LoadTrace(Path.Combine(GodotGoldenDirectory, (string)t[0]));
                Assert.Equal(Balance.Ruleset, current["ruleset"]);
                Assert.Equal("alignment-3", godot["ruleset"]);
                Assert.Equal(Commands(godot), Commands(current));
            }
        }
        static List<string> Commands(Dictionary<string, object> trace) => ((List<object>)trace["steps"]).Cast<Dictionary<string, object>>()
            .Select(step => string.Join(";", ((List<object>)step["commands"]).Cast<Dictionary<string, object>>()
                .Select(c => string.Join(",", new[] { "op", "region", "id", "choice" }.Where(c.ContainsKey).Select(k => (string)c[k])))) + "|" + Convert.ToString(step["months"], CultureInfo.InvariantCulture)).ToList();

        // alignment-5 keeps the base rules of alignment-3 except where it says so. Until a trace reaches one of the known
        // divergences, the current engine must still match the Godot oracle exactly in every state, log line, derived
        // value, command result and every preview other than those of the changed actions. The divergences:
        // concealing while its world effect rests (alignment-4), manifesting (Mysticism), choosing a doctrine (now a
        // talent), an occult threat against a site on Easy (4 months, not 6) and the site hunt from month 200 (0.3, not 0.15).
        // alignment-6 adds three more: threat pressure from month 100 (+0.05), the site hunt (0.6) and Easy's threat
        // pressure (1.0, not 0.75), which can change any threat roll on Easy from the first one (month 4).
        // alignment-7 adds Normal's and Hard's threat pressure (1.45 and 1.15), which change every roll there from the
        // first; manifestations and sacrifices (Madness and its Dread); obstructing and silencing (the grace that follows
        // changes later rolls); and Easy sites (20 months) and talents.
        [Theory]
        [MemberData(nameof(GodotTraces))]
        public void GodotTracesStillMatchUntilAlignment7Diverges(string file)
        {
            var trace = GoldenTraces.LoadTrace(Path.Combine(GodotGoldenDirectory, file));
            var state = GameEngine.CreateGameState((string)trace["difficulty"], (string)trace["seed"]);
            RequireGodot((Dictionary<string, object>)trace["initial"], state, 1, file + " initial");
            int logFrom = state.NextLogId, matched = 0;
            foreach (Dictionary<string, object> step in (List<object>)trace["steps"])
            {
                var commands = ((List<object>)step["commands"]).Cast<Dictionary<string, object>>().ToList();
                double months = Convert.ToDouble(step["months"], CultureInfo.InvariantCulture);
                bool diverges = commands.Any(c => (string)c["op"] == "doctrine" || (string)c["op"] == "action" && Alignment7Actions.Contains((string)c["id"]))
                    || state.Difficulty != "easy" && state.Status != "placing" && state.ElapsedMonths + months >= Balance.ThreatRollMonths
                    || state.ElapsedMonths + months >= Balance.PreparationMonths && state.Regions.Values.Any(r => r.Site.Invested)
                    || state.ElapsedMonths + months >= Balance.MidgamePressureMonth
                    || state.Difficulty == "easy" && state.Status != "placing" && state.ElapsedMonths + months >= Balance.ThreatRollMonths;
                foreach (var command in commands)
                {
                    if (diverges || (string)command["op"] == "action" && (string)command["id"] == "conceal" && GameEngine.ConcealWorldCooldown(state) > 0 && state.Awareness > 0)
                    {
                        Assert.True(matched > 0, file + " diverged before its first step");
                        return;
                    }
                    var result = (string)command["op"] switch
                    {
                        "place" => GameEngine.PlaceStartingFoothold(state, (string)command["region"]),
                        "action" => GameEngine.PerformAction(state, (string)command["id"], (string)command["region"]),
                        _ => GameEngine.StartAwakening(state),
                    };
                    Assert.True((bool)command["ok"] == result.Ok, file + " step " + matched + " " + command["op"]);
                    state = result.State;
                }
                state = GameEngine.AdvanceSimulation(state, months);
                if (state.Difficulty == "easy" && state.Regions.Values.Any(r => r.Site.Invested && (r.Threat?.Kind == "occult" || r.Site.Recoverable > 0)))
                {
                    Assert.True(matched > 0, file + " diverged before its first step");
                    return;
                }
                RequireGodot((Dictionary<string, object>)step["after"], state, logFrom, file + " step " + matched);
                logFrom = state.NextLogId;
                matched++;
            }
            Assert.Equal(trace["final"], state.Status);
        }
        static readonly string[] Alignment7Actions = { "manifest", "sacrifice", "counter", "silence", "prepare" };
        static readonly string[] ChangedPreviews = { "conceal", "manifest", "bury", "restore", "sacrifice", "prepare", "counter", "silence" };
        static void RequireGodot(Dictionary<string, object> expected, GameState state, int logFrom, string where)
        {
            var actual = GoldenTraces.Snapshot(state, logFrom, expected.ContainsKey("previews"));
            var plain = (Dictionary<string, object>)actual["state"];
            plain.Remove("worldConcealReadyAt");
            plain.Remove("resultCode");
            plain.Remove("talents");
            plain.Remove("buryUses");
            foreach (string key in new[] { "talentYield", "capstonesUsed", "riteMonthsRequired", "riteSites", "dreamRiteMonths" }) plain.Remove(key);
            var telemetry = (Dictionary<string, object>)plain["telemetry"];
            telemetry.Remove("madnessDread"); telemetry.Remove("quickenedMonths");
            plain["specialization"] = null;
            foreach (Dictionary<string, object> region in ((Dictionary<string, object>)plain["regions"]).Values)
            {
                ((Dictionary<string, object>)region["site"]).Remove("recoverable");
                ((Dictionary<string, object>)region["site"]).Remove("quickening");
                region.Remove("threatGraceUntil");
            }
            plain["ruleset"] = "alignment-3";
            var godot = new Dictionary<string, object>(expected);
            // The Godot oracle retains the region's historical display name.
            godot["newLog"] = ((List<object>)expected["newLog"]).Cast<Dictionary<string, object>>().Select(entry =>
            {
                var renamed = new Dictionary<string, object>(entry);
                renamed["message"] = ((string)entry["message"]).Replace("Latin America", "South America");
                return (object)renamed;
            }).ToList();
            if (state.Difficulty == "easy")
            {
                // alignment-7: Easy's forecast counts 20-month sites from the first month.
                godot["derived"] = new Dictionary<string, object>((Dictionary<string, object>)expected["derived"]);
                ((Dictionary<string, object>)godot["derived"]).Remove("forecast");
                ((Dictionary<string, object>)actual["derived"]).Remove("forecast");
            }
            if (expected.ContainsKey("previews"))
            {
                godot["previews"] = WithoutChanged(expected["previews"]);
                actual["previews"] = WithoutChanged(actual["previews"]);
            }
            GoldenTraces.RequireEqual(godot, actual, where);
        }
        static Dictionary<string, object> WithoutChanged(object previews) => ((Dictionary<string, object>)previews).ToDictionary(
            region => region.Key, region => (object)((Dictionary<string, object>)region.Value).Where(a => !ChangedPreviews.Contains(a.Key)).ToDictionary(a => a.Key, a => a.Value));
        public static IEnumerable<object[]> PolicyRuns()
        {
            var endings = GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, "policy-endings.json.gz"));
            Assert.Equal(Balance.Ruleset, endings["ruleset"]);
            foreach (Dictionary<string, object> run in (List<object>)endings["runs"]) yield return new[] { run["strategy"], run["seed"], run["state"] };
        }
        [Theory]
        [MemberData(nameof(PolicyRuns))]
        public void PlaythroughPolicyReachesRecordedEndings(string strategy, string seed, object expected) => GoldenTraces.RequireEqual(expected, GoldenTraces.PlainState(Playthrough.Play(strategy, seed)), strategy + "/" + seed);
        [Fact]
        public void GoldenReplayIgnoresChileCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-CL");
                GoldenTraces.Replay(GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, "strategy-early-resume.json.gz")));
            }
            finally { CultureInfo.CurrentCulture = previous; CultureInfo.CurrentUICulture = previousUi; }
        }
        [Fact]
        public void RegeneratorReplaysAndRewritesTheSameOracle()
        {
            string directory = Path.Combine(Path.GetTempPath(), "awakening-goldens-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                const string file = "greedy-easy-unicode-seed.json.gz";
                File.Copy(Path.Combine(GoldenDirectory, file), Path.Combine(directory, file));
                GoldenTraces.Regenerate(directory);
                GoldenTraces.RequireEqual(GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, file)), GoldenTraces.LoadTrace(Path.Combine(directory, file)), "regenerated trace");
                GoldenTraces.RequireEqual(GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, "policy-endings.json.gz")), GoldenTraces.LoadTrace(Path.Combine(directory, "policy-endings.json.gz")), "regenerated policies");
                GoldenTraces.Replay(GoldenTraces.LoadTrace(Path.Combine(directory, file)));
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}
