using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;

namespace Awakening.Core.Dev
{
    /// <summary>
    /// Scripted policy from godot/dev/playthrough.gd, one strategy per talent branch (alignment-5): early is the
    /// public congregation, late the hidden hand that goes public at month 160, terror the harbinger that never does.
    /// All gains use normal commands.
    /// </summary>
    public static class Playthrough
    {
        public static readonly string[] Strategies = { "early", "late", "terror" };
        /// <summary>Talents each strategy takes, in order, whenever a seal is open and Influence allows.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> TalentPlans = new Dictionary<string, string[]>
        {
            ["early"] = new[] { "swelling_flock", "open_worship", "faith_among_faiths", "quiet_friends", "contingency" },
            ["late"] = new[] { "quiet_friends", "contingency", "bought_press", "swelling_flock", "martyrs" },
            ["terror"] = new[] { "red_altars", "scapegoats", "offerings", "quiet_friends", "contingency" },
        };
        public static readonly string[] Targets = { "latin_america", "middle_east", "oceania" };
        public const string Farm = "africa";
        /// <summary>World awareness at which the policy buries the record with spare Influence.</summary>
        public const int BuryAt = 60;
        /// <summary>The policy only silences while world awareness stays at or below this.</summary>
        public const int SilenceCeiling = 75;
        /// <summary>Spare Influence is saved for talents only while world awareness is below this.</summary>
        public const int SaveBelow = 50;
        /// <summary>The public congregation waits for Open worship before going public, unless it is this late.</summary>
        public const int WorshipDeadline = 120;
        public static int DreadOwed(GameState state)
        {
            int funded = 0;
            foreach (string id in Targets) if (state.Regions[id].Site.Invested) funded++;
            int owed = Balance.FinalDread;
            for (int i = funded; i < Targets.Length; i++) owed += Balance.SiteDreadCosts[Math.Min(i, Balance.SiteDreadCosts.Length - 1)];
            return owed;
        }
        public static double Reserve(GameState state)
        {
            double owed = 0.0;
            foreach (string id in GameData.RegionIds)
            {
                var r = state.Regions[id];
                if (r.Site.Invested) owed += Balance.SiteUpkeep * 4;
                if (r.Threat != null && GameEngine.Cooldown(r, "counter") <= state.ElapsedMonths) owed += GameEngine.CounterCost(state, r);
            }
            return owed;
        }
        /// <summary>While a seal is open, spare Influence is saved for the strategy's next talent.</summary>
        public static int Saving(GameState state, string strategy)
        {
            if (GameEngine.TalentSlotsUnused(state) == 0 || state.Awareness >= SaveBelow) return 0;
            foreach (string talent in TalentPlans[strategy]) if (!GameEngine.HasTalent(state, talent)) return GameEngine.TalentCost(state);
            return 0;
        }
        public static string ConcealNeed(GameState state, RegionState r)
        {
            double floor = r.Site.Invested ? 40.0 : r.Public ? 20.0 : 0.0;
            if (r.Exposure > Math.Max(30.0, floor + 10.0)) return "evidence";
            // Concealing for awareness only pays while its world effect is ready (alignment-4).
            if (state.Awareness >= 50 && GameEngine.ConcealWorldCooldown(state) == 0 && state.Influence >= GameEngine.ConcealCost(state) + 40) return "awareness";
            return "";
        }
        public static GameState Respond(GameState state, string id)
        {
            var r = state.Regions[id];
            if (Targets.Contains(id) && GameEngine.PreviewAction(state, "counter", id).Ok) return GameEngine.PerformAction(state, "counter", id).State;
            var c = Threats.Consequences(state, r);
            var silence = GameEngine.PreviewAction(state, "silence", id);
            int owed = state.Status == "awakening" ? 0 : DreadOwed(state);
            bool matters = c.DestroysCell || r.Site.Invested && (c.InterruptsSite || c.PreparationLost > 0);
            // Offerings to the deep: feed the hunters instead of silencing, if the flock can spare it.
            if (matters && GameEngine.HasTalent(state, "offerings") && r.Population.Followers - Balance.SacrificeFollowers >= (r.Site.Invested ? Derived.SiteRequirement(r) + 20 : 60)
                && GameEngine.PreviewAction(state, "sacrifice", id).Ok)
                return GameEngine.PerformAction(state, "sacrifice", id).State;
            if (silence.Ok && state.Dread - silence.Dread >= owed && matters && state.Awareness + silence.Awareness <= SilenceCeiling)
                return GameEngine.PerformAction(state, "silence", id).State;
            double hidingCost = GameEngine.ScatterMonths(state) * Derived.RecruitmentPerMonth(state, r);
            if (!r.Site.Invested && !r.Public && c.Followers > hidingCost && GameEngine.PreviewAction(state, "scatter", id).Ok)
                return GameEngine.PerformAction(state, "scatter", id).State;
            return state;
        }
        /// <param name="onMonth">Optional observer, called with the state after each month's orders and before the month passes.</param>
        public static GameState Play(string strategy, string seed, int stopMonth = Balance.Deadline, Action<GameState> onMonth = null, string difficulty = "normal")
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState(difficulty, seed), Farm).State;
            Action<string, string> act = (id, region) =>
            {
                if (GameEngine.PreviewAction(s, id, region).Ok) s = GameEngine.PerformAction(s, id, region).State;
            };
            Action<string, string> spare = (id, region) =>
            {
                var preview = GameEngine.PreviewAction(s, id, region);
                if (preview.Ok && s.Influence - preview.Influence >= Reserve(s) + Saving(s, strategy)) s = GameEngine.PerformAction(s, id, region).State;
            };
            Action spareBury = () =>
            {
                // Bury where the extra Exposure hurts least: the cell with the least of it.
                string best = null;
                foreach (string id in GameData.RegionIds)
                    if (s.Regions[id].Population.Followers > 0 && GameEngine.PreviewAction(s, "bury", id).Ok && (best == null || s.Regions[id].Exposure < s.Regions[best].Exposure)) best = id;
                if (best != null) spare("bury", best);
            };
            for (int step = 0; step < stopMonth; step++)
            {
                if (GameEngine.IsOver(s)) break;
                foreach (string id in GameData.RegionIds) if (s.Regions[id].Threat != null) s = Respond(s, id);
                foreach (string id in Targets)
                {
                    var r = s.Regions[id];
                    if (r.Opportunity != null && (r.Opportunity.Kind == "nightmare" || r.Opportunity.Kind == "eclipse" && r.Site.Invested)) act("exploit", id);
                }
                foreach (string id in Targets) if (s.Regions[id].Population.Followers <= 0) act("expand", id);
                foreach (string talent in TalentPlans[strategy])
                {
                    if (GameEngine.HasTalent(s, talent)) continue;
                    if (GameEngine.TalentReason(s, talent).Length == 0)
                        s = GameEngine.TakeTalent(s, talent).State;
                    break;
                }
                foreach (string id in Targets)
                    if (s.Regions[id].Site.Recoverable > 0) spare("restore", id);
                foreach (string id in Targets) if (!s.Regions[id].Site.Invested) act("prepare", id);
                int owed = DreadOwed(s);
                // Keep these pre-action references, just like the Godot policy's dictionaries.
                var farm = s.Regions[Farm];
                if (farm.Population.Followers < 700) spare("recruit", Farm);
                if (s.Dread < owed && farm.Population.Followers - Balance.SacrificeFollowers >= 260) act("sacrifice", Farm);
                string farmNeed = ConcealNeed(s, farm);
                if (farmNeed == "evidence") act("conceal", Farm);
                else if (farmNeed == "awareness") spare("conceal", Farm);
                foreach (string id in Targets)
                {
                    var r = s.Regions[id];
                    if (r.Population.Followers <= 0) continue;
                    double need = Derived.SiteRequirement(r);
                    double buffer = need + 160;
                    if (r.Population.Followers < buffer) spare("recruit", id);
                    int revealMonth = strategy == "late" ? 160 : 8;
                    bool allowsPublic = s.ElapsedMonths >= revealMonth && (strategy != "terror" || id == Farm)
                        && (strategy != "early" || GameEngine.HasTalent(s, "open_worship") || s.ElapsedMonths >= WorshipDeadline);
                    if (allowsPublic && !r.Public && r.Population.Followers < need) spare("reveal", id);
                    // Open worship takes away most of the reason to go back underground.
                    bool stayPublic = GameEngine.HasTalent(s, "open_worship");
                    if (r.Public && (!allowsPublic || r.Population.Followers >= buffer && !stayPublic)) act("hide", id);
                    string needConceal = ConcealNeed(s, r);
                    if (needConceal == "evidence") act("conceal", id);
                    else if (needConceal == "awareness") spare("conceal", id);
                    if (s.Dread < owed && r.Population.Followers - Balance.SacrificeFollowers >= need + 120) act("sacrifice", id);
                }
                if (s.Awareness >= BuryAt) spareBury();
                if (s.Status == "alignment" && Derived.ReadySites(s) >= 3) s = GameEngine.StartAwakening(s).State;
                onMonth?.Invoke(s);
                s = GameEngine.AdvanceSimulation(s, 1);
            }
            return s;
        }
    }
}
