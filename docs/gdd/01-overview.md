# 01 — Overview

## Premise

You are the hidden will behind a global Cthulhu cult. Humanity is the resource, secrecy
is the constraint, and the stars come right exactly once. Build congregations across eight
world regions, convert their belief into Dread, raise three ritual sites, and complete the
final rite inside the alignment window — before the world works out what you are.

## Design pillars

1. **Growth is a liability, not just a reward.** A cult large enough to matter is a cult
   large enough to be noticed. Every follower you hold pushes world awareness up.
2. **You cannot defend everywhere.** Obstructing a threat burns a region's connections for
   a year. Triage is the core tactical decision.
3. **Placement is strategy.** A ritual site must be carried by a share of its region's
   population, so populous regions recruit well and host rituals badly.
4. **Every clock is visible.** The alignment date, awareness, site preparation, and threat
   deadlines are all on screen. Losing should feel like a decision you made, not a dice roll.

## The shape of a run

| Phase | `GameStatus` | What is happening |
| --- | --- | --- |
| Placing | `placing` | Choose a starting region. No time passes. |
| Preparation | `playing` | The long game: expand, recruit, manage visibility, fund and prepare three ritual sites. |
| Alignment | `alignment` | The stars come right and time pauses. The final rite becomes available. |
| Final rite | `awakening` | Hold three supported sites for the required months while the world attacks them. |
| Resolved | `won` / `lost` | The run is over and immutable. |

Time advances in months. The preparation phase runs to `BALANCE.preparationMonths`, then
the alignment window opens for `BALANCE.alignmentMonths` more. The hard deadline is
`deadline` (`src/game/derived.ts:18`), the sum of the two. At 1× speed a month is
`BALANCE.secondsPerMonth`, putting a full run in the region of seven minutes of
unpaused play.

**The rite can only be started during the alignment window** (`awakeningReason`,
`src/game/engine.ts:161`). You cannot finish early; you can very much finish late, which
is to say not at all.

## The monthly loop

Each simulated month, in this order (`advanceSimulation`, `src/game/engine.ts:173`):

1. Advance the calendar.
2. Resolve the world: expire opportunities, land any threat that has come due.
3. Collect Influence income, throttled by awareness.
4. Per region: automatic recruitment, exposure drift, madness decay, ritual site progress.
5. Raise awareness by the share of humanity that follows you.
6. Advance the final rite if three sites are supported.
7. Check victory, then the awareness loss, then the calendar deadline.
8. Run the director: roll for new threats and opportunities.

Player actions are **free of time cost** — they are gated by Influence, Dread, followers,
and per-region cooldowns, not by the clock. Acting while paused is legitimate and expected.

## Victory

All of the following (`awakeningReason` and `advanceSimulation`):

- Three ritual sites that are funded, fully prepared, and currently supported.
- `BALANCE.finalDread` Dread in reserve, spent to begin the rite.
- The rite begun during the alignment window.
- Three sites held supported for `BALANCE.finalMonths` cumulative months before the deadline.

Rite progress is **cumulative, not consecutive**. Losing support pauses the count; it does
not reset it. This is what makes a damaged finale recoverable.

## Defeat

Two ways to lose:

- **The world knows.** Awareness reaches 100. Checked every month; ends the run immediately.
- **The alignment passes.** The deadline arrives with the rite unfinished.

The first is the pressure against playing greedily, the second against playing slowly.
Most runs are a negotiation between them. See [04 — Visibility and
awareness](04-visibility.md).

## Determinism

A run is fully determined by its seed and the player's commands. The RNG is a seeded
integer stream carried in `GameState.rngState` (`src/game/random.ts`) and advanced only
by the director. Previewing an action never consumes randomness, and every command
returns a new state rather than mutating the old one (`performAction`,
`src/game/engine.ts:99`). This is what makes the playtest harnesses in `src/tests/`
reproducible.
