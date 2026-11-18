using System;
using System.Collections.Generic;
using System.Globalization;

namespace Awakening.Core.Rules
{
    public static class Forecast
    {
        static string F(string text, params object[] args) => string.Format(CultureInfo.InvariantCulture, text, args);
        public static RitualForecast Ritual(GameState state)
        {
            if (GameEngine.IsOver(state)) return new RitualForecast { Urgent = false, Message = state.ResultReason ?? "Run complete." };
            var result = Timetable(state);
            int huntIn = Balance.PreparationMonths - state.ElapsedMonths;
            if (huntIn <= 0) result.Message += " The world hunts ritual sites; keep Influence and Dread to answer it.";
            else if (huntIn <= Balance.FinaleWarningMonths) result.Message += F(" Threats against ritual sites intensify in {0} months.", huntIn);
            if (Derived.NetInfluencePerMonth(state) < 0)
            {
                result.Urgent = true;
                result.Message += " Site upkeep exceeds income.";
            }
            return result;
        }
        static RitualForecast Timetable(GameState state)
        {
            int ready = Derived.ReadySites(state);
            if (state.Status == "awakening")
            {
                int remaining = Balance.FinalMonths - state.FinalProgress;
                int left = Balance.Deadline - state.ElapsedMonths;
                return new RitualForecast { Urgent = ready < Balance.RequiredSites || left <= remaining,
                    Message = F("Final rite: {0} supported months needed; {1} months remain.", remaining, left) + (ready < Balance.RequiredSites ? " Restore three supported sites now." : "") };
            }
            var sites = new List<RegionState>();
            foreach (string id in GameData.RegionIds) if (state.Regions[id].Site.Invested) sites.Add(state.Regions[id]);
            var remainingPreparations = new List<int>();
            foreach (var r in sites) remainingPreparations.Add(Balance.SiteMonths - r.Site.Progress);
            remainingPreparations.Sort();
            if (remainingPreparations.Count >= Balance.RequiredSites)
            {
                int preparation = remainingPreparations[Balance.RequiredSites - 1];
                int earliest = Math.Max(Balance.PreparationMonths, state.ElapsedMonths + preparation) + Balance.FinalMonths;
                int unsupported = 0;
                foreach (var r in sites) if (!Derived.Supported(r)) unsupported++;
                string message;
                if (earliest > Balance.Deadline)
                    message = F("Preparation is too late: these sites need {0} months, then the final rite needs 12. Accelerate preparation or the window closes {1} months too soon.", preparation, earliest - Balance.Deadline);
                else
                    message = (state.ElapsedMonths >= Balance.PreparationMonths && ready >= 3 ? "The stars are right — begin the final rite. " : F("Funded sites need up to {0} more preparation months. ", preparation))
                        + "The final rite needs 12 months and 100 Dread." + (unsupported != 0 ? F(" Restore follower support at {0} site(s).", unsupported) : "");
                return new RitualForecast { Urgent = earliest > Balance.Deadline || unsupported > 0 || state.ElapsedMonths >= Balance.PreparationMonths, Message = message };
            }
            int monthsLeft = Balance.LatestSiteStart - state.ElapsedMonths;
            string timing = monthsLeft >= 0 ? (monthsLeft <= 24 ? "Only " : "") + F("{0} months left to start new sites without acceleration. ", monthsLeft) : "New sites now need acceleration to finish in time. ";
            return new RitualForecast { Urgent = monthsLeft <= 24, Message = F("Fund {0} more ritual site(s). ", Balance.RequiredSites - sites.Count) + timing + "Allow 24 months of preparation plus 12 for the final rite." };
        }
    }
}
