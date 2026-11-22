using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;

namespace Awakening.UI
{
    /// <summary>How a finished run ended.</summary>
    public sealed class Ending
    {
        public string Cause = "";
        public string Fact = "";
        public readonly List<(bool ok, string name, string detail)> Checks = new List<(bool, string, string)>();
        public readonly List<string> Advice = new List<string>();
    }

    /// <summary>One dated milestone for a player's first runs, derived from state and calendar.</summary>
    public sealed class GoalStep
    {
        public string Id = "";
        public string Title = "";
        public string Detail = "";
        public int DeadlineMonth = -1;
        public bool Done;
        public bool Overdue;
    }

    /// <summary>
    /// Short player-facing wording derived from the rules: first-time notes, case-file tab guide,
    /// first-run milestones, the headline of the ritual timetable, threat consequences, one-line
    /// reasons an action is unavailable, and the account of how a run ended. Nothing here changes
    /// the rules. Port of godot/ui/notes.gd.
    /// </summary>
    public static class Notes
    {
        public const string TabsHintId = "file_tabs";
        public const string TabsHintTitle = "Case file tabs";
        public const string TabsHintText = "CULT grows cells, conceals evidence, and answers threats. RITES turns followers into Dread and raises sites. OMENS claims passing signs and figures.";
        public const string RiteDoomedHeadline = "The stars will pass without you";

        static readonly Dictionary<string, string> ShortRegions = new Dictionary<string, string>
        {
            ["north_america"] = "N. America",
            ["latin_america"] = "L. America",
            ["europe"] = "Europe",
            ["africa"] = "Africa",
            ["middle_east"] = "W. Asia",
            ["south_asia"] = "S. Asia",
            ["east_asia"] = "E. Asia",
            ["oceania"] = "Oceania",
        };

        static int RiteMonths(GameState s) => s != null ? GameEngine.RiteMonthsFor(s) : Balance.FinalMonths;
        static int LatestStartFor(GameState s) => Balance.Deadline - RiteMonths(s);
        static int LatestSiteStartFor(GameState s) => LatestStartFor(s) - Balance.SiteMonths;
        static bool IsRiteStartLog(LogEntry e) =>
            e.Message.StartsWith("The final rite begins.", StringComparison.Ordinal)
            || e.Message.StartsWith("The Great Procession begins:", StringComparison.Ordinal);

        /// <summary>Human-readable label for a Forecast.Rite blocker reason.</summary>
        public static string RiteBlockerReason(string reason)
        {
            switch (reason)
            {
                case "scattered": return "scattered";
                case "unsupported": return "unsupported";
                case "dormant": return "dormant (unpaid upkeep)";
                case "destroyed": return "destroyed";
                case "unprepared": return "unprepared";
                default: return reason ?? "";
            }
        }

        /// <summary>One-line summary of the regions and reasons halting the final rite.</summary>
        public static string RiteHaltedSummary(RiteStatus rite, bool compact = false)
        {
            if (rite == null || rite.Blockers == null || rite.Blockers.Count == 0) return "";
            return string.Join(" · ", rite.Blockers.Select(b =>
            {
                string name = compact && ShortRegions.TryGetValue(b.Region, out string shortName)
                    ? shortName
                    : GameData.Regions[b.Region].Name;
                string why = compact && b.Reason == "dormant" ? "dormant" : RiteBlockerReason(b.Reason);
                return $"{name}: {why}";
            }));
        }

        /// <summary>True when performing this action would halt an active final rite.</summary>
        public static bool RiskHaltsRite(ActionPreview preview) =>
            preview?.Risk != null && preview.Risk.IndexOf("The final rite halts", StringComparison.Ordinal) >= 0;

        /// <summary>Separates a rite-halting warning sentence from the rest of PreviewAction.Risk.</summary>
        public static (string other, string halt) SplitRiteHaltRisk(string risk)
        {
            if (string.IsNullOrEmpty(risk)) return ("", "");
            int at = risk.IndexOf("The final rite halts", StringComparison.Ordinal);
            if (at < 0) return (risk, "");
            return (risk.Substring(0, at).Trim(), risk.Substring(at).Trim());
        }

        /// <summary>Short stamp for Conceal while its global awareness reduction is resting, or "".</summary>
        public static string ConcealGlobalStamp(GameState s)
        {
            int cd = GameEngine.ConcealWorldCooldown(s);
            return cd > 0 ? $"GLOBAL EFFECT IN {cd} MO" : "";
        }

        /// <summary>Prominence tier for the rite margin strip: none, quiet, tight, doomed, or rite.</summary>
        public static string RiteBandLevel(GameState s, RiteMargin margin, RitualForecast forecast)
        {
            if (s == null || s.Status == "placing" || GameEngine.IsOver(s)) return "none";
            if (margin.Started) return "rite";
            if (!margin.Fits) return "doomed";
            int latestSite = margin.LatestStart - Balance.SiteMonths;
            bool tightMargin = margin.EarliestStart >= 0 && margin.Margin <= 6;
            bool tightUnfunded = margin.EarliestStart < 0 && latestSite - s.ElapsedMonths <= 24;
            bool anyStalled = margin.Sites.Any(site => !site.Supported || site.Scattered || s.Regions[site.Region].Site.Dormant);
            if (tightMargin || tightUnfunded || anyStalled || forecast.Urgent) return "tight";
            if (margin.Sites.Count > 0 || s.ElapsedMonths >= 72) return "quiet";
            return "none";
        }

