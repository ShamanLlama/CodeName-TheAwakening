using System;
using System.Linq;
using Awakening.Core.Rules;

namespace Awakening.Core.Dev
{
    /// <summary>Scripted policy from godot/dev/playthrough.gd. All gains use normal commands.</summary>
    public static class Playthrough
    {
        public static readonly string[] Targets = { "latin_america", "middle_east", "oceania" };
        public const string Farm = "africa";
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
        public static string ConcealNeed(GameState state, RegionState r)
        {
            double floor = r.Site.Invested ? 40.0 : r.Public ? 20.0 : 0.0;
            if (r.Exposure > Math.Max(30.0, floor + 10.0)) return "evidence";
            if (state.Awareness >= 50 && state.Influence >= GameEngine.ConcealCost(state) + 40) return "awareness";
            return "";
        }
        public static GameState Respond(GameState state, string id)
        {
            var r = state.Regions[id];
            if (Targets.Contains(id) && GameEngine.PreviewAction(state, "counter", id).Ok) return GameEngine.PerformAction(state, "counter", id).State;
            var c = Threats.Consequences(r);
            var silence = GameEngine.PreviewAction(state, "silence", id);
            int owed = state.Status == "awakening" ? 0 : DreadOwed(state);
            if (silence.Ok && state.Dread - silence.Dread >= owed && (c.DestroysCell || r.Site.Invested && (c.InterruptsSite || c.PreparationLost > 0)))
                return GameEngine.PerformAction(state, "silence", id).State;
            double hidingCost = Balance.ScatterMonths * Derived.RecruitmentPerMonth(state, r);
            if (!r.Site.Invested && !r.Public && c.Followers > hidingCost && GameEngine.PreviewAction(state, "scatter", id).Ok)
                return GameEngine.PerformAction(state, "scatter", id).State;
            return state;
        }
        public static GameState Play(string strategy, string seed, int stopMonth = Balance.Deadline)
        {
            var s = GameEngine.PlaceStartingFoothold(GameEngine.CreateGameState("normal", seed), Farm).State;
            Action<string, string> act = (id, region) =>
            {
                if (GameEngine.PreviewAction(s, id, region).Ok) s = GameEngine.PerformAction(s, id, region).State;
            };
            Action<string, string> spare = (id, region) =>
            {
                var preview = GameEngine.PreviewAction(s, id, region);
                if (preview.Ok && s.Influence - preview.Influence >= Reserve(s)) s = GameEngine.PerformAction(s, id, region).State;
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
                if (s.Specialization == null && s.ElapsedMonths >= 20 && s.Influence >= 70)
                    s = GameEngine.ChooseSpecialization(s, strategy == "early" ? "recruitment" : strategy == "late" ? "infiltration" : "terror").State;
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
                    bool allowsPublic = s.ElapsedMonths >= revealMonth && (strategy != "hub" || id == Farm);
                    if (allowsPublic && !r.Public && r.Population.Followers < need) spare("reveal", id);
                    if (r.Public && (!allowsPublic || r.Population.Followers >= buffer)) act("hide", id);
                    string needConceal = ConcealNeed(s, r);
                    if (needConceal == "evidence") act("conceal", id);
                    else if (needConceal == "awareness") spare("conceal", id);
                    if (s.Dread < owed && r.Population.Followers - Balance.SacrificeFollowers >= need + 120) act("sacrifice", id);
                }
                if (s.Status == "alignment" && Derived.ReadySites(s) >= 3) s = GameEngine.StartAwakening(s).State;
                s = GameEngine.AdvanceSimulation(s, 1);
            }
            return s;
        }
    }
}
