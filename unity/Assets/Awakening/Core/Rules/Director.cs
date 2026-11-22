using System;
using System.Collections.Generic;
using System.Globalization;

namespace Awakening.Core.Rules
{
    public static class Director
    {
        static string F(string text, params object[] args) => string.Format(CultureInfo.InvariantCulture, text, args);
        public static double Random(GameState state)
        {
            var value = Rng.Next(state.RngState);
            state.RngState = value.State;
            return value.Value;
        }
        public static void ScheduleThreat(GameState state, string id, bool ritual = false)
        {
            var r = state.Regions[id];
            if (r.Threat != null || r.Population.Followers <= 0) return;
            int warning = Balance.ThreatWarningMonths + GameData.Difficulties[state.Difficulty].Warning
                + (GameEngine.HasTalent(state, "quiet_friends") ? Balance.QuietFriendsWarning : 0)
                + (r.Figure != null && r.Figure.Kind == "politician" && !r.Figure.Revealed ? 2 : 0);
            string kind = ritual || r.Site.Invested ? "occult" : r.Public ? "raid" : "investigation";
            int severity = (int)Derived.ClampPct(1 + Math.Floor((r.Exposure + state.Awareness) / 60.0), 1, 3);
            r.Threat = new Threat { Kind = kind, DueMonth = state.ElapsedMonths + warning, Severity = severity };
            Derived.AddLog(state, GameData.ThreatNames[kind] + " in " + GameData.Regions[id].Name + F(". Respond within {0} months. If ignored: ", warning) + Threats.Describe(state, r));
        }
        public static void ResolveWorld(GameState state)
        {
            foreach (string id in GameData.RegionIds)
            {
                var r = state.Regions[id];
                string regionName = GameData.Regions[id].Name;
                if (r.Opportunity != null && r.Opportunity.ExpiresMonth <= state.ElapsedMonths)
                {
                    Derived.AddLog(state, GameData.OpportunityNames[r.Opportunity.Kind] + " passes in " + regionName + ".");
                    r.Opportunity = null;
                }
                if (r.Threat != null && r.Threat.DueMonth <= state.ElapsedMonths)
                {
                    var threat = r.Threat;
                    var c = Threats.Consequences(state, r);
                    double loss = Derived.LoseFollowers(r, c.Followers);
                    state.Telemetry.RaidLosses += loss;
                    if (threat.Kind == "occult" && r.Site.Invested)
                    {
                        r.Site.Progress -= c.PreparationLost;
                        // Restore the site can buy back the latest loss; an older one is forgotten.
                        if (c.PreparationLost > 0) r.Site.Recoverable = c.PreparationLost;
                    }
                    r.Exposure = Derived.ClampPct(r.Exposure + 5);
                    r.Threat = null;
                    string detail = "";
                    if (c.DestroysCell) detail = " The cell and any ritual site are destroyed.";
                    else if (c.PreparationLost != 0) detail = F(" Ritual loses {0} preparation months.", c.PreparationLost);
                    if (!c.DestroysCell && c.InterruptsSite) detail += " Ritual support is interrupted.";
                    if (GameEngine.HasTalent(state, "martyrs") && loss > 0)
                    {
                        double before = state.Dread;
                        GameEngine.GrantDread(state, Js.RoundHalfUp(loss * Balance.MartyrDreadPerFollower));
                        if (state.Dread > before) detail += " The martyrs feed the dark: +" + Js.Fixed(state.Dread - before, 0) + " Dread.";
                    }
                    if (GameEngine.HasTalent(state, "scapegoats") && state.Awareness > 0)
                    {
                        double before = state.Awareness;
                        state.Awareness = Derived.ClampPct(state.Awareness - Balance.ScapegoatSeverityAwareness * threat.Severity);
                        detail += " Scapegoats take the blame: −" + Js.Fixed(before - state.Awareness, 0) + " world awareness.";
                    }
                    Derived.AddLog(state, regionName + " loses " + Js.Fixed(loss, 0) + " followers to " + threat.Kind + "; income −" + Js.Fixed(c.IncomeLost, 2) + "/month." + detail);
                }
            }
        }
        public static bool SiteHunt(GameState state) => state.ElapsedMonths >= Balance.PreparationMonths;
        public static void Tick(GameState state)
        {
            if (state.ElapsedMonths == Balance.PreparationMonths - Balance.FinaleWarningMonths)
                Derived.AddLog(state, F("The world senses the coming alignment. In {0} months threats against ritual sites will intensify; keep Influence in reserve.", Balance.FinaleWarningMonths));
            bool hunt = SiteHunt(state);
            if (state.ElapsedMonths % Balance.ThreatRollMonths == 0)
            {
                foreach (string id in GameData.RegionIds)
                {
                    var r = state.Regions[id];
                    if (r.Population.Followers <= 0 || r.Threat != null || r.Scattered) continue;
                    double believers = r.Population.Believers / Math.Max(1.0, Derived.LivePopulation(r));
                    double pressure = (0.02 + r.Exposure / 500.0 + (r.Public ? 0.1 : 0.0) + (r.Site.Invested ? 0.12 : 0.0) + (hunt && r.Site.Invested ? Balance.FinaleSitePressure : 0.0)
                        + state.Awareness / 1500.0 + believers * 0.15 + r.Madness / 2000.0)
                        * GameData.Difficulties[state.Difficulty].Pressure * (0.6 + GameData.Regions[id].InstitutionalResistance / 150.0);
                    if (Random(state) < pressure) ScheduleThreat(state, id);
                }
            }
            if (state.ElapsedMonths % 12 == 0)
            {
                var available = new List<string>();
                foreach (string candidate in GameData.RegionIds)
                    if (state.Regions[candidate].Opportunity == null && state.Regions[candidate].Population.Followers > 0) available.Add(candidate);
                // Consume this draw even if no cell qualifies.
                int index = (int)Math.Floor(Random(state) * available.Count);
                if (available.Count == 0) return;
                string id = available[index];
                var r = state.Regions[id];
                string[] kinds = r.Figure != null ? new[] { "nightmare", "eclipse" } : new[] { "nightmare", "eclipse", "celebrity", "politician" };
                string kind = kinds[(int)Math.Floor(Random(state) * kinds.Length)];
                r.Opportunity = new Opportunity { Kind = kind, ExpiresMonth = state.ElapsedMonths + 8 };
                Derived.AddLog(state, GameData.OpportunityNames[kind] + " in " + GameData.Regions[id].Name + ". Opportunity lasts 8 months.");
            }
        }
    }
}