        /// <summary>Why the final rite can no longer fit before the alignment closes.</summary>
        public static string RiteDoomedCause(GameState s, RiteMargin margin)
        {
            int riteMonths = RiteMonths(s);
            if (s.ElapsedMonths > margin.LatestStart)
            {
                int left = Math.Max(0, Balance.Deadline - s.ElapsedMonths);
                if (Derived.ReadySites(s) >= Balance.RequiredSites && s.Dread < Balance.FinalDread)
                    return $"Only {Format.Months(left)} remain before {Format.Date(Balance.Deadline)}; the final rite needs {riteMonths} months and {Balance.FinalDread} Dread.";
                return $"Only {Format.Months(left)} remain before {Format.Date(Balance.Deadline)}; the final rite needs {riteMonths} months and can no longer finish.";
            }
            if (margin.Sites.Count < Balance.RequiredSites)
                return $"Only {margin.Sites.Count} of {Balance.RequiredSites} ritual sites are funded; a new site cannot be prepared before the {Format.Date(margin.LatestStart)} cutoff.";
            SiteMargin third = margin.Sites[Balance.RequiredSites - 1];
            int lateBy = Math.Max(1, margin.EarliestStart - margin.LatestStart);
            if (third.Scattered)
                return $"The flock in {GameData.Regions[third.Region].Name} is scattered; the third site misses the {Format.Date(margin.LatestStart)} cutoff by {Format.Months(lateBy)}.";
            return $"The third site needs {Format.Months(third.MonthsLeft)} more preparation, missing the {Format.Date(margin.LatestStart)} cutoff by {Format.Months(lateBy)} with no acceleration left.";
        }

        /// <summary>Calendar line summarizing earliest start, latest start, and margin (or active rite slack).</summary>
        public static string RiteMarginLine(GameState s, RiteMargin margin, bool compact = false)
        {
            Func<int, string> fmt = compact ? (Func<int, string>)Format.DateShort : Format.Date;
            Func<int, string> mo = compact ? (Func<int, string>)(n => n + " mo") : Format.Months;
            string head = compact ? "Earliest" : "Earliest rite";
            if (margin.Started)
            {
                RiteStatus rite = Forecast.Rite(s);
                int left = Math.Max(0, Balance.Deadline - s.ElapsedMonths);
                int need = Math.Max(0, rite.Required - rite.Progress);
                int slack = left - need;
                string label = GameEngine.InProcession(s) ? (compact ? "Procession" : "Great Procession") : "Final rite";
                if (!margin.Fits)
                    return $"{label}: {rite.Progress}/{rite.Required} mo · deadline {fmt(Balance.Deadline)} · {mo(-slack)} short";
                return $"{label}: {rite.Progress}/{rite.Required} mo · deadline {fmt(Balance.Deadline)} · margin {mo(slack)}";
            }
            if (margin.EarliestStart >= 0)
            {
                if (!margin.Fits)
                    return $"{head}: {fmt(margin.EarliestStart)} · latest: {fmt(margin.LatestStart)} · {mo(-margin.Margin)} too late";
                if (margin.Margin < 0)
                    return $"{head}: {fmt(margin.EarliestStart)} · latest: {fmt(margin.LatestStart)} · margin −{-margin.Margin} mo (needs acceleration)";
                return $"{head}: {fmt(margin.EarliestStart)} · latest: {fmt(margin.LatestStart)} · margin {mo(margin.Margin)}";
            }
            int latestSite = margin.LatestStart - Balance.SiteMonths;
            if (!margin.Fits)
                return $"{head}: none ({margin.Sites.Count}/{Balance.RequiredSites} sites) · latest: {fmt(margin.LatestStart)} · window closed";
            if (s.ElapsedMonths > latestSite)
                return $"{head}: pending ({margin.Sites.Count}/{Balance.RequiredSites} sites) · latest: {fmt(margin.LatestStart)} · needs acceleration";
            return $"{head}: pending ({margin.Sites.Count}/{Balance.RequiredSites} sites) · latest: {fmt(margin.LatestStart)} · fund by {fmt(latestSite)}";
        }

        /// <summary>Compact label for one funded site chip in the rite margin strip.</summary>
        public static string SiteChipText(GameState s, SiteMargin site, bool compact)
        {
            RegionState r = s.Regions[site.Region];
            string name = compact && ShortRegions.TryGetValue(site.Region, out string shortName) ? shortName : GameData.Regions[site.Region].Name;
            if (r.Site.Dormant) return $"× {name} · dormant";
            if (site.Scattered) return $"× {name} · scattered ({site.MonthsLeft}m)";
            if (!site.Supported) return $"× {name} · {site.Followers}/{site.Required} ({site.MonthsLeft}m)";
            return site.MonthsLeft == 0 ? $"◆ {name} · ready" : $"◇ {name} · {site.MonthsLeft}m";
        }

