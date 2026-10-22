# The Awakening of Cthulhu

A single-player world-map strategy game by Lucid Labworks Studio. Build cults, spend Influence, sacrifice followers for Dread, and prepare Cthulhu's awakening before the stars pass out of alignment.

The game is built in Godot 4.7, mainly for the browser on itch.io, with Linux and Windows desktop builds. Every graphic and sound is generated in code. The design source of truth is the [GDD](docs/gdd/README.md).

This repository holds two versions:

- `godot/`: the game. Setup, tests and export are in [godot/README.md](godot/README.md).
- `src/`: the original React and TypeScript web prototype. Its rules engine is still the reference that the Godot rules are tested against. The prototype as it was before the port is kept on the [`old-web`](https://github.com/ShamanLlama/CodeName-TheAwakening/tree/old-web) branch.

## Play

Requires Godot 4.7 on your `PATH`.

~~~bash
godot --path godot
~~~

1. Choose a difficulty, optionally enter a world seed, and click **BEGIN**.
2. Read the guide, click a region on the map, then click **BEGIN HERE** to place your first cult.
3. Click any region to open its panel. Its actions are grouped under **CULT**, **RITES** and **OMENS**. Hover an action to see its effect, risk, and why it is unavailable.
4. Threats stack at the top left. Click **OBSTRUCT** on a card to counter one.
5. Click the event line in the bottom bar to open the chronicle.

Space pauses, 1 and 2 set the speed, and Esc closes the region panel or opens the menu. Immediate actions work while paused. The game saves after actions and monthly updates, and reloading resumes the run. Play pauses when the window loses focus. Use **Menu → Export playtest report** to save a run report for feedback.

Debug builds, including runs from the editor, add playtest controls to the menu that advance 1, 12 or 120 months. They advance recruitment, threats and deadlines, and grant no resources.

## Rules

- Cult strength is your living followers. They recruit automatically and generate Influence.
- Influence funds recruitment, expansion, concealment, investigations, and prominent-person conversions.
- Sacrificing 40 local followers grants 25 Dread, reducing future income. It also shrinks the cult, which slows world awareness.
- Public worship doubles recruitment before other modifiers; backlash temporarily reduces that benefit.
- Deniers resist ordinary recruitment. Manifestations create believers, who can also strengthen opposition.
- Each ritual site must be supported by a quarter of its region's population, so a site in Oceania needs 75 followers and one in South Asia needs 450. Populous regions recruit well but host rituals badly.
- Sites cost 40 Influence and 60, 100, then 160 Dread, and take 24 supported months to prepare.
- World awareness rises with the share of humanity that follows you. It throttles income and recruitment as it climbs, and at 100% the world hunts every cell and the run ends. A large cult is a liability.
- Obstructing a threat costs 30 Influence per severity level and burns that region's connections for 12 months, so you cannot defend everywhere. Concealing evidence is the only way to push awareness back down.
- Generic celebrities and politicians appear as opportunities. Secret celebrities earn Influence; secret politicians delay new threats. Public revelation grants recruits and local ritual progress once, but ends their secret benefit and triggers backlash.
- One permanent doctrine further changes recruitment, infiltration, or sacrifice rewards.
- Reserve 100 Dread for the final rite.
- After 200 months the alignment opens and the game pauses. Start the final rite, then maintain three sites for 12 months within the 24-month window.
- Missing the deadline loses the run. Interrupted rites retain progress, but the calendar keeps advancing.

A campaign takes about 7 minutes at 1×, excluding pauses. These are initial playtest values, not final balance.

## What to test

Try an early public cult, a late public surge, and a public stronghold supported by hidden cells. Tell us where you started, your seed and difficulty, when the game felt slow or unclear, and which actions seemed too strong or too costly.

The exported report records sacrifices, spending, revelations, threats, regional snapshots and the result. It does not record real session time, so please note roughly how long you played. The first human run and the changes it prompted are recorded in the [implementation status](docs/implementation-checklist.md#human-playtest--seed-ff53a4ca-2026-09-06).

## Develop

Install the Node tooling once (Node.js 22 or newer). It runs the prototype, its tests, and the rule-parity recordings.

~~~bash
npm install
~~~

| Command | What it does |
|---|---|
| `npm run godot:test` | Runs the Godot rule, save and parity tests |
| `npm run godot:web` | Exports the web build to `godot/build/web/` |
| `npm test` | Runs the TypeScript rule tests, and fails if the parity recordings are stale |
| `npm run golden` | Re-records the parity recordings after a TypeScript rule change |
| `npm run lint`, `npm run typecheck` | Checks the TypeScript code |
| `npm run dev` | Runs the web prototype in the browser |
| `npm run e2e`, `npm run build` | Browser tests and production build for the web prototype |

To change a rule, change it in `src/game/` first, run `npm run golden`, port the change to `godot/core/`, then run `npm run godot:test`. The parity tests replay nine recorded TypeScript runs in Godot and require identical results. See [Rule parity](godot/README.md#rule-parity).

Desktop export commands and the itch.io upload steps are in [godot/README.md](godot/README.md#export).

### Layout

| Path | Contents |
|---|---|
| `godot/core/` | Godot rules: balance, data, RNG, engine, threats, saves |
| `godot/map/`, `godot/ui/` | Map projection and drawing, screens, panels, dialogs, sound |
| `godot/tests/` | Headless test runner and the recorded parity runs |
| `src/data/balance.ts` | Reference calendar, economy and ritual values |
| `src/game/` | Reference rules: commands, monthly simulation, threat director, saves |
| `src/tests/` | TypeScript rule tests, scripted playthroughs and the parity recorder |
| `scripts/build-earth-map.mjs` | Regenerates the map geometry for both versions |
| `docs/` | GDD, implementation plan and status, playtest notes |

The map uses Natural Earth's public-domain country outlines, grouped into the eight gameplay regions. [Map source and attribution](src/assets/NATURAL_EARTH.md) covers the pinned release and how to regenerate the geometry.

[Implementation status](docs/implementation-checklist.md) records what was checked, and [the implementation plan](docs/implementation-plan.md) explains the design-to-code changes. Human playtests are still needed to establish pacing, difficulty, comprehension and replay appeal.
