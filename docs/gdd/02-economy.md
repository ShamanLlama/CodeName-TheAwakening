# 02 — Economy

Three quantities matter: **Influence**, **Dread**, and **followers**. Only the first two
are spendable currencies. Followers are both a resource and a liability, which is the
point — see [04 — Visibility and awareness](04-visibility.md).

## Influence

The operational currency: reach, favours, bribes, silence. Global, not per-region.

**Income** is the sum of every region's contribution, multiplied by the awareness drag
(`influencePerMonth`, `src/game/derived.ts:23`):

```
income = Σ regionIncome(r) × awarenessDrag(state)
regionIncome(r) = followers × influencePerFollower  + 0.6 if a secret celebrity is converted here
```

A region with no followers contributes nothing. The awareness multiplier is the important
term: **as the world notices you, your income falls**, so a large exposed cult earns less
per follower than a small quiet one.

Income is clamped by `BALANCE.influenceCap`. That cap is deliberately set far above normal
play; it exists as a sanity bound for save validation (`src/game/persistence.ts:24`), not
as a balancing device. If a run is sitting at the cap, that is a balance signal worth
investigating, not intended play.

**Influence buys:** recruitment drives, new cells, concealment, obstructing threats,
funding ritual sites, converting prominent figures, and the one-off doctrine choice.

## Dread

The ritual currency. It is not earned by waiting — **there is no passive Dread income**,
and a test enforces that (`src/game/engine.test.ts`, "does not generate Dread by waiting").

**Sources:**

- **Sacrifice** — consumes `BALANCE.sacrificeFollowers` local followers for
  `BALANCE.sacrificeDread` Dread (more under the Harbinger doctrine). Six-month regional
  cooldown. Adds exposure.
- **Shared nightmare** — an opportunity that can be exploited once for a flat amount.

Dread is clamped by `BALANCE.dreadCap`. Unlike the Influence cap this one is reachable and
is a real constraint: you cannot stockpile indefinitely ahead of the escalating site costs.

**Dread buys:** ritual sites (at an escalating price) and the final rite.

### Sacrifice is the game's central trade

Sacrifice converts followers into Dread, and it does three things at once:

1. Gains Dread you cannot get any other way in bulk.
2. **Permanently** destroys followers — they move to the `losses` bucket and never return
   ([03 — Population](03-population.md)).
3. Lowers your income *and* lowers world awareness, because the cult is now smaller.

That third effect means sacrifice is not purely a cost. When awareness is the thing
killing you, burning followers is a defensive move as well as an economic one. The tension
is that ritual sites need local followers to stay supported, so sacrificing at a site
region can starve the very ritual you are funding.

## Followers

Held per region, as part of the regional population model. They:

- generate Influence,
- support ritual sites (a share of the region's population — [06](06-ritual-sites.md)),
- are what threats destroy,
- are the fuel for Dread,
- and drive world awareness upward every month.

There is no global follower pool. 500 followers spread across five regions is not
interchangeable with 500 in one region, because site support is regional and threats are
regional.

## Action costs

Costs live in `src/data/abilities.ts`. Most are flat. Two are computed, and both exist to
make a previously free decision expensive:

| Action | Cost | Notes |
| --- | --- | --- |
| `counter` | `counterSeverityCost × severity`, ×⅔ under Hidden Hand | `counterCost`, `src/game/engine.ts:39`. Plus a regional cooldown — see [05](05-opposition.md). |
| `prepare` | Flat Influence + escalating Dread by sites already funded | `siteDreadCost`, `src/game/engine.ts:44`. |

Every other action is a flat price with a per-region cooldown. Cooldowns are stored as the
month the action becomes available again (`RegionState.cooldowns`), so they survive saves
without extra bookkeeping.

## Previewing

`previewAction` (`src/game/engine.ts:49`) is the single authority on whether an action is
legal, what it costs, and what it will do. The UI, the tests, and the AI playtest harnesses
all go through it, so a thing the UI offers is a thing the engine will accept. It returns a
refusal `reason` string that is shown directly to the player.

`performAction` re-runs the preview and refuses if it fails, so there is no path that
charges a player for an illegal action.