        /// <summary>Full detail for one funded site on hover or tap.</summary>
        public static string SiteMarginDetail(GameState s, SiteMargin site)
        {
            RegionState r = s.Regions[site.Region];
            string regionName = GameData.Regions[site.Region].Name;
            string support;
            if (r.Site.Dormant) support = "Dormant (upkeep unpaid)";
            else if (site.Scattered) support = $"Scattered ({Math.Max(1, r.ScatteredUntil - s.ElapsedMonths)} mo) · unsupported";
            else if (!site.Supported) support = $"Unsupported (needs {Math.Max(1, site.Required - site.Followers)} more)";
            else support = "Supported";
            string prep = site.MonthsLeft == 0 ? "Prepared" : $"{Format.Months(site.MonthsLeft)} left";
            return $"{regionName} · {Format.Count(site.Followers)}/{Format.Count(site.Required)} followers · {support} · {prep}";
        }

        /// <summary>Detail text for an unfunded required site slot.</summary>
        public static string UnfundedSiteDetail(int slotNumber, GameState s = null) =>
            $"Site {slotNumber} of {Balance.RequiredSites} is unfunded. Small regions need fewer followers; safe start by {Format.Date(LatestSiteStartFor(s))}.";

        public static readonly Dictionary<string, string> TabDescriptions = new Dictionary<string, string>
        {
            ["CULT"] = "Grow cells, conceal or bury evidence, and answer threats.",
            ["RITES"] = "Sacrifice for Dread, manifestations, and ritual sites.",
            ["OMENS"] = "Passing nightmares, eclipses, and prominent figures.",
        };

        /// <summary>id: (title, one sentence). Each appears once per device, the first time it applies.</summary>
        public static readonly Dictionary<string, (string title, string text)> Hints = new Dictionary<string, (string, string)>
        {
            ["first_cell"] = ("Your first cell", ""),
            ["threat"] = ("A threat is coming", "The clipping on the left counts down to a strike. Obstruct it with Influence, scatter the cell for a few months, silence the witnesses with Dread, or accept the losses."),
            ["omen"] = ("An omen appeared", "The violet eye marks a short-lived opportunity. Open that region to use it before it fades."),
            ["talents"] = ("A talent seal has opened", ""),
            ["sacrifice"] = ("Ritual sites take two years", "Sacrifice followers in the Rites tab for Dread, then Prepare a site in a region you hold firmly."),
            ["awareness"] = ("The world is noticing", "World awareness slows you and ends the run at 100%. Conceal the evidence or Bury the record to lower it."),
            ["unsupported"] = ("A site stopped counting", "A struck seal stalls: its region needs more followers, its flock is scattered, or its upkeep went unpaid. Open the region to see which."),
        };

        /// <summary>Dynamic wording for a contextual note; reads live numbers from state and GameData.</summary>
        public static (string title, string text) Hint(GameState s, string id)
        {
            if (id == "first_cell")
            {
                string influence = Format.Count(s.Influence);
                int gather = GameData.Actions["recruit"].Influence;
                int expand = GameData.Actions["expand"].Influence;
                return ("Your first cell", $"You have {influence} Influence. Gathering followers costs {gather}; opening a neighbouring cell costs {expand}. Every action works while paused.");
            }
            if (id == "talents")
            {
                int total = GameData.Difficulties[s.Difficulty].TalentSlots;
                int cost = GameEngine.TalentCost(s);
                return ("A talent seal has opened", $"Only {total} seals open in this run — the tree cannot be completed, and each middle pair closes the other. Open Talents in the ledger; the first costs {cost} Influence.");
            }
            return Hints[id];
        }

