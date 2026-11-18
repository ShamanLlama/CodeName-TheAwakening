using System;
using System.Globalization;
using Awakening.Core.Dev;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "measure" && args.Length <= 2)
        {
            int seeds = 24;
            if (args.Length == 2 && (!args[1].StartsWith("--seeds=", StringComparison.Ordinal)
                || !int.TryParse(args[1].Substring(8), NumberStyles.None, CultureInfo.InvariantCulture, out seeds) || seeds < 1)) return Usage();
            Measure.Run(seeds);
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
        Console.Error.WriteLine("Usage: measure [--seeds=N] | regenerate-goldens <dir>");
        return 2;
    }
}
