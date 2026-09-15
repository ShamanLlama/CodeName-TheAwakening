# 08 — Design rationale

Why the systems look the way they do. Almost every rule here is a response to a measured
problem in a recorded playtest. Read this before changing balance — several of these
decisions look arbitrary in isolation and are not.

## The evidence base

Three human runs of the pre-change build, plus AI harness sweeps across seeds and doctrines.
Raw reports live in [`docs/playtests/`](../playtests/).

| Run | Result | The telling number |
| --- | --- | --- |
| A (`ff53a4ca`, Congregation) | Lost on the deadline month | 3/3 sites ready, rite 0/12 — lost to the schedule, never to the world |
| B (`72ee92cc`, Hidden Hand) | Won | **`raidLosses: 0`** — 103 counters, not one threat landed |
| C (`f5c82100`, Harbinger) | Won on the **earliest legal month** | `raidLosses: 0`, zero risk taken |

All three finished at the Influence cap. All three ended with roughly 30× more followers
than the win condition required. Across all three, the three opportunity verbs were used
**zero** times.

## Problem 1 — the win condition was a calendar, not a goal

The critical path is preparation plus the rite — around 36 months. The rite could not start
before month 600. **Over 90% of a run was strategically empty**, and players correctly
responded by idling and then doing the real game at the end.

**Fix:** the preparation calendar was compressed to a third of its length, with every
per-month rate scaled to match so the *shape* of the run was preserved. The critical path
was deliberately left unchanged, so it now occupies a much larger share of the run.

**The trap when changing this:** the calendar is not one number. Director cadences,
warning windows, drift rates, and difficulty/doctrine warning bonuses are all
calendar-denominated. Scaling only `preparationMonths` would have cut threats-per-run by
the same factor and quietly made the game far easier. Cooldowns were *deliberately* left
unscaled — measurement showed players used 26 of ~416 available recruit slots, so cooldowns
were never the binding constraint. They still are not.

## Problem 2 — obstruction was a universal solvent

The single most important finding. `counter` cost a flat amount, had **no cooldown**, and
always succeeded.

In run C, income was ~134 Influence/month against a counter price of 30 — **4.5 counters
affordable per month against a maximum arrival rate of 2**. Defensive capacity permanently
exceeded the world's offensive capacity by more than double. Counter was 40% of all actions
in run B and 35% in run C, and it removed the entire opposition system in exchange for
clicking.

**Fix, in two parts:**

1. **Cost scales with severity**, so the threat you let grow is the expensive one. This
   connects exposure and awareness to the price of defence.
2. **A per-region cooldown.** This is the load-bearing change. You cannot answer
   consecutive threats in the same region, so you must triage — and triage is what makes
   exposure management, site placement, and the public/hidden decision matter at all.

Making threats hurt more would not have worked on its own; the player would simply have
countered them. Scarcity had to come first.

**Cooldown length was tuned, not guessed.** At 24 months the finale became unwinnable —
harness runs lost at 10/12 and 11/12 rite months with no legal recovery. At 12 months the
finale is defensible with planning and the turtle strategy still dies. That is the window.

## Problem 3 — every resource was post-scarcity

Followers were ~30× oversupplied against a flat 60-per-site requirement. Influence pinned
at its cap in all three runs, so most income was silently discarded. Dread was a slow drip
gated only by clicking. Nothing you protected was scarce, so `conceal`, `reveal`, and
`hide` were housekeeping.

**Fixes:** site support scaled to regional population (below), escalating Dread per site,
proportional threat losses, and income reduced and throttled by awareness. Influence now
ends a run in the low hundreds instead of pinned at the ceiling.

## Problem 4 — turtling was strictly dominant

Run C never went public once. Five of its eight regions ended at **exposure 0** — the world
never looked at them. Staying hidden everywhere had no cost, and the reward for going public
(double recruitment) bought more of the resource already in 30× surplus.

**Fix: awareness rises with the share of humanity that follows you.** This is the keystone
of the current design. It cannot be dodged by being careful, because it is a function of
being *large*. It:

- punishes follower hoarding directly, which was the root of every other surplus;
- makes sacrifice a defensive move as well as an economic one;
- gives `conceal` a real job as the only brake;
- and sets a **maximum sustainable cult size**, where per-capita awareness gain outpaces
  the concealment you can physically apply given eight regions and a cooldown.

An earlier attempt — awareness rising only from *conspicuous acts* — was measured and
failed: the turtle reached only 24–28 awareness and the clock never ticked. Visibility-based
awareness cannot punish an invisible strategy. It had to be size-based.

### Measured result

The same turtle strategy, before and after, across nine seeds and three doctrines:

| Strategy | Before | After |
| --- | --- | --- |
| Turtle (hoard, counter everything, three sites) | Won at the earliest legal month | **Loses 9/9**, awareness 100 around months 195–211 |
| Disciplined, small cult, cheap site regions | — | Wins reliably at ~10–12% cult share |
| Disciplined, sites in populous regions | — | Wins roughly 1 in 3 |

The turtle now usually dies **with three sites ready** — it reaches the finish line and is
too large to hide. That gradient between the three rows is the game.

## Problem 5 — a flat site requirement made the map meaningless

Every region was an equally good place for a ritual, so there was no spatial decision.

**Fix:** support is a share of the region's population, which is why the population
conservation invariant in [03](03-population.md) is load-bearing. Populous regions became
good at recruiting and bad at hosting; small regions the reverse.

### A bug this created, and the lesson

The first implementation kept the old flat loss as a **floor** under the new proportional
loss. That made small regions lose a far larger *share* of their support requirement per hit
than large ones — Oceania lost 43% of its requirement where Latin America lost 16%. The
cheap site became a trap, exactly inverting the intended trade-off. Harness runs failed with
sites stuck below support in small regions.

**Lesson, recorded because it is easy to reintroduce:** when a cost becomes proportional,
any flat floor must be small enough that it only affects genuinely tiny cases. Otherwise it
silently re-punishes the case the proportionality was meant to protect.

## Problem 6 — the game sold provably useless purchases

Run A funded a third site on the final month of the alignment. The engine knew the site
could not finish, computed that fact, put it in the *advisory* text, and took the payment
anyway. The run then lost with 3/3 sites and 0/12 rite months.

**Fix:** the check became a refusal, not a warning — while still permitting the legitimate
late-acceleration play by subtracting acceleration the region could actually still receive.

## Consequences accepted deliberately

**The finale is now dangerous.** The rite window contains several director rolls, occult
hits strip preparation, and the counter cooldown means you cannot answer everything. A rite
that needs twelve months routinely takes twenty. Starting it and walking away loses. This
was not the goal of the calendar compression — it emerged from holding the critical path
fixed while the calendar shrank — but it is kept because it gives the endgame stakes it
never had.

**Response timing became genuinely ambiguous.** With a counter cooldown, obstructing a
threat the moment it appears can leave the region defenceless when the next one arrives.
The endgame probe previously asserted that early response always wins; it now asserts only
that responding beats ignoring, because the ranking is no longer true and the ambiguity is
a feature.
