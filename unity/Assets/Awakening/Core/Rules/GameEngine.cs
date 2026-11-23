using System;
using System.Collections.Generic;
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
                Talents = new List<string>(), BuryUses = 0, TalentYield = new Dictionary<string, double>(), CapstonesUsed = new List<string>(),
                RiteMonthsRequired = Balance.FinalMonths, RiteSites = new List<string>(), FinalProgress = 0, Regions = regions, NextLogId = 1, ResultReason = null
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
        static int BaseCounterCost(RegionState r) => Balance.CounterSeverityCost * (r.Threat != null ? r.Threat.Severity : 1);
        public static int CounterCost(GameState state, RegionState r)
        {
            int cost = BaseCounterCost(r);
            return HasTalent(state, "quiet_friends") ? (int)Js.RoundHalfUp(cost * 2 / 3.0) : cost;
        }
        public static int SilenceCost(GameState state, RegionState r)
        {
            int each = HasTalent(state, "red_altars") ? Balance.SilenceSeverityDreadTerror : Balance.SilenceSeverityDread;
            return each * (r.Threat != null ? r.Threat.Severity : 1);
        }
        public static int SacrificeDread(GameState state) => HasTalent(state, "red_altars") ? Balance.RedAltarsSacrificeDread : Balance.SacrificeDread;
        /// <summary>World awareness an action adds when performed (bury and conceal lower it in their effect instead).</summary>
        public static double AwarenessCost(GameState state, string id, string regionId)
        {
            var r = state.Regions[regionId];
            switch (id)
            {
                case "reveal": return r.Revealed || HasTalent(state, "open_worship") ? 0.0 : Balance.RevealAwareness;
                case "manifest": return Derived.ManifestAwareness(regionId);
                case "prepare": return Balance.SiteAwareness;
                case "reveal_figure": return Balance.FigureAwareness;
                case "silence": return (HasTalent(state, "mute_witnesses") ? Balance.MuteWitnessesSeverityAwareness : Balance.SilenceSeverityAwareness) * (r.Threat != null ? r.Threat.Severity : 1);
                case "sacrifice": return DreamAdvances(state, regionId) ? Balance.DreamsAwareness : 0.0;
                default: return 0.0;
            }
        }
        /// <summary>Months a scattered flock stays hidden (Sleeper cells shortens it).</summary>
        public static int ScatterMonths(GameState state) => HasTalent(state, "sleeper_cells") ? Balance.SleeperScatterMonths : Balance.ScatterMonths;
        /// <summary>Months concealment's world effect rests after use, by difficulty.</summary>
        public static int ConcealWorldMonths(GameState state) => GameData.Difficulties[state.Difficulty].ConcealWorldMonths;
        /// <summary>Preparation months an ignored occult threat takes, by difficulty.</summary>
        public static int OccultPreparationLoss(GameState state) => GameData.Difficulties[state.Difficulty].OccultPreparationLoss;
        /// <summary>Months a funded site needs to be prepared, by difficulty (alignment-7: 20 on Easy, 24 otherwise).</summary>
        public static int SiteMonths(GameState state) => GameData.Difficulties[state.Difficulty].SiteMonths;
        /// <summary>The last month a new site can be funded and still finish, unaccelerated, before a 12-month rite.</summary>
        public static int LatestSiteStart(GameState state) => Balance.Deadline - Balance.FinalMonths - SiteMonths(state);
        /// <summary>Months the threat rolls still pass this region over after an obstruction or a silencing (alignment-7); 0 when none.</summary>
        public static int ThreatGraceLeft(GameState s, string regionId) => Math.Max(0, s.Regions[regionId].ThreatGraceUntil - s.ElapsedMonths);

        // ---- Madness (alignment-7) ----
        /// <summary>Dread this region's Madness yields every month: Madness / 20 where the cult has a cell.</summary>
        public static double MadnessDread(RegionState r) => r.Population.Followers > 0 ? r.Madness / Balance.MadnessDreadDivisor : 0.0;
        /// <summary>Dread all Madness yields every month, before the Dread cap.</summary>
        public static double MadnessDreadTotal(GameState s) => GameData.RegionIds.Sum(id => MadnessDread(s.Regions[id]));
        /// <summary>Madness is high here (Balance.MadnessHigh, with a cell): new threats against a hidden cell are occult and the world notices.</summary>
        public static bool MadnessHigh(RegionState r) => r.Population.Followers > 0 && r.Madness >= Balance.MadnessHigh;
        /// <summary>A funded site here progresses and Madness quickens it (whether it is already prepared is not checked).</summary>
        public static bool MadnessQuickens(RegionState r) => r.Population.Followers > 0 && r.Madness > 0 && r.Site.Invested && !r.Site.Dormant && Derived.Supported(r);
        /// <summary>The region's site is unfinished and Madness quickens it.</summary>
        public static bool MadnessQuickens(GameState s, string regionId) => MadnessQuickens(s.Regions[regionId]) && s.Regions[regionId].Site.Progress < SiteMonths(s);
        /// <summary>Extra preparation months a quickened site gains a month: Madness / Balance.MadnessQuickenFull (0.5 at 50).</summary>
        public static double MadnessHaste(RegionState r) => MadnessQuickens(r) ? r.Madness / Balance.MadnessQuickenFull : 0.0;
        /// <summary>One English line for the region panel: what this region's Madness does now; "" when there is none or no cell.</summary>
        public static string MadnessText(GameState s, string regionId)
        {
            var r = s.Regions[regionId];
            if (r.Population.Followers <= 0 || r.Madness <= 0) return "";
            var parts = new List<string> { "+" + Js.Num(Js.RoundHalfUp(MadnessDread(r) * 10) / 10) + " Dread/month" };
            if (MadnessQuickens(s, regionId)) parts.Add("site prepares " + Js.Num(Js.RoundHalfUp(MadnessHaste(r) * 100)) + "% faster");
            parts.Add("draws occultists");
            if (MadnessHigh(r)) parts.Add("+" + Js.Num(Balance.MadnessAwareness) + " world awareness/month");
            return string.Join(" · ", parts);
        }
        /// <summary>Madness a sacrifice leaves in its region: Balance.SacrificeMadness, more with Dreams of R'lyeh.</summary>
        public static double SacrificeMadness(GameState s) => Balance.SacrificeMadness * (HasTalent(s, "dreams_of_rlyeh") ? Balance.DreamsSacrificeMadness : 1.0);
        static void AddMadness(RegionState r, double amount) => r.Madness = Derived.ClampPct(r.Madness + amount);

        // ---- Talent tree (alignment-5) ----
        public static bool HasTalent(GameState s, string id) => s.Talents != null && s.Talents.Contains(id);
        static int[] SealMonths(GameState s) => GameData.Difficulties[s.Difficulty].TalentSlotMonths;
        /// <summary>Seals opened so far, used or not.</summary>
        public static int TalentSlotsOpen(GameState s) => SealMonths(s).Count(month => month <= s.ElapsedMonths);
        /// <summary>Seals opened and not yet spent.</summary>
        public static int TalentSlotsUnused(GameState s) => Math.Max(0, TalentSlotsOpen(s) - s.Talents.Count);
        /// <summary>Month the next seal opens; -1 when every seal of the run is open.</summary>
        public static int NextTalentSlotMonth(GameState s)
        {
            foreach (int month in SealMonths(s)) if (month > s.ElapsedMonths) return month;
            return -1;
        }
        /// <summary>Influence the next talent costs; it rises with every talent taken.</summary>
        public static int TalentCost(GameState s) => GameData.Difficulties[s.Difficulty].TalentBaseCost + GameData.Difficulties[s.Difficulty].TalentCostStep * s.Talents.Count;
        static string TalentName(string id) => GameData.Talents[id].Name;
        /// <summary>"" when the talent can be taken now; otherwise why not.</summary>
        public static string TalentReason(GameState s, string id)
        {
            if (id == null || !GameData.Talents.ContainsKey(id)) return "Unknown talent.";
            if (!IsActive(s)) return "Start a run before taking talents; completed runs cannot change.";
            var t = GameData.Talents[id];
            if (HasTalent(s, id)) return "Already taken.";
            if (t.Excludes != null && HasTalent(s, t.Excludes)) return "Closed for this run: the cult chose " + TalentName(t.Excludes) + ".";
            if (t.Requires.Length > 0 && !HasTalent(s, t.Requires[0]))
                return t.Requires.Length == 1 ? "Requires " + TalentName(t.Requires[0]) + "."
                    : "Requires " + TalentName(t.Requires[0]) + " and " + string.Join(" or ", t.Requires.Skip(1).Select(TalentName)) + ".";
            if (t.Requires.Length > 1 && !t.Requires.Skip(1).Any(r => HasTalent(s, r)))
                return "Requires " + string.Join(" or ", t.Requires.Skip(1).Select(TalentName)) + ".";
            if (TalentSlotsUnused(s) == 0)
            {
                int next = NextTalentSlotMonth(s);
                return next < 0 ? "Every seal of this run is spent." : F("The next seal opens in {0} months.", next - s.ElapsedMonths);
            }
            int cost = TalentCost(s);
            if (s.Influence < cost) return F("Need {0} Influence.", cost);
            return "";
        }
        public static CommandResult TakeTalent(GameState s, string id)
        {
            string reason = TalentReason(s, id);
            if (reason.Length != 0) return Fail(s, reason);
            var t = GameData.Talents[id];
            int cost = TalentCost(s);
            var next = s.Clone();
            next.Influence -= cost;
            next.Telemetry.InfluenceSpent += cost;
            next.Talents.Add(id);
            next.TalentYield[id] = 0.0;
            Derived.AddLog(next, "The cult takes up " + t.Name + " (" + GameData.Branches[t.Branch].Name + "). "
                + (t.Excludes != null ? TalentName(t.Excludes) + " is closed for this run." : "No talent is undone."));
            return Success(next);
        }

        // ---- What each talent gave (alignment-6) ----
        /// <summary>Adds to a taken talent's yield; nothing happens for a talent the cult does not hold.</summary>
        internal static void AddYield(GameState s, string id, double amount)
        {
            if (!HasTalent(s, id) || !(amount > 0)) return;
            s.TalentYield[id] = (s.TalentYield.TryGetValue(id, out double value) ? value : 0.0) + amount;
        }
        public static double TalentYieldOf(GameState s, string id) => s.TalentYield != null && s.TalentYield.TryGetValue(id, out double value) ? value : 0.0;
        /// <summary>One English line: what the talent has given this run. "" when the talent was not taken.</summary>
        public static string TalentYieldText(GameState s, string id)
        {
            if (s == null || id == null || !HasTalent(s, id)) return "";
            string n = Js.Fixed(TalentYieldOf(s, id), 0);
            switch (id)
            {
                case "swelling_flock": return "+" + n + " followers so far";
                case "open_worship": return n + " world awareness spared so far";
                case "martyrs": return n + " Dread so far";
                case "quiet_friends": return n + " Influence saved so far";
                case "sleeper_cells": return n + " months of hiding saved so far";
                case "contingency": return n + " more preparation months restored so far";
                case "red_altars": return n + " Dread gained or saved so far";
                case "mute_witnesses": return n + " world awareness spared so far";
                case "scapegoats": return n + " world awareness lifted so far";
                case "great_procession":
                    if (s.Status == "awakening" || IsOver(s) && s.RiteSites.Count > 0)
                        return s.RiteMonthsRequired < Balance.FinalMonths
                            ? F("The rite needs {0} months, not {1}", s.RiteMonthsRequired, Balance.FinalMonths)
                            : F("Too few walked: the rite needs {0} months", s.RiteMonthsRequired);
                    return F("{0} of {1} regions worship in public; the rite would need {2} months", ProcessionRegions(s), Balance.ProcessionPublicRegions, RiteMonthsFor(s));
                case "world_forgets": return s.CapstonesUsed.Contains(id) ? "Invoked: −" + n + " world awareness" : "Not yet invoked";
                case "dreams_of_rlyeh":
                    return s.Status == "awakening" || s.DreamRiteMonths > 0
                        ? F("+{0} Madness spread · {1} rite months dreamed forward so far", n, s.DreamRiteMonths)
                        : "+" + n + " Madness spread so far";
                default: return "";
            }
        }

        // ---- Capstones (alignment-6) ----
        /// <summary>Capstones that act once, when invoked from the tree or the ledger, rather than on their own.</summary>
        public static bool IsInvokable(string talentId) => talentId != null && GameData.InvokableTalentIds.Contains(talentId);
        /// <summary>"" when the capstone can be invoked now; otherwise why not.</summary>
        public static string InvokeReason(GameState s, string talentId)
        {
            if (!IsInvokable(talentId)) return "This talent is not invoked; it works on its own.";
            if (s == null || !IsActive(s)) return "Start a run before invoking; completed runs cannot change.";
            if (!HasTalent(s, talentId)) return "Take " + TalentName(talentId) + " first.";
            if (s.CapstonesUsed.Contains(talentId)) return "Already invoked this run.";
            if (s.Awareness <= 0 && GameData.RegionIds.All(id => s.Regions[id].Threat == null)) return "The world has nothing to forget yet.";
            return "";
        }
        /// <summary>The price of invoking, in one line for the confirmation.</summary>
        public static string InvokeRisk(GameState s, string talentId)
        {
            if (talentId != "world_forgets") return "";
            string risk = F("Every cell goes to ground for {0} months: no recruits, no income, no site progress. Public worship ends.", Balance.WorldForgetsScatterMonths);
            if (s != null && s.Status == "awakening") risk += F(" The final rite halts for {0} months.", Balance.WorldForgetsScatterMonths);
            return risk;
        }
        public static CommandResult InvokeCapstone(GameState s, string talentId)
        {
            string reason = InvokeReason(s, talentId);
            if (reason.Length != 0) return Fail(s, reason);
            var next = s.Clone();
            next.CapstonesUsed.Add(talentId);
            double before = next.Awareness;
            next.Awareness = Derived.ClampPct(next.Awareness - Balance.WorldForgetsAwareness);
            AddYield(next, talentId, before - next.Awareness);
            int cancelled = 0;
            foreach (string id in GameData.RegionIds)
            {
                var r = next.Regions[id];
                if (r.Threat != null) { r.Threat = null; cancelled++; }
                if (r.Population.Followers <= 0) continue;
                r.Public = false; r.Scattered = true;
                r.ScatteredUntil = Math.Max(r.ScatteredUntil, next.ElapsedMonths + Balance.WorldForgetsScatterMonths);
            }
            Derived.AddLog(next, F("The world forgets: −{0} world awareness, {1} threat(s) dissolve. Every cell goes to ground for {2} months.",
                Js.Fixed(before - next.Awareness, 0), cancelled, Balance.WorldForgetsScatterMonths));
            return Success(next);
        }

        // ---- The final rite (alignment-6) ----
        /// <summary>Regions with a cell that worships in public now.</summary>
        public static int ProcessionRegions(GameState s) => GameData.RegionIds.Count(id => s.Regions[id].Public && s.Regions[id].Population.Followers > 0);
        /// <summary>Months the final rite needs: fixed once begun; before that, what it would need if begun now.</summary>
        public static int RiteMonthsFor(GameState s)
        {
            if (s.Status == "awakening" || IsOver(s) && s.RiteSites.Count > 0) return s.RiteMonthsRequired;
            return HasTalent(s, "great_procession") && ProcessionRegions(s) >= Balance.ProcessionPublicRegions ? Balance.ProcessionRiteMonths : Balance.FinalMonths;
        }
        /// <summary>The final rite is under way as The Great Procession.</summary>
        public static bool InProcession(GameState s) => s.Status == "awakening" && s.RiteMonthsRequired < Balance.FinalMonths;
        internal static bool SiteReady(GameState s, RegionState r) => r.Site.Invested && !r.Site.Dormant && r.Site.Progress >= SiteMonths(s) && Derived.Supported(r);
        /// <summary>During the rite, whether this region's site is one the rite cannot spare: without it fewer than three sites stand.</summary>
        static bool CarriesRite(GameState s, string regionId) => s.Status == "awakening" && SiteReady(s, s.Regions[regionId]) && Derived.ReadySites(s) - 1 < Balance.RequiredSites;
        /// <summary>Rite months a dream sacrifice here would carry (alignment-7): two where Madness reaches Balance.DreamsRiteMadness, else one; 0 when it does not carry.</summary>
        public static int DreamMonths(GameState s, string regionId)
        {
            if (!DreamAdvances(s, regionId)) return 0;
            int months = s.Regions[regionId].Madness >= Balance.DreamsRiteMadness ? Balance.DreamsMadRiteMonths : 1;
            return Math.Min(months, s.RiteMonthsRequired - s.FinalProgress);
        }
        /// <summary>With Dreams of R'lyeh, whether a sacrifice here now carries the rite forward: three supported sites must still stand after it.</summary>
        public static bool DreamAdvances(GameState s, string regionId)
        {
            if (s.Status != "awakening" || !HasTalent(s, "dreams_of_rlyeh") || s.FinalProgress >= s.RiteMonthsRequired) return false;
            var r = s.Regions[regionId];
            int ready = Derived.ReadySites(s);
            if (SiteReady(s, r) && r.Population.Followers - Balance.SacrificeFollowers < Derived.SiteRequirement(r)) ready--;
            return ready >= Balance.RequiredSites;
        }

        // ---- Bury the record and Restore the site (alignment-5) ----
        public static int BuryStep(GameState s) => GameData.Difficulties[s.Difficulty].BuryStep;
        public static int BuryCost(GameState s) => Balance.BuryBaseCost + BuryStep(s) * s.BuryUses;
        public static int BuryAwareness(GameState s) => Balance.BuryAwareness;
        /// <summary>Preparation months Restore the site would return in this region now.</summary>
        public static int RestoreMonths(GameState s, string regionId)
        {
            var site = s.Regions[regionId].Site;
            if (!site.Invested || site.Recoverable <= 0) return 0;
            int months = HasTalent(s, "contingency") ? site.Recoverable : (site.Recoverable + 1) / 2;
            return Math.Max(0, Math.Min(months, SiteMonths(s) - site.Progress));
        }
        public static int RestoreCost(GameState s, string regionId) => Balance.RestoreMonthCost * RestoreMonths(s, regionId);
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
            int costInfluence = id == "counter" ? CounterCost(state, r) : id == "conceal" ? ConcealCost(state)
                : id == "bury" ? BuryCost(state) : id == "restore" ? RestoreCost(state, regionId) : a.Influence;
            int costDread = id == "manifest" && HasTalent(state, "red_altars") ? Balance.RedAltarsManifestDread : id == "prepare" ? SiteDreadCost(state) : id == "silence" ? SilenceCost(state, r) : a.Dread;
            double costAwareness = AwarenessCost(state, id, regionId);
            int costFollowers = a.Followers;
            string reason = IsActive(state) ? "" : "Start a run before acting; completed runs cannot change.";
            int cooldown = Cooldown(r, id) - state.ElapsedMonths;
            if (reason.Length == 0 && cooldown > 0) reason = F("Available in {0} months.", cooldown);
            if (reason.Length == 0 && state.Influence < costInfluence) reason = F("Need {0} Influence.", costInfluence);
            if (reason.Length == 0 && state.Dread < costDread) reason = F("Need {0} Dread.", costDread);
            if (reason.Length == 0 && p.Followers < costFollowers) reason = F("Need {0} local followers.", costFollowers);
            if (reason.Length == 0 && id != "expand" && p.Followers <= 0) reason = "Establish a cult here first.";
            if (reason.Length == 0 && id == "bury" && state.Awareness <= 0) reason = "Nothing to bury: the world has not noticed you.";
            if (reason.Length == 0 && id == "restore" && (!r.Site.Invested || r.Site.Recoverable <= 0)) reason = "No lost preparation to restore here.";
            if (reason.Length == 0 && id == "restore" && RestoreMonths(state, regionId) <= 0) reason = "This site is already prepared.";
            if (reason.Length == 0 && r.Scattered && new[] { "recruit", "reveal", "prepare", "reveal_figure" }.Contains(id))
                reason = F("The flock is scattered for {0} more months.", r.ScatteredUntil - state.ElapsedMonths);
            if (reason.Length == 0 && (id == "recruit" || id == "expand") && Derived.Recruitable(r) < 1) reason = "No recruits available. Use a manifestation to change disbelief.";
            if (reason.Length == 0 && id == "expand" && p.Followers > 0) reason = "This region already has a cell.";
            if (reason.Length == 0 && id == "expand" && Derived.TotalFollowers(state) > 0 && !HasAdjacentCell(state, regionId)) reason = "Requires a cult in an adjacent region.";
            if (reason.Length == 0 && id == "reveal" && r.Public) reason = "Worship is already public.";
            if (reason.Length == 0 && id == "hide" && !r.Public) reason = "This cult is already underground.";
            if (reason.Length == 0 && id == "hide" && InProcession(state)) reason = "The Great Procession does not turn back underground.";
            if (reason.Length == 0 && id == "conceal" && r.Exposure <= EvidenceFloor(r) && !ConcealsWorld(state))
                reason = state.Awareness <= 0 ? "Nothing to conceal. Public rites remain visible."
                    : F("Nothing to conceal here. The world effect returns in {0} months.", ConcealWorldCooldown(state));
            if (reason.Length == 0 && id == "manifest" && p.Deniers + p.Unaware < 1) reason = "Everyone here already believes.";
            if (reason.Length == 0 && id == "prepare" && r.Site.Invested) reason = "This site's preparation is already funded.";
            if (reason.Length == 0 && id == "prepare" && !Derived.Supported(r)) reason = "Maintain at least " + Js.Num(Derived.SiteRequirement(r)) + " local followers.";
            int acceleration = Acceleration(r);
            int preparationLeft = Balance.Deadline - state.ElapsedMonths - Balance.FinalMonths;
            int siteMonths = SiteMonths(state);
            if (reason.Length == 0 && id == "prepare" && preparationLeft < siteMonths - acceleration)
                reason = F("Too late: preparation needs {0} more months and the final rite needs {1}, but only {2} remain.", siteMonths - acceleration, Balance.FinalMonths, Math.Max(0, Balance.Deadline - state.ElapsedMonths));
            if (reason.Length == 0 && new[] { "counter", "scatter", "silence" }.Contains(id) && r.Threat == null) reason = "No approaching threat in this region.";
            if (reason.Length == 0 && id == "exploit" && (r.Opportunity == null || !new[] { "nightmare", "eclipse" }.Contains(r.Opportunity.Kind))) reason = "Requires an active nightmare or eclipse.";
            if (reason.Length == 0 && id == "exploit" && r.Opportunity != null && r.Opportunity.Kind == "eclipse" && (!r.Site.Invested || r.Site.Progress >= siteMonths)) reason = "An eclipse needs an unfinished ritual site.";
            if (reason.Length == 0 && id == "convert" && (r.Figure != null || r.Opportunity == null || !new[] { "celebrity", "politician" }.Contains(r.Opportunity.Kind))) reason = "Requires an unconverted prominent-person opportunity.";
            if (reason.Length == 0 && id == "reveal_figure" && (r.Figure == null || r.Figure.Revealed)) reason = "Requires a secretly converted figure.";
            if (reason.Length == 0 && id == "reveal_figure" && !r.Site.Invested) reason = "Prepare a local ritual site to receive their awakening contribution.";
            string effect = a.Description;
            string risk = "";
            switch (id)
            {
                case "sacrifice":
                    int gain = SacrificeDread(state);
                    bool dream = DreamAdvances(state, regionId);
                    int dreamt = DreamMonths(state, regionId);
                    effect = "+" + Js.Fixed(Math.Min(gain, Balance.DreadCap - state.Dread), 0) + " Dread. " + Js.Fixed(Math.Max(0.0, p.Followers - costFollowers), 0)
                        + " followers remain; income falls by " + Js.Fixed(costFollowers * Balance.InfluencePerFollower, 2) + "/month."
                        + F(" Madness here +{0} ({1}).", Js.Num(SacrificeMadness(state)), Js.Num(Js.RoundHalfUp(Math.Min(100.0, r.Madness + SacrificeMadness(state)))))
                        + (dream ? F(" The dream carries the final rite {0} forward ({1}/{2}).", dreamt == 1 ? "one month" : dreamt + " months", state.FinalProgress + dreamt, state.RiteMonthsRequired) : "");
                    risk = (dream ? "+" + Js.Num(costAwareness) + " world awareness, " : "") + "+12 Exposure. Fewer than " + Js.Num(Derived.SiteRequirement(r)) + " followers interrupts this site's ritual.";
                    if (CarriesRite(state, regionId) && p.Followers - costFollowers < Derived.SiteRequirement(r))
                        risk += " The final rite halts until this site again holds " + Js.Num(Derived.SiteRequirement(r)) + " followers.";
                    else if (state.Status == "awakening" && HasTalent(state, "dreams_of_rlyeh") && !dream && state.FinalProgress < state.RiteMonthsRequired)
                        risk += " The dream does not carry: three supported sites must stand after the knife.";
                    break;
                case "reveal":
                    risk = "+18 Exposure" + (r.Revealed ? "" : ", +" + Js.Num(costAwareness) + " world awareness") + "; recruitment backlash for 6 months, then full public growth.";
                    break;
                case "scatter":
                    effect = "The threat finds nobody. −" + Js.Fixed(Math.Min(Balance.ScatterExposure, Math.Max(0.0, r.Exposure - (r.Site.Invested ? 40.0 : 0.0))), 0) + " Exposure.";
                    risk = F("For {0} months this cell recruits nobody and earns nothing (−{1} Influence/month).", ScatterMonths(state), Js.Fixed(Derived.RegionIncome(r), 2))
                        + (r.Site.Invested ? " Its ritual site stalls and cannot carry the final rite." : "") + (r.Public ? " Public worship ends." : "")
                        + (CarriesRite(state, regionId) ? F(" The final rite halts while this site hides: {0} months.", ScatterMonths(state)) : "");
                    break;
                case "silence":
                    effect = F("The threat ends tonight. No new threat here for {0} months.", Balance.ThreatGraceMonths);
                    risk = "+" + Js.Num(costAwareness) + " world awareness, +" + Js.Num(Balance.SilenceExposure) + " Exposure. The disappearances are noticed.";
                    break;
                case "counter": risk = F("No further obstruction here for {0} months. No new threat here for {1} months.", a.Cooldown, Balance.ThreatGraceMonths); break;
                case "conceal":
                    string local = "−" + Js.Fixed(Math.Min(25.0, Math.Max(0.0, r.Exposure - EvidenceFloor(r))), 0) + " Exposure";
                    if (ConcealsWorld(state))
                    {
                        effect = local + ", −" + Js.Fixed(Math.Min(Balance.ConcealAwareness, state.Awareness), 0) + " world awareness. The price rises with awareness.";
                        risk = F("The world effect then rests for {0} months in every region.", ConcealWorldMonths(state));
                    }
                    else if (state.Awareness > 0) effect = local + F(" only. The world effect returns in {0} months.", ConcealWorldCooldown(state));
                    else effect = local + ".";
                    break;
                case "manifest":
                    effect = F("Up to {0} deniers and {1} unaware people become believers. Madness here +{2} ({3}): each month it yields Madness/{4} Dread and quickens a preparing site by Madness%.",
                        Derived.ManifestDeniers(regionId), Derived.ManifestUnaware(regionId), Js.Num(Balance.ManifestMadness),
                        Js.Num(Js.RoundHalfUp(Math.Min(100.0, r.Madness + Balance.ManifestMadness))), Js.Num(Balance.MadnessDreadDivisor));
                    risk = "+15 Exposure and +" + Js.Num(costAwareness) + " awareness. New believers can strengthen resistance. Madness draws occultists.";
                    break;
                case "bury":
                    effect = "−" + Js.Fixed(Math.Min(BuryAwareness(state), state.Awareness), 0) + " world awareness. Concealment's own world effect is not spent.";
                    risk = "+" + Js.Num(Balance.BuryExposure) + F(" Exposure. The next burial costs {0} Influence.", costInfluence + BuryStep(state));
                    break;
                case "restore":
                    effect = F("+{0} preparation months recovered.", RestoreMonths(state, regionId));
                    risk = HasTalent(state, "contingency") ? "" : "+" + Js.Num(Balance.RestoreExposure) + " Exposure.";
                    break;
                case "prepare":
                    effect = F("Commit {0} Dread and {1} Influence, then pay {2} Influence/month and keep local support for {3} months. Each further site costs more.",
                        costDread, costInfluence, Js.Num(Balance.SiteUpkeep), siteMonths);
                    risk = "Upkeep " + Js.Num(Balance.SiteUpkeep) + " Influence/month while funded; unpaid months stall the site. Site remains exposed, even underground. An occult response is scheduled.";
                    if (preparationLeft < siteMonths) risk += F(" Needs acceleration to finish in time: {0} preparation months, then a {1}-month final rite.", siteMonths - acceleration, Balance.FinalMonths);
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
                    int dreamt = DreamMonths(state, regionId);
                    t.Sacrificed += Derived.LoseFollowers(r, preview.Followers);
                    double dreadBefore = next.Dread;
                    GrantDread(next, SacrificeDread(next));
                    AddYield(next, "red_altars", next.Dread - dreadBefore - Math.Min(Balance.SacrificeDread, Balance.DreadCap - dreadBefore));
                    r.Exposure = Derived.ClampPct(r.Exposure + 12);
                    double madBefore = r.Madness;
                    AddMadness(r, SacrificeMadness(next));
                    AddYield(next, "dreams_of_rlyeh", r.Madness - madBefore - Math.Min(Balance.SacrificeMadness, 100.0 - madBefore));
                    if (dreamt > 0)
                    {
                        next.FinalProgress = Math.Min(next.RiteMonthsRequired, next.FinalProgress + dreamt);
                        next.DreamRiteMonths += dreamt;
                    }
                    break;
                case "reveal":
                    if (!r.Revealed && HasTalent(next, "open_worship")) AddYield(next, "open_worship", Balance.RevealAwareness);
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
                        next.WorldConcealReadyAt = next.ElapsedMonths + ConcealWorldMonths(next);
                    }
                    break;
                case "manifest":
                    AddYield(next, "red_altars", GameData.Actions["manifest"].Dread - preview.Dread);
                    var p = r.Population;
                    double denied = Math.Min(p.Deniers, Derived.ManifestDeniers(regionId));
                    double unaware = Math.Min(p.Unaware, Derived.ManifestUnaware(regionId));
                    p.Deniers -= denied; p.Unaware -= unaware; p.Believers += denied + unaware;
                    AddMadness(r, Balance.ManifestMadness);
                    r.Exposure = Derived.ClampPct(r.Exposure + 15);
                    break;
                case "prepare":
                    r.Site.Invested = true; r.Site.Dormant = false;
                    r.Exposure = Math.Max(40.0, r.Exposure);
                    Director.ScheduleThreat(next, regionId, true);
                    break;
                case "counter":
                    AddYield(next, "quiet_friends", BaseCounterCost(state.Regions[regionId]) - preview.Influence);
                    r.Threat = null; t.ThreatsCountered++;
                    r.ThreatGraceUntil = next.ElapsedMonths + Balance.ThreatGraceMonths;
                    break;
                case "scatter":
                    AddYield(next, "sleeper_cells", Balance.ScatterMonths - ScatterMonths(next));
                    r.Threat = null; r.Public = false; r.Scattered = true;
                    r.ScatteredUntil = next.ElapsedMonths + ScatterMonths(next);
                    r.Exposure = Math.Max(EvidenceFloor(r), r.Exposure - Balance.ScatterExposure);
                    break;
                case "silence":
                    int severity = r.Threat.Severity;
                    AddYield(next, "red_altars", Balance.SilenceSeverityDread * severity - preview.Dread);
                    AddYield(next, "mute_witnesses", Balance.SilenceSeverityAwareness * severity - preview.Awareness);
                    r.Threat = null; r.Exposure = Derived.ClampPct(r.Exposure + Balance.SilenceExposure);
                    r.ThreatGraceUntil = next.ElapsedMonths + Balance.ThreatGraceMonths;
                    break;
                case "exploit":
                    if (r.Opportunity.Kind == "nightmare") GrantDread(next, 30);
                    else r.Site.Progress = Math.Min(SiteMonths(next), r.Site.Progress + 8);
                    r.Opportunity = null;
                    break;
                case "convert":
                    r.Figure = new Figure { Kind = r.Opportunity.Kind, Revealed = false };
                    r.Opportunity = null;
                    r.Exposure = Derived.ClampPct(r.Exposure + 5);
                    break;
                case "bury":
                    next.Awareness = Derived.ClampPct(next.Awareness - BuryAwareness(state));
                    next.BuryUses++;
                    r.Exposure = Derived.ClampPct(r.Exposure + Balance.BuryExposure);
                    break;
                case "restore":
                    int restored = RestoreMonths(state, regionId);
                    AddYield(next, "contingency", restored - Math.Min(restored, (r.Site.Recoverable + 1) / 2));
                    r.Site.Progress = Math.Min(SiteMonths(next), r.Site.Progress + restored);
                    r.Site.Recoverable = 0;
                    if (!HasTalent(state, "contingency")) r.Exposure = Derived.ClampPct(r.Exposure + Balance.RestoreExposure);
                    break;
                case "reveal_figure":
                    r.Figure.Revealed = true;
                    Derived.Recruit(r, 60);
                    r.Site.Progress = Math.Min(SiteMonths(next), r.Site.Progress + Balance.FigureSiteProgress);
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
        /// <summary>Dreams of R'lyeh: every region at Balance.DreamsSpreadFrom Madness or more passes some to each neighbouring cell.</summary>
        static void DreamSpread(GameState s)
        {
            var sources = GameData.RegionIds.Where(id => s.Regions[id].Madness >= Balance.DreamsSpreadFrom).ToList();
            foreach (string source in sources)
                foreach (string other in GameData.Regions[source].Adjacent)
                {
                    var r = s.Regions[other];
                    if (r.Population.Followers <= 0) continue;
                    double before = r.Madness;
                    AddMadness(r, Balance.DreamsSpread);
                    AddYield(s, "dreams_of_rlyeh", r.Madness - before);
                }
        }
        internal static void GrantDread(GameState s, double amount)
        {
            double gain = Math.Min(amount, Balance.DreadCap - s.Dread);
            s.Dread += gain;
            s.Telemetry.DreadEarned += gain;
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
            int months = RiteMonthsFor(state);
            if (Balance.Deadline - state.ElapsedMonths < months) return F("Fewer than {0} months remain. The final rite can no longer finish in time.", months);
            return "";
        }
        public static CommandResult StartAwakening(GameState state)
        {
            string reason = AwakeningReason(state);
            if (reason.Length != 0) return Fail(state, reason);
            var next = state.Clone();
            next.Dread -= Balance.FinalDread;
            next.Telemetry.DreadSpent += Balance.FinalDread;
            int required = RiteMonthsFor(state);
            next.Status = "awakening";
            next.RiteMonthsRequired = required;
            next.RiteSites = GameData.RegionIds.Where(id => SiteReady(next, next.Regions[id])).ToList();
            AddYield(next, "great_procession", Balance.FinalMonths - required);
            Derived.AddLog(next, required < Balance.FinalMonths
                ? F("The Great Procession begins: {0} regions walk in the open. Maintain three supported sites for {1} months.", ProcessionRegions(next), required)
                : F("The final rite begins. Maintain three supported sites for {0} months.", required));
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
                int siteMonths = SiteMonths(next);
                foreach (string id in GameData.RegionIds)
                {
                    var r = next.Regions[id];
                    double recruitable = Derived.Recruitable(r);
                    double withoutFlock = HasTalent(next, "swelling_flock") ? Math.Min(recruitable, Derived.RecruitmentPerMonth(next, r, "swelling_flock")) : 0.0;
                    double recruited = Derived.Recruit(r, Derived.RecruitmentPerMonth(next, r));
                    AddYield(next, "swelling_flock", recruited - withoutFlock);
                    if (r.Population.Followers > 0)
                    {
                        r.Exposure = Derived.ClampPct(Math.Max(EvidenceFloor(r), r.Exposure + (r.Public ? 0.6 : -0.36)));
                        if (r.Public && !HasTalent(next, "open_worship") || r.Site.Invested) next.Awareness = Derived.ClampPct(next.Awareness + 0.075);
                        else if (r.Public) AddYield(next, "open_worship", 0.075);
                    }
                    double madnessDread = Math.Min(MadnessDread(r), Math.Max(0.0, Balance.DreadCap - next.Dread));
                    if (madnessDread > 0) { GrantDread(next, madnessDread); next.Telemetry.MadnessDread += madnessDread; }
                    if (MadnessHigh(r)) next.Awareness = Derived.ClampPct(next.Awareness + Balance.MadnessAwareness);
                    if (r.Site.Invested && !r.Site.Dormant && Derived.Supported(r) && r.Site.Progress < siteMonths)
                    {
                        r.Site.Progress++;
                        r.Site.Quickening += MadnessHaste(r);
                        if (r.Site.Quickening >= 1.0)
                        {
                            r.Site.Quickening -= 1.0;
                            if (r.Site.Progress < siteMonths) { r.Site.Progress++; next.Telemetry.QuickenedMonths++; }
                        }
                        if (r.Site.Progress == siteMonths) Derived.AddLog(next, "The ritual site in " + GameData.Regions[id].Name + " is prepared.");
                    }
                    r.Madness = Math.Max(0.0, r.Madness - Balance.MadnessDecay);
                }
                if (HasTalent(next, "dreams_of_rlyeh")) DreamSpread(next);
                double sizeAwareness = Derived.CultShare(next) * Balance.AwarenessPerCapita;
                double faithShare = Derived.OpenWorshipShare(next) * Balance.AwarenessPerCapita;
                AddYield(next, "open_worship", faithShare);
                next.Awareness = Derived.ClampPct(next.Awareness + sizeAwareness - faithShare);
                if (InProcession(next)) next.Awareness = Derived.ClampPct(next.Awareness + Balance.ProcessionAwareness * ProcessionRegions(next));
                if (next.Status == "awakening" && Derived.ReadySites(next) >= Balance.RequiredSites) next.FinalProgress = Math.Min(next.RiteMonthsRequired, next.FinalProgress + 1);
                if (next.Status == "awakening" && next.FinalProgress >= next.RiteMonthsRequired)
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
                    next.ResultReason = F("The alignment has passed. {0}/3 sites supported; final rite {1}/{2} months.", Derived.ReadySites(next), next.FinalProgress, RiteMonthsFor(next));
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
