class_name GameData
## Static game definitions. Mirrors src/data/{regions,abilities,difficulties,events}.ts and src/game/types.ts.

const REGION_IDS: Array[String] = ["north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"]
const ACTION_IDS: Array[String] = ["recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "exploit", "convert", "reveal_figure"]
const STATUSES: Array[String] = ["placing", "playing", "alignment", "awakening", "won", "lost"]
const DIFFICULTY_IDS: Array[String] = ["easy", "normal", "hard"]
const SPECIALIZATION_IDS: Array[String] = ["recruitment", "infiltration", "terror"]
const THREAT_KINDS: Array[String] = ["investigation", "raid", "occult"]
const OPPORTUNITY_KINDS: Array[String] = ["nightmare", "eclipse", "celebrity", "politician"]

const REGIONS := {
	"north_america": { "id": "north_america", "name": "North America", "populationWeight": 14, "skepticism": 65, "institutionalResistance": 75, "mysticism": 35, "adjacent": ["latin_america", "europe", "east_asia"], "tags": ["coastal", "media"] },
	"latin_america": { "id": "latin_america", "name": "Latin America", "populationWeight": 12, "skepticism": 45, "institutionalResistance": 45, "mysticism": 65, "adjacent": ["north_america", "africa", "oceania"], "tags": ["coastal", "mystical"] },
	"europe": { "id": "europe", "name": "Europe", "populationWeight": 13, "skepticism": 70, "institutionalResistance": 80, "mysticism": 30, "adjacent": ["north_america", "africa", "middle_east"], "tags": ["coastal", "institutional"] },
	"africa": { "id": "africa", "name": "Africa", "populationWeight": 15, "skepticism": 40, "institutionalResistance": 35, "mysticism": 70, "adjacent": ["latin_america", "europe", "middle_east", "south_asia"], "tags": ["coastal", "mystical", "populous"] },
	"middle_east": { "id": "middle_east", "name": "West Asia", "populationWeight": 10, "skepticism": 55, "institutionalResistance": 70, "mysticism": 60, "adjacent": ["europe", "africa", "south_asia", "east_asia"], "tags": ["religious", "institutional"] },
	"south_asia": { "id": "south_asia", "name": "South Asia", "populationWeight": 18, "skepticism": 50, "institutionalResistance": 50, "mysticism": 65, "adjacent": ["africa", "middle_east", "east_asia", "oceania"], "tags": ["coastal", "mystical", "populous"] },
	"east_asia": { "id": "east_asia", "name": "East Asia", "populationWeight": 15, "skepticism": 75, "institutionalResistance": 85, "mysticism": 35, "adjacent": ["north_america", "middle_east", "south_asia", "oceania"], "tags": ["coastal", "institutional", "populous"] },
	"oceania": { "id": "oceania", "name": "Oceania", "populationWeight": 3, "skepticism": 50, "institutionalResistance": 45, "mysticism": 55, "adjacent": ["latin_america", "south_asia", "east_asia"], "tags": ["coastal", "rlyeh"] },
}
const POPULATION_WEIGHT_TOTAL := 100
const WORLD_POPULATION := POPULATION_WEIGHT_TOTAL * Balance.POPULATION_SCALE

const ACTIONS := {
	"recruit": { "name": "Secret gathering", "description": "Recruit up to 35 unaware people or believers.", "influence": 18, "dread": 0, "followers": 0, "cooldown": 12 },
	"expand": { "name": "Establish a cell", "description": "Send the dream to an adjacent region. Rebuild anywhere if all cells are lost.", "influence": 30, "dread": 0, "followers": 0, "cooldown": 0 },
	"sacrifice": { "name": "Sacrifice followers", "description": "Consume 40 followers for 25 Dread. Their future income is lost.", "influence": 0, "dread": 0, "followers": Balance.SACRIFICE_FOLLOWERS, "cooldown": 6 },
	"reveal": { "name": "Begin public worship", "description": "Double ongoing recruitment. Invite scrutiny and backlash.", "influence": 10, "dread": 0, "followers": 0, "cooldown": 6 },
	"hide": { "name": "Return underground", "description": "End public recruitment. Existing evidence and rituals remain visible.", "influence": 0, "dread": 0, "followers": 0, "cooldown": 6 },
	"conceal": { "name": "Conceal the evidence", "description": "Remove up to 25 Exposure and 1 world awareness. Public worship and rituals retain an evidence floor.", "influence": 20, "dread": 0, "followers": 0, "cooldown": 12 },
	"manifest": { "name": "Impossible manifestation", "description": "Turn deniers and unaware people into believers. Belief also fuels resistance.", "influence": 0, "dread": 20, "followers": 0, "cooldown": 18 },
	"prepare": { "name": "Prepare a ritual site", "description": "Commit Dread and 40 Influence, then maintain local support for 24 months. Each further site costs more.", "influence": 40, "dread": 60, "followers": 0, "cooldown": 0 },
	"counter": { "name": "Obstruct the investigation", "description": "Burn your local connections to neutralize this region's threat. They take 12 months to rebuild.", "influence": 30, "dread": 0, "followers": 0, "cooldown": 12 },
	"exploit": { "name": "Exploit the omen", "description": "Claim a shared nightmare's Dread or accelerate a ritual during an eclipse.", "influence": 10, "dread": 0, "followers": 0, "cooldown": 0 },
	"convert": { "name": "Convert a prominent figure", "description": "A hidden celebrity earns Influence; a politician slows investigations.", "influence": 45, "dread": 0, "followers": 0, "cooldown": 0 },
	"reveal_figure": { "name": "Reveal their allegiance", "description": "Recruit up to 60 people and advance the local site by 8 months once. Ends the secret benefit.", "influence": 0, "dread": 0, "followers": 0, "cooldown": 0 },
}

const SPECIALIZATIONS := {
	"recruitment": { "name": "Congregation", "description": "+35% automatic recruitment. Build a broad following." },
	"infiltration": { "name": "Hidden hand", "description": "Threats arrive 2 months later; obstructing them costs a third less." },
	"terror": { "name": "Harbinger", "description": "Sacrifices yield 35 Dread; manifestations cost 15 Dread." },
}
const SPECIALIZATION_COST := 40

const DIFFICULTIES := {
	"easy": { "name": "Easy", "recruitment": 1.2, "pressure": 0.75, "warning": 2 },
	"normal": { "name": "Normal", "recruitment": 1.0, "pressure": 1.0, "warning": 0 },
	"hard": { "name": "Hard", "recruitment": 0.85, "pressure": 1.25, "warning": -1 },
}

const THREAT_NAMES := { "investigation": "Investigators follow the dream", "raid": "Authorities prepare a raid", "occult": "An occult society targets the rite" }
const OPPORTUNITY_NAMES := { "nightmare": "A shared nightmare", "eclipse": "A lightless eclipse", "celebrity": "A celebrated artist seeks the truth", "politician": "A minister studies forbidden texts" }
