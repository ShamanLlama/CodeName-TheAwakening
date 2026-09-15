# MVP implementation checklist

Canonical source: `../The_Awakening_of_Cthulhu_MVP_GDD.md`. Status markers: TODO, PASS, HUMAN.

## Gates

- PASS Gate 1: seeded setup, eight selectable regions, difficulty, region details.
- PASS Gate 2: foothold, fixed-step Dread, Influence, Suppression, pause/1×/2×, determinism.
- PASS Gate 3: six abilities, three upgrades, cooldowns, target errors, milestones, feedback.
- PASS Gate 4: eight events, eligibility/director limits, interactions, 45-second Awakening, win/loss.
- PASS Gate 5: tutorial, versioned save/resume, settings/audio placeholder, debug/telemetry, accessibility, docs, tests, production build.

## High-risk invariants

- PASS Population weights total 100; Global Influence is population-weighted.
- PASS Regional/global bounded values clamp to 0–100; Dread caps at 150.
- PASS Pause advances nothing; 2× advances two simulation seconds per real second.
- PASS Seed and command sequence are deterministic across save/load.
- PASS Timers and cooldowns use simulation time; invalid commands do not mutate state.
- PASS Director: max three events, one per region, last-three exclusion, early severity limits.
- PASS Milestone Insight awards once; cascade Insight only for newly influenced regions.
- PASS Immediate one-time loss at Suppression 100; legal Awakening and exact win resolution.
- PASS Invalid/obsolete saves fail safely; development controls excluded from production.

## Content and experience

- PASS Eight canonical regions and explicit adjacency graph.
- PASS Six canonical abilities, three upgrades, and eight events match GDD behavior.
- PASS HUD, SVG map, ability/upgrade panel, region preview, headlines/log, corruption tiers.
- PASS Setup, five-step tutorial, end screen, mouse/touch, focus, contrast, reduced motion, tablet layout.
- PASS Local settings, tutorial completion, suspended run, best result, telemetry JSON export.
- HUMAN Session pacing, comprehension, target win rates, branch variety, and replay desire require playtests.

## Required automated evidence

- PASS `npm run lint`
- PASS `npm run typecheck`
- PASS `npm test` — 29 tests.
- PASS `npm run build`
- PASS `npm run e2e` — 5 Chromium journeys.
- PASS 1280×720 browser smoke test with no console errors; screenshot in `test-results/cthulhu-1280x720.png`.
