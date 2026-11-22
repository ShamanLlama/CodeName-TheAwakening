# 07 — Balance values

## Policy

**[`unity/Assets/Awakening/Core/Rules/Balance.cs`](../../unity/Assets/Awakening/Core/Rules/Balance.cs)
is the single source of truth** (since game 0.5.0; before that `godot/core/balance.gd`, and
before `alignment-2` `src/data/balance.ts`). Regions, actions, talents and difficulties
live in [`GameData.cs`](../../unity/Assets/Awakening/Core/Rules/GameData.cs). Nothing in this
folder overrides them. The snapshots below are dated and will drift; when they disagree
with the code, the code is right and this file is stale.

**Checked 2026-11-21 against game 0.7.0 (Unity, `alignment-5`):** the latest snapshotted value of
every constant matches `Balance.cs` and `GameData.cs` (older values, such as `concealAwareness`
1 in the 2026-09-14 snapshot, are superseded by a later snapshot), and
`dotnet run --project core-tests/Tools -- measure --seeds=48` gives the 95/144 bot wins
of the `alignment-5` snapshot (early 29, late 38, terror 28). Add `--difficulty=easy` or
`--difficulty=hard` for the other modes (143/144 and 17/144).

Balance changed three times in a single working session during the tuning pass that
produced the current build. Duplicating numbers across a dozen prose documents is how the
previous GDD became unusable, so the other documents in this folder describe what constants
*mean* and leave their values here.

**Related constants must move together.** Several values are coupled, and changing one
alone silently changes difficulty:

| If you change… | Also change… | Because |
| --- | --- | --- |
| `preparationMonths` | Director cadences, `threatWarningMonths`, difficulty/talent warning bonuses, per-month drift rates | Otherwise threats-per-run and drift-per-run scale with the calendar and difficulty moves with it |
| `siteSupportShare` | Test fixtures that set follower counts | Fixtures assume a support threshold |
| `raidLossShare` / `threatLossShare` | `raidLossFloor` / `threatLossFloor` | A high flat floor makes small regions disproportionately fragile |
| `awarenessPerCapita` | `concealAwareness`, `ConcealWorldMonths`, `BuryAwareness` and the burial step | Together they set the maximum sustainable cult size |
| `TalentBaseCost` / `TalentCostStep` | The seal months of each difficulty | Together they decide when a policy can afford its second node; that timing, not the total price, decided whether public worship works |
| `FinaleSitePressure` | `FinaleWarningMonths`, `RestoreMonthCost` | The site hunt is what restoring and the Hidden hand answer |

## Snapshot — 2026-11-21 (`alignment-5`, Unity 0.7.0)

Fuente de verdad: `Balance.cs` y `GameData.cs`. Solo se listan los valores nuevos o cambiados desde `alignment-4`.

| Constante | Valor | Controla |
| --- | --- | --- |
| `Ruleset` | `alignment-5` | Los saves de `alignment-4` se retiran con aviso; los ajustes se conservan |
| `TalentBaseCost` / `TalentCostStep` | 40 / 30 | Precio del primer talento y lo que sube con cada uno: 40, 70, 100, 130, 160 (190 el sexto, en Easy) |
| `SwellingFlockRecruitment` | 0,35 | Raíz de Congregation (antes la doctrina) |
| `OpenWorshipRecruitment` | 2,5 | Multiplicador de reclutamiento público con Open worship (`PublicRecruitment` sigue en 2) |
| `MartyrDreadPerFollower` | 1/3 | Dread por seguidor perdido ante una amenaza con Martyrs, redondeado |
| `FaithAmongFaithsShare` | 0,5 | Parte de la awareness por tamaño del culto que queda con A faith among faiths |
| `QuietFriendsWarning` | 2 | Meses de aviso extra de la raíz de Hidden hand (antes la doctrina) |
| `SleeperScatterMonths` | 5 | Meses dispersa con Sleeper cells (`ScatterMonths` sigue en 8) |
| `BoughtPressAwareness` | 8 | Awareness que baja enterrar con Bought press; el paso de precio se reduce a la mitad, redondeado |
| `RedAltarsSacrificeDread` / `RedAltarsManifestDread` | 35 / 15 | Raíz de Harbinger (antes la doctrina); silenciar sigue en `SilenceSeverityDreadTerror` 13 |
| `MuteWitnessesSeverityAwareness` | 2 | Awareness por severidad al silenciar con Mute witnesses (3 sin el nodo) |
| `ScapegoatSeverityAwareness` | 1 | Awareness que baja por severidad cada amenaza que cae, con Scapegoats |
| `OfferingAwareness` | 1 | Awareness que suma un sacrificio que termina una amenaza (Offerings to the deep) |
| `BuryAwareness` / `BuryBaseCost` / `BuryExposure` / `BuryCooldown` | 5 / 50 / 8 / 6 | Enterrar el registro: awareness, precio inicial, exposición local y cooldown regional |
| `RestoreMonthCost` / `RestoreExposure` | 30 / 10 | Restaurar el sitio: Influence por mes recuperado y exposición |
| `OccultPreparationLoss` | 6 | Meses que quita una amenaza oculta ignorada en Normal y Hard (Easy: 4, en `DifficultyDefinition`) |
| `MysticismPivot` | 50 | Mysticism neutro: manifestar suma `ManifestAwareness × (100 − (m − 50))/100` y convierte `× (100 + (m − 50))/100` |
| `FinaleSitePressure` | 0,3 | Antes 0,15. Presión extra sobre sitios desde el mes 200 |

