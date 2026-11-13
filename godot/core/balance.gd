class_name Balance
## Tuning values. Calendar and population scale are game abstractions.
## alignment-1 mirrored src/data/balance.ts; alignment-2 diverged from the TypeScript prototype;
## alignment-3 gives every threat three answers.
## Any change here needs a new RULESET and an entry in docs/playtests/versiones.md.

const RULESET := "alignment-3"

const PREPARATION_MONTHS := 200
const ALIGNMENT_MONTHS := 24
const FINAL_MONTHS := 12
const SECONDS_PER_MONTH := 2.0
const POPULATION_SCALE := 100
const INFLUENCE_CAP := 4000.0
const DREAD_CAP := 400.0
const STARTING_FOLLOWERS := 100
const STARTING_INFLUENCE := 35.0
const STARTING_DREAD := 20.0
const INFLUENCE_PER_FOLLOWER := 0.014
const RECRUITMENT_BASE := 0.6
const RECRUITMENT_RATE := 0.0075
const PUBLIC_RECRUITMENT := 2.0
const RECRUIT_AMOUNT := 35
const EXPAND_AMOUNT := 35
const SACRIFICE_FOLLOWERS := 40
const SACRIFICE_DREAD := 25
const MANIFESTATION_DENIERS := 65
const MANIFESTATION_UNAWARE := 50
const SITE_MONTHS := 24
const SITE_FOLLOWERS := 60
const REQUIRED_SITES := 3
const FINAL_DREAD := 100
# A ritual site is supported by a share of its region's whole population, so where you
# build it matters and sacrificing followers can starve it.
const SITE_SUPPORT_SHARE := 0.25
const SITE_DREAD_COSTS := [60, 100, 160]
# Threats take a share of the local cell, so a large cell is not automatically safe.
# The flat minimum only bites small cells, so it stays low.
const RAID_LOSS_SHARE := 0.12
const THREAT_LOSS_SHARE := 0.08
const RAID_LOSS_FLOOR := 8
const THREAT_LOSS_FLOOR := 6
# Obstructing a threat is a scarce favour, not a purchase: one region at a time.
const COUNTER_SEVERITY_COST := 30
# A threat has three answers, each paid in a different currency. Obstructing costs Influence now.
# Scattering costs time: the cell hides, and for these months it neither recruits nor earns, and
# cannot carry a ritual site. Nobody hunts a cell that is not there.
const SCATTER_MONTHS := 8
const SCATTER_EXPOSURE := 10.0
# Silencing the witnesses costs Dread, and the bodies are noticed.
const SILENCE_SEVERITY_DREAD := 20
const SILENCE_SEVERITY_DREAD_TERROR := 13
const SILENCE_SEVERITY_AWARENESS := 3.0
const SILENCE_EXPOSURE := 6.0
# What the world learns from a conspicuous act. Shown as part of each action's price.
const REVEAL_AWARENESS := 5.0
const MANIFEST_AWARENESS := 8.0
const SITE_AWARENESS := 8.0
const FIGURE_AWARENESS := 8.0
const AWARENESS_DRAG := 150.0
# Concealment is the Influence sink against world awareness. Its price rises with awareness:
# cheap evidence control early, an expensive cover-up once the world is watching.
const CONCEAL_AWARENESS := 3.0
const CONCEAL_AWARENESS_PRICE := 0.5
# Funded sites cost Influence every month. An unpaid site lies dormant: no preparation, and it
# cannot carry the final rite that month.
const SITE_UPKEEP := 1.0
# Once the stars come right the world hunts ritual sites. Players are warned in advance.
const FINALE_SITE_PRESSURE := 0.15
const FINALE_WARNING_MONTHS := 24
# A cult this large cannot stay secret: awareness climbs with the share of the world
# that follows you, so hoarding followers is itself a risk.
const AWARENESS_PER_CAPITA := 1.5
const FIGURE_SITE_PROGRESS := 8
const THREAT_WARNING_MONTHS := 5
# How often the world looks for each cell. Every 3 months was measured for alignment-3 and cut
# the scripted policy from 103 to 59 wins in 144 runs, so the cadence stays.
const THREAT_ROLL_MONTHS := 4

const DEADLINE := PREPARATION_MONTHS + ALIGNMENT_MONTHS
const LATEST_SITE_START := DEADLINE - FINAL_MONTHS - SITE_MONTHS
const LATEST_RITE_START := DEADLINE - FINAL_MONTHS
