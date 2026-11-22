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
        if (args.Length == 2 && args[0] == "regenerate-goldens")
        {
            GoldenTraces.Regenerate(args[1], Console.Out);
            return 0;
        }
        return Usage();
    }
    static int Usage()
    {
        Console.Error.WriteLine("Usage: measure [--seeds=N] [--difficulty=easy|normal|hard] | regenerate-goldens <dir>");
        return 2;
    }
}
