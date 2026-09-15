# The Awakening of Cthulhu — Game Design Document

This folder is the **design source of truth** for the current build. It is written by
reverse-engineering the shipped code, so every rule here describes what the game
actually does, not what was once proposed.

## How to read it

| Document | Answers |
| --- | --- |
| [01 — Overview](01-overview.md) | What is this game, what does a run look like, how do you win or lose? |
| [02 — Economy](02-economy.md) | Influence, Dread, and what they buy |
| [03 — Population](03-population.md) | Belief, recruitment, and the conservation rule everything else relies on |
| [04 — Visibility and awareness](04-visibility.md) | Exposure, the awareness clock, secrecy versus growth |
| [05 — Opposition](05-opposition.md) | How the world notices you, threatens you, and what it costs to push back |
| [06 — Ritual sites and the endgame](06-ritual-sites.md) | Where rituals go, what they cost, and how the final rite resolves |
| [07 — Balance values](07-balance.md) | Where the numbers live and what each one controls |
| [08 — Design rationale](08-rationale.md) | Why the systems are shaped this way, with playtest evidence |
| [09 — Open problems](09-open-problems.md) | Known design debt that is measured but not yet fixed |

New to the project? Read 01, then 08. The rationale document explains the reasoning
behind most of the rules and will save you from re-deriving decisions that were already
settled by playtesting.

## Conventions

**Numbers are not authoritative here.** `src/data/balance.ts` is the single source of
truth for every tunable value. These documents explain what each constant *means* and
how systems interact; where a value appears in prose it is illustrative. [07 — Balance
values](07-balance.md) carries a dated snapshot and says plainly when it was taken.

**Code pointers use `path:line`.** They were accurate when written and are there to get
you to the right function, not to be exact forever. If a pointer misses, search for the
named export.

**"The world" means the simulation's opposition**, driven by `src/game/director.ts`. It is
not an AI opponent; it is a set of scheduled pressures.

## Status and history

This build is the `alignment-1` ruleset (`GameState.ruleset`). Two earlier designs are
kept for provenance and should not be used as design references:

- [`docs/archive/GDD_mvp_proposal.md`](../archive/GDD_mvp_proposal.md) — the pre-implementation
  proposal this folder supersedes. Useful for original intent and the fiction; its numbers,
  calendar, and several systems no longer match the build.
- [`docs/archive/GDD_legacy_prototype.md`](../archive/GDD_legacy_prototype.md) — the superseded
  percentage-influence and Suppression rules, replaced before `alignment-1`.

Related working documents that are *not* design authority:
[implementation plan](../implementation-plan.md), [implementation
checklist](../implementation-checklist.md), and [playtest reports](../playtests/).
