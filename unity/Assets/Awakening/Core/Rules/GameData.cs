using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

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

    public sealed class BranchDefinition
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
    }

    /// <summary>A node of the in-run talent tree (alignment-5). It replaces the doctrine.</summary>
    public sealed class TalentDefinition
    {
        public string Id { get; internal set; }
        public string Branch { get; internal set; }
        /// <summary>1 root, 2 middle (the two middles of a branch exclude each other), 3 capstone.</summary>
        public int Tier { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        /// <summary>The sibling middle node that taking this one closes for the run; null for roots and capstones.</summary>
        public string Excludes { get; internal set; }
        /// <summary>The first id is always required; when more follow, at least one of them is (the capstone needs the root and either middle).</summary>
        public string[] Requires { get; internal set; }
    }

    public sealed class DifficultyDefinition
    {
        public string Name { get; internal set; }
        public double Recruitment { get; internal set; }
        public double Pressure { get; internal set; }
        public int Warning { get; internal set; }
        /// <summary>Months at which the talent seals open, ascending.</summary>
        public int[] TalentSlotMonths { get; internal set; }
        public int TalentSlots => TalentSlotMonths.Length;
        /// <summary>Preparation months an ignored occult threat takes from a funded site.</summary>
        public int OccultPreparationLoss { get; internal set; }
        /// <summary>Influence each earlier burial adds to the price of Bury the record.</summary>
        public int BuryStep { get; internal set; }
        /// <summary>Months concealment's world effect rests after it is used.</summary>
        public int ConcealWorldMonths { get; internal set; }
        /// <summary>One line for the start screen: the rules this difficulty changes.</summary>
        public string Rule { get; internal set; }
    }

    public static class GameData
    {
        public static readonly string[] RegionIds = {"north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"};
        public static readonly string[] ActionIds = {"recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "scatter", "silence", "exploit", "convert", "reveal_figure", "bury", "restore"};
        public static readonly string[] Statuses = {"placing", "playing", "alignment", "awakening", "won", "lost"};
        public static readonly string[] DifficultyIds = {"easy", "normal", "hard"};
        public static readonly string[] BranchIds = {"congregation", "hidden_hand", "harbinger"};
        /// <summary>Ordered by branch, then tier; the two middles of a branch are listed in display order.</summary>
        public static readonly string[] TalentIds = {
            "swelling_flock", "open_worship", "martyrs", "faith_among_faiths",
            "quiet_friends", "sleeper_cells", "contingency", "bought_press",
            "red_altars", "mute_witnesses", "scapegoats", "offerings"};
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

        static string F(string text, params object[] args) => string.Format(CultureInfo.InvariantCulture, text, args);
        static DifficultyDefinition Difficulty(string name, double recruitment, double pressure, int warning, int[] seals, int occultLoss, int buryStep, int concealMonths) => new DifficultyDefinition
        {
            Name = name, Recruitment = recruitment, Pressure = pressure, Warning = warning, TalentSlotMonths = seals,
            OccultPreparationLoss = occultLoss, BuryStep = buryStep, ConcealWorldMonths = concealMonths,
            Rule = F("{0} talent seals. An ignored occult society costs a site {1} months. Each burial costs {2} Influence more. Concealment's world effect rests {3} months.",
                seals.Length, occultLoss, buryStep, concealMonths)
        };
        public static readonly IReadOnlyDictionary<string, DifficultyDefinition> Difficulties =
            new ReadOnlyDictionary<string, DifficultyDefinition>(new Dictionary<string, DifficultyDefinition>
            {
                ["easy"] = Difficulty("Easy", 1.2, 0.75, 2, new[] { 24, 44, 64, 104, 144, 176 }, 4, 15, Balance.ConcealWorldMonths),
                ["normal"] = Difficulty("Normal", 1.0, 1.0, 0, new[] { 24, 64, 104, 144, 176 }, Balance.OccultPreparationLoss, 25, Balance.ConcealWorldMonths),
                ["hard"] = Difficulty("Hard", 0.85, 1.25, -1, new[] { 24, 72, 120, 168 }, Balance.OccultPreparationLoss, 40, 9),
            });

        public static readonly IReadOnlyDictionary<string, ActionDefinition> Actions =
            new ReadOnlyDictionary<string, ActionDefinition>(new Dictionary<string, ActionDefinition>
            {
                ["recruit"] = new ActionDefinition { Id = "recruit", Name = "Secret gathering", Description = "Recruit up to 35 unaware people or believers.", Influence = 18, Dread = 0, Followers = 0, Cooldown = 12 },
                ["expand"] = new ActionDefinition { Id = "expand", Name = "Establish a cell", Description = "Send the dream to an adjacent region. Rebuild anywhere if all cells are lost.", Influence = 30, Dread = 0, Followers = 0, Cooldown = 0 },
                ["sacrifice"] = new ActionDefinition { Id = "sacrifice", Name = "Sacrifice followers", Description = "Consume 40 followers for 25 Dread. Their future income is lost.", Influence = 0, Dread = 0, Followers = 40, Cooldown = 6 },
                ["reveal"] = new ActionDefinition { Id = "reveal", Name = "Begin public worship", Description = "Double ongoing recruitment. Invite scrutiny and backlash. The world notices the first time.", Influence = 10, Dread = 0, Followers = 0, Cooldown = 6 },
                ["hide"] = new ActionDefinition { Id = "hide", Name = "Return underground", Description = "End public recruitment. Existing evidence and rituals remain visible.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 6 },
                ["conceal"] = new ActionDefinition { Id = "conceal", Name = "Conceal the evidence", Description = F("Remove up to 25 Exposure. Once every {0} months ({1} on Hard) it also removes 3 world awareness, adding half the world awareness to the 20 Influence cost.", Balance.ConcealWorldMonths, Difficulties["hard"].ConcealWorldMonths), Influence = 20, Dread = 0, Followers = 0, Cooldown = 12 },
                ["manifest"] = new ActionDefinition { Id = "manifest", Name = "Impossible manifestation", Description = "Turn deniers and unaware people into believers. Belief also fuels resistance.", Influence = 0, Dread = 20, Followers = 0, Cooldown = 18 },
                ["prepare"] = new ActionDefinition { Id = "prepare", Name = "Prepare a ritual site", Description = "Commit Dread and 40 Influence, then pay 1 Influence/month and keep local support for 24 months. Each further site costs more.", Influence = 40, Dread = 60, Followers = 0, Cooldown = 0 },
                ["counter"] = new ActionDefinition { Id = "counter", Name = "Obstruct the investigation", Description = "Burn your local connections to neutralize this region's threat. They take 12 months to rebuild.", Influence = 30, Dread = 0, Followers = 0, Cooldown = 12 },
                ["scatter"] = new ActionDefinition { Id = "scatter", Name = "Scatter the flock", Description = "The cell goes to ground and the threat finds nobody. For 8 months it neither recruits nor earns, and cannot carry a ritual site.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 24 },
                ["silence"] = new ActionDefinition { Id = "silence", Name = "Silence the witnesses", Description = "Spend Dread to end this region's threat. The disappearances are noticed.", Influence = 0, Dread = 20, Followers = 0, Cooldown = 12 },
                ["exploit"] = new ActionDefinition { Id = "exploit", Name = "Exploit the omen", Description = "Claim a shared nightmare's Dread or accelerate a ritual during an eclipse.", Influence = 10, Dread = 0, Followers = 0, Cooldown = 0 },
                ["convert"] = new ActionDefinition { Id = "convert", Name = "Convert a prominent figure", Description = "A hidden celebrity earns Influence; a politician slows investigations.", Influence = 45, Dread = 0, Followers = 0, Cooldown = 0 },
                ["reveal_figure"] = new ActionDefinition { Id = "reveal_figure", Name = "Reveal their allegiance", Description = "Recruit up to 60 people and advance the local site by 8 months once. Ends the secret benefit.", Influence = 0, Dread = 0, Followers = 0, Cooldown = 0 },
                ["bury"] = new ActionDefinition { Id = "bury", Name = "Bury the record", Description = F("Editors, coroners and archivists lose the file. Remove {0} world awareness for {1} Influence, more for every earlier burial. The bribes leave traces here: +{2} Exposure.", Balance.BuryAwareness, Balance.BuryBaseCost, Js.Num(Balance.BuryExposure)), Influence = Balance.BuryBaseCost, Dread = 0, Followers = 0, Cooldown = Balance.BuryCooldown },
                ["restore"] = new ActionDefinition { Id = "restore", Name = "Restore the site", Description = F("Redraw what the occultists defaced. Recover half the preparation months they took, rounded up, at {0} Influence a month. +{1} Exposure.", Balance.RestoreMonthCost, Js.Num(Balance.RestoreExposure)), Influence = 0, Dread = 0, Followers = 0, Cooldown = 0 },
            });

        public static readonly IReadOnlyDictionary<string, BranchDefinition> Branches =
            new ReadOnlyDictionary<string, BranchDefinition>(new Dictionary<string, BranchDefinition>
            {
                ["congregation"] = new BranchDefinition { Id = "congregation", Name = "Congregation", Description = "Grow, and worship where the world can see." },
                ["hidden_hand"] = new BranchDefinition { Id = "hidden_hand", Name = "Hidden hand", Description = "Stay unseen, and bend the inquiry that hunts you." },
                ["harbinger"] = new BranchDefinition { Id = "harbinger", Name = "Harbinger", Description = "Terror, Dread and the knife." },
            });

        static string Pct(double share) => Js.Num(Math.Round(share * 100.0)) + "%";
        static TalentDefinition Talent(string id, string branch, int tier, string name, string description, string excludes = null, params string[] requires) =>
            new TalentDefinition { Id = id, Branch = branch, Tier = tier, Name = name, Description = description, Excludes = excludes, Requires = requires };
        public static readonly IReadOnlyDictionary<string, TalentDefinition> Talents =
            new ReadOnlyDictionary<string, TalentDefinition>(new Dictionary<string, TalentDefinition>
            {
                ["swelling_flock"] = Talent("swelling_flock", "congregation", 1, "Swelling flock",
                    "The whisper passes from hearth to hearth. Automatic recruitment +" + Pct(Balance.SwellingFlockRecruitment) + "."),
                ["open_worship"] = Talent("open_worship", "congregation", 2, "Open worship",
                    F("Let them sing in the streets. Public cells recruit ×{0} instead of ×{1}, world awareness no longer slows them, and they add nothing to world awareness month by month.",
                        Js.Num(Balance.OpenWorshipRecruitment), Js.Num(Balance.PublicRecruitment)), "martyrs", "swelling_flock"),
                ["martyrs"] = Talent("martyrs", "congregation", 2, "Martyrs",
                    F("Every believer the hunters take goes gladly into the dark. Followers lost to a threat yield 1 Dread for every {0}.", Js.Num(Math.Round(1.0 / Balance.MartyrDreadPerFollower))),
                    "open_worship", "swelling_flock"),
                ["faith_among_faiths"] = Talent("faith_among_faiths", "congregation", 3, "A faith among faiths",
                    "So many kneel that one more creed passes unremarked. The size of the cult adds " + Pct(Balance.FaithAmongFaithsShare) + " of its usual world awareness each month.",
                    null, "swelling_flock", "open_worship", "martyrs"),
                ["quiet_friends"] = Talent("quiet_friends", "hidden_hand", 1, "Quiet friends",
                    F("Clerks and constables owe you favours. Threats arrive {0} months later; obstructing them costs a third less.", Balance.QuietFriendsWarning)),
                ["sleeper_cells"] = Talent("sleeper_cells", "hidden_hand", 2, "Sleeper cells",
                    F("The flock knows how to vanish and how to return. A scattered cell gathers again after {0} months, not {1}.", Balance.SleeperScatterMonths, Balance.ScatterMonths),
                    "contingency", "quiet_friends"),
                ["contingency"] = Talent("contingency", "hidden_hand", 2, "Contingency",
                    "Copies of every rite are kept elsewhere. Restoring a site recovers every lost month, not half, and leaves no new trace.",
                    "sleeper_cells", "quiet_friends"),
                ["bought_press"] = Talent("bought_press", "hidden_hand", 3, "Bought press",
                    F("Editors print what they are paid to print. Burying the record removes {0} world awareness, not {1}, and its price rises half as fast.", Balance.BoughtPressAwareness, Balance.BuryAwareness),
                    null, "quiet_friends", "sleeper_cells", "contingency"),
                ["red_altars"] = Talent("red_altars", "harbinger", 1, "Red altars",
                    F("Fear is a harvest. Sacrifices yield {0} Dread; manifestations cost {1} Dread; silencing witnesses costs a third less.", Balance.RedAltarsSacrificeDread, Balance.RedAltarsManifestDread)),
                ["mute_witnesses"] = Talent("mute_witnesses", "harbinger", 2, "Mute witnesses",
                    F("Those who saw too much never speak again. Silencing adds {0} world awareness per severity, not {1}.", Js.Num(Balance.MuteWitnessesSeverityAwareness), Js.Num(Balance.SilenceSeverityAwareness)),
                    "scapegoats", "red_altars"),
                ["scapegoats"] = Talent("scapegoats", "harbinger", 2, "Scapegoats",
                    F("Let the hunters find someone else to blame. A threat that lands lowers world awareness by {0} per severity.", Js.Num(Balance.ScapegoatSeverityAwareness)),
                    "mute_witnesses", "red_altars"),
                ["offerings"] = Talent("offerings", "harbinger", 3, "Offerings to the deep",
                    F("Give the hunters what they came for. A sacrifice in a threatened region ends its threat, at +{0} world awareness.", Js.Num(Balance.OfferingAwareness)),
                    null, "red_altars", "mute_witnesses", "scapegoats"),
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
    }
}