        /// <summary>Dated chained milestones for the first runs; dates derive from Format's calendar.</summary>
        public static List<GoalStep> FirstRunGoals(GameState s)
        {
            int by1911 = Format.MonthOf(1911);
            int by1917 = Format.MonthOf(1917);
            int by1923 = Format.MonthOf(1923);
            var regions = AllRegions(s).ToList();
            int cells = regions.Count(r => r.Population.Followers > 0);
            bool sacrificed = s.Telemetry.Sacrificed > 0
                || (s.Telemetry.Actions.TryGetValue("sacrifice", out int sacrifices) && sacrifices > 0);
            int sites = regions.Count(r => r.Site.Invested);

            bool cellsDone = cells >= 3;
            bool cellsOverdue = !cellsDone && s.ElapsedMonths >= by1911;
            bool firstSiteDone = sites >= 1;
            bool firstSiteOverdue = !firstSiteDone && s.ElapsedMonths >= by1917;
            bool threeSitesDone = sites >= Balance.RequiredSites;
            bool threeSitesOverdue = !threeSitesDone && s.ElapsedMonths >= by1923;

            return new List<GoalStep>
            {
                new GoalStep
                {
                    Id = "three_cells",
                    Title = $"Three cells before {Format.Date(by1911)}",
                    Detail = cellsOverdue
                        ? $"{cells} of 3 cells · past {Format.Date(by1911)}"
                        : $"{cells} of 3 cells · open neighbouring regions",
                    DeadlineMonth = by1911,
                    Done = cellsDone,
                    Overdue = cellsOverdue,
                },
                new GoalStep
                {
                    Id = "first_sacrifice",
                    Title = "First sacrifice",
                    Detail = "Sacrifice followers in the Rites tab for Dread.",
                    DeadlineMonth = -1,
                    Done = sacrificed,
                    Overdue = false,
                },
                new GoalStep
                {
                    Id = "first_site",
                    Title = $"First ritual site before {Format.Date(by1917)}",
                    Detail = firstSiteOverdue
                        ? $"0 of 1 site · small regions hold rites cheaply"
                        : "Small regions hold rites cheaply.",
                    DeadlineMonth = by1917,
                    Done = firstSiteDone,
                    Overdue = firstSiteOverdue,
                },
                new GoalStep
                {
                    Id = "three_sites",
                    Title = $"Three sites before {Format.Date(by1923)}",
                    Detail = threeSitesOverdue
                        ? $"{sites} of {Balance.RequiredSites} sites · past {Format.Date(by1923)}"
                        : $"{sites} of {Balance.RequiredSites} sites funded",
                    DeadlineMonth = by1923,
                    Done = threeSitesDone,
                    Overdue = threeSitesOverdue,
                },
            };
        }

        /// <summary>The current incomplete milestone during preparation, or null once all are met.</summary>
        public static GoalStep ActiveGoal(GameState s)
        {
            if (s == null || s.Status != "playing") return null;
            return FirstRunGoals(s).FirstOrDefault(g => !g.Done);
        }

        static IEnumerable<RegionState> AllRegions(GameState s) => GameData.RegionIds.Select(id => s.Regions[id]);

        /// <summary>The first unseen note whose situation is on the board, or "".</summary>
        public static string NextHint(GameState s, Func<string, bool> seen)
        {
            if (s.Status == "placing" || s.Status == "won" || s.Status == "lost") return "";
            var regions = AllRegions(s).ToList();
            var checks = new Dictionary<string, bool>
            {
                ["first_cell"] = true,
                ["threat"] = regions.Any(r => r.Threat != null),
                ["omen"] = regions.Any(r => r.Opportunity != null),
                ["unsupported"] = regions.Any(r => r.Site.Invested && IsStalled(r)),
                ["awareness"] = s.Awareness >= 30,
                ["sacrifice"] = s.ElapsedMonths >= 24 && !regions.Any(r => r.Site.Invested),
                ["talents"] = s.Talents.Count == 0 && GameEngine.TalentSlotsUnused(s) > 0,
            };
            foreach (string id in new[] { "first_cell", "threat", "unsupported", "awareness", "sacrifice", "omen", "talents" })
                if (checks[id] && !seen(id)) return id;
            return "";
        }

        /// <summary>
        /// One reading of a funded site for every place that shows it (card, map, seals, checklists):
        /// "dormant" (upkeep unpaid), "unsupported", "ready" or "preparing". Matches Derived.ReadySites.
        /// </summary>
        public static string SiteState(RegionState r)
        {
            if (r.Site.Dormant) return "dormant";
            if (!Derived.Supported(r)) return "unsupported";
            return r.Site.Progress >= Balance.SiteMonths ? "ready" : "preparing";
        }

        public static bool IsStalled(RegionState r)
        {
            string state = SiteState(r);
            return state == "dormant" || state == "unsupported";
        }

        /// <summary>Names the timetable's most pressing problem in a few words.</summary>
        public static string ForecastHeadline(GameState s)
        {
            if (s.Status == "awakening")
            {
                RiteStatus rite = Forecast.Rite(s);
                if (!rite.Advancing && rite.Progress < rite.Required)
                    return $"RITE HALTED · {rite.Progress} of {rite.Required} months";
                return GameEngine.InProcession(s)
                    ? $"Great Procession · {rite.Progress} of {rite.Required} months"
                    : $"Final rite · {rite.Progress} of {rite.Required} months";
            }
            RiteMargin margin = Forecast.Margin(s);
            if (!margin.Fits) return RiteDoomedHeadline;
            var sites = AllRegions(s).Where(r => r.Site.Invested).ToList();
            int unsupported = sites.Count(IsStalled);
            if (Derived.NetInfluencePerMonth(s) < 0) return "Upkeep exceeds income";
            if (margin.EarliestStart >= 0)
            {
                if (margin.Margin < 0) return "Rite needs acceleration";
                if (unsupported > 0) return $"{unsupported} site{(unsupported == 1 ? "" : "s")} stopped counting";
                if (margin.Margin <= 6) return $"Tight rite margin · {Format.Months(margin.Margin)}";
                return s.ElapsedMonths >= Balance.PreparationMonths ? "The stars are right" : "Ritual timetable";
            }
            if (unsupported > 0) return $"{unsupported} site{(unsupported == 1 ? "" : "s")} stopped counting";
            int missing = Balance.RequiredSites - sites.Count;
            int latestSite = margin.LatestStart - Balance.SiteMonths;
            if (latestSite - s.ElapsedMonths < 0) return "Too late for new sites without help";
            return $"Fund {missing} more site{(missing == 1 ? "" : "s")} soon";
        }

