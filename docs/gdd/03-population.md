# 03 — Population

Every region holds five population buckets (`Population`, `src/game/types.ts`):

| Bucket | Meaning |
| --- | --- |
| `unaware` | Ordinary people who have never heard the dream. Recruitable. |
| `deniers` | People who have heard and refused. **Not recruitable** by ordinary means. |
| `believers` | People who accept the impossible but have not joined. Recruitable, and preferentially. |
| `followers` | Your cult. Generates income, supports rituals, is what you lose. |
| `losses` | The dead and the taken. A permanent sink. |

## The conservation invariant

**The five buckets always sum to the region's original capacity.** Nothing in the game
creates or destroys people; it only moves them between buckets.

```
capacity(r) = unaware + deniers + believers + followers + losses   // derived.ts:10
```

Capacity is `populationWeight × BALANCE.populationScale`, fixed at world creation. The
eight weights sum to 100, so the world holds `worldPopulation` people
(`src/game/derived.ts:14`).

This invariant is load-bearing, not decorative. **Ritual site support is derived from
`capacity`**, which is only meaningful because the sum is stable. A regression test asserts
conservation across sacrifice and destruction (`src/game/engine.test.ts`, "destroys a site
when its last followers are lost and does not recreate population").

> **If you write code or fixtures that touch population, transfer between buckets — never
> assign `followers` directly.** Assigning inflates `capacity`, which silently raises that
> region's site requirement. This bug was hit while writing test fixtures and is easy to
> reintroduce; see `setFollowers` in `src/tests/endgame-finale.playtest.test.ts` for the
> correct pattern.

## Starting composition

At world creation (`createGameState`, `src/game/engine.ts:14`), a region's deniers are
seeded from its `skepticism` and the rest begin unaware. A sceptical region therefore
starts with a larger unrecruitable block; a mystical one starts more open. Skepticism does
not change afterwards — it sets the initial wall, and only manifestations move it.

The starting foothold recruits `BALANCE.startingFollowers` into one chosen region and sets
a little exposure (`placeStartingFoothold`, `src/game/engine.ts:31`).

## Recruitment

The recruitable pool is `unaware + believers` (`recruitable`, `src/game/derived.ts:8`).
Deniers are excluded. **Believers are consumed first** (`recruit`,
`src/game/derived.ts:42`) — converting disbelief into belief and then harvesting it is a
real strategy, not an accident.

Automatic monthly recruitment per region (`recruitmentPerMonth`, `src/game/derived.ts:24`)
is a base rate plus a proportion of existing followers, multiplied by:

- **Public worship** — a flat multiplier, the main reason to accept exposure.
- **Doctrine** — Congregation multiplies it.
- **Backlash** — a penalty for a window after revealing, so going public is not free.
- **Madness** — a small bonus from recent manifestations.
- **Difficulty**.
- **Awareness drag** — the same global multiplier that throttles income.

The result is capped by the recruitable pool, so a region that has run out of open minds
stops growing no matter how large the cult is.

`recruit` (the action) is an instant top-up of a flat amount with a long regional cooldown.
It is the tool for repairing a raided cell; the automatic rate is what compounds.

## Manifestations

`manifest` spends Dread to move a block of **deniers and unaware people into believers**.
It is the only way through the denier wall.

The costs are deliberate: exposure, world awareness, and `madness`. Madness slightly raises
recruitment while it lasts and decays monthly. Crucially, **believers also raise the
region's threat pressure** ([05](05-opposition.md)) — a population that accepts the
impossible is a population that starts looking for you. Manifesting is buying growth with
attention.

## Losing followers

`loseFollowers` (`src/game/derived.ts:49`) moves followers into `losses`. If a region hits
zero followers the cell collapses: public worship ends and **any ritual site there is
destroyed outright**, funding included. Losing a region is not a setback to repair, it is a
site to rebuild from nothing.
