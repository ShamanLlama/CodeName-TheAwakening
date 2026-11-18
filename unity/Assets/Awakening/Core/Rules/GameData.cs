using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Awakening.Core.Rules
{
    public sealed class RegionDefinition
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public int PopulationWeight { get; internal set; }
        public int Skepticism { get; internal set; }
        public int InstitutionalResistance { get; internal set; }
        public int Mysticism { get; internal set; }
        public string[] Adjacent { get; internal set; }
        public string[] Tags { get; internal set; }
    }

    public sealed class ActionDefinition
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public int Influence { get; internal set; }
        public int Dread { get; internal set; }
        public int Followers { get; internal set; }
        public int Cooldown { get; internal set; }
    }

    public sealed class SpecializationDefinition
    {
        public string Name { get; internal set; }
        public string Description { get; internal set; }
    }

    public sealed class DifficultyDefinition
    {
        public string Name { get; internal set; }
        public double Recruitment { get; internal set; }
        public double Pressure { get; internal set; }
        public int Warning { get; internal set; }
    }

    public static class GameData
    {
        public static readonly string[] RegionIds = {"north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"};
        public static readonly string[] ActionIds = {"recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "scatter", "silence", "exploit", "convert", "reveal_figure"};
        public static readonly string[] Statuses = {"placing", "playing", "alignment", "awakening", "won", "lost"};
        public static readonly string[] DifficultyIds = {"easy", "normal", "hard"};
        public static readonly string[] SpecializationIds = {"recruitment", "infiltration", "terror"};
        public static readonly string[] ThreatKinds = {"investigation", "raid", "occult"};
        public static readonly string[] OpportunityKinds = {"nightmare", "eclipse", "celebrity", "politician"};
        public static readonly IReadOnlyDictionary<string, RegionDefinition> Regions =
            new ReadOnlyDictionary<string, RegionDefinition>(new Dictionary<string, RegionDefinition>
            {
                ["north_america"] = new RegionDefinition { Id = "north_america", Name = "North America", PopulationWeight = 14, Skepticism = 65, InstitutionalResistance = 75, Mysticism = 35, Adjacent = new[] { "latin_america", "europe", "east_asia" }, Tags = new[] { "coastal", "media" } },
                ["latin_america"] = new RegionDefinition { Id = "latin_america", Name = "Latin America", PopulationWeight = 12, Skepticism = 45, InstitutionalResistance = 45, Mysticism = 65, Adjacent = new[] { "north_america", "africa", "oceania" }, Tags = new[] { "coastal", "mystical" } },
                ["europe"] = new RegionDefinition { Id = "europe", Name = "Europe", PopulationWeight = 13, Skepticism = 70, InstitutionalResistance = 80, Mysticism = 30, Adjacent = new[] { "north_america", "africa", "middle_east" }, Tags = new[] { "coastal", "institutional" } },
                ["africa"] = new RegionDefinition { Id = "africa", Name = "Africa", PopulationWeight = 15, Skepticism = 40, InstitutionalResistance = 35, Mysticism = 70, Adjacent = new[] { "latin_america", "europe", "middle_east", "south_asia" }, Tags = new[] { "coastal", "mystical", "populous" } },
                ["middle_east"] = new RegionDefinition { Id = "middle_east", Name = "West Asia", PopulationWeight = 10, Skepticism = 55, InstitutionalResistance = 70, Mysticism = 60, Adjacent = new[] { "europe", "africa", "south_asia", "east_asia" }, Tags = new[] { "religious", "institutional" } },
                ["south_asia"] = new RegionDefinition { Id = "south_asia", Name = "South Asia", PopulationWeight = 18, Skepticism = 50, InstitutionalResistance = 50, Mysticism = 65, Adjacent = new[] { "africa", "middle_east", "east_asia", "oceania" }, Tags = new[] { "coastal", "mystical", "populous" } },
                ["east_asia"] = new RegionDefinition { Id = "east_asia", Name = "East Asia", PopulationWeight = 15, Skepticism = 75, InstitutionalResistance = 85, Mysticism = 35, Adjacent = new[] { "north_america", "middle_east", "south_asia", "oceania" }, Tags = new[] { "coastal", "institutional", "populous" } },
                ["oceania"] = new RegionDefinition { Id = "oceania", Name = "Oceania", PopulationWeight = 3, Skepticism = 50, InstitutionalResistance = 45, Mysticism = 55, Adjacent = new[] { "latin_america", "south_asia", "east_asia" }, Tags = new[] { "coastal", "rlyeh" } },
            });

        public static readonly IReadOnlyDictionary<string, ActionDefinition> Actions =
            new ReadOnlyDictionary<string, ActionDefinition>(new Dictionary<string, ActionDefinition>
            {
                ["recruit"] = new ActionDefinition { Id = "recruit", Name = "Secret gathering", Description = "Recruit up to 35 unaware people or believers.", Influence = 18, Dread = 0, Followers = 0, Cooldown = 12 },
                ["expand"] = new ActionDefinition { Id = "expand", Name = "Establish a cell", Description = "Send the dream to an adjacent region. Rebuild anywhere if all cells are lost.", Influence = 30, Dread = 0, Followers = 0, Cooldown = 0 },
                ["sacrifice"] = new ActionDefinition { Id = "sacrifice", Name = "Sacrifice followers", Description = "Consume 40 followers for 25 Dread. Their future income is lost.", Influence = 0, Dread = 0, Followers = 40, Cooldown = 6 },
                ["reveal"] = new ActionDefinition { Id = "reveal", Name = "Begin public worship", Description = "Double ongoing recruitment. Invite scrutiny and backlash. The world notices the first time.", Influence = 10, Dread = 0, Followers = 0, Cooldown = 6 },
                ["hide"] = new ActionDefinition { Id = "hide", Name = "Return underground", Description = "End public recruitment. Existing evidence and rituals remain visible.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 6 },
                ["conceal"] = new ActionDefinition { Id = "conceal", Name = "Conceal the evidence", Description = "Remove up to 25 Exposure and 3 world awareness. Costs 20 Influence plus half the world awareness.", Influence = 20, Dread = 0, Followers = 0, Cooldown = 12 },
                ["manifest"] = new ActionDefinition { Id = "manifest", Name = "Impossible manifestation", Description = "Turn deniers and unaware people into believers. Belief also fuels resistance.", Influence = 0, Dread = 20, Followers = 0, Cooldown = 18 },
                ["prepare"] = new ActionDefinition { Id = "prepare", Name = "Prepare a ritual site", Description = "Commit Dread and 40 Influence, then pay 1 Influence/month and keep local support for 24 months. Each further site costs more.", Influence = 40, Dread = 60, Followers = 0, Cooldown = 0 },
                ["counter"] = new ActionDefinition { Id = "counter", Name = "Obstruct the investigation", Description = "Burn your local connections to neutralize this region's threat. They take 12 months to rebuild.", Influence = 30, Dread = 0, Followers = 0, Cooldown = 12 },
                ["scatter"] = new ActionDefinition { Id = "scatter", Name = "Scatter the flock", Description = "The cell goes to ground and the threat finds nobody. For 8 months it neither recruits nor earns, and cannot carry a ritual site.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 24 },
                ["silence"] = new ActionDefinition { Id = "silence", Name = "Silence the witnesses", Description = "Spend Dread to end this region's threat. The disappearances are noticed.", Influence = 0, Dread = 20, Followers = 0, Cooldown = 12 },
                ["exploit"] = new ActionDefinition { Id = "exploit", Name = "Exploit the omen", Description = "Claim a shared nightmare's Dread or accelerate a ritual during an eclipse.", Influence = 10, Dread = 0, Followers = 0, Cooldown = 0 },
                ["convert"] = new ActionDefinition { Id = "convert", Name = "Convert a prominent figure", Description = "A hidden celebrity earns Influence; a politician slows investigations.", Influence = 45, Dread = 0, Followers = 0, Cooldown = 0 },
                ["reveal_figure"] = new ActionDefinition { Id = "reveal_figure", Name = "Reveal their allegiance", Description = "Recruit up to 60 people and advance the local site by 8 months once. Ends the secret benefit.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 0 },
            });

        public static readonly IReadOnlyDictionary<string, SpecializationDefinition> Specializations =
            new ReadOnlyDictionary<string, SpecializationDefinition>(new Dictionary<string, SpecializationDefinition>
            {
                ["recruitment"] = new SpecializationDefinition { Name = "Congregation", Description = "+35% automatic recruitment. Build a broad following." },
                ["infiltration"] = new SpecializationDefinition { Name = "Hidden hand", Description = "Threats arrive 2 months later; obstructing them costs a third less." },
                ["terror"] = new SpecializationDefinition { Name = "Harbinger", Description = "Sacrifices yield 35 Dread; manifestations cost 15 Dread; silencing witnesses costs a third less." },
            });

        public static readonly IReadOnlyDictionary<string, DifficultyDefinition> Difficulties =
            new ReadOnlyDictionary<string, DifficultyDefinition>(new Dictionary<string, DifficultyDefinition>
            {
                ["easy"] = new DifficultyDefinition { Name = "Easy", Recruitment = 1.2, Pressure = 0.75, Warning = 2 },
                ["normal"] = new DifficultyDefinition { Name = "Normal", Recruitment = 1.0, Pressure = 1.0, Warning = 0 },
                ["hard"] = new DifficultyDefinition { Name = "Hard", Recruitment = 0.85, Pressure = 1.25, Warning = -1 },
            });

        public static readonly IReadOnlyDictionary<string, string> ThreatNames =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                ["investigation"] = "Investigators follow the dream",
                ["raid"] = "Authorities prepare a raid",
                ["occult"] = "An occult society targets the rite",
            });

        public static readonly IReadOnlyDictionary<string, string> OpportunityNames =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                ["nightmare"] = "A shared nightmare",
                ["eclipse"] = "A lightless eclipse",
                ["celebrity"] = "A celebrated artist seeks the truth",
                ["politician"] = "A minister studies forbidden texts",
            });

        public const int PopulationWeightTotal = 100;
        public const int WorldPopulation = PopulationWeightTotal * Balance.PopulationScale;
        public const int SpecializationCost = 40;
    }
}