        /// <summary>The three answers to a threat, in the order they are offered.</summary>
        public static readonly string[] Responses = { "counter", "scatter", "silence" };

        /// <summary>The stamp for one answer: the verb and what it is paid in.</summary>
        public static string ResponseStamp(GameState s, ActionPreview preview, string id)
        {
            switch (id)
            {
                case "counter": return $"OBSTRUCT · {preview.Influence} INF";
                case "scatter": return $"SCATTER · {GameEngine.ScatterMonths(s)} MO";
                default: return $"SILENCE · {preview.Dread} DREAD";
            }
        }

        /// <summary>Short plain explanation of why a ThreatOption cannot be used now.</summary>
        public static string ThreatOptionReason(ThreatOption opt)
        {
            string reason = opt?.Reason ?? "";
            if (reason.Length == 0) return "";
            if (reason.StartsWith("Available in ", StringComparison.Ordinal))
            {
                string months = Between(reason, "Available in ", " months.", " month.");
                string verb = opt.Response == "counter" ? "counter" : opt.Response == "scatter" ? "scatter" : "silence";
                string unit = months == "1" ? "month" : "months";
                return $"{verb} rests {months} more {unit}";
            }
            if (reason.StartsWith("Need ", StringComparison.Ordinal))
                return "need " + Between(reason, "Need ", ".");
            return reason.TrimEnd('.');
        }

        /// <summary>Banner text when Threats.Cornered is true: neither obstruction nor silencing is available.</summary>
        public static string ThreatCorneredLabel(GameState s, string regionId, bool compact = false)
        {
            var options = Threats.Options(s, regionId);
            bool canScatter = options.Count > 1 && options[1].Ok;
            if (compact) return canScatter ? "CORNERED · SCATTER OR ENDURE" : "CORNERED · NO ANSWER READY";
            return canScatter ? "CORNERED · ONLY SCATTER OR ENDURE" : "CORNERED · NO ANSWER READY";
        }

        /// <summary>What that answer costs besides its price, in a few words.</summary>
        public static string ResponseNote(RegionState r, ActionPreview preview, string id)
        {
            switch (id)
            {
                case "counter": return $"No obstruction here for {GameData.Actions["counter"].Cooldown} months";
                case "scatter": return "No growth or income" + (r.Site.Invested ? "; the site stalls" : "") + (r.Public ? "; worship goes underground" : "");
                default: return "+" + Js.Num(preview.Awareness) + " world awareness";
            }
        }

        /// <summary>What a threat does if it lands, one plain phrase per line.</summary>
        public static List<string> ThreatEffects(GameState s, RegionState r)
        {
            ThreatConsequences c = Threats.Consequences(s, r);
            var lines = new List<string> { $"Kills {(int)Math.Ceiling(c.Followers)} followers" };
            if (c.DestroysCell)
            {
                lines[0] = "Destroys the cell" + (r.Site.Invested ? " and its site" : "");
                return lines;
            }
            if (c.PreparationLost > 0) lines.Add($"Site loses {c.PreparationLost} months");
            if (c.InterruptsSite) lines.Add("Site stops counting for the rite");
            return lines;
        }

        /// <summary>
        /// Why an action is unavailable: (short, long). The engine names the first failed check, which
        /// is often the price; a missing cell, threat or omen is the real obstacle, so it wins.
        /// </summary>
        public static (string shortText, string longText) Blocker(GameState s, string id, string regionId, ActionPreview preview)
        {
            if (preview.Ok) return ("", "");
            RegionState r = s.Regions[regionId];
            Population p = r.Population;
            (string, string) found = ("", "");
            if (id != "expand" && p.Followers <= 0)
            {
                found = ("NO CELL HERE", "Establish a cult here first.");
            }
            else
            {
                switch (id)
                {
                    case "expand":
                        if (p.Followers > 0) found = ("ALREADY HELD", "This region already has a cell.");
                        else if (Derived.TotalFollowers(s) > 0 && !GameData.Regions[regionId].Adjacent.Any(o => s.Regions[o].Population.Followers > 0))
                            found = ("NOT NEXT TO A CELL", "Requires a cult in an adjacent region.");
                        break;
                    case "counter":
                    case "scatter":
                    case "silence":
                        if (r.Threat == null) found = ("NO THREAT", "No approaching threat in this region.");
                        break;
                    case "exploit":
                        if (r.Opportunity == null || (r.Opportunity.Kind != "nightmare" && r.Opportunity.Kind != "eclipse"))
                            found = ("NEEDS AN OMEN", "Requires an active nightmare or eclipse.");
                        else if (r.Opportunity.Kind == "eclipse" && (!r.Site.Invested || r.Site.Progress >= Balance.SiteMonths))
                            found = ("NEEDS A SITE", "An eclipse needs an unfinished ritual site.");
                        break;
                    case "convert":
                        if (r.Figure != null || r.Opportunity == null || (r.Opportunity.Kind != "celebrity" && r.Opportunity.Kind != "politician"))
                            found = ("NEEDS A FIGURE", "Requires an unconverted prominent-person opportunity.");
                        break;
                    case "reveal_figure":
                        if (r.Figure == null || r.Figure.Revealed) found = ("NO SECRET FIGURE", "Requires a secretly converted figure.");
                        else if (!r.Site.Invested) found = ("NEEDS A SITE", "Prepare a local ritual site to receive their awakening contribution.");
                        break;
                    case "prepare":
                        if (r.Site.Invested) found = ("FUNDED", "This site's preparation is already funded.");
                        break;
                    case "manifest":
                        if (p.Deniers + p.Unaware < 1) found = ("ALL BELIEVE", "Everyone here already believes.");
                        break;
                    case "recruit":
                        if (Derived.Recruitable(r) < 1) found = ("NO RECRUITS LEFT", "No recruits available. Use a manifestation to change disbelief.");
                        break;
                    case "bury":
                        if (s.Awareness <= 0) found = ("NO AWARENESS", "Nothing to bury: the world has not noticed you.");
                        break;
                    case "restore":
                        if (!r.Site.Invested || r.Site.Recoverable <= 0) found = ("NO LOST PREP", "No lost preparation to restore here.");
                        else if (GameEngine.RestoreMonths(s, regionId) <= 0) found = ("SITE READY", "This site is already prepared.");
                        break;
                }
            }
            if (found.Item1 == "") found = (ShortReason(preview), preview.Reason ?? "");
            return found;
        }

