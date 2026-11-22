using System;

namespace Awakening.Core.Rules
{
    public static class Balance
    {
        public const string Ruleset = "alignment-5";
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
        public const double FinaleSitePressure = 0.3;
        public const int FinaleWarningMonths = 24;
        public const double AwarenessPerCapita = 1.5;
        public const int FigureSiteProgress = 8;
        public const int ThreatWarningMonths = 5;
        public const int ThreatRollMonths = 4;
        // alignment-5: the talent tree (seal months and the costs of each difficulty live in GameData.Difficulties).
        public const int TalentBaseCost = 40;
        public const int TalentCostStep = 30;
        public const double SwellingFlockRecruitment = 0.35;
        public const int QuietFriendsWarning = 2;
        public const int RedAltarsSacrificeDread = 35;
        public const int RedAltarsManifestDread = 15;
        public const double OpenWorshipRecruitment = 2.5;
        public const double MartyrDreadPerFollower = 1.0 / 3.0;
        public const double FaithAmongFaithsShare = 0.5;
        public const int SleeperScatterMonths = 5;
        public const int BoughtPressAwareness = 8;
        public const double MuteWitnessesSeverityAwareness = 2.0;
        public const double ScapegoatSeverityAwareness = 1.0;
        public const double OfferingAwareness = 1.0;
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
        public const int LatestSiteStart = Deadline - FinalMonths - SiteMonths;
        public const int LatestRiteStart = Deadline - FinalMonths;
    }
}
