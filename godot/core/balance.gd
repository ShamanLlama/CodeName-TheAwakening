class_name Balance
## First-playtest values. Calendar and population scale are game abstractions.
## Mirrors src/data/balance.ts; parity is checked by tests/test_golden.gd.

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
const AWARENESS_DRAG := 150.0
const CONCEAL_AWARENESS := 1.0
# A cult this large cannot stay secret: awareness climbs with the share of the world
# that follows you, so hoarding followers is itself a risk.
const AWARENESS_PER_CAPITA := 1.5
const FIGURE_SITE_PROGRESS := 8
const THREAT_WARNING_MONTHS := 5

const DEADLINE := PREPARATION_MONTHS + ALIGNMENT_MONTHS
const LATEST_SITE_START := DEADLINE - FINAL_MONTHS - SITE_MONTHS
const LATEST_RITE_START := DEADLINE - FINAL_MONTHS
