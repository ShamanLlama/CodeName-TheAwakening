using System.Collections.Generic;
using System.Linq;
using System;
using Awakening.Core.Save;

namespace Awakening.Core.Rules
{
    /// <summary>Explicit save-key mapping; no reflection or engine serializer.</summary>
    public static class GameStateJson
    {
        public static GameState FromJson(object value) => SaveData.IsGameState(value) ? ReadGameState((Dictionary<string, object>)value) : null;
        static object Get(Dictionary<string, object> value, string key) => value.TryGetValue(key, out var entry) ? entry : null;
        static Dictionary<string, object> WriteCounts(Dictionary<string, int> counts) => counts.ToDictionary(e => e.Key, e => (object)e.Value);
        static Dictionary<string, int> ReadCounts(object value) => ((Dictionary<string, object>)value).ToDictionary(e => e.Key, e => (int)Convert.ToDouble(e.Value, System.Globalization.CultureInfo.InvariantCulture));

        public static Dictionary<string, object> ToJson(GameState value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["version"] = value.Version,
                ["ruleset"] = value.Ruleset,
                ["status"] = value.Status,
                ["difficulty"] = value.Difficulty,
                ["seed"] = value.Seed,
                ["rngState"] = value.RngState,
                ["elapsedMonths"] = value.ElapsedMonths,
                ["speed"] = value.Speed,
                ["influence"] = value.Influence,
                ["dread"] = value.Dread,
                ["awareness"] = value.Awareness,
                ["specialization"] = value.Specialization,
                ["finalProgress"] = value.FinalProgress,
                ["regions"] = value.Regions.ToDictionary(e => e.Key, e => (object)ToJson(e.Value)),
                ["log"] = value.Log.Select(e => (object)ToJson(e)).ToList(),
                ["nextLogId"] = value.NextLogId,
                ["resultReason"] = value.ResultReason,
                ["telemetry"] = ToJson(value.Telemetry),
            };
        }

        static GameState ReadGameState(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new GameState
            {
                Version = (int)Convert.ToDouble(Get(value, "version"), System.Globalization.CultureInfo.InvariantCulture),
                Ruleset = (string)Get(value, "ruleset"),
                Status = (string)Get(value, "status"),
                Difficulty = (string)Get(value, "difficulty"),
                Seed = (string)Get(value, "seed"),
                RngState = (uint)Convert.ToDouble(Get(value, "rngState"), System.Globalization.CultureInfo.InvariantCulture),
                ElapsedMonths = (int)Convert.ToDouble(Get(value, "elapsedMonths"), System.Globalization.CultureInfo.InvariantCulture),
                Speed = (int)Convert.ToDouble(Get(value, "speed"), System.Globalization.CultureInfo.InvariantCulture),
                Influence = (double)Convert.ToDouble(Get(value, "influence"), System.Globalization.CultureInfo.InvariantCulture),
                Dread = (double)Convert.ToDouble(Get(value, "dread"), System.Globalization.CultureInfo.InvariantCulture),
                Awareness = (double)Convert.ToDouble(Get(value, "awareness"), System.Globalization.CultureInfo.InvariantCulture),
                Specialization = (string)Get(value, "specialization"),
                FinalProgress = (int)Convert.ToDouble(Get(value, "finalProgress"), System.Globalization.CultureInfo.InvariantCulture),
                Regions = ReadRegions(Get(value, "regions")),
                Log = ((List<object>)Get(value, "log")).Select(e => ReadLogEntry((Dictionary<string, object>)e)).ToList(),
                NextLogId = (int)Convert.ToDouble(Get(value, "nextLogId"), System.Globalization.CultureInfo.InvariantCulture),
                ResultReason = (string)Get(value, "resultReason"),
                Telemetry = ReadTelemetry((Dictionary<string, object>)Get(value, "telemetry")),
            };
        }

