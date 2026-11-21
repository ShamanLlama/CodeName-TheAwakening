using System;
using System.Globalization;
using System.Linq;

namespace Awakening.Core.Rules
{
    /// <summary>Monthly simulation. Commands clone accepted input and preserve refused input.</summary>
    public static class GameEngine
    {
        static string F(string text, params object[] args) => string.Format(CultureInfo.InvariantCulture, text, args);
        public static bool IsActive(GameState s) => s.Status == "playing" || s.Status == "alignment" || s.Status == "awakening";
        public static bool IsOver(GameState s) => s.Status == "won" || s.Status == "lost";
        static CommandResult Fail(GameState state, string message) => new CommandResult { Ok = false, Message = message, State = state };
        static CommandResult Success(GameState state) => new CommandResult { Ok = true, State = state };
        public static GameState CreateGameState(string difficulty, string seed)
        {
            var regions = new RegionMap();
            foreach (string id in GameData.RegionIds)
            {
                int capacity = GameData.Regions[id].PopulationWeight * Balance.PopulationScale;
                double deniers = Js.RoundHalfUp(capacity * GameData.Regions[id].Skepticism / 200.0);
                regions[id] = new RegionState { Population = new Population { Unaware = capacity - deniers, Deniers = deniers } };
            }
            return new GameState
            {
                Version = 2, Ruleset = Balance.Ruleset, Status = "placing", Difficulty = difficulty, Seed = seed,
                RngState = Rng.SeedToState(seed), ElapsedMonths = 0, Speed = 1,
                Influence = Balance.StartingInfluence, Dread = Balance.StartingDread, Awareness = 0.0,
                Specialization = null, FinalProgress = 0, Regions = regions, NextLogId = 1, ResultReason = null
            };
        }
        public static CommandResult PlaceStartingFoothold(GameState state, string id)
        {
            if (state.Status != "placing" || !GameData.RegionIds.Contains(id)) return Fail(state, "Choose a region before the run begins.");
            var next = state.Clone();
            Derived.Recruit(next.Regions[id], Balance.StartingFollowers);
            next.Regions[id].Exposure = 5.0;
            next.Status = "playing";
            Derived.AddLog(next, "A hidden congregation gathers in " + GameData.Regions[id].Name + F(". The alignment is {0} months away.", Balance.PreparationMonths));
            return Success(next);
        }
        public static int CounterCost(GameState state, RegionState r)
        {
            int cost = Balance.CounterSeverityCost * (r.Threat != null ? r.Threat.Severity : 1);
            return state.Specialization == "infiltration" ? (int)Js.RoundHalfUp(cost * 2 / 3.0) : cost;
        }
        public static int SilenceCost(GameState state, RegionState r)
        {
            int each = state.Specialization == "terror" ? Balance.SilenceSeverityDreadTerror : Balance.SilenceSeverityDread;
            return each * (r.Threat != null ? r.Threat.Severity : 1);
        }
        public static double AwarenessCost(string id, RegionState r)
        {
            switch (id)
            {
                case "reveal": return r.Revealed ? 0.0 : Balance.RevealAwareness;
                case "manifest": return Balance.ManifestAwareness;
                case "prepare": return Balance.SiteAwareness;
                case "reveal_figure": return Balance.FigureAwareness;
                case "silence": return Balance.SilenceSeverityAwareness * (r.Threat != null ? r.Threat.Severity : 1);
                default: return 0.0;
            }
        }
        /// <summary>Months until concealment lowers world awareness again; 0 when it is available.</summary>
        public static int ConcealWorldCooldown(GameState state) => Math.Max(0, state.WorldConcealReadyAt - state.ElapsedMonths);
        /// <summary>Whether a conceal now would also lower world awareness.</summary>
        static bool ConcealsWorld(GameState state) => ConcealWorldCooldown(state) == 0 && state.Awareness > 0;
        public static int ConcealCost(GameState state) => GameData.Actions["conceal"].Influence
            + (ConcealWorldCooldown(state) > 0 ? 0 : (int)Js.RoundHalfUp(state.Awareness * Balance.ConcealAwarenessPrice));
        public static int SiteDreadCost(GameState state)
        {
            int funded = 0;
            foreach (string id in GameData.RegionIds) if (state.Regions[id].Site.Invested) funded++;
            return Balance.SiteDreadCosts[Math.Min(funded, Balance.SiteDreadCosts.Length - 1)];
        }
        /// <summary>Preparation months the region's current eclipse and secret figure could still add to a site.</summary>
        internal static int Acceleration(RegionState r) => (r.Opportunity != null && r.Opportunity.Kind == "eclipse" ? 8 : 0) + (r.Figure != null && !r.Figure.Revealed ? Balance.FigureSiteProgress : 0);
        internal static int Cooldown(RegionState r, string id) => r.Cooldowns.TryGetValue(id, out int value) ? value : 0;
        public static ActionPreview PreviewAction(GameState state, string id, string regionId)
        {
            if (!GameData.Actions.ContainsKey(id) || !state.Regions.ContainsKey(regionId))
                return new ActionPreview { Ok = false, Reason = "Unknown action or region.", Effect = "", Risk = "" };
            var a = GameData.Actions[id];
            var r = state.Regions[regionId];
            var p = r.Population;
            int costInfluence = id == "counter" ? CounterCost(state, r) : id == "conceal" ? ConcealCost(state) : a.Influence;
            int costDread = id == "manifest" && state.Specialization == "terror" ? 15 : id == "prepare" ? SiteDreadCost(state) : id == "silence" ? SilenceCost(state, r) : a.Dread;
            double costAwareness = AwarenessCost(id, r);
            int costFollowers = a.Followers;
            string reason = IsActive(state) ? "" : "Start a run before acting; completed runs cannot change.";
            int cooldown = Cooldown(r, id) - state.ElapsedMonths;
            if (reason.Length == 0 && cooldown > 0) reason = F("Available in {0} months.", cooldown);
            if (reason.Length == 0 && state.Influence < costInfluence) reason = F("Need {0} Influence.", costInfluence);
            if (reason.Length == 0 && state.Dread < costDread) reason = F("Need {0} Dread.", costDread);
            if (reason.Length == 0 && p.Followers < costFollowers) reason = F("Need {0} local followers.", costFollowers);
            if (reason.Length == 0 && id != "expand" && p.Followers <= 0) reason = "Establish a cult here first.";
            if (reason.Length == 0 && r.Scattered && new[] { "recruit", "reveal", "prepare", "reveal_figure" }.Contains(id))
                reason = F("The flock is scattered for {0} more months.", r.ScatteredUntil - state.ElapsedMonths);
            if (reason.Length == 0 && (id == "recruit" || id == "expand") && Derived.Recruitable(r) < 1) reason = "No recruits available. Use a manifestation to change disbelief.";
            if (reason.Length == 0 && id == "expand" && p.Followers > 0) reason = "This region already has a cell.";
            if (reason.Length == 0 && id == "expand" && Derived.TotalFollowers(state) > 0 && !HasAdjacentCell(state, regionId)) reason = "Requires a cult in an adjacent region.";
            if (reason.Length == 0 && id == "reveal" && r.Public) reason = "Worship is already public.";
            if (reason.Length == 0 && id == "hide" && !r.Public) reason = "This cult is already underground.";
            if (reason.Length == 0 && id == "conceal" && r.Exposure <= EvidenceFloor(r) && !ConcealsWorld(state))
                reason = state.Awareness <= 0 ? "Nothing to conceal. Public rites remain visible."
                    : F("Nothing to conceal here. The world effect returns in {0} months.", ConcealWorldCooldown(state));
            if (reason.Length == 0 && id == "manifest" && p.Deniers + p.Unaware < 1) reason = "Everyone here already believes.";
            if (reason.Length == 0 && id == "prepare" && r.Site.Invested) reason = "This site's preparation is already funded.";
            if (reason.Length == 0 && id == "prepare" && !Derived.Supported(r)) reason = "Maintain at least " + Js.Num(Derived.SiteRequirement(r)) + " local followers.";
            int acceleration = Acceleration(r);
            int preparationLeft = Balance.Deadline - state.ElapsedMonths - Balance.FinalMonths;
            if (reason.Length == 0 && id == "prepare" && preparationLeft < Balance.SiteMonths - acceleration)
                reason = F("Too late: preparation needs {0} more months and the final rite needs {1}, but only {2} remain.", Balance.SiteMonths - acceleration, Balance.FinalMonths, Math.Max(0, Balance.Deadline - state.ElapsedMonths));
            if (reason.Length == 0 && new[] { "counter", "scatter", "silence" }.Contains(id) && r.Threat == null) reason = "No approaching threat in this region.";
            if (reason.Length == 0 && id == "exploit" && (r.Opportunity == null || !new[] { "nightmare", "eclipse" }.Contains(r.Opportunity.Kind))) reason = "Requires an active nightmare or eclipse.";
            if (reason.Length == 0 && id == "exploit" && r.Opportunity != null && r.Opportunity.Kind == "eclipse" && (!r.Site.Invested || r.Site.Progress >= Balance.SiteMonths)) reason = "An eclipse needs an unfinished ritual site.";
            if (reason.Length == 0 && id == "convert" && (r.Figure != null || r.Opportunity == null || !new[] { "celebrity", "politician" }.Contains(r.Opportunity.Kind))) reason = "Requires an unconverted prominent-person opportunity.";
            if (reason.Length == 0 && id == "reveal_figure" && (r.Figure == null || r.Figure.Revealed)) reason = "Requires a secretly converted figure.";
            if (reason.Length == 0 && id == "reveal_figure" && !r.Site.Invested) reason = "Prepare a local ritual site to receive their awakening contribution.";
            string effect = a.Description;
            string risk = "";
            switch (id)
            {
                case "sacrifice":
                    int gain = state.Specialization == "terror" ? 35 : Balance.SacrificeDread;
                    effect = "+" + Js.Fixed(Math.Min(gain, Balance.DreadCap - state.Dread), 0) + " Dread. " + Js.Fixed(Math.Max(0.0, p.Followers - costFollowers), 0)
                        + " followers remain; income falls by " + Js.Fixed(costFollowers * Balance.InfluencePerFollower, 2) + "/month.";
                    risk = "+12 Exposure. Fewer than " + Js.Num(Derived.SiteRequirement(r)) + " followers interrupts this site's ritual.";
                    break;
                case "reveal":
                    risk = "+18 Exposure" + (r.Revealed ? "" : ", +" + Js.Num(costAwareness) + " world awareness") + "; recruitment backlash for 6 months, then full public growth.";
                    break;
                case "scatter":
                    effect = "The threat finds nobody. −" + Js.Fixed(Math.Min(Balance.ScatterExposure, Math.Max(0.0, r.Exposure - (r.Site.Invested ? 40.0 : 0.0))), 0) + " Exposure.";
                    risk = F("For {0} months this cell recruits nobody and earns nothing (−{1} Influence/month).", Balance.ScatterMonths, Js.Fixed(Derived.RegionIncome(r), 2))
                        + (r.Site.Invested ? " Its ritual site stalls and cannot carry the final rite." : "") + (r.Public ? " Public worship ends." : "");
                    break;
                case "silence":
                    effect = "The threat ends tonight.";
                    risk = "+" + Js.Num(costAwareness) + " world awareness, +" + Js.Num(Balance.SilenceExposure) + " Exposure. The disappearances are noticed.";
                    break;
                case "counter": risk = F("No further obstruction here for {0} months.", a.Cooldown); break;
                case "conceal":
                    string local = "−" + Js.Fixed(Math.Min(25.0, Math.Max(0.0, r.Exposure - EvidenceFloor(r))), 0) + " Exposure";
                    if (ConcealsWorld(state))
                    {
                        effect = local + ", −" + Js.Fixed(Math.Min(Balance.ConcealAwareness, state.Awareness), 0) + " world awareness. The price rises with awareness.";
                        risk = F("The world effect then rests for {0} months in every region.", Balance.ConcealWorldMonths);
                    }
                    else if (state.Awareness > 0) effect = local + F(" only. The world effect returns in {0} months.", ConcealWorldCooldown(state));
                    else effect = local + ".";
                    break;
                case "manifest": risk = "+15 Exposure and +8 awareness. New believers can strengthen resistance."; break;
                case "prepare":
                    risk = "Upkeep " + Js.Num(Balance.SiteUpkeep) + " Influence/month while funded; unpaid months stall the site. Site remains exposed, even underground. An occult response is scheduled.";
                    if (preparationLeft < Balance.SiteMonths) risk += F(" Needs acceleration to finish in time: {0} preparation months, then a {1}-month final rite.", Balance.SiteMonths - acceleration, Balance.FinalMonths);
                    break;
                case "reveal_figure": risk = "+25 Exposure, +8 awareness; 12 months of recruitment backlash. Secret benefits end."; break;
                case "recruit": effect = "+" + Js.Fixed(Math.Min(Balance.RecruitAmount, Derived.Recruitable(r)), 0) + " followers; +3 Exposure."; break;
                case "exploit": effect = r.Opportunity != null && r.Opportunity.Kind == "nightmare" ? "+30 Dread once, up to the resource cap." : "+8 months of site preparation once."; break;
            }
            return new ActionPreview { Ok = reason.Length == 0, Reason = reason, Influence = costInfluence, Dread = costDread, Followers = costFollowers, Awareness = costAwareness, Effect = effect, Risk = risk };
        }
        static bool HasAdjacentCell(GameState state, string regionId)
        {
            foreach (string other in GameData.Regions[regionId].Adjacent) if (state.Regions[other].Population.Followers > 0) return true;
            return false;
        }
        static double EvidenceFloor(RegionState r) => r.Site.Invested ? 40.0 : r.Public ? 20.0 : 0.0;
        public static CommandResult PerformAction(GameState state, string id, string regionId)
        {
            var preview = PreviewAction(state, id, regionId);
            if (!preview.Ok) return Fail(state, preview.Reason);
            var next = state.Clone();
            var r = next.Regions[regionId];
            var t = next.Telemetry;
            next.Influence -= preview.Influence;
            next.Dread -= preview.Dread;
            t.InfluenceSpent += preview.Influence;
            t.DreadSpent += preview.Dread;
            t.Actions[id] = (t.Actions.TryGetValue(id, out int count) ? count : 0) + 1;
            r.Cooldowns[id] = next.ElapsedMonths + GameData.Actions[id].Cooldown;
            next.Awareness = Derived.ClampPct(next.Awareness + preview.Awareness);
            switch (id)
            {
                case "recruit": case "expand":
                    Derived.Recruit(r, id == "recruit" ? Balance.RecruitAmount : Balance.ExpandAmount);
                    r.Exposure = Derived.ClampPct(r.Exposure + 3);
                    break;
                case "sacrifice":
                    t.Sacrificed += Derived.LoseFollowers(r, preview.Followers);
                    GrantDread(next, next.Specialization == "terror" ? 35 : Balance.SacrificeDread);
                    r.Exposure = Derived.ClampPct(r.Exposure + 12);
                    break;
                case "reveal":
                    r.Public = true; r.Revealed = true;
                    r.Exposure = Derived.ClampPct(r.Exposure + 18);
                    r.BacklashUntil = Math.Max(r.BacklashUntil, next.ElapsedMonths + 6);
                    if (!t.Revelations.ContainsKey(regionId)) t.Revelations[regionId] = next.ElapsedMonths;
                    break;
                case "hide": r.Public = false; break;
                case "conceal":
                    r.Exposure = Math.Max(EvidenceFloor(r), r.Exposure - 25);
                    if (ConcealsWorld(state))
                    {
                        next.Awareness = Derived.ClampPct(next.Awareness - Balance.ConcealAwareness);
                        next.WorldConcealReadyAt = next.ElapsedMonths + Balance.ConcealWorldMonths;
                    }
                    break;
                case "manifest":
                    var p = r.Population;
                    double denied = Math.Min(p.Deniers, Balance.ManifestationDeniers);
                    double unaware = Math.Min(p.Unaware, Balance.ManifestationUnaware);
                    p.Deniers -= denied; p.Unaware -= unaware; p.Believers += denied + unaware;
                    r.Madness = Derived.ClampPct(r.Madness + 20);
                    r.Exposure = Derived.ClampPct(r.Exposure + 15);
                    break;
                case "prepare":
                    r.Site.Invested = true; r.Site.Dormant = false;
                    r.Exposure = Math.Max(40.0, r.Exposure);
                    Director.ScheduleThreat(next, regionId, true);
                    break;
                case "counter": r.Threat = null; t.ThreatsCountered++; break;
                case "scatter":
                    r.Threat = null; r.Public = false; r.Scattered = true;
                    r.ScatteredUntil = next.ElapsedMonths + Balance.ScatterMonths;
                    r.Exposure = Math.Max(EvidenceFloor(r), r.Exposure - Balance.ScatterExposure);
                    break;
                case "silence": r.Threat = null; r.Exposure = Derived.ClampPct(r.Exposure + Balance.SilenceExposure); break;
                case "exploit":
                    if (r.Opportunity.Kind == "nightmare") GrantDread(next, 30);
                    else r.Site.Progress = Math.Min(Balance.SiteMonths, r.Site.Progress + 8);
                    r.Opportunity = null;
                    break;
                case "convert":
                    r.Figure = new Figure { Kind = r.Opportunity.Kind, Revealed = false };
                    r.Opportunity = null;
                    r.Exposure = Derived.ClampPct(r.Exposure + 5);
                    break;
                case "reveal_figure":
                    r.Figure.Revealed = true;
                    Derived.Recruit(r, 60);
                    r.Site.Progress = Math.Min(Balance.SiteMonths, r.Site.Progress + Balance.FigureSiteProgress);
                    r.Exposure = Derived.ClampPct(r.Exposure + 25);
                    r.BacklashUntil = next.ElapsedMonths + 12;
                    break;
            }
            Derived.AddLog(next, GameData.Actions[id].Name + " · " + GameData.Regions[regionId].Name + ". " + preview.Effect);
            return Success(next);
        }
        static void PayUpkeep(GameState s)
        {
            foreach (string id in GameData.RegionIds)
            {
                var site = s.Regions[id].Site;
                if (!site.Invested) continue;
                bool wasDormant = site.Dormant;
                site.Dormant = s.Influence < Balance.SiteUpkeep;
                if (!site.Dormant) { s.Influence -= Balance.SiteUpkeep; s.Telemetry.UpkeepPaid += Balance.SiteUpkeep; }
                if (site.Dormant && !wasDormant)
                    Derived.AddLog(s, "The ritual site in " + GameData.Regions[id].Name + " lies dormant: upkeep of " + Js.Num(Balance.SiteUpkeep) + " Influence is unpaid.");
            }
        }
        static void GatherScattered(GameState s)
        {
            foreach (string id in GameData.RegionIds)
            {
                var r = s.Regions[id];
                if (r.Scattered && s.ElapsedMonths >= r.ScatteredUntil)
                {
                    r.Scattered = false;
                    Derived.AddLog(s, "The flock in " + GameData.Regions[id].Name + " gathers again.");
                }
            }
        }
        static void GrantDread(GameState s, double amount)
        {
            double gain = Math.Min(amount, Balance.DreadCap - s.Dread);
            s.Dread += gain;
            s.Telemetry.DreadEarned += gain;
        }
        public static CommandResult ChooseSpecialization(GameState state, string choice)
        {
            if (!IsActive(state) || state.Specialization != null || !GameData.SpecializationIds.Contains(choice)) return Fail(state, "Choose one doctrine per run.");
            if (state.Influence < GameData.SpecializationCost) return Fail(state, "A doctrine requires 40 Influence.");
            var next = state.Clone();
            next.Influence -= GameData.SpecializationCost;
            next.Telemetry.InfluenceSpent += GameData.SpecializationCost;
            next.Specialization = choice;
            Derived.AddLog(next, "The cult commits to " + choice + ". Other doctrines are closed for this run.");
            return Success(next);
        }
        public static GameState SetGameSpeed(GameState state, int speed)
        {
            if (IsOver(state) || speed < 0 || speed > 2) return state;
            var next = state.Clone();
            next.Speed = speed;
            return next;
        }
        public static string AwakeningReason(GameState state)
        {
            if (state.Status != "alignment") return state.Status == "playing" ? F("The stars align in {0} months.", Balance.PreparationMonths - state.ElapsedMonths) : "The final rite is not available now.";
            if (Derived.ReadySites(state) < Balance.RequiredSites) return F("Need {0} prepared sites, each supported by a quarter of its region.", Balance.RequiredSites);
            if (state.Dread < Balance.FinalDread) return "Reserve 100 Dread for the final rite.";
            if (Balance.Deadline - state.ElapsedMonths < Balance.FinalMonths) return "Fewer than 12 months remain. The final rite can no longer finish in time.";
            return "";
        }
        public static CommandResult StartAwakening(GameState state)
        {
            string reason = AwakeningReason(state);
            if (reason.Length != 0) return Fail(state, reason);
            var next = state.Clone();
            next.Dread -= Balance.FinalDread;
            next.Telemetry.DreadSpent += Balance.FinalDread;
            next.Status = "awakening";
            Derived.AddLog(next, "The final rite begins. Maintain three supported sites for 12 months.");
            return Success(next);
        }
        public const string ConcededReason = "The cult lets the stars pass unanswered. Its cells scatter into rumor, and in R'lyeh the dreamer sleeps on.";
        /// <summary>The player may end the run as a loss once the final rite can no longer be completed and has not begun.</summary>
        public static bool CanConcede(GameState state) => IsActive(state) && state.Status != "awakening" && !Forecast.Margin(state).Fits;
        public static GameState Concede(GameState state)
        {
            if (!CanConcede(state)) return state;
            var next = state.Clone();
            next.Status = "lost"; next.Speed = 0; next.ResultCode = "conceded";
            next.ResultReason = ConcededReason;
            Derived.AddLog(next, next.ResultReason);
            return next;
        }
        public static GameState AdvanceSimulation(GameState state, double months)
        {
            if (!IsActive(state) || double.IsNaN(months) || double.IsInfinity(months) || months < 1) return state;
            var next = state.Clone();
            for (int month = 0; month < Math.Min(Math.Floor(months), Balance.Deadline); month++)
            {
                if (!IsActive(next)) break;
                next.ElapsedMonths++;
                GatherScattered(next);
                Director.ResolveWorld(next);
                double income = Math.Min(Derived.InfluencePerMonth(next), Math.Max(0.0, Balance.InfluenceCap - next.Influence));
                next.Influence += income;
                next.Telemetry.InfluenceEarned += income;
                PayUpkeep(next);
                foreach (string id in GameData.RegionIds)
                {
                    var r = next.Regions[id];
                    Derived.Recruit(r, Derived.RecruitmentPerMonth(next, r));
                    if (r.Population.Followers > 0)
                    {
                        r.Exposure = Derived.ClampPct(Math.Max(EvidenceFloor(r), r.Exposure + (r.Public ? 0.6 : -0.36)));
                        if (r.Public || r.Site.Invested) next.Awareness = Derived.ClampPct(next.Awareness + 0.075);
                    }
                    r.Madness = Math.Max(0.0, r.Madness - 0.3);
                    if (r.Site.Invested && !r.Site.Dormant && Derived.Supported(r) && r.Site.Progress < Balance.SiteMonths)
                    {
                        r.Site.Progress++;
                        if (r.Site.Progress == Balance.SiteMonths) Derived.AddLog(next, "The ritual site in " + GameData.Regions[id].Name + " is prepared.");
                    }
                }
                next.Awareness = Derived.ClampPct(next.Awareness + Derived.CultShare(next) * Balance.AwarenessPerCapita);
                if (next.Status == "awakening" && Derived.ReadySites(next) >= Balance.RequiredSites) next.FinalProgress++;
                if (next.FinalProgress >= Balance.FinalMonths)
                {
                    next.Status = "won"; next.Speed = 0; next.ResultCode = "awakened";
                    next.ResultReason = "Three surviving ritual sites opened the way. Cthulhu awakens.";
                    Derived.AddLog(next, next.ResultReason);
                    break;
                }
                if (next.Awareness >= 100)
                {
                    next.Status = "lost"; next.Speed = 0; next.ResultCode = "world_awareness";
                    next.ResultReason = "The world knows. Every cell is hunted and the rite dies with them.";
                    Derived.AddLog(next, next.ResultReason);
                    break;
                }
                if (next.ElapsedMonths >= Balance.Deadline)
                {
                    next.Status = "lost"; next.Speed = 0; next.ResultCode = "deadline";
                    next.ResultReason = F("The alignment has passed. {0}/3 sites supported; final rite {1}/12 months.", Derived.ReadySites(next), next.FinalProgress);
                    Derived.AddLog(next, next.ResultReason);
                    break;
                }
                Director.Tick(next);
                if (next.ElapsedMonths == Balance.PreparationMonths)
                {
                    next.Status = "alignment"; next.Speed = 0;
                    Derived.AddLog(next, F("The stars align. Time is paused. You have {0} months to complete the awakening.", Balance.AlignmentMonths));
                    break;
                }
            }
            return next;
        }
    }
}
