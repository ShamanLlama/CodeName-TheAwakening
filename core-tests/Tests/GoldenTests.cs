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
        public static string GoldenDirectory
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "godot", "tests", "golden"))) directory = directory.Parent;
                return directory == null ? throw new DirectoryNotFoundException("Godot golden fixtures not found") : Path.Combine(directory.FullName, "godot", "tests", "golden");
            }
        }
        public static IEnumerable<object[]> Traces() => Directory.GetFiles(GoldenDirectory, "*.json.gz")
            .Where(p => Path.GetFileName(p) != "policy-endings.json.gz").OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { Path.GetFileName(p) });
        [Theory]
        [MemberData(nameof(Traces))]
        public void EveryTraceMatchesTheRecordedEngine(string file) => GoldenTraces.Replay(GoldenTraces.LoadTrace(Path.Combine(GoldenDirectory, file)));
        [Fact]
        public void GoldenCorpusIncludesNineTraces() => Assert.True(Traces().Count() >= 9);
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
