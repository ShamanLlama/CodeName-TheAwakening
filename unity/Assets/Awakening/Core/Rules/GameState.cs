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
        public string Specialization;
        public int FinalProgress;
        public RegionMap Regions = new RegionMap();
        public List<LogEntry> Log = new List<LogEntry>();
        public int NextLogId;
        public string ResultReason;
        public Telemetry Telemetry = new Telemetry();

        public GameState Clone()
        {
            var copy = (GameState)MemberwiseClone();
            copy.Regions = Regions?.Clone();
            copy.Log = Log.Select(entry => entry.Clone()).ToList();
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
    public sealed class RitualForecast { public bool Urgent; public string Message; }
}