        static string Between(string text, string prefix, params string[] suffixes)
        {
            string rest = text.StartsWith(prefix, StringComparison.Ordinal) ? text.Substring(prefix.Length) : text;
            foreach (string suffix in suffixes)
                if (rest.EndsWith(suffix, StringComparison.Ordinal)) rest = rest.Substring(0, rest.Length - suffix.Length);
            return rest;
        }

        /// <summary>A few typed words for why an action cannot be taken, shown where its cost would be.</summary>
        public static string ShortReason(ActionPreview preview)
        {
            string reason = preview.Reason ?? "";
            if (reason.StartsWith("Available in", StringComparison.Ordinal)) return "IN " + Between(reason, "Available in ", " months.", " month.") + " MO";
            if (reason.StartsWith("Need ", StringComparison.Ordinal) && reason.EndsWith(" Influence.", StringComparison.Ordinal)) return "NEED " + Between(reason, "Need ", " Influence.") + " INF";
            if (reason.StartsWith("Need ", StringComparison.Ordinal) && reason.EndsWith(" Dread.", StringComparison.Ordinal)) return "NEED " + Between(reason, "Need ", " Dread.") + " DREAD";
            if (reason.StartsWith("Need ", StringComparison.Ordinal) && reason.EndsWith(" local followers.", StringComparison.Ordinal)) return "NEED " + Between(reason, "Need ", " local followers.") + " HERE";
            if (reason.StartsWith("The flock is scattered for ", StringComparison.Ordinal)) return "SCATTERED " + Between(reason, "The flock is scattered for ", " more months.") + " MO";
            if (reason.StartsWith("Maintain at least", StringComparison.Ordinal)) return "NEED " + Between(reason, "Maintain at least ", " local followers.") + " HERE";
            var table = new[]
            {
                ("Establish a cult here", "NO CELL HERE"), ("No recruits available", "NO RECRUITS LEFT"),
                ("already has a cell", "ALREADY HELD"), ("adjacent region", "NOT NEXT TO A CELL"),
                ("Nothing to conceal", "NOTHING TO HIDE"), ("already believes", "ALL BELIEVE"),
                ("already funded", "FUNDED"), ("Too late", "TOO LATE"), ("No approaching threat", "NO THREAT"),
                ("nightmare or eclipse", "NEEDS AN OMEN"), ("eclipse needs", "NEEDS A SITE"),
                ("prominent-person", "NEEDS A FIGURE"), ("secretly converted", "NO SECRET FIGURE"),
                ("receive their awakening", "NEEDS A SITE"), ("already public", "ALREADY PUBLIC"),
                ("already underground", "ALREADY HIDDEN"), ("does not turn back underground", "IN PROCESSION"),
                ("Nothing to bury", "NO AWARENESS"),
                ("No lost preparation", "NO LOST PREP"), ("already prepared", "SITE READY"),
            };
            foreach (var (text, label) in table)
                if (reason.Contains(text)) return label;
            return "UNAVAILABLE";
        }