        public static Dictionary<string, object> ToJson(RegionState value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["population"] = ToJson(value.Population),
                ["public"] = value.Public,
                ["revealed"] = value.Revealed,
                ["scattered"] = value.Scattered,
                ["scatteredUntil"] = value.ScatteredUntil,
                ["exposure"] = value.Exposure,
                ["madness"] = value.Madness,
                ["backlashUntil"] = value.BacklashUntil,
                ["cooldowns"] = WriteCounts(value.Cooldowns),
                ["site"] = ToJson(value.Site),
                ["threat"] = ToJson(value.Threat),
                ["opportunity"] = ToJson(value.Opportunity),
                ["figure"] = ToJson(value.Figure),
            };
        }

        static RegionState ReadRegionState(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new RegionState
            {
                Population = ReadPopulation((Dictionary<string, object>)Get(value, "population")),
                Public = (bool)Get(value, "public"),
                Revealed = (bool)Get(value, "revealed"),
                Scattered = (bool)Get(value, "scattered"),
                ScatteredUntil = (int)Convert.ToDouble(Get(value, "scatteredUntil"), System.Globalization.CultureInfo.InvariantCulture),
                Exposure = (double)Convert.ToDouble(Get(value, "exposure"), System.Globalization.CultureInfo.InvariantCulture),
                Madness = (double)Convert.ToDouble(Get(value, "madness"), System.Globalization.CultureInfo.InvariantCulture),
                BacklashUntil = (int)Convert.ToDouble(Get(value, "backlashUntil"), System.Globalization.CultureInfo.InvariantCulture),
                Cooldowns = ReadCounts(Get(value, "cooldowns")),
                Site = ReadSite((Dictionary<string, object>)Get(value, "site")),
                Threat = ReadThreat((Dictionary<string, object>)Get(value, "threat")),
                Opportunity = ReadOpportunity((Dictionary<string, object>)Get(value, "opportunity")),
                Figure = ReadFigure((Dictionary<string, object>)Get(value, "figure")),
            };
        }

        public static Dictionary<string, object> ToJson(Population value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["unaware"] = value.Unaware,
                ["deniers"] = value.Deniers,
                ["believers"] = value.Believers,
                ["followers"] = value.Followers,
                ["losses"] = value.Losses,
            };
        }

        static Population ReadPopulation(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Population
            {
                Unaware = (double)Convert.ToDouble(Get(value, "unaware"), System.Globalization.CultureInfo.InvariantCulture),
                Deniers = (double)Convert.ToDouble(Get(value, "deniers"), System.Globalization.CultureInfo.InvariantCulture),
                Believers = (double)Convert.ToDouble(Get(value, "believers"), System.Globalization.CultureInfo.InvariantCulture),
                Followers = (double)Convert.ToDouble(Get(value, "followers"), System.Globalization.CultureInfo.InvariantCulture),
                Losses = (double)Convert.ToDouble(Get(value, "losses"), System.Globalization.CultureInfo.InvariantCulture),
            };
        }

        public static Dictionary<string, object> ToJson(Site value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["invested"] = value.Invested,
                ["progress"] = value.Progress,
                ["dormant"] = value.Dormant,
            };
        }

        static Site ReadSite(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Site
            {
                Invested = (bool)Get(value, "invested"),
                Progress = (int)Convert.ToDouble(Get(value, "progress"), System.Globalization.CultureInfo.InvariantCulture),
                Dormant = (bool)Get(value, "dormant"),
            };
        }

        public static Dictionary<string, object> ToJson(Threat value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["kind"] = value.Kind,
                ["dueMonth"] = value.DueMonth,
                ["severity"] = value.Severity,
            };
        }

        static Threat ReadThreat(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Threat
            {
                Kind = (string)Get(value, "kind"),
                DueMonth = (int)Convert.ToDouble(Get(value, "dueMonth"), System.Globalization.CultureInfo.InvariantCulture),
                Severity = (int)Convert.ToDouble(Get(value, "severity"), System.Globalization.CultureInfo.InvariantCulture),
            };
        }

        public static Dictionary<string, object> ToJson(Opportunity value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["kind"] = value.Kind,
                ["expiresMonth"] = value.ExpiresMonth,
            };
        }

        static Opportunity ReadOpportunity(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Opportunity
            {
                Kind = (string)Get(value, "kind"),
                ExpiresMonth = (int)Convert.ToDouble(Get(value, "expiresMonth"), System.Globalization.CultureInfo.InvariantCulture),
            };
        }

        public static Dictionary<string, object> ToJson(Figure value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["kind"] = value.Kind,
                ["revealed"] = value.Revealed,
            };
        }

        static Figure ReadFigure(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Figure
            {
                Kind = (string)Get(value, "kind"),
                Revealed = (bool)Get(value, "revealed"),
            };
        }

        public static Dictionary<string, object> ToJson(LogEntry value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["id"] = value.Id,
                ["month"] = value.Month,
                ["message"] = value.Message,
            };
        }

        static LogEntry ReadLogEntry(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new LogEntry
            {
                Id = (int)Convert.ToDouble(Get(value, "id"), System.Globalization.CultureInfo.InvariantCulture),
                Month = (int)Convert.ToDouble(Get(value, "month"), System.Globalization.CultureInfo.InvariantCulture),
                Message = (string)Get(value, "message"),
            };
        }

        public static Dictionary<string, object> ToJson(Telemetry value)
        {
            if (value == null) return null;
            return new Dictionary<string, object>
            {
                ["actions"] = WriteCounts(value.Actions),
                ["sacrificed"] = value.Sacrificed,
                ["raidLosses"] = value.RaidLosses,
                ["influenceEarned"] = value.InfluenceEarned,
                ["influenceSpent"] = value.InfluenceSpent,
                ["dreadEarned"] = value.DreadEarned,
                ["dreadSpent"] = value.DreadSpent,
                ["upkeepPaid"] = value.UpkeepPaid,
                ["threatsCountered"] = value.ThreatsCountered,
                ["revelations"] = WriteCounts(value.Revelations),
            };
        }

        static Telemetry ReadTelemetry(Dictionary<string, object> value)
        {
            if (value == null) return null;
            return new Telemetry
            {
                Actions = ReadCounts(Get(value, "actions")),
                Sacrificed = (double)Convert.ToDouble(Get(value, "sacrificed"), System.Globalization.CultureInfo.InvariantCulture),
                RaidLosses = (double)Convert.ToDouble(Get(value, "raidLosses"), System.Globalization.CultureInfo.InvariantCulture),
                InfluenceEarned = (double)Convert.ToDouble(Get(value, "influenceEarned"), System.Globalization.CultureInfo.InvariantCulture),
                InfluenceSpent = (double)Convert.ToDouble(Get(value, "influenceSpent"), System.Globalization.CultureInfo.InvariantCulture),
                DreadEarned = (double)Convert.ToDouble(Get(value, "dreadEarned"), System.Globalization.CultureInfo.InvariantCulture),
                DreadSpent = (double)Convert.ToDouble(Get(value, "dreadSpent"), System.Globalization.CultureInfo.InvariantCulture),
                UpkeepPaid = (double)Convert.ToDouble(Get(value, "upkeepPaid"), System.Globalization.CultureInfo.InvariantCulture),
                ThreatsCountered = (int)Convert.ToDouble(Get(value, "threatsCountered"), System.Globalization.CultureInfo.InvariantCulture),
                Revelations = ReadCounts(Get(value, "revelations")),
            };
        }
        static RegionMap ReadRegions(object value)
        {
            var result = new RegionMap();
            var records = (Dictionary<string, object>)value;
            foreach (string id in GameData.RegionIds) result[id] = ReadRegionState((Dictionary<string, object>)records[id]);
            return result;
        }
    }
}
