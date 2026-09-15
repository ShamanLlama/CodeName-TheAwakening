# 06 — Ritual sites and the endgame

Three prepared, supported ritual sites are the only way to win. Everything else in the game
exists to get you three sites and keep them alive.

## Support: a share of the region

**A site is supported when local followers meet a share of that region's total population**
(`siteRequirement` / `supported`, `src/game/derived.ts:11`):

```
requirement = max(floor, round(capacity(r) × BALANCE.siteSupportShare))
```

Because `capacity` is the region's whole population ([03](03-population.md)), the
requirement scales with how big the region is. With the current share, requirements run
from roughly 75 followers in Oceania to roughly 450 in South Asia.

**This is the central strategic decision in the game.** It inverts the naive read of the
map:

| | Populous region | Small region |
| --- | --- | --- |
| Recruiting | Large pool, compounds well | Runs dry quickly |
| Hosting a ritual | Expensive — needs many followers held permanently | Cheap |
| After a raid | Loses more followers in absolute terms | Recovers slowly |

The intended play is therefore **farm where people are, build where they are not.** Africa
is the strongest recruiting ground and one of the worst places to put a site. Both AI
harnesses encode this: Africa is the starting cell and the sacrifice farm, and the sites go
in small regions (`src/tests/playthrough.ts`).

Support is checked **continuously**, not once. A site that drops below its requirement stops
progressing and stops counting toward the rite until followers are restored.

## Funding

`prepare` costs flat Influence plus **escalating Dread**: each site funded raises the price
of the next (`siteDreadCost`, `src/game/engine.ts:44`). The third site is a serious
commitment, which is what forces sustained sacrifice — and therefore sustained tension with
the support requirement it is paying for.

Funding also: sets a permanent high exposure floor, adds world awareness, and immediately
schedules an occult threat.

**A site cannot be funded if it provably cannot finish.** `previewAction`
(`src/game/engine.ts:62`) refuses when the remaining months cannot cover preparation plus
the final rite. The check subtracts acceleration the region could still legitimately
receive — a present eclipse, or an unrevealed converted figure — so the late-acceleration
strategy stays open while a hopeless purchase is blocked outright. Before this guard, the
game would happily sell a site that was mathematically dead on arrival; one recorded human
run lost that way.

## Preparation

A funded site gains one preparation month per month **while supported**, up to
`BALANCE.siteMonths`. Progress is not lost when support lapses — it pauses.

Preparation can be accelerated by exploiting an eclipse in that region, or by unmasking a
converted figure there.

Occult threats **remove** preparation months from a funded site. A site that had reached
full preparation can be knocked back below it and must recover, which is the main way a
finale goes wrong.

## The final rite

1. **Reach the alignment.** The rite cannot be started before it
   (`awakeningReason`, `src/game/engine.ts:161`).
2. **Hold three ready sites** — funded, fully prepared, currently supported (`readySites`,
   `src/game/derived.ts:17`).
3. **Pay the reserve.** Starting the rite spends `BALANCE.finalDread`.
4. **Hold for the duration.** Each month with three supported sites advances the rite by one.

Progress is **cumulative**. Losing a site pauses the count and does not reset it, so the
alignment window is wider than the rite is long — that margin is the recovery budget, and
it is directly measured by a synthetic control test.

### The finale is defended, not idle

**You cannot start the rite and walk away.** The alignment window contains several director
rolls, occult threats strip preparation, and the counter cooldown means you cannot answer
all of them. An unattended alignment window loses.

This is verified both ways: the browser test plays the finale month by month, countering
what the chronicle offers, and wins; the same state advanced without input does not
(`e2e/game.spec.ts`). A finale that took twelve months on paper routinely takes twenty in
practice.

## Forecasting

`ritualForecast` (`src/game/forecast.ts`) is the player's planning aid. It reports how many
more sites are needed, how many preparation months remain, whether the plan still fits in
the calendar, and whether any funded site has lost support. `latestSiteStart` and
`latestRiteStart` are the derived no-later-than dates.
