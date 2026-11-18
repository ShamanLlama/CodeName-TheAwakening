using System;
using System.Collections.Generic;
using System.Globalization;

namespace Awakening.Core.Rules
{
    public static class Threats
    {
        public static ThreatConsequences Consequences(RegionState region)
        {
            var threat = region.Threat;
            bool has = threat != null;
            double followers = 0.0;
            if (has)
            {
                bool raid = threat.Kind == "raid";
                double share = threat.Severity * (raid ? Balance.RaidLossShare : Balance.ThreatLossShare);
                followers = Math.Min(region.Population.Followers, Math.Max(
                    threat.Severity * (raid ? Balance.RaidLossFloor : Balance.ThreatLossFloor), region.Population.Followers * share));
            }
            double remaining = region.Population.Followers - followers;
            bool destroysCell = has && region.Population.Followers > 0 && remaining <= 0;
            int preparationLost = 0;
            if (has && threat.Kind == "occult" && region.Site.Invested) preparationLost = Math.Min(region.Site.Progress, 6);
            return new ThreatConsequences
            {
                Followers = followers, Remaining = remaining, DestroysCell = destroysCell, PreparationLost = preparationLost,
                IncomeLost = destroysCell ? Derived.RegionIncome(region) : followers * Balance.InfluencePerFollower,
                ExposureAdded = has ? Math.Min(5.0, 100.0 - region.Exposure) : 0.0,
                InterruptsSite = has && region.Site.Invested && (remaining < Derived.SiteRequirement(region)
                    || region.Site.Progress >= Balance.SiteMonths && region.Site.Progress - preparationLost < Balance.SiteMonths)
            };
        }
        public static string Describe(RegionState region)
        {
            var c = Consequences(region);
            if (region.Threat == null) return "No approaching threat.";
            var effects = new List<string> { "Lose " + Js.Num(Math.Ceiling(c.Followers)) + " followers", "income −" + Js.Fixed(c.IncomeLost, 2) + " Influence/month" };
            if (c.PreparationLost != 0) effects.Add(string.Format(CultureInfo.InvariantCulture, "ritual −{0} preparation months", c.PreparationLost));
            if (c.DestroysCell) effects.Add(region.Site.Invested ? "cell and ritual site destroyed" : "cell destroyed");
            else if (c.InterruptsSite) effects.Add("site cannot support the final rite");
            if (c.ExposureAdded != 0) effects.Add("Exposure +" + Js.Num(c.ExposureAdded));
            return string.Join(" · ", effects) + ".";
        }
    }
}
