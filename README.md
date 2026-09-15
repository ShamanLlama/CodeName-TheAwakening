# The Awakening of Cthulhu

A playable, single-player world-map strategy prototype. The design source of truth is the [GDD](docs/gdd/README.md). Build cults, spend Influence, sacrifice followers for Dread, and prepare Cthulhu's awakening before the stars pass out of alignment.

## Start playing

Requires Node.js 22 or newer.

~~~bash
npm install
npm run dev
~~~

Open the local URL printed by Vite. For a fixed address:

~~~bash
npm run dev -- --host 127.0.0.1 --port 4173 --strictPort
~~~

Then open http://127.0.0.1:4173 in your browser.

1. Choose Easy for a first test and optionally enter a seed.
2. Click **Enter the dream**, read the guide, and choose your first region.
3. Select a region, then use its actions in **Cult**, **Rituals**, or **Opportunities**.
4. Pause whenever you need to plan. Immediate actions work while paused.
5. Expand **Playtest controls** at the bottom to advance a month, year, or decade. These controls advance recruitment, threats, and deadlines; they grant no resources.
6. Use **Settings → Export playtest report** to download a run report for feedback.

The game saves locally after actions and monthly updates. Reloading resumes your run. Switching away from the browser tab pauses play. Audio is muted by default and can be enabled in Settings.

On desktop, the world chronicle stays at the bottom of the viewport. Its threat watch lists deadlines, projected follower and income losses, and ritual disruption, with direct counter actions. Click a threat marker or region name to inspect it. The forecast above the map explains how much preparation and final-rite time you still need.

## First-run objective

- Cult strength is your living followers. They recruit automatically and generate Influence.
- Influence funds recruitment, expansion, concealment, investigations, and prominent-person conversions.
- Sacrificing 40 local followers grants 25 Dread, reducing future income. It also shrinks the cult, which slows world awareness.
- Public worship doubles recruitment before other modifiers; backlash temporarily reduces that benefit.
- Deniers resist ordinary recruitment. Manifestations create believers, who can also strengthen opposition.
- Each ritual site must be supported by a quarter of its region's population, so a site in Oceania needs 75 followers and one in South Asia needs 450. Populous regions recruit well but host rituals badly.
- Sites cost 40 Influence and 60, 100, then 160 Dread, and take 24 supported months to prepare.
- World awareness rises with the share of humanity that follows you. It throttles income and recruitment as it climbs, and at 100% the world hunts every cell and the run ends. A large cult is a liability.
- Obstructing a threat costs 30 Influence per severity level and burns that region's connections for 12 months, so you cannot defend everywhere. Concealing evidence is the only way to push awareness back down.
- Reserve 100 Dread for the final rite.
- After 200 months, the alignment opens and the game pauses. Start the final rite, then resume time and maintain three sites for 12 months within the 24-month window.
- Missing the deadline loses the run. Interrupted rites retain progress, but the calendar keeps advancing.

A campaign takes about 7 minutes at 1×, excluding pauses. These are initial playtest values, not final balance.

## What to test

Try an early public cult, a late public surge, and a public stronghold supported by hidden cells. Tell us where you started, your seed and difficulty, when the game felt slow or unclear, and which actions seemed too strong or too costly. The exported report records sacrifices, spending, revelations, threats, and the result.

Reports also include regional snapshots and identify how much of the event log is retained. They do not record real session duration; please note roughly how long you played. The first human run and resulting feedback changes are recorded in the [implementation status](docs/implementation-checklist.md#human-playtest--seed-ff53a4ca-2026-09-06).

Generic celebrities and politicians appear as opportunities. Secret celebrities earn Influence; secret politicians delay new threats. Public revelation grants recruits and local ritual progress once, but ends their secret benefit and triggers backlash. One permanent doctrine further changes recruitment, infiltration, or sacrifice rewards.

## Saves

New rules use the separate LocalStorage key `cthulhu-awakening-save-v2`. Earlier saves remain under `cthulhu-awakening-save`; compatible audio settings are copied, but old runs do not resume under new rules. Corrupt new-format saves are retained under a recovery key before starting fresh. If browser storage is blocked, play continues without saving.

## Code and validation

The interactive Earth map uses Natural Earth's public-domain country outlines. Geometry is bundled locally and grouped into the eight gameplay regions. [Map source and attribution](src/assets/NATURAL_EARTH.md) includes the pinned release and regeneration command.

- `src/data/balance.ts`: initial calendar, economy, and ritual values.
- `src/game/engine.ts`: pure commands and monthly simulation.
- `src/game/director.ts`: seeded regional threats and opportunities.
- `src/game/derived.ts`: population transfers, income, and readiness.
- `src/game/persistence.ts`: save validation, recovery, and playtest export.
- `src/App.tsx`: browser clock, map, actions, and feedback.
- `src/tests/playthrough.ts`: test-only player policies using legal commands.

~~~bash
npm run lint
npm run typecheck
npm test
npm run e2e
npm run build
~~~

The current tests include nine legal strategy playthroughs across three seeds, population conservation, sacrifices, event responses, alignment boundaries, and save compatibility. Browser tests cover player actions, pause/reload, a legally prepared finale, deadline loss, legacy preservation, and responsive layouts.

[Implementation status](docs/implementation-checklist.md) records what was checked. [The implementation plan](docs/implementation-plan.md) explains the design-to-code changes. Human playtests are still needed to establish pacing, difficulty, comprehension, and replay appeal.
