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
        public static readonly string[] Strategies = { "early", "late", "hub" };
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
        public static void Run(int seeds = 24, TextWriter output = null)
        {
            if (seeds < 1) throw new ArgumentOutOfRangeException(nameof(seeds));
            output = output ?? Console.Out;
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, "ruleset {0} · normal · {1} seeds per strategy", Balance.Ruleset, seeds));
            int totalWins = 0;
            foreach (string strategy in Strategies)
            {
                int wins = 0, lostAwareness = 0, lostDeadline = 0, burstMonths = 0, worstMonth = 0;
                var conceals = new List<double>(); var winMonths = new List<double>();
                var influence = new List<double>(); var awareness = new List<double>(); var dread = new List<double>(); var landed = new List<double>();
                var used = Responses.ToDictionary(r => r, r => new List<double>());
                for (int i = 0; i < seeds; i++)
                {
                    int previous = 0;
                    var final = Playthrough.Play(strategy, "measure-" + i.ToString(CultureInfo.InvariantCulture), Balance.Deadline, month =>
                    {
                        int total = Concealed(month), inMonth = total - previous;
                        previous = total;
                        if (inMonth >= BurstConceals) burstMonths++;
                        worstMonth = Math.Max(worstMonth, inMonth);
                    });
                    conceals.Add(Concealed(final));
                    if (final.Status == "won") { wins++; winMonths.Add(final.ElapsedMonths); }
                    else if (final.Awareness >= 100) lostAwareness++;
                    else lostDeadline++;
                    influence.Add(final.Influence); awareness.Add(final.Awareness); dread.Add(final.Dread); landed.Add(final.Telemetry.RaidLosses);
                    foreach (string response in Responses) used[response].Add(final.Telemetry.Actions.TryGetValue(response, out int count) ? count : 0);
                }
                totalWins += wins;
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} wins {1}/{2} · lost: awareness {3}, deadline {4} · final Influence {5}, awareness {6}, Dread {7} · followers lost to threats {8} · {9}",
                    strategy, wins, seeds, lostAwareness, lostDeadline, Print(Median(influence), 0), Print(Median(awareness), 0), Print(Median(dread), 0), Print(Median(landed), 0),
                    string.Join(", ", Responses.Select(r => r + " " + Print(Median(used[r]), 1)))));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-6} conceal {1} per run · months with {2}+ conceals {3} · most in one month {4} · victory month {5}",
                    "", Print(Median(conceals), 1), BurstConceals, burstMonths, worstMonth, winMonths.Count == 0 ? "-" : Print(Median(winMonths), 1)));
            }
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, "total wins {0}/{1}", totalWins, seeds * Strategies.Length));
        }
    }
}
