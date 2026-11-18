using System;

namespace Awakening.Core.Rules
{
    public static class Balance
    {
        public const string Ruleset = "alignment-3";
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
        public const double SiteUpkeep = 1.0;
        public const double FinaleSitePressure = 0.15;
        public const int FinaleWarningMonths = 24;
        public const double AwarenessPerCapita = 1.5;
        public const int FigureSiteProgress = 8;
        public const int ThreatWarningMonths = 5;
        public const int ThreatRollMonths = 4;
        public const int Deadline = PreparationMonths + AlignmentMonths;
        public const int LatestSiteStart = Deadline - FinalMonths - SiteMonths;
        public const int LatestRiteStart = Deadline - FinalMonths;
    }
}
