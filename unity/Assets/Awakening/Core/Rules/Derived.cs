using System;
using System.Globalization;

namespace Awakening.Core.Rules
{
    public static class Derived
    {
        public static double ClampPct(double value, double minimum = 0.0, double maximum = 100.0) => Math.Min(maximum, Math.Max(minimum, value));
        public static double TotalFollowers(GameState state)
        {
            double sum = 0.0;
            foreach (string id in GameData.RegionIds) sum += state.Regions[id].Population.Followers;
            return sum;
        }
        public static double LivePopulation(RegionState r)
        {
            var p = r.Population;
            return p.Unaware + p.Deniers + p.Believers + p.Followers;
        }
        public static double Recruitable(RegionState r) => r.Population.Unaware + r.Population.Believers;
        public static double Capacity(RegionState r)
        {
            var p = r.Population;
            return p.Unaware + p.Deniers + p.Believers + p.Followers + p.Losses;
        }
        public static double SiteRequirement(RegionState r) => Math.Max(Balance.SiteFollowers, Js.RoundHalfUp(Capacity(r) * Balance.SiteSupportShare));
        public static bool Supported(RegionState r) => !r.Scattered && r.Population.Followers >= SiteRequirement(r);
        public static double CultShare(GameState state) => TotalFollowers(state) / GameData.WorldPopulation;
        public static double AwarenessDrag(GameState state) => Math.Max(0.0, 1.0 - state.Awareness / Balance.AwarenessDrag);
        public static int ReadySites(GameState state)
        {
            int count = 0;
            foreach (string id in GameData.RegionIds)
            {
                var r = state.Regions[id];
                if (r.Site.Invested && !r.Site.Dormant && r.Site.Progress >= Balance.SiteMonths && Supported(r)) count++;
            }
            return count;
        }
        public static string Calendar(int month) => string.Format(CultureInfo.InvariantCulture, "Year {0} · Month {1}", (int)Math.Floor(month / 12.0) + 1, month % 12 + 1);
        public static double RegionIncome(RegionState r)
        {
            if (r.Population.Followers <= 0 || r.Scattered) return 0.0;
            double celebrity = r.Figure != null && r.Figure.Kind == "celebrity" && !r.Figure.Revealed ? 0.6 : 0.0;
            return r.Population.Followers * Balance.InfluencePerFollower + celebrity;
        }
        public static double InfluencePerMonth(GameState state)
        {
            double sum = 0.0;
            foreach (string id in GameData.RegionIds) sum += RegionIncome(state.Regions[id]);
            return sum * AwarenessDrag(state);
        }
        public static double RecruitmentPerMonth(GameState state, RegionState r)
        {
            if (r.Population.Followers <= 0 || r.Scattered) return 0.0;
            bool openWorship = GameEngine.HasTalent(state, "open_worship");
            return Math.Min(Recruitable(r), (Balance.RecruitmentBase + r.Population.Followers * Balance.RecruitmentRate)
                * (r.Public ? openWorship ? Balance.OpenWorshipRecruitment : Balance.PublicRecruitment : 1.0)
                * (GameEngine.HasTalent(state, "swelling_flock") ? 1.0 + Balance.SwellingFlockRecruitment : 1.0)
                * (state.ElapsedMonths < r.BacklashUntil ? 0.55 : 1.0)
                * (1.0 + r.Madness / 500.0) * GameData.Difficulties[state.Difficulty].Recruitment * (r.Public && openWorship ? 1.0 : AwarenessDrag(state)));
        }
        /// <summary>World awareness a manifestation draws here: less where the impossible is half expected.</summary>
        // Integer percentages keep exact halves (57.5 rounds to 58, not 57.4999...).
        static int MysticismPercent(string regionId) => GameData.Regions[regionId].Mysticism - Balance.MysticismPivot;
        public static double ManifestAwareness(string regionId) => Js.RoundHalfUp(Balance.ManifestAwareness * (100 - MysticismPercent(regionId)) / 100.0);
        public static int ManifestDeniers(string regionId) => (int)Js.RoundHalfUp(Balance.ManifestationDeniers * (100 + MysticismPercent(regionId)) / 100.0);
        public static int ManifestUnaware(string regionId) => (int)Js.RoundHalfUp(Balance.ManifestationUnaware * (100 + MysticismPercent(regionId)) / 100.0);
        /// <summary>One line for the region panel: what this region's Mysticism does to a manifestation.</summary>
        public static string MysticismNote(string regionId)
        {
            int mysticism = GameData.Regions[regionId].Mysticism;
            string label = mysticism > Balance.MysticismPivot ? "Mystical" : mysticism < Balance.MysticismPivot ? "Skeptical" : "Wavering";
            double awareness = ManifestAwareness(regionId);
            return string.Format(CultureInfo.InvariantCulture, "{0}: a manifestation here draws {1} world awareness{2} and turns up to {3} deniers and {4} unaware into believers.",
                label, Js.Num(awareness), awareness != Balance.ManifestAwareness ? ", not " + Js.Num(Balance.ManifestAwareness) + "," : "",
                ManifestDeniers(regionId), ManifestUnaware(regionId));
        }
        public static double SiteUpkeep(GameState state)
        {
            double sum = 0.0;
            foreach (string id in GameData.RegionIds) if (state.Regions[id].Site.Invested) sum += Balance.SiteUpkeep;
            return sum;
        }
        public static double NetInfluencePerMonth(GameState state) => InfluencePerMonth(state) - SiteUpkeep(state);
        public static string SiteLabel(RegionState r)
        {
            if (!r.Site.Invested) return "No ritual site";
            if (r.Site.Dormant) return "Dormant · upkeep unpaid";
            if (r.Scattered) return "Stalled · the flock is scattered";
            if (!Supported(r)) return "Interrupted · needs " + Js.Num(SiteRequirement(r)) + " followers";
            return r.Site.Progress >= Balance.SiteMonths ? "Ritual prepared" : string.Format(CultureInfo.InvariantCulture, "Preparing · {0}/{1} months", r.Site.Progress, Balance.SiteMonths);
        }
        public static void AddLog(GameState state, string message)
        {
            state.Log.Add(new LogEntry { Id = state.NextLogId, Month = state.ElapsedMonths, Message = message });
            state.NextLogId++;
            if (state.Log.Count > 100) state.Log = state.Log.GetRange(state.Log.Count - 100, 100);
        }
        public static double Recruit(RegionState r, double amount)
        {
            var p = r.Population;
            double fromBelievers = Math.Min(p.Believers, amount);
            double fromUnaware = Math.Min(p.Unaware, Math.Max(0.0, amount - fromBelievers));
            p.Believers -= fromBelievers;
            p.Unaware -= fromUnaware;
            p.Followers += fromBelievers + fromUnaware;
            return fromBelievers + fromUnaware;
        }
        public static double LoseFollowers(RegionState r, double amount)
        {
            var p = r.Population;
            double removed = Math.Min(p.Followers, amount);
            p.Followers -= removed;
            p.Losses += removed;
            if (p.Followers <= 0)
            {
                r.Public = false;
                r.Scattered = false;
                r.ScatteredUntil = 0;
                r.Site = new Site();
            }
            return removed;
        }
    }
}
