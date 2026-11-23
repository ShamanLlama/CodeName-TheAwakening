using System;

namespace Awakening.Core.Rules
{
    public static class Balance
    {
        public const string Ruleset = "alignment-7";
        public const int PreparationMonths = 200;
        public const int AlignmentMonths = 24;
        public const int FinalMonths = 12;
        public const double SecondsPerMonth = 2.0;
        public const int PopulationScale = 100;
        public const double InfluenceCap = 4000.0;
        public const double DreadCap = 400.0;
        public const int StartingFollowers = 100;
        public const double StartingInfluence = 35.0;
        public const double StartingDread = 20.0;
        public const double InfluencePerFollower = 0.014;
        public const double RecruitmentBase = 0.6;
        public const double RecruitmentRate = 0.0075;
        public const double PublicRecruitment = 2.0;
        public const int RecruitAmount = 35;
        public const int ExpandAmount = 35;
        public const int SacrificeFollowers = 40;
        public const int SacrificeDread = 25;
        public const int ManifestationDeniers = 65;
        public const int ManifestationUnaware = 50;
        /// <summary>Normal's preparation months; GameEngine.SiteMonths(state) rules, since alignment-7 it depends on difficulty.</summary>
        public const int SiteMonths = 24;
        public const int SiteFollowers = 60;
        public const int RequiredSites = 3;
        public const int FinalDread = 100;
        public const double SiteSupportShare = 0.25;
        public static readonly int[] SiteDreadCosts = new[] { 60, 100, 160 };
        public const double RaidLossShare = 0.12;
        public const double ThreatLossShare = 0.08;
        public const int RaidLossFloor = 8;
        public const int ThreatLossFloor = 6;
        public const int CounterSeverityCost = 30;
        public const int ScatterMonths = 8;
        public const double ScatterExposure = 10.0;
        public const int SilenceSeverityDread = 20;
        public const int SilenceSeverityDreadTerror = 13;
        public const double SilenceSeverityAwareness = 3.0;
        public const double SilenceExposure = 6.0;
        public const double RevealAwareness = 5.0;
        public const double ManifestAwareness = 8.0;
        public const double SiteAwareness = 8.0;
        public const double FigureAwareness = 8.0;
        public const double AwarenessDrag = 150.0;
        public const double ConcealAwareness = 3.0;
        public const double ConcealAwarenessPrice = 0.5;
        /// <summary>Months the world-awareness part of concealment rests after it is used, across every region.</summary>
        public const int ConcealWorldMonths = 6;
        public const double SiteUpkeep = 1.0;
        /// <summary>Extra threat pressure on regions with a site once the stars align (alignment-6: 0.3 → 0.6, the finale every capstone answers).</summary>
        public const double FinaleSitePressure = 0.6;
        /// <summary>alignment-6: from this month every threat roll weighs a little more, so the middle of the run is not quiet.</summary>
        public const int MidgamePressureMonth = 100;
        public const double MidgamePressure = 0.05;
        public const int FinaleWarningMonths = 24;
        public const double AwarenessPerCapita = 1.5;
        public const int FigureSiteProgress = 8;
        public const int ThreatWarningMonths = 5;
        public const int ThreatRollMonths = 4;
        // alignment-5: the talent tree (seal months and the costs of each difficulty live in GameData.Difficulties).
        /// <summary>Normal's talent prices; DifficultyDefinition.TalentBaseCost and TalentCostStep rule (alignment-7).</summary>
        public const int TalentBaseCost = 40;
        public const int TalentCostStep = 25;
        public const double SwellingFlockRecruitment = 0.35;
        public const int QuietFriendsWarning = 2;
        public const int RedAltarsSacrificeDread = 35;
        public const int RedAltarsManifestDread = 15;
        public const double OpenWorshipRecruitment = 2.5;
        public const double MartyrDreadPerFollower = 1.0 / 3.0;
        public const int SleeperScatterMonths = 5;
        public const double MuteWitnessesSeverityAwareness = 2.0;
        public const double ScapegoatSeverityAwareness = 1.0;
        // alignment-6: capstones that open another way to win.
        /// <summary>The Great Procession: public regions needed when the final rite begins.</summary>
        public const int ProcessionPublicRegions = 5;
        /// <summary>The Great Procession: months the final rite needs instead of FinalMonths.</summary>
        public const int ProcessionRiteMonths = 8;
        /// <summary>The Great Procession: world awareness each public region adds every month of the rite.</summary>
        public const double ProcessionAwareness = 0.3;
        /// <summary>The world forgets: world awareness removed when invoked.</summary>
        public const int WorldForgetsAwareness = 25;
        /// <summary>The world forgets: months every cell stays scattered.</summary>
        public const int WorldForgetsScatterMonths = 6;
        /// <summary>Dreams of R'lyeh: world awareness each rite-advancing sacrifice adds.</summary>
        public const double DreamsAwareness = 2.0;
        /// <summary>Dreams of R'lyeh (alignment-7): regions at or above this Madness pass it to neighbouring cells every month.</summary>
        public const double DreamsSpreadFrom = 30.0;
        /// <summary>Dreams of R'lyeh: Madness each such region adds to each neighbouring region with a cell, every month after it fades (net +1).</summary>
        public const double DreamsSpread = 2.0;
        /// <summary>Dreams of R'lyeh: Madness a sacrifice leaves is multiplied by this.</summary>
        public const double DreamsSacrificeMadness = 2.0;
        /// <summary>Dreams of R'lyeh: during the rite, a sacrifice where Madness is at least this carries it DreamsMadRiteMonths forward, not one.</summary>
        public const double DreamsRiteMadness = 30.0;
        public const int DreamsMadRiteMonths = 2;
        // alignment-7: Madness is a regional resource (0–100). Manifestations and sacrifices raise it; it fades every month.
        public const double ManifestMadness = 20.0;
        public const double SacrificeMadness = 10.0;
        public const double MadnessDecay = 1.0;
        /// <summary>Each month a region with a cell yields Madness / MadnessDreadDivisor Dread.</summary>
        public const double MadnessDreadDivisor = 20.0;
        /// <summary>From this Madness, new threats against a hidden cell without a site are occult, and the world notices the region.</summary>
        public const double MadnessHigh = 50.0;
        /// <summary>A preparing site gains Madness / MadnessQuickenFull extra preparation months every month (at 50, one every 2 months).</summary>
        public const double MadnessQuickenFull = 100.0;
        /// <summary>Threat pressure adds Madness / MadnessPressureDivisor (alignment-6: 2000).</summary>
        public const double MadnessPressureDivisor = 400.0;
        /// <summary>World awareness each region of high Madness with a cell adds every month.</summary>
        public const double MadnessAwareness = 0.05;
        // alignment-7: after obstructing or silencing, the region draws no new threat for these months.
        public const int ThreatGraceMonths = 5;
        // Bury the record: Influence for world awareness, dearer with each use (the step depends on difficulty).
        public const int BuryAwareness = 5;
        public const int BuryBaseCost = 50;
        public const double BuryExposure = 8.0;
        public const int BuryCooldown = 6;
        // Restore the site: buy back months an ignored occult threat took.
        public const int RestoreMonthCost = 30;
        public const double RestoreExposure = 10.0;
        /// <summary>Normal's preparation loss from an ignored occult threat; DifficultyDefinition.OccultPreparationLoss rules.</summary>
        public const int OccultPreparationLoss = 6;
        // Mysticism: a manifestation in a region of Mysticism m draws ManifestAwareness × (1 − (m − 50)/100)
        // awareness and converts its usual numbers × (1 + (m − 50)/100), both rounded half up.
        public const int MysticismPivot = 50;
        public const int Deadline = PreparationMonths + AlignmentMonths;
        public const int LatestRiteStart = Deadline - FinalMonths;
    }
}
