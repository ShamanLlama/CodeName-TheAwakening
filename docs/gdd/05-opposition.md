# 05 — Opposition

The world is not an AI opponent. It is a scheduled pressure system in
`src/game/director.ts` that reacts to what you have made visible.

## The director

Runs at the end of every month (`tickDirector`). Two independent cadences:

- **Threat rolls** — on a fixed month interval, every region that has followers and no
  pending threat rolls once against its pressure value.
- **Opportunity spawns** — on a longer interval, one eligible region receives a single
  opportunity.

Both cadences are calendar constants and were scaled with the calendar, so the number of
rolls per run is stable regardless of run length. If you change `preparationMonths`, change
these too or you silently change difficulty. See [08 — Rationale](08-rationale.md).

## Threat pressure

Per region, per roll (`tickDirector`, `src/game/director.ts`):

```
pressure = (base
          + exposure/500
          + 0.10 if public
          + 0.12 if a ritual site is funded
          + awareness/1500
          + believerShare × 0.15
          + madness/2000)
          × difficulty.pressure
          × (0.6 + institutionalResistance/150)
```

Reading that formula as design intent:

- **A funded ritual site is the single largest self-inflicted term.** Sites attract attention
  permanently, which is why site regions need defending rather than hiding.
- **Believers raise pressure.** Manifesting to break the denier wall makes that region hotter.
- **Institutional resistance is regional character.** East Asia and Europe react hard; Africa
  and Oceania react softly. It multiplies everything, so a conspicuous cult in a strong
  state is far more dangerous than the same cult in a weak one.
- **Awareness feeds back.** Global attention makes every region more reactive.

## Threat kinds

Chosen by what the region visibly is (`scheduleThreat`, `src/game/director.ts:14`):

| Kind | When | Extra consequence |
| --- | --- | --- |
| `investigation` | Underground cell | — |
| `raid` | Public worship | Higher proportional losses |
| `occult` | A funded ritual site | **Strips preparation months from the site** |

Funding a site schedules an occult threat immediately — the act of building draws a
response, it is not left to the dice.

## Warning and severity

A threat is scheduled with a **due month**, giving you a window to respond. The window is
extended by the Hidden Hand doctrine and by a secretly converted politician.

**Severity** (1–3) is set at scheduling time from `exposure + awareness`. It scales the
losses *and* the price of obstructing the threat. This is the mechanism that turns
carelessness into compounding cost: a region you let grow conspicuous produces threats that
are both worse and more expensive to stop.

## Consequences

Resolved by `threatConsequences` (`src/game/threats.ts`) and applied in `resolveWorld`.
The same function feeds the on-screen forecast, so **the projection the player reads is
exactly the calculation that will run**.

Follower losses are **proportional to the cell**, with a small flat minimum:

```
lost = min(followers, max(severity × flatFloor, followers × severity × share))
```

Proportional losses mean a large cell is not automatically safe — the thing that made the
old build trivial. The flat floor is deliberately small so that it only bites tiny cells;
if it is raised, small regions are punished twice for being small and cheap ritual sites
become a trap. That was a real bug — see [08 — Rationale](08-rationale.md).

Other effects: exposure rises, occult threats strip preparation months from a funded site,
and if losses take a region to zero followers the cell and its site are destroyed entirely.

## Obstruction is scarce

`counter` removes a pending threat outright. It always succeeds. Its scarcity is the entire
tactical layer:

- **Cost scales with severity** — the threat you allowed to grow is the expensive one.
- **It has a long per-region cooldown.** Obstructing in a region burns your connections
  there; you cannot answer consecutive threats in the same place.

The cooldown is the load-bearing part. With it, you must decide *which* region eats a hit,
and the answer depends on which sites you can afford to have interrupted. Without it,
`counter` is an always-affordable tax that deletes the opposition system — which is exactly
what the pre-change build measured.

A consequence worth knowing: **spending a counter early is no longer strictly better.**
Countering a threat the month it appears leaves the region undefended if another arrives
before the cooldown expires. The endgame probe records this and deliberately does not rank
early versus late response (`src/tests/endgame-finale.playtest.test.ts`).

## Opportunities

A region can hold one opportunity at a time, expiring after a window.

| Opportunity | Used by | Effect |
| --- | --- | --- |
| Shared nightmare | `exploit` | A block of Dread, once |
| Lightless eclipse | `exploit` | Advances an unfinished ritual site by several months |
| Celebrated artist | `convert` → `reveal_figure` | Extra regional income while secret |
| Studious minister | `convert` → `reveal_figure` | Longer threat warnings while secret |

Converting a figure keeps them secret and useful. `reveal_figure` cashes them in for a
block of recruits and site progress, at the cost of heavy exposure, awareness, and a long
backlash — and the secret benefit ends.

**These are currently dead content.** Across every recorded playtest the three
opportunity verbs have been used zero times and every spawned opportunity expired unused.
See [09 — Open problems](09-open-problems.md).