        /// <summary>How a finished run ended. The cause is the most basic failure; advice covers up to two of the problems found.</summary>
        public static Ending Ending(GameState s)
        {
            var result = new Ending { Fact = DatedFact(s) };
            RiteStatus rite = Forecast.Rite(s);
            int req = rite.Started ? rite.Required : RiteMonths(s);
            int latestSite = Balance.Deadline - req - Balance.SiteMonths;
            if (s.Status == "won")
            {
                result.Cause = s.RiteMonthsRequired < Balance.FinalMonths
                    ? $"The Great Procession held three sites for {s.RiteMonthsRequired} months."
                    : "Three sites held through the final rite.";
                return result;
            }
            var sites = AllRegions(s).Where(r => r.Site.Invested).ToList();
            if (s.Awareness >= 100)
            {
                result.Cause = "World awareness reached 100%. The world found the cult before the stars were right.";
                result.Advice.Add("Conceal the evidence or Bury the record before awareness passes half; both grow dearer as awareness or burials climb.");
                result.Advice.Add("Public worship, sites and a very large cult all raise awareness. Grow only as large as the rite needs.");
                return result;
            }
            foreach (string id in GameData.RegionIds)
            {
                RegionState r = s.Regions[id];
                if (!r.Site.Invested) continue;
                bool ready = r.Site.Progress >= Balance.SiteMonths && Derived.Supported(r) && !r.Site.Dormant;
                result.Checks.Add((ready, GameData.Regions[id].Name, Derived.SiteLabel(r)));
            }
            for (int i = 0; i < Math.Max(0, Balance.RequiredSites - sites.Count); i++) result.Checks.Add((false, "Unfunded site", "Never funded"));
            bool started = rite.Started || s.FinalProgress > 0;
            result.Checks.Add((started, "Final rite", $"{s.FinalProgress} of {req} months"));
            var issues = new List<(string cause, string advice)>();
            if (started)
                issues.Add(($"The rite began but lost a supported site before its {req} months were done.",
                    "During the rite the world hunts your sites. Keep Influence to obstruct and Dread to silence the threats against them."));
            if (sites.Count < Balance.RequiredSites)
            {
                int nextCost = Balance.SiteDreadCosts[Math.Min(sites.Count, Balance.SiteDreadCosts.Length - 1)];
                issues.Add(($"Only {sites.Count} of 3 ritual sites were funded.",
                    $"Fund sites earlier. Each needs 24 months, then the rite needs {req}; the last safe start is {Format.Date(latestSite)}."
                    + (s.Dread < nextCost ? $" Bank Dread by sacrifice: the next site costs {nextCost}." : "")));
            }
            if (sites.Any(r => !Derived.Supported(r)))
                issues.Add(($"{Derived.ReadySites(s)} of 3 sites were ready; others lost the followers they need.",
                    "Build sites in small regions, where a quarter of the people is easier to hold, and obstruct threats there."));
            if (sites.Any(r => Derived.Supported(r) && r.Site.Progress < Balance.SiteMonths))
                issues.Add(("The sites were not finished in time.",
                    $"Start preparation by {Format.Date(latestSite)}, or speed it with eclipses and revealed figures."));
            if (!started && s.Dread < Balance.FinalDread)
                issues.Add(("The rite needed 100 Dread.", "Bank 100 Dread before September 1924; sacrifice and nightmares give Dread."));
            if (issues.Count == 0)
                issues.Add(("The sites were ready, but the rite was not begun in time.", $"Begin the final rite as soon as the stars are right; it needs {req} months."));
            result.Cause = s.ResultCode == "conceded"
                ? "The cult let the stars pass unanswered: " + char.ToLowerInvariant(issues[0].cause[0]) + issues[0].cause.Substring(1)
                : issues[0].cause;
            foreach (var issue in issues.Take(2)) result.Advice.Add(issue.advice);
            return result;
        }

