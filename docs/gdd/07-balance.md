# 07 — Balance values

## Policy

**`src/data/balance.ts` is the single source of truth.** Nothing in this folder overrides
it. The snapshot below is dated and will drift; when it disagrees with the code, the code
is right and this file is stale.

Balance changed three times in a single working session during the tuning pass that
produced the current build. Duplicating numbers across a dozen prose documents is how the
previous GDD became unusable, so the other documents in this folder describe what constants
*mean* and leave their values here.

**Related constants must move together.** Several values are coupled, and changing one
alone silently changes difficulty:

| If you change… | Also change… | Because |
| --- | --- | --- |
| `preparationMonths` | Director cadences, `threatWarningMonths`, difficulty/doctrine warning bonuses, per-month drift rates | Otherwise threats-per-run and drift-per-run scale with the calendar and difficulty moves with it |
| `siteSupportShare` | Test fixtures that set follower counts | Fixtures assume a support threshold |
| `raidLossShare` / `threatLossShare` | `raidLossFloor` / `threatLossFloor` | A high flat floor makes small regions disproportionately fragile |
| `awarenessPerCapita` | `concealAwareness` and the conceal cooldown | Together they set the maximum sustainable cult size |

## Snapshot — 2026-09-14

Taken from `src/data/balance.ts` at the `alignment-1` ruleset. Grouped by what they control.

### Calendar

| Constant | Value | Controls |
| --- | --- | --- |
| `preparationMonths` | 200 | Months before the alignment opens |
| `alignmentMonths` | 24 | The window in which the rite can run |
| `finalMonths` | 12 | Supported months the rite requires |
| `siteMonths` | 24 | Preparation months a site needs |
| `secondsPerMonth` | 2 | Real time per month at 1× |

Derived: the hard deadline is 224, the latest site start is 188, the latest rite start 212.
A full run is about seven minutes of unpaused play.

### Economy

| Constant | Value | Controls |
| --- | --- | --- |
| `influencePerFollower` | 0.014 | Income per follower per month |
| `influenceCap` | 4000 | Sanity bound, not a balance device |
| `dreadCap` | 400 | A real stockpiling constraint |
| `sacrificeFollowers` / `sacrificeDread` | 40 / 25 | The Dread trade |
| `startingFollowers` / `startingInfluence` / `startingDread` | 100 / 35 / 20 | Opening position |

### Population and recruitment

| Constant | Value | Controls |
| --- | --- | --- |
| `populationScale` | 100 | Multiplier on region weight → world holds 10,000 |
| `recruitmentBase` / `recruitmentRate` | 0.6 / 0.0075 | Flat and compounding growth per month |
| `publicRecruitment` | 2 | Public worship multiplier |
| `recruitAmount` / `expandAmount` | 35 / 35 | Instant action top-ups |
| `manifestationDeniers` / `manifestationUnaware` | 65 / 50 | Belief conversion per manifestation |

### Ritual sites

| Constant | Value | Controls |
| --- | --- | --- |
| `siteSupportShare` | 0.25 | Share of regional population that supports a site |
| `siteFollowers` | 60 | Floor under the support requirement |
| `siteDreadCosts` | 60, 100, 160 | Escalating Dread per site funded |
| `requiredSites` / `finalDread` | 3 / 100 | Victory requirements |
| `figureSiteProgress` | 8 | Months granted by unmasking a figure |

Derived support requirements, by region weight:

| Region | Weight | Followers to support a site |
| --- | --- | --- |
| Oceania | 3 | 75 |
| West Asia | 10 | 250 |
| Latin America | 12 | 300 |
| Europe | 13 | 325 |
| North America | 14 | 350 |
| Africa | 15 | 375 |
| East Asia | 15 | 375 |
| South Asia | 18 | 450 |

### Opposition

| Constant | Value | Controls |
| --- | --- | --- |
| `threatWarningMonths` | 5 | Base response window |
| `counterSeverityCost` | 30 | Influence per severity level to obstruct |
| `raidLossShare` / `threatLossShare` | 0.12 / 0.08 | Proportional losses per severity |
| `raidLossFloor` / `threatLossFloor` | 8 / 6 | Flat minimum per severity — keep low |

### Awareness

| Constant | Value | Controls |
| --- | --- | --- |
| `awarenessPerCapita` | 1.5 | Monthly awareness per unit of cult share — **the anti-turtle clock** |
| `awarenessDrag` | 150 | Divisor for the income/recruitment penalty |
| `concealAwareness` | 1 | Awareness removed per conceal — the only brake |

## Difficulty

`src/data/difficulties.ts`. Three multipliers, no special cases.

| | Recruitment | Threat pressure | Warning months |
| --- | --- | --- | --- |
| Easy | ×1.2 | ×0.75 | +2 |
| Normal | ×1.0 | ×1.0 | +0 |
| Hard | ×0.85 | ×1.25 | −1 |

## Doctrines

`src/data/abilities.ts`. One per run, bought once, permanent.

| Doctrine | Effect |
| --- | --- |
| Congregation | +35% automatic recruitment |
| Hidden Hand | Longer threat warnings; obstruction costs ⅔ |
| Harbinger | Larger Dread yield from sacrifice; cheaper manifestations |

Doctrine balance is **not settled** — see [09 — Open problems](09-open-problems.md).
