# Codex Build Prompt - The Awakening of Cthulhu

Paste the following prompt at the root of the project repository.

```text
You are the lead implementation engineer for a small browser strategy game called "The Awakening of Cthulhu."

Build a polished, testable MVP from the specification below. Work incrementally, keep the codebase runnable after every milestone, and prefer simple maintainable systems over clever abstractions.

TECH STACK
- React
- TypeScript with strict mode
- Vite
- SVG for the world map
- Plain CSS or CSS Modules
- Zustand or a small reducer-based store
- Vitest for unit tests
- Playwright for end-to-end tests
- LocalStorage only; no backend and no external API

PRODUCT GOAL
Create a single-player real-time strategy/incremental web game. The player spreads Cthulhu's influence across eight world regions by using Ritual, Madness, and Omen abilities. Influence generates Dread, strong actions create Exposure, and Exposure contributes to Global Suppression. The player wins by reaching the difficulty's Global Influence threshold, spending 100 Dread to start Awakening, and surviving a 45-second final response phase without Global Suppression reaching 100.

REGIONS
Implement these region IDs:
- north_america
- latin_america
- europe
- africa
- middle_east
- south_asia
- east_asia
- oceania

Store static region definitions in src/data/regions.ts. Include populationWeight, skepticism, institutionalResistance, mysticism, tags, and an explicit adjacency list. Population weights must total 100.

REGIONAL STATE
Each region tracks values clamped from 0 to 100:
- influence
- cultStrength
- resistanceModifier
- exposure
- instability
- activeEventId
- milestoneFlags

GLOBAL STATE
Track:
- status: setup, playing, awakening, won, lost
- difficulty: easy, normal, hard
- seed
- elapsedSeconds
- speed: 0, 1, 2
- dread, capped at 150
- insight
- globalInfluence
- suppression
- awakeningSecondsRemaining
- regions
- unlockedAbilities
- cooldowns
- activeEvents
- recentEventIds
- action log

DIFFICULTY
- Easy: 60 Global Influence target, 0.8 suppression multiplier, 0.85 event pressure
- Normal: 70 target, 1.0, 1.0
- Hard: 80 target, 1.2, 1.2

CORE RULES
- Starting resources: 20 Dread, 0 Insight.
- Player places one free starting Cult Foothold: Influence +8, Cult Strength +20, Exposure +5.
- Global Influence is the sum of populationWeight * influence / 100.
- Passive Dread per second is:
  0.20 + sum(cultStrength * populationWeight) / 9000 + globalInfluence / 250
- Process simulation in fixed one-second steps. The browser timer may fire every 250 ms, but balance must not depend on frame rate.
- Use a seeded PRNG and display the seed in a debug panel.
- All simulation logic must be pure and separate from React components.

INSIGHT
Award 1 Insight when a region crosses Influence 25, 50, or 75 for the first time, when a hostile event is fully neutralized before expiration, or when a cascade creates influence in a previously uninfluenced region.

ABILITIES
Implement typed ability data and engine handlers for:

1. Secret Gathering - Ritual
- Start unlocked
- Cost 12, cooldown 20 sec
- Requires Influence > 0
- Cult Strength +15, Influence +4, Exposure +4
- If mysticism >= 60, additional Cult Strength +5

2. Sacrament of the Deep - Ritual
- Unlock cost 2 Insight
- Cost 28, cooldown 45 sec
- Requires Cult Strength >= 25
- Influence +14, Cult Strength +8, Instability +10, Exposure +12
- Has an exposure-based chance to trigger Investigators Arrive

3. Whispered Dreams - Madness
- Start unlocked
- Cost 10, cooldown 15 sec
- Any region
- Influence +6, Instability +5, Exposure +3
- If no cult, create Cult Strength 5

4. Mass Delusion - Madness
- Unlock cost 2 Insight
- Cost 30, cooldown 50 sec
- Requires Influence >= 20
- Influence +16, Instability +18, Exposure +15
- Adjacent regions gain +3 Influence

5. Impossible Omen - Omen
- Unlock cost 1 Insight
- Cost 18, cooldown 35 sec
- Any region
- Resistance -10 for 45 sec, Influence +5, Instability +8
- Converts Religious Revival into Apocalyptic Schism

6. Rewrite the Headline - Omen
- Unlock cost 2 Insight
- Cost 22, cooldown 40 sec
- Requires hostile event or Exposure > 20
- Exposure -15, current event severity -1, Influence -2

UPGRADES
- Hidden Cells: 3 Insight after Secret Gathering. Secret Gathering adds 3 less Exposure. Crackdowns remove 25% less Cult Strength.
- Contagious Dreams: 3 Insight after Whispered Dreams. Every 20 seconds, the highest-influence region spreads +1 Influence to an adjacent region below 30 Influence.
- False Consensus: 3 Insight after Impossible Omen. Exposure-based suppression generation is reduced by 15%; opportunity events last 20% longer.

EVENT DIRECTOR
Implement a rule-based director, not generative AI.
- First event after 60-90 seconds.
- Normal cadence 45-75 seconds.
- Max 3 active events.
- One event per region.
- Do not repeat an event from the last three spawns.
- Pressure check every 5 seconds:
  elapsedMinutes * 5 + globalInfluence * 0.55 + averageExposure * 0.35 + difficultyModifier - activeEventCount * 12
- Initial spawn threshold 35. After a spawn, add a seeded random 22-32.
- Severity 1 before 25 influence, 2 after 25, 3 after 50 or during Awakening.

Implement these events as typed data plus explicit engine behavior:
HOSTILE
- Investigators Arrive
- Government Crackdown
- Religious Revival
- Secret Society Intervention
OPPORTUNITY
- Shared Nightmare
- Solar Eclipse
- Political Scandal
- Deep-Sea Signal

Use the detailed event behavior from The_Awakening_of_Cthulhu_MVP_GDD.md in the repository. If that file is not present, create docs/GDD.md from this prompt before coding and document reasonable constants in data files.

SUPPRESSION
Every 5 seconds calculate:
max(0, averageExposure - 15) * 0.025
+ hostileEventSeveritySum * 0.10
+ highInfluenceRegionCount * 0.05
Then apply difficulty multiplier. Multiply by 2 during Awakening.

WIN AND LOSS
- Lose immediately at Suppression >= 100.
- When Global Influence reaches the difficulty target, enable Awaken Cthulhu.
- Awakening costs 100 Dread and lasts 45 simulation seconds.
- During Awakening, Dread generation stops, hostile event pressure doubles, and cooldown recovery is 30% faster.
- Win when the timer ends if influence is still at or above the target and suppression is below 100.

UI
Create one main game screen:
- Top HUD: Global Influence, target marker, Suppression, Dread with per-second rate, Insight, phase, pause, 1x, 2x.
- Left panel: ability cards grouped by branch and an upgrade view.
- Center: stylized SVG map with eight clickable regions. Use simple shapes/placeholders; accuracy is less important than readability.
- Right panel: selected-region stats, traits, active event, remaining time, and a preview of the selected ability's effects.
- Bottom: news ticker and action log.
- Tutorial overlay with five steps.
- Setup modal for difficulty and optional seed.
- End screen with result, duration, final meters, most-used ability, and restart.

VISUAL DIRECTION
Use a dark occult pixel-inspired interface with legible text and restrained effects. Use CSS/SVG placeholders and no copyrighted or AI-generated assets. Add visible corruption tiers based on Influence. Add hostile and opportunity event outlines. Include mute and volume controls, but placeholder audio is acceptable.

PERSISTENCE
Version a LocalStorage save object. Save the suspended run, settings, tutorial completion, and best result by difficulty. Invalid saves must fail safely.

TESTING
Unit tests must cover:
- Global Influence math
- Value clamping
- Ability validation and costs
- Cooldowns at each speed
- Seeded RNG reproducibility
- Event repetition and active-event limits
- Milestone Insight awards
- Win/loss transitions
- Save/load round trip

Playwright must cover:
- Start run and place foothold
- Cast Whispered Dreams
- Unlock an ability
- Pause simulation
- Save and resume after reload
- Scripted win
- Scripted loss

IMPLEMENTATION ORDER
1. Scaffold project, linting, test commands, and README.
2. Add typed data and pure game engine.
3. Add map and HUD.
4. Add abilities and progression.
5. Add event director and events.
6. Add Awakening, win/loss, and end screen.
7. Add tutorial, persistence, debug tools, and tests.
8. Run all checks and fix errors.

ENGINEERING REQUIREMENTS
- TypeScript strict mode must pass without suppressing errors.
- No game rule should live only inside a React component.
- Keep balance constants in data/config modules.
- Do not add a backend or unnecessary dependencies.
- Prefer explicit pure functions and discriminated unions.
- Provide helpful validation errors rather than silently ignoring invalid actions.
- Add a development-only debug panel that can add Dread, add Insight, set meters, force an event, and export the current state as JSON.
- Keep the app usable at 1280x720 and above.
- Add accessible labels, keyboard focus states, and reduced-motion support.

DELIVERABLES
- Working source code
- README with setup, controls, architecture, rules, scripts, and balance locations
- docs/GDD.md
- Unit and end-to-end tests
- No known console errors

Begin by creating a concise implementation checklist in the repository, then complete Milestone 1 and run the tests before proceeding. Continue through all milestones unless blocked by a real technical constraint. When choosing between extra polish and a reliable full game loop, prioritize the reliable loop.
```
