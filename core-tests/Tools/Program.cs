using System;
using System.Globalization;
using Awakening.Core.Dev;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "measure" && args.Length <= 3)
        {
            int seeds = 24;
            string difficulty = "normal";
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("--seeds=", StringComparison.Ordinal)
                    && int.TryParse(args[i].Substring(8), NumberStyles.None, CultureInfo.InvariantCulture, out seeds) && seeds >= 1) continue;
                if (args[i].StartsWith("--difficulty=", StringComparison.Ordinal) && Array.IndexOf(Awakening.Core.Rules.GameData.DifficultyIds, args[i].Substring(13)) >= 0)
                {
                    difficulty = args[i].Substring(13);
                    continue;
                }
                return Usage();
            }
            Measure.Run(seeds, null, difficulty);
            return 0;
        }
        if (args.Length >= 3 && args.Length <= 4 && args[0] == "trace")
        {
            // trace <strategy> <seed> [difficulty]: every 6th month of one bot run, for tuning.
            string difficulty = args.Length == 4 ? args[3] : "normal";
            var end = Playthrough.Play(args[1], args[2], Awakening.Core.Rules.Balance.Deadline, s =>
            {
                if (s.ElapsedMonths % 6 != 0 && s.Status != "awakening") return;
                var parts = new System.Collections.Generic.List<string>();
                foreach (string id in Awakening.Core.Rules.GameData.RegionIds)
                {
                    var r = s.Regions[id];
                    if (r.Population.Followers <= 0) continue;
                    parts.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1:0}f M{2:0} E{3:0}{4}{5}{6}", id.Substring(0, 3), r.Population.Followers, r.Madness, r.Exposure,
                        r.Site.Invested ? " s" + r.Site.Progress : "", r.Threat != null ? " T" + r.Threat.Kind.Substring(0, 1) + r.Threat.Severity : "", r.Scattered ? " X" : ""));
                }
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "m{0} {1} I{2:0} D{3:0} A{4:0.0} rite{5} t[{6}] | {7}", s.ElapsedMonths, s.Status, s.Influence, s.Dread, s.Awareness,
                    s.FinalProgress, string.Join(",", s.Talents), string.Join(" | ", parts)));
            }, difficulty);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "END m{0} {1} {2} A{3:0.0} D{4:0} rite{5}/{6} dreams{7}", end.ElapsedMonths, end.Status, end.ResultCode, end.Awareness, end.Dread, end.FinalProgress, end.RiteMonthsRequired, end.DreamRiteMonths));
            return 0;
        }
        if (args.Length == 2 && args[0] == "regenerate-goldens")
        {
            GoldenTraces.Regenerate(args[1], Console.Out);
            return 0;
        }
        return Usage();
    }
    static int Usage()
    {
        Console.Error.WriteLine("Usage: measure [--seeds=N] [--difficulty=easy|normal|hard] | trace <strategy> <seed> [difficulty] | regenerate-goldens <dir>");
        return 2;
    }
}
