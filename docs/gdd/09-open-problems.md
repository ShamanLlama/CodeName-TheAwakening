# 09 — Open problems

Design debt that is **measured but not fixed**. Each entry states the evidence, so the next
session can act rather than rediscover. Ordered by how much of the game they affect.

## 1. A quarter of the verb set is dead content

`exploit`, `convert`, and `reveal_figure` have been used **zero times in every recorded
human playtest**. Every opportunity that spawned expired unused — roughly fifteen across
three runs, with log lines reading `A celebrated artist seeks the truth passes in Africa.`

This is not a cost problem. All three runs sat at the Influence cap and still ignored them.
The causes look structural:

- Opportunities spawn rarely, one region at a time, and expire.
- Nothing in the win condition requires them, so they read as optional flavour.
- The chronicle surfaces threats with deadlines and consequences; opportunities are a log line.
- The figure chain is two actions deep (`convert` then `reveal_figure`) before any payoff.

**Why it is now worth fixing:** obstruction became scarce. A secret politician extends
threat warnings and an eclipse rescues a site that is behind schedule — these are exactly
the tools a player needs when they cannot counter everything. The subsystem that was
pointless under infinite counters has an obvious job under scarce ones.

**Likely shape:** spawn more often, allow more than one concurrent, surface them in the
chronicle with the same urgency as threats. Adding a second live opportunity slot is a
one-line change in `tickDirector`.

## 2. Doctrine balance is not settled

Hidden Hand discounts obstruction, which was the dominant cost in the old build — in run B
that was over a thousand Influence saved plus total threat immunity. Congregation boosted
recruitment, the resource already in 30× surplus.

The keystone changes have narrowed the gap: follower surplus is now a liability, so
Congregation's growth is no longer free, and Harbinger's Dread yield matters more against
escalating site costs. But **this has not been measured properly**. The post-change harness
sweep showed only small differences between doctrines, which may mean they are balanced or
may mean the harness does not exploit them.

**Needed:** a doctrine-differentiated sweep where each bot plays to its doctrine's strength
rather than running identical policy under three labels.

## 3. Influence is still loosely constrained

Better than before — runs now end in the low hundreds instead of pinned at the cap — but
income still exceeds what a careful player spends. The sinks are obstruction, concealment,
rebuilding raided cells, and site funding; the first two are cooldown-limited, so beyond a
point extra Influence has nothing to buy.

**Options, in rough order of laziness:** raise counter/conceal costs; add an Influence sink
that competes with defence; or accept it, since the binding constraint is now awareness
rather than money. Worth re-measuring before acting — this may no longer be a real problem.

## 4. Stale player-facing strings drift from the rules

Found by writing this document. Several descriptions in `src/data/abilities.ts` and
`src/game/engine.ts` still described pre-change behaviour and have been corrected — the
Hidden Hand doctrine still quoted the old warning window and obstruction price, and the
awakening requirement still claimed a flat sixty followers per site. The underlying issue
is that **descriptions are hand-written prose containing numbers** while
the numbers live in `balance.ts`, so they drift silently on every rebalance and the game
lies to the player.

**Possible fix:** derive the numeric parts of descriptions from `BALANCE` the way
`previewAction` already derives its effect and risk text, so a rebalance updates the copy
automatically. Anything that states a number to the player is a drift risk.

## 5. The difficulty levels have not been re-validated

`DIFFICULTIES` multiplies recruitment, threat pressure, and warning months. Those
multipliers were tuned against a build where threats were harmless and obstruction was
free. With proportional losses, scarce obstruction, and a lethal awareness clock, **Hard may
now be unreasonable and Easy may not be forgiving in the way it intends** — nothing in the
difficulty table touches awareness, which is the main way runs now end.

**Needed:** a difficulty sweep with the current rules. The harness exists
(`src/tests/endgame-difficulty.playtest.test.ts`); it needs its assertions re-pointed at
win rates per difficulty.

## 6. Unverified by human play

Every claim about the current build's balance comes from AI harnesses, which play
consistently and never misread the UI. The specific things a human run would test:

- Is the awareness clock **legible** before it kills you? The HUD shows cult share and the
  drag percentage, but nobody has confirmed a player connects "my cult is large" to "I am
  about to lose".
- Is the defended finale readable, or does it feel like an ambush?
- Does the site-placement lesson (farm in populous regions, build in small ones) come
  across, or does it need teaching?

## 7. Deferred and never re-examined

Carried from the archived proposal and still unbuilt: rival cults, per-region unique events,
a persistent investigator actor that tracks your strongest region, and escalating world
events at awareness thresholds. These were scoped out deliberately, not forgotten. The
awareness clock now provides an obvious hook for threshold events if that direction is
picked up.
