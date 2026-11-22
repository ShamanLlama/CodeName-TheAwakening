using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Awakening.Core.Rules
{
    public sealed class GameState
    {
        public int Version = 2;
        public string Ruleset;
        public string Status;
        public string Difficulty;
        public string Seed;
        public uint RngState;
        public int ElapsedMonths;
        public int Speed;
        public double Influence;
        public double Dread;
        public double Awareness;
        /// <summary>Talent ids taken this run, in order (alignment-5; replaces the doctrine).</summary>
        public List<string> Talents = new List<string>();
        /// <summary>Times Bury the record was used this run; each use raises its price.</summary>
        public int BuryUses;
        /// <summary>What each taken talent has given this run (alignment-6), in the unit GameEngine.TalentYieldText names.</summary>
        public Dictionary<string, double> TalentYield = new Dictionary<string, double>();
        /// <summary>Invokable capstones already spent this run.</summary>
        public List<string> CapstonesUsed = new List<string>();
        /// <summary>Months the final rite needs: Balance.FinalMonths, or fewer with The Great Procession. Fixed when the rite begins.</summary>
        public int RiteMonthsRequired = Balance.FinalMonths;
        /// <summary>Regions whose prepared sites carried the final rite when it began (empty before), so a destroyed one can still be named.</summary>
        public List<string> RiteSites = new List<string>();
        public int FinalProgress;
        public RegionMap Regions = new RegionMap();
        public List<LogEntry> Log = new List<LogEntry>();
        public int NextLogId;
        public string ResultReason;
        /// <summary>Month from which concealment lowers world awareness again (0 = available).</summary>
        public int WorldConcealReadyAt;
        /// <summary>Null while the run is active; then "awakened", "world_awareness", "deadline" or "conceded".</summary>
        public string ResultCode;
        public Telemetry Telemetry = new Telemetry();

        public GameState Clone()
        {
            var copy = (GameState)MemberwiseClone();
            copy.Regions = Regions?.Clone();
            copy.Log = Log.Select(entry => entry.Clone()).ToList();
            copy.Talents = new List<string>(Talents);
            copy.TalentYield = new Dictionary<string, double>(TalentYield);
            copy.CapstonesUsed = new List<string>(CapstonesUsed);
            copy.RiteSites = new List<string>(RiteSites);
            copy.Telemetry = Telemetry?.Clone();
            return copy;
        }
    }

    public sealed class RegionState
    {
        public Population Population = new Population();
        public bool Public;
        public bool Revealed;
        public bool Scattered;
        public int ScatteredUntil;
        public double Exposure;
        public double Madness;
        public int BacklashUntil;
        public Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
        public Site Site = new Site();
        public Threat Threat;
        public Opportunity Opportunity;
        public Figure Figure;

        public RegionState Clone()
        {
            var copy = (RegionState)MemberwiseClone();
            copy.Population = Population?.Clone();
            copy.Cooldowns = new Dictionary<string, int>(Cooldowns);
            copy.Site = Site?.Clone();
            copy.Threat = Threat?.Clone();
            copy.Opportunity = Opportunity?.Clone();
            copy.Figure = Figure?.Clone();
            return copy;
        }
    }

    public sealed class Population
    {
        public double Unaware;
        public double Deniers;
        public double Believers;
        public double Followers;
        public double Losses;

        public Population Clone()
        {
            var copy = (Population)MemberwiseClone();
            return copy;
        }
    }

    public sealed class Site
    {
        public bool Invested;
        public int Progress;
        public bool Dormant;
        /// <summary>Preparation months the last ignored occult threat took, which Restore the site can still buy back (0 = nothing).</summary>
        public int Recoverable;

        public Site Clone()
        {
            var copy = (Site)MemberwiseClone();
            return copy;
        }
    }

    public sealed class Threat
    {
        public string Kind;
        public int DueMonth;
        public int Severity;

        public Threat Clone()
        {
            var copy = (Threat)MemberwiseClone();
            return copy;
        }
    }

    public sealed class Opportunity
    {
        public string Kind;
        public int ExpiresMonth;

        public Opportunity Clone()
        {
            var copy = (Opportunity)MemberwiseClone();
            return copy;
        }
    }

    public sealed class Figure
    {
        public string Kind;
        public bool Revealed;

        public Figure Clone()
        {
            var copy = (Figure)MemberwiseClone();
            return copy;
        }
    }

    public sealed class LogEntry
    {
        public int Id;
        public int Month;
        public string Message;

        public LogEntry Clone()
        {
            var copy = (LogEntry)MemberwiseClone();
            return copy;
        }
    }

    public sealed class Telemetry
    {
        public Dictionary<string, int> Actions = new Dictionary<string, int>();
        public double Sacrificed;
        public double RaidLosses;
        public double InfluenceEarned;
        public double InfluenceSpent;
        public double DreadEarned;
        public double DreadSpent;
        public double UpkeepPaid;
        public int ThreatsCountered;
        public Dictionary<string, int> Revelations = new Dictionary<string, int>();

        public Telemetry Clone()
        {
            var copy = (Telemetry)MemberwiseClone();
            copy.Actions = new Dictionary<string, int>(Actions);
            copy.Revelations = new Dictionary<string, int>(Revelations);
            return copy;
        }
    }

    /// <summary>Region lookup and enumeration always follow GameData.RegionIds.</summary>
    public sealed class RegionMap : IReadOnlyDictionary<string, RegionState>
    {
        readonly RegionState[] values = new RegionState[GameData.RegionIds.Length];
        static int Index(string id) => Array.IndexOf(GameData.RegionIds, id);
        public RegionState this[string id]
        {
            get { int i = Index(id); return i < 0 ? throw new KeyNotFoundException(id) : values[i]; }
            set { int i = Index(id); if (i < 0) throw new KeyNotFoundException(id); values[i] = value; }
        }
        public int Count => values.Length;
        public IEnumerable<string> Keys => GameData.RegionIds;
        public IEnumerable<RegionState> Values => values;
        public bool ContainsKey(string id) => Index(id) >= 0;
        public bool TryGetValue(string id, out RegionState value)
        {
            int i = Index(id);
            value = i < 0 ? null : values[i];
            return i >= 0;
        }
        public RegionMap Clone()
        {
            var copy = new RegionMap();
            for (int i = 0; i < values.Length; i++) copy.values[i] = values[i]?.Clone();
            return copy;
        }
        public IEnumerator<KeyValuePair<string, RegionState>> GetEnumerator()
        {
            for (int i = 0; i < values.Length; i++)
                yield return new KeyValuePair<string, RegionState>(GameData.RegionIds[i], values[i]);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class CommandResult { public bool Ok; public string Message; public GameState State; }
    public sealed class ActionPreview
    {
        public bool Ok;
        public string Reason;
        public int Influence, Dread, Followers;
        public double Awareness;
        public string Effect, Risk;
    }
    public sealed class ThreatConsequences
    {
        public double Followers, Remaining;
        public bool DestroysCell;
        public int PreparationLost;
        public double IncomeLost, ExposureAdded;
        public bool InterruptsSite;
    }
    /// <summary>One answer to a region's threat, as it stands now.</summary>
    public sealed class ThreatOption
    {
        /// <summary>"counter", "scatter" or "silence".</summary>
        public string Response;
        public bool Ok;
        /// <summary>"" when legal; otherwise why not (cooldown, missing Dread or Influence, ...).</summary>
        public string Reason;
        /// <summary>What it costs, in one short phrase.</summary>
        public string Cost;
    }
    public sealed class RitualForecast { public bool Urgent; public string Message; }
    /// <summary>A site that keeps the final rite from advancing (Forecast.Rite).</summary>
    public sealed class RiteBlocker
    {
        public string Region;
        /// <summary>"destroyed", "dormant", "scattered", "unsupported" or "unprepared" (an occult society took months from it).</summary>
        public string Reason;
    }
    /// <summary>The final rite as it stands: how far it is, whether the next month advances it and what stops it.</summary>
    public sealed class RiteStatus
    {
        public bool Started;
        /// <summary>Months of the rite completed (GameState.FinalProgress).</summary>
        public int Progress;
        /// <summary>Months the rite needs: fixed once it begins; before that, what it would need if begun now.</summary>
        public int Required;
        /// <summary>The rite is under way and the next month advances it, if nothing else happens.</summary>
        public bool Advancing;
        /// <summary>While the rite is under way and halted: each site that does not count, in region order.</summary>
        public List<RiteBlocker> Blockers = new List<RiteBlocker>();
    }
    /// <summary>Whether the final rite still fits before the deadline, derived from the current sites.</summary>
    public sealed class RiteMargin
    {
        /// <summary>Last month the final rite can begin and still finish (Balance.LatestRiteStart).</summary>
        public int LatestStart;
        /// <summary>First month the rite could begin with the funded sites if each keeps progressing; -1 with fewer than three funded sites.</summary>
        public int EarliestStart;
        /// <summary>LatestStart - EarliestStart; negative means the plan is late. Meaningless when EarliestStart is -1.</summary>
        public int Margin;
        /// <summary>The rite can still be completed before Balance.Deadline, counting accelerations available now (or, once started, the months left).</summary>
        public bool Fits;
        public bool Started;
        /// <summary>The funded sites, fewest months left first.</summary>
        public List<SiteMargin> Sites = new List<SiteMargin>();
    }
    public sealed class SiteMargin
    {
        public string Region;
        /// <summary>Months until this site is prepared if it keeps progressing, including months the flock stays scattered.</summary>
        public int MonthsLeft;
        public bool Supported;
        public bool Scattered;
        public int Followers;
        public int Required;
    }
}