        /// <summary>One dated fact from the run's state or chronicle explaining how it ended.</summary>
        public static string DatedFact(GameState s)
        {
            RiteStatus rite = Forecast.Rite(s);
            int req = rite.Started ? rite.Required : RiteMonths(s);
            int latestStart = Balance.Deadline - req;
            int latestSite = latestStart - Balance.SiteMonths;

            if (s.Status == "won")
            {
                int slack = Balance.Deadline - s.ElapsedMonths;
                LogEntry riteStart = s.Log.LastOrDefault(IsRiteStartLog);
                string riteTitle = s.RiteMonthsRequired < Balance.FinalMonths ? "The Great Procession" : "The final rite";
                if (riteStart != null)
                {
                    return slack > 0
                        ? $"{riteTitle} began in {Format.Date(riteStart.Month)} and finished with {Format.Months(slack)} to spare."
                        : $"{riteTitle} began in {Format.Date(riteStart.Month)} and finished in the final month of the alignment.";
                }
                return slack > 0
                    ? $"The awakening closed in {Format.Date(s.ElapsedMonths)}, with {Format.Months(slack)} left in the alignment."
                    : $"The awakening closed in {Format.Date(s.ElapsedMonths)}, as the alignment ended.";
            }

            if (s.ResultCode == "conceded")
            {
                int remaining = Math.Max(0, Balance.Deadline - s.ElapsedMonths);
                var fundedSites = AllRegions(s).Where(r => r.Site.Invested).ToList();
                if (s.ElapsedMonths > latestStart)
                    return $"You let the stars pass in {Format.Date(s.ElapsedMonths)}, past the {Format.Date(latestStart)} cutoff, with {Format.Months(remaining)} left in the alignment.";
                if (fundedSites.Count < Balance.RequiredSites)
                    return $"You let the stars pass in {Format.Date(s.ElapsedMonths)} with only {fundedSites.Count} of {Balance.RequiredSites} ritual sites funded and too little time before {Format.Date(latestStart)}.";
                int readySites = Derived.ReadySites(s);
                return $"You let the stars pass in {Format.Date(s.ElapsedMonths)} with {readySites} of {Balance.RequiredSites} sites ready and no path to begin by {Format.Date(latestStart)}.";
            }

            if (s.Awareness >= 100)
            {
                int silences = s.Telemetry.Actions.TryGetValue("silence", out int sc) ? sc : 0;
                LogEntry lastSilence = s.Log.LastOrDefault(e => e.Message.StartsWith("Silence the witnesses · ", StringComparison.Ordinal));
                if (silences > 0 && lastSilence != null)
                    return $"By {Format.Date(s.ElapsedMonths)} awareness reached 100%; witnesses were silenced {silences} {(silences == 1 ? "time" : "times")}, last in {Format.Date(lastSilence.Month)}.";
                if (s.Telemetry.Revelations.Count > 0)
                {
                    var firstRev = s.Telemetry.Revelations.OrderBy(p => p.Value).First();
                    return $"Public worship began in {GameData.Regions[firstRev.Key].Name} in {Format.Date(firstRev.Value)}; by {Format.Date(s.ElapsedMonths)} awareness reached 100%.";
                }
                int conceals = s.Telemetry.Actions.TryGetValue("conceal", out int cc) ? cc : 0;
                if (conceals == 0)
                    return $"Awareness reached 100% in {Format.Date(s.ElapsedMonths)} with {Format.Count(Derived.TotalFollowers(s))} followers and evidence never concealed.";
                int cells = AllRegions(s).Count(r => r.Population.Followers > 0);
                return $"Awareness reached 100% in {Format.Date(s.ElapsedMonths)} with {Format.Count(Derived.TotalFollowers(s))} followers across {cells} {(cells == 1 ? "region" : "regions")}.";
            }

            if (rite.Started || s.FinalProgress > 0)
            {
                LogEntry riteStart = s.Log.LastOrDefault(IsRiteStartLog);
                if (riteStart != null)
                {
                    LogEntry hit = s.Log.LastOrDefault(e => e.Month >= riteStart.Month
                        && (e.Message.Contains("Ritual support is interrupted.") || e.Message.Contains("The cell and any ritual site are destroyed.") || e.Message.Contains("lies dormant")));
                    if (hit != null)
                        return $"The rite began in {Format.Date(riteStart.Month)}, but lost support in {Format.Date(hit.Month)} ({s.FinalProgress} of {req} months held).";
                    if (riteStart.Month > latestStart)
                        return $"The rite did not begin until {Format.Date(riteStart.Month)}, past the {Format.Date(latestStart)} cutoff.";
                    return $"The rite began in {Format.Date(riteStart.Month)}, yet only {s.FinalProgress} of {req} months were held before {Format.Date(Balance.Deadline)}.";
                }
                return $"When {Format.Date(s.ElapsedMonths)} closed, the final rite stood at {s.FinalProgress} of {req} months.";
            }

            int lateOrders = s.Log.Count(e => e.Month > latestStart && e.Message.Contains(" · "));
            if (lateOrders > 0)
                return $"When {Format.Date(latestStart)} passed you had not begun the rite, though {lateOrders} {(lateOrders == 1 ? "order was" : "orders were")} still given afterward.";

            var funded = AllRegions(s).Where(r => r.Site.Invested).ToList();
            if (funded.Count < Balance.RequiredSites)
            {
                LogEntry lastSite = s.Log.LastOrDefault(e => e.Message.StartsWith("Prepare a ritual site · ", StringComparison.Ordinal));
                if (funded.Count == 0)
                    return $"When {Format.Date(latestStart)} passed, no ritual site had been funded and the rite could not begin.";
                if (lastSite != null)
                    return $"Your last ritual site was funded in {Format.Date(lastSite.Month)}; by {Format.Date(latestStart)} only {funded.Count} of {Balance.RequiredSites} existed.";
                return $"By {Format.Date(latestStart)} only {funded.Count} of {Balance.RequiredSites} ritual sites were funded, and the rite had not begun.";
            }

            LogEntry lateSite = s.Log.LastOrDefault(e => e.Month > latestSite && e.Message.StartsWith("Prepare a ritual site · ", StringComparison.Ordinal));
            if (lateSite != null)
                return $"A ritual site was funded in {Format.Date(lateSite.Month)}, past the {Format.Date(latestSite)} safe date, and the rite never began.";

            LogEntry siteHit = s.Log.LastOrDefault(e => e.Message.Contains("Ritual loses ") || e.Message.Contains("Ritual support is interrupted."));
            if (siteHit != null)
                return $"After a strike in {Format.Date(siteHit.Month)}, the cult reached {Format.Date(latestStart)} without beginning the rite.";

            return $"When {Format.Date(latestStart)} closed you had not begun the rite, leaving too little time before {Format.Date(Balance.Deadline)}.";
        }
    }
}
