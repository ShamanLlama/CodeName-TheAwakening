using System;
using System.Collections.Generic;
using System.Globalization;

namespace Awakening.Core.Rules
{
    public static class Threats
    {
        public static readonly string[] Responses = { "counter", "scatter", "silence" };
        public static ThreatConsequences Consequences(GameState state, RegionState region)
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
            if (has && threat.Kind == "occult" && region.Site.Invested) preparationLost = Math.Min(region.Site.Progress, GameEngine.OccultPreparationLoss(state));
            return new ThreatConsequences
            {
                Followers = followers, Remaining = remaining, DestroysCell = destroysCell, PreparationLost = preparationLost,
                IncomeLost = destroysCell ? Derived.RegionIncome(region) : followers * Balance.InfluencePerFollower,
                ExposureAdded = has ? Math.Min(5.0, 100.0 - region.Exposure) : 0.0,
                InterruptsSite = has && region.Site.Invested && (remaining < Derived.SiteRequirement(region)
                    || region.Site.Progress >= Balance.SiteMonths && region.Site.Progress - preparationLost < Balance.SiteMonths)
            };
        }
        public static string Describe(GameState state, RegionState region)
        {
            var c = Consequences(state, region);
            if (region.Threat == null) return "No approaching threat.";
            var effects = new List<string> { "Lose " + Js.Num(Math.Ceiling(c.Followers)) + " followers", "income −" + Js.Fixed(c.IncomeLost, 2) + " Influence/month" };
            if (c.PreparationLost != 0) effects.Add(string.Format(CultureInfo.InvariantCulture, "ritual −{0} preparation months", c.PreparationLost));
            if (c.DestroysCell) effects.Add(region.Site.Invested ? "cell and ritual site destroyed" : "cell destroyed");
            else if (c.InterruptsSite) effects.Add("site cannot support the final rite");
            if (c.ExposureAdded != 0) effects.Add("Exposure +" + Js.Num(c.ExposureAdded));
            return string.Join(" · ", effects) + ".";
        }
        /// <summary>
        /// Counter, scatter and silence for this region's threat, in that order: whether each is legal now, why not
        /// (cooldown, missing Influence or Dread, ...) and its price. Shown when the threat appears, not when it lands.
        /// </summary>
        public static List<ThreatOption> Options(GameState state, string regionId)
        {
            var options = new List<ThreatOption>();
            foreach (string id in Responses)
            {
                var p = GameEngine.PreviewAction(state, id, regionId);
                string cost;
                switch (id)
                {
                    case "counter": cost = p.Influence.ToString(CultureInfo.InvariantCulture) + " Influence"; break;
                    case "scatter": cost = string.Format(CultureInfo.InvariantCulture, "{0} months scattered", GameEngine.ScatterMonths(state)); break;
                    default: cost = p.Dread.ToString(CultureInfo.InvariantCulture) + " Dread · +" + Js.Num(p.Awareness) + " awareness"; break;
                }
                options.Add(new ThreatOption { Response = id, Ok = p.Ok, Reason = p.Reason, Cost = cost });
            }
            return options;
        }
        /// <summary>True when the region has a threat that can be neither obstructed nor silenced now: only scattering or the blow remain.</summary>
        public static bool Cornered(GameState state, string regionId)
        {
            if (state.Regions[regionId].Threat == null) return false;
            var options = Options(state, regionId);
            return !options[0].Ok && !options[2].Ok;
        }
    }
}
