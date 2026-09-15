# The Awakening of Cthulhu

A deterministic, single-player strategy MVP built from `The_Awakening_of_Cthulhu_MVP_GDD.md`. Spread influence through eight world regions, manage Exposure and Suppression, exploit events, and survive the 45-second Awakening.

## Run it

Requires Node.js 22 or newer.

```bash
npm install
npm run dev
```

Open the local URL printed by Vite. Choose a difficulty and optional seed, place a foothold, select an ability, then choose a highlighted region. The top bar pauses or changes simulation speed. Upgrades spend Insight; the gear opens audio settings and restart. Runs, tutorial state, settings, and best results save locally.

## Rules

- Influence is population-weighted across eight explicit regions; the target is 60/70/80% on Easy/Normal/Hard.
- Regional cults generate Dread. Abilities spend Dread and usually add Exposure or Instability.
- Every five simulation seconds, Exposure, hostile events, and broad influence raise Suppression. Reaching 100 loses immediately.
- At the Influence target and 100 Dread, start the 45-second Awakening. Keep Influence above the target and Suppression below 100 to win.
- Seeds make random event timing and outcomes reproducible.

## Architecture

- `src/data/`: balance constants for regions, difficulties, abilities, upgrades, and events.
- `src/game/engine.ts`: pure fixed-step commands, progression, and win/loss rules.
- `src/game/director.ts`: deterministic event eligibility, pressure, spawn, and expiry logic.
- `src/game/persistence.ts`: validated versioned LocalStorage envelope and telemetry export.
- `src/App.tsx`: React presentation and browser clock; game rules stay in the engine.
- `src/tests/fixtures/`: deterministic scenario inputs from the GDD.

The browser advances the pure engine in whole simulated seconds. Speed changes how many simulated seconds are supplied; pause supplies none. Balance changes belong in `src/data/` or the named formulas in `src/game/derived.ts` and `src/game/director.ts`.

## Checks

```bash
npm run lint
npm run typecheck
npm test
npm run e2e
npm run build
```

Playwright covers setup, foothold placement, casting, unlocking, pause, save/resume, and scripted win/loss. Development builds expose a collapsible debug panel with the run seed, meter controls, forced events, scripted outcomes, and telemetry export; production builds omit it.

The automated checks validate correctness and reproducibility. Session pacing, comprehension, branch variety, target win rates, and replay appeal still require human playtests.