Los campos nuevos del estado son `Talents` (ids tomados, en orden; reemplaza a
`Specialization`), `BuryUses` y `Site.Recoverable`. Las dificultades suman
`TalentSlotMonths`, `OccultPreparationLoss`, `BuryStep`, `ConcealWorldMonths` y `Rule`
(véase [Difficulty](#difficulty)).

Medición con bot (Normal, 48 seeds por estrategia, una política por rama): 80/144 → 95/144
(early 5 → 29, late 45 → 38, terror 30 → 28). Easy 111 → 143 y Hard 27 → 17. Detalle y
descartes en [Experiencia y loops](02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-5--2026-11-21).

## Snapshot — 2026-11-21 (`alignment-4`, Unity 0.6.0)

Fuente de verdad: `Balance.cs`. Solo se listan los valores nuevos o cambiados desde `alignment-3`.

| Constante | Valor | Controla |
| --- | --- | --- |
| `Ruleset` | `alignment-4` | Ruleset que registran los saves y los eventos de playtest; los saves de `alignment-3` se retiran |
| `ConcealWorldMonths` | 6 | Meses que descansa el −3 de awareness mundial de `conceal` después de usarse, en todas las regiones. Mientras descansa, ocultar cuesta las 20 de Influence base y solo limpia la exposición local. El cooldown regional sigue en 12 meses |

Los campos nuevos del estado son `WorldConcealReadyAt` (mes desde el que el efecto mundial
vuelve a estar listo) y `ResultCode` (`awakened`, `world_awareness`, `deadline` o
`conceded`). Cerrar la partida no tiene constante propia: se habilita cuando
`Forecast.Margin` dice que el rito ya no cabe.

Acoplamiento: `ConcealWorldMonths` con `SilenceSeverityAwareness` (decide si el precio de
silenciar todavía se puede borrar) y con `AwarenessDrag` (el lastre es lo que ahora hunde
al culto público).

Medición con bot (Normal, 48 seeds por estrategia; la política oculta por awareness solo
cuando el efecto mundial está listo): 103/144 → 80/144 frente a `alignment-3` (early 21 → 5,
late 48 → 45, hub 34 → 30). Con la política anterior da 79/144. La regla de respaldo era bajar
a 4 meses si las victorias caían por debajo de 80; con 4 meses serían 86/144 (8, 46, 32) y
con 3 meses, 91/144 (11, 47, 33). Detalle en
[Experiencia y loops](02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-4--2026-11-21).

## Snapshot — 2026-11-12 (`alignment-3`, Godot 0.4.0; unchanged in Unity 0.5.x)

Source of truth at the time: `godot/core/balance.gd`; now `Balance.cs` with the same names in
PascalCase (`SCATTER_MONTHS` → `ScatterMonths`). Only the values added or changed since
`alignment-2` are listed. Reproduce the bot measurement with
`dotnet run --project core-tests/Tools -- measure --seeds=48` (in Godot it was
`godot --headless --path godot -s res://dev/measure.gd -- --seeds=48`).

| Constant | Value | Controls |
| --- | --- | --- |
| `RULESET` | `alignment-3` | Ruleset recorded in saves and playtest events |
| `SCATTER_MONTHS` | 8 | Months a scattered cell stays silent (no recruiting, income, site progress or new threats) |
| `SCATTER_EXPOSURE` | 10.0 | Exposure removed by scattering, down to the evidence floor |
| `SILENCE_SEVERITY_DREAD` / `_TERROR` | 20 / 13 | Dread per threat severity to silence it (Harbinger pays a third less) |
| `SILENCE_SEVERITY_AWARENESS` | 3.0 | World awareness per severity when silencing |
| `SILENCE_EXPOSURE` | 6.0 | Regional exposure added by silencing |
| `REVEAL_AWARENESS` | 5.0 | Awareness the first time a region goes public; later reveals are free |
| `MANIFEST_AWARENESS` / `SITE_AWARENESS` / `FIGURE_AWARENESS` | 8.0 each | Awareness shown as part of the price of manifest, prepare and reveal figure |
| `THREAT_ROLL_MONTHS` | 4 | Months between threat rolls (3 was measured: bot wins 103 → 59 of 144) |

Coupling: `SILENCE_SEVERITY_AWARENESS` with `CONCEAL_AWARENESS` — conceal removes 3
awareness per use, so a player with spare Influence can erase the price of silencing.
The designer's first human run (2026-11-13, Normal, Harbinger) won with 12 silences, 46
conceals and final awareness 11. `alignment-4` adds a world cooldown on conceal's global effect.

Bot measurement (Normal, 48 seeds per strategy): 95/144 → 103/144 wins against
`alignment-2` (early 17 → 21, late 43 → 48, hub 35 → 34).

## Snapshot — 2026-11-02 (`alignment-2`, Godot)

Source of truth: `godot/core/balance.gd`. Only the values that changed or were added since
`alignment-1` are listed; everything else matches the snapshot below. The calendar is 200
preparation months in the Godot build.

| Constant | Value | Controls |
| --- | --- | --- |
| `RULESET` | `alignment-2` | Ruleset recorded in saves and playtest events |
| `SITE_UPKEEP` | 1.0 | Influence per funded site per month; unpaid sites lie dormant |
| `CONCEAL_AWARENESS` | 3.0 (was 1) | Awareness removed per conceal |
| `CONCEAL_AWARENESS_PRICE` | 0.5 | Extra Influence per point of awareness on top of the base 20 |
| `FINALE_SITE_PRESSURE` | 0.15 | Added threat pressure on site regions from month 200 |
| `FINALE_WARNING_MONTHS` | 24 | Months of warning before the site hunt |

Coupling: `SITE_UPKEEP` with `INFLUENCE_PER_FOLLOWER` (a minimal site's own followers
roughly pay its upkeep, so dormancy only bites an empty treasury), and
`CONCEAL_AWARENESS` / `CONCEAL_AWARENESS_PRICE` with `AWARENESS_PER_CAPITA`.

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

`GameData.Difficulties`. Three multipliers and, since `alignment-5`, four rules that change
decisions. `DifficultyDefinition.Rule` states the rules in one line for the start screen.

| | Recruitment | Threat pressure | Warning months | Talent seals (months) | Occult loss | Burial step | Conceal world rest |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Easy | ×1.2 | ×0.75 | +2 | 6 (24, 44, 64, 104, 144, 176) | 4 months | +15 | 6 months |
| Normal | ×1.0 | ×1.0 | +0 | 5 (24, 64, 104, 144, 176) | 6 months | +25 | 6 months |
| Hard | ×0.85 | ×1.25 | −1 | 4 (24, 72, 120, 168) | 6 months | +40 | 9 months |

## Talents (replace the doctrines since `alignment-5`)

`GameData.Branches` and `GameData.Talents`. Three branches of a root, two mutually exclusive
middles and a capstone that needs the root and either middle. A seal opens at each month of
the difficulty's `TalentSlotMonths`; each talent costs 30 Influence more than the last, from
40. Permanent; no respecialization. The table of the twelve nodes is in
[Experiencia y loops](02_Game_Design/Experiencia_y_loops.md#cambios-de-alignment-5--2026-11-21).
The roots keep the old doctrines:

| Root (old doctrine) | Effect |
| --- | --- |
| Swelling flock (Congregation) | +35% automatic recruitment |
| Quiet friends (Hidden Hand) | Threats arrive 2 months later; obstruction costs ⅔ |
| Red altars (Harbinger) | Sacrifice yields 35 Dread (not 25); manifestation costs 15 Dread (not 20); silencing costs 13 Dread per severity (not 20) |

Talent balance is **not settled with players** — see [09 — Open problems](09-open-problems.md).
