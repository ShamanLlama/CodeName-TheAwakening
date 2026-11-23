using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Awakening.Core.Rules;

namespace Awakening.Core.Dev
{
    public static class Measure
    {
        public static readonly string[] Strategies = Playthrough.Strategies;
        public static readonly string[] Responses = { "counter", "scatter", "silence" };
        static double Median(List<double> values)
        {
            values.Sort();
            int half = values.Count / 2;
            return values.Count % 2 == 1 ? values[half] : (values[half - 1] + values[half]) / 2.0;
        }
        // Godot's printf uses ties-to-even formatting here, unlike Js.Fixed.
        static string Print(double value, int digits) => Math.Round(value, digits, MidpointRounding.ToEven).ToString("F" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        public const int BurstConceals = 4;
        static int Concealed(GameState state) => state.Telemetry.Actions.TryGetValue("conceal", out int count) ? count : 0;
        public static void Run(int seeds = 24, TextWriter output = null, string difficulty = "normal")
        {
            if (seeds < 1) throw new ArgumentOutOfRangeException(nameof(seeds));
            output = output ?? Console.Out;
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, "ruleset {0} · {2} · {1} seeds per strategy", Balance.Ruleset, seeds, difficulty));
            int totalWins = 0;
            foreach (string strategy in Strategies)
            {
                int wins = 0, lostAwareness = 0, lostDeadline = 0, burstMonths = 0, worstMonth = 0;
                var conceals = new List<double>(); var winMonths = new List<double>();
                var influence = new List<double>(); var awareness = new List<double>(); var dread = new List<double>(); var landed = new List<double>();
                var used = Responses.ToDictionary(r => r, r => new List<double>());
                var talents = new List<double>(); var buried = new List<double>(); var restored = new List<double>();
                int processions = 0, forgotten = 0, dreamt = 0, dreamMonths = 0, forgottenWins = 0, processionWins = 0, dreamWins = 0;
                var madnessDread = new List<double>(); var quickened = new List<double>(); var spread = new List<double>();
                int quickenedRuns = 0, quickenedWins = 0;
                var spending = new List<Dictionary<string, double>>(); var threats = new List<double>(); var preRite = new List<double>();
                for (int i = 0; i < seeds; i++)
                {
                    int previous = 0, spawned = 0, spawnedBefore = 0;
                    var seen = new HashSet<string>();
                    var ledger = new Dictionary<string, double>();
                    spending.Add(ledger);
                    var final = Playthrough.Play(strategy, "measure-" + i.ToString(CultureInfo.InvariantCulture), Balance.Deadline, month =>
                    {
                        int total = Concealed(month), inMonth = total - previous;
                        previous = total;
                        if (inMonth >= BurstConceals) burstMonths++;
                        worstMonth = Math.Max(worstMonth, inMonth);
                        foreach (string id in GameData.RegionIds)
                        {
                            var t = month.Regions[id].Threat;
                            if (t != null && seen.Add(id + "@" + t.DueMonth.ToString(CultureInfo.InvariantCulture)))
                            {
                                spawned++;
                                if (month.ElapsedMonths < Balance.PreparationMonths) spawnedBefore++;
                            }
                        }
                    }, difficulty, ledger);
                    conceals.Add(Concealed(final)); threats.Add(spawned); preRite.Add(spawnedBefore);
                    if (final.Status == "won") { wins++; winMonths.Add(final.ElapsedMonths); }
                    else if (final.Awareness >= 100) lostAwareness++;
                    else lostDeadline++;
                    influence.Add(final.Influence); awareness.Add(final.Awareness); dread.Add(final.Dread); landed.Add(final.Telemetry.RaidLosses);
                    talents.Add(final.Talents.Count); buried.Add(final.BuryUses);
                    bool won = final.Status == "won";
                    if (final.RiteMonthsRequired < Balance.FinalMonths) { processions++; if (won) processionWins++; }
                    if (final.CapstonesUsed.Contains("world_forgets")) { forgotten++; if (won) forgottenWins++; }
                    int dreams = final.DreamRiteMonths;
                    madnessDread.Add(final.Telemetry.MadnessDread); quickened.Add(final.Telemetry.QuickenedMonths);
                    spread.Add(GameEngine.TalentYieldOf(final, "dreams_of_rlyeh"));
                    if (final.Telemetry.QuickenedMonths > 0) { quickenedRuns++; if (won) quickenedWins++; }
                    if (dreams > 0) { dreamt++; dreamMonths += dreams; if (won) dreamWins++; }
                    restored.Add(final.Telemetry.Actions.TryGetValue("restore", out int restores) ? restores : 0);
                    foreach (string response in Responses) used[response].Add(final.Telemetry.Actions.TryGetValue(response, out int count) ? count : 0);
                }
                totalWins += wins;
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} wins {1}/{2} · lost: awareness {3}, deadline {4} · final Influence {5}, awareness {6}, Dread {7} · followers lost to threats {8} · {9}",
                    strategy, wins, seeds, lostAwareness, lostDeadline, Print(Median(influence), 0), Print(Median(awareness), 0), Print(Median(dread), 0), Print(Median(landed), 0),
                    string.Join(", ", Responses.Select(r => r + " " + Print(Median(used[r]), 1)))));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} conceal {1} per run · months with {2}+ conceals {3} · most in one month {4} · victory month {5} · threats {6} per run ({7} before month {8})",
                    "", Print(Median(conceals), 1), BurstConceals, burstMonths, worstMonth, winMonths.Count == 0 ? "-" : Print(Median(winMonths), 1),
                    Print(threats.Average(), 1), Print(preRite.Average(), 1), Balance.PreparationMonths));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} talents {1} · bury {2} (runs with any {3}) · restore {4} (runs with any {5})",
                    "", Print(Median(talents), 1), Print(Median(buried), 1), buried.Count(b => b > 0), Print(Median(restored), 1), restored.Count(r => r > 0)));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} capstones: procession {1} (won {2}) · world forgets {3} (won {4}) · dreams {5} runs, {6} rite months (won {7})",
                    "", processions, processionWins, forgotten, forgottenWins, dreamt, dreamMonths, dreamWins));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} madness: Dread {1} per run · site months quickened {2} per run, {3} in all (runs with any {4}, won {5}) · dreams spread {6} per run",
                    "", Print(Median(madnessDread), 0), Print(Median(quickened), 1), quickened.Sum(), quickenedRuns, quickenedWins, Print(Median(spread), 0)));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-6} Influence spent per run (mean): {1}", "",
                    string.Join(", ", new[] { "talent", "counter", "bury", "conceal", "restore", "recruit", "prepare", "expand" }
                        .Select(k => k + " " + Print(spending.Average(l => l.TryGetValue(k, out double v) ? v : 0.0), 0)))));
            }
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, "total wins {0}/{1}", totalWins, seeds * Strategies.Length));
        }
    }
}
