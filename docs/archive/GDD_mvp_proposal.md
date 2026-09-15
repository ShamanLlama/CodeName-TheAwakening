# The Awakening of Cthulhu - MVP Game Design Document

**Document purpose:** A development-ready plan for a small, testable web-game MVP that can be implemented iteratively with Codex.

**Working title:** The Awakening of Cthulhu  
**Genre:** Single-player real-time strategy / incremental world-map game  
**Platform:** Desktop web browser, with responsive support for tablets  
**Target session:** 12-18 minutes  
**Primary input:** Mouse or touch  
**MVP technology:** React, TypeScript, Vite, SVG map, CSS, Vitest, Playwright  
**Audience:** Players who enjoy short strategy games, incremental progression, world-state management, and Lovecraftian horror.

---

## 1. Source Adaptation and Design Direction

The project sources contain two related but thematically different designs:

- `High level design.pdf` defines the Cthulhu fantasy: spreading influence through dark rituals, madness, and mystical events; overcoming governments, religions, and secret societies; and awakening Cthulhu after reaching a global influence threshold.
- The Pacha documents define a more complete systems framework: a clickable world map, resource generation, ability progression, dynamic events, a global state bar, news feedback, regional compatibility, and escalating pressure.
- The flowchart on page 5 of `Avance GDD 5-1-24.pdf` shows the strongest reusable loop: establish a presence, earn points, learn abilities, observe events, act or wait, and receive world-state feedback.
- The two ecosystem spreadsheets suggest a useful data-driven pattern: content should define which events and abilities are valid or strong in each region rather than hard-coding every interaction.

This GDD keeps the **Cthulhu theme and win condition** while adapting the **proven systems structure** from the Pacha design.

### System translation

| Source system | Cthulhu MVP adaptation |
|---|---|
| Healing Beacon | Cult Foothold |
| Restoration Points | Dread |
| Healing abilities | Occult Methods |
| Ecosystem health | Regional Influence and Stability |
| Environmental events | Opposition and Mystical Events |
| Global healing bar | Global Influence bar |
| Planet collapse | Global Suppression reaches 100 |
| News ticker | World headlines and cult reports |

### MVP design principle

The game should be understandable in under two minutes, produce a meaningful decision every 20-40 seconds, and finish before its systems become repetitive.

---

## 2. High Concept

The player is an ancient intelligence stirring beneath the Pacific. Cthulhu cannot awaken directly. The player must seed cults, invade dreams, corrupt institutions, and exploit global crises until enough of humanity accepts, fears, or serves the Great Old One.

Every action increases influence but may also increase exposure. As the cult spreads, governments, religious movements, investigators, and secret societies organize a coordinated response. The player wins by reaching the required global influence before suppression becomes total.

### Player fantasy

> "I am an unseen cosmic force manipulating belief, fear, and political power across the world while humanity slowly realizes what is happening."

### Design pillars

1. **Global manipulation:** The player acts through regions, networks, and cascading effects rather than controlling an avatar.
2. **Power with consequences:** Strong actions spread influence quickly but create exposure and resistance.
3. **Readable systemic strategy:** The player can understand why a region is vulnerable, why an event happened, and what changed after an action.
4. **Short replayable runs:** Different starting regions, event sequences, upgrades, and difficulty settings create variation without requiring a large campaign.
5. **Atmosphere through feedback:** The map, headlines, sound, and interface become more eldritch as awakening approaches.

---

## 3. MVP Scope

### Included

- One world-map screen with 8 clickable regions.
- Three occult ability branches: Ritual, Madness, and Omens.
- Six basic abilities, three branch upgrades, and one final Awakening action.
- Eight world events.
- Regional simulation with population weight, influence, skepticism, cult strength, resistance, and exposure.
- Dread resource generation.
- Global Influence and Global Suppression meters.
- Ability unlock progression.
- News ticker and action log.
- Easy, Normal, and Hard difficulty presets.
- Pause, speed controls, restart, seed display, and local save.
- A short tutorial overlay.
- Automated unit and end-to-end tests for core rules.

### Explicitly out of scope

- Multiplayer.
- Accounts, cloud saves, backend services, or live operations.
- Runtime generative AI.
- A geographically accurate country-level map.
- Complex diplomacy or individual non-player characters.
- Full campaign narrative.
- Procedural art generation inside the game.
- Mobile-first layout.
- More than one final entity or faction.
- Real-world political parties, living politicians, or current conflicts.

The MVP must use fictionalized headlines and institutions to avoid making the content dependent on current events.

---

## 4. Core Game Loop

### Moment-to-moment loop

1. **Select a region.**
2. **Inspect vulnerability and active events.**
3. **Generate or wait for Dread.**
4. **Use an Occult Method.**
5. **Watch influence, exposure, cult strength, and resistance change.**
6. **Respond to opposition or exploit a mystical opportunity.**
7. **Spend earned Insight to unlock stronger methods.**
8. **Repeat until Awakening or suppression.**

### Run-level loop

1. Choose difficulty and optional random seed.
2. Establish the first Cult Foothold.
3. Build Dread and spread regional influence.
4. Unlock abilities that complement the player's chosen strategy.
5. Manage escalating opposition.
6. Reach the global influence requirement.
7. Activate **Awaken Cthulhu** and survive the final response window.

### Why the loop works

- Waiting produces resources but also allows resistance and events to progress.
- Spending Dread creates immediate gains but may raise exposure.
- Low-risk abilities are efficient but slow.
- High-risk abilities can create large swings or dangerous cascades.
- The player must continually choose between spreading wide, strengthening existing cults, reducing exposure, or exploiting an event.

---

## 5. Win, Loss, and Run Structure

### Difficulty thresholds

| Difficulty | Required Global Influence | Suppression growth | Event pressure | Suggested audience |
|---|---:|---:|---:|---|
| Easy | 60% | 0.8x | 0.85x | Learning the systems |
| Normal | 70% | 1.0x | 1.0x | Intended experience |
| Hard | 80% | 1.2x | 1.2x | Experienced players |

These influence thresholds preserve the values proposed in the original Cthulhu high-level design.

### Win condition

When Global Influence reaches the difficulty threshold, the **Awaken Cthulhu** action becomes available. It costs 100 Dread and starts a 45-second final response phase.

The player wins if:

- Global Influence remains at or above the threshold when the response phase ends, and
- Global Suppression remains below 100.

This final phase prevents the game from ending the instant a bar crosses a number and creates a dramatic climax.

### Loss condition

The player loses if Global Suppression reaches 100.

Optional fail-safe: if all regional cult strength values are zero and the player has less than 10 Dread, offer a restart prompt because recovery is no longer practical.

### Run phases

| Phase | Typical time | Design purpose |
|---|---:|---|
| Stirring | 0-3 min | Learn controls, establish two regions, see first event |
| Spreading | 3-8 min | Build a strategy and unlock branch upgrades |
| Revelation | 8-13 min | Opposition strengthens and regional interactions matter |
| Awakening | Final 45 sec | Survive coordinated global response |

---

## 6. World Map and Regions

The MVP uses a stylized SVG map with eight large regions. These are gameplay zones, not exact political borders.

| Region | Population Weight | Skepticism | Institutional Resistance | Mysticism | Notes |
|---|---:|---:|---:|---:|---|
| North America | 14 | 65 | 75 | 35 | High resistance, strong media cascades |
| Latin America | 12 | 45 | 45 | 65 | Strong ritual potential |
| Europe | 13 | 70 | 80 | 30 | Difficult but valuable institutional targets |
| Africa | 15 | 40 | 35 | 70 | Rapid cult growth, lower exposure control |
| Middle East | 10 | 55 | 70 | 60 | High religious counter-response risk |
| South Asia | 18 | 50 | 50 | 65 | High population value, fast rumor spread |
| East Asia | 15 | 75 | 85 | 35 | High surveillance and suppression |
| Oceania | 3 | 50 | 45 | 55 | Close to R'lyeh; cheap starting foothold |

Population weights total 100 and determine each region's contribution to Global Influence.

### Regional state

Each region tracks:

- **Influence:** 0-100. Percentage of the regional population that fears, believes in, or serves Cthulhu.
- **Cult Strength:** 0-100. Local infrastructure and resilience. Strong cults improve Dread generation and reduce losses from crackdowns.
- **Resistance:** 0-100. Organized local ability to oppose the cult.
- **Exposure:** 0-100. How visible the cult is in the region.
- **Instability:** 0-100. Temporary social disruption. It boosts some abilities and increases event risk.
- **Active event:** Zero or one event in the MVP.

### Derived values

```text
regionalContribution = populationWeight * (influence / 100)

globalInfluence = sum(regionalContribution)

localSpreadRate =
  baseSpread
  * (0.5 + cultStrength / 100)
  * (1.25 - skepticism / 200)
  * eventSpreadModifier

localSuppressionRisk =
  (exposure * 0.45 + resistance * 0.35 + influence * 0.20)
  / 100
```

All calculated values are clamped to their valid ranges.

### Region visual states

- 0-19 Influence: muted, no tentacle overlay.
- 20-39: faint purple pulse.
- 40-59: visible occult sigils.
- 60-79: darkened terrain and animated veins.
- 80-100: full eldritch corruption effect.
- Active hostile event: red border and warning icon.
- Active opportunity event: cyan border and omen icon.

For the MVP, these effects can be CSS classes and simple SVG overlays rather than custom animation assets.

---

## 7. Resources and Progression

### Dread

Dread is the spendable resource used to activate abilities and awaken Cthulhu.

**Passive generation per second:**

```text
dreadPerSecond = 0.20
  + sum(cultStrength * populationWeight) / 9000
  + globalInfluence / 250
```

Expected values:

- Early game: about 0.3-0.6 Dread/sec.
- Mid game: about 0.8-1.4 Dread/sec.
- Late game: about 1.5-2.5 Dread/sec.

Dread is capped at 150 to encourage spending.

### Insight

Insight is a progression currency used only to unlock methods.

Earn 1 Insight when:

- A region crosses 25, 50, or 75 Influence for the first time.
- The player successfully resolves a hostile event.
- A chain reaction spreads influence to a new region.

Expected unlock pace: one new ability every 2-3 minutes.

### Starting state

- 20 Dread.
- 0 Insight.
- One free Cult Foothold placement.
- `Whispered Dreams` unlocked.
- All regions at 0 Influence, 0 Cult Strength, 10 base Resistance, and 0 Exposure, modified by their static data.

### Cult Foothold

The first click on a region establishes the starting cult:

- Influence +8.
- Cult Strength +20.
- Exposure +5.
- Enables passive regional spread.

After the starting placement, new footholds are created through abilities rather than a separate button.

---

## 8. Ability System

Abilities are data-driven. Each ability defines cost, cooldown, target rules, immediate effects, delayed effects, tags, and upgrade prerequisites.

### Branch A - Rituals

Rituals build durable cult infrastructure. They are efficient in regions with high Mysticism and vulnerable to crackdowns.

#### 1. Secret Gathering

- **Unlocked:** Start
- **Cost:** 12 Dread
- **Cooldown:** 20 sec
- **Target:** Any region with Influence above 0
- **Effects:** Cult Strength +15, Influence +4, Exposure +4
- **Bonus:** If Mysticism is 60 or higher, Cult Strength gains an additional +5
- **Purpose:** Safe foundational growth

#### 2. Sacrament of the Deep

- **Unlock cost:** 2 Insight
- **Cost:** 28 Dread
- **Cooldown:** 45 sec
- **Target:** Region with Cult Strength at least 25
- **Effects:** Influence +14, Cult Strength +8, Instability +10, Exposure +12
- **Risk:** 20% base chance to trigger `Investigators Arrive`; chance rises with Exposure
- **Purpose:** Strong regional push with a readable risk

#### Ritual branch upgrade - Hidden Cells

- **Unlock cost:** 3 Insight after Secret Gathering
- Secret Gathering grants 3 less Exposure.
- Crackdowns remove 25% less Cult Strength.

### Branch B - Madness

Madness spreads influence quickly and creates cascades, but it raises exposure and instability.

#### 3. Whispered Dreams

- **Unlocked:** Start
- **Cost:** 10 Dread
- **Cooldown:** 15 sec
- **Target:** Any region
- **Effects:** Influence +6, Instability +5, Exposure +3
- **Bonus:** If the region has no cult, create Cult Strength 5
- **Purpose:** Primary spreading tool

#### 4. Mass Delusion

- **Unlock cost:** 2 Insight
- **Cost:** 30 Dread
- **Cooldown:** 50 sec
- **Target:** Region with Influence at least 20
- **Effects:** Influence +16, Instability +18, Exposure +15
- **Cascade:** Adjacent regions gain +3 Influence
- **Purpose:** Fast wide growth and combo setup

#### Madness branch upgrade - Contagious Dreams

- **Unlock cost:** 3 Insight after Whispered Dreams
- Every 20 seconds, the highest-Influence region spreads +1 Influence to one adjacent region.
- The passive spread cannot raise a region above 30 Influence.

### Branch C - Omens

Omens manipulate events and global perception. They are tactical, lower direct growth, and better at managing pressure.

#### 5. Impossible Omen

- **Unlock cost:** 1 Insight
- **Cost:** 18 Dread
- **Cooldown:** 35 sec
- **Target:** Any region
- **Effects:** Resistance -10 for 45 sec, Influence +5, Instability +8
- **Event interaction:** Converts `Religious Revival` into `Apocalyptic Schism`
- **Purpose:** Prepare a hard target or transform an event

#### 6. Rewrite the Headline

- **Unlock cost:** 2 Insight
- **Cost:** 22 Dread
- **Cooldown:** 40 sec
- **Target:** Region with a hostile event or Exposure above 20
- **Effects:** Exposure -15 and event severity -1
- **Tradeoff:** Influence -2
- **Purpose:** Pressure management

#### Omen branch upgrade - False Consensus

- **Unlock cost:** 3 Insight after Impossible Omen
- Global Suppression generated from regional Exposure is reduced by 15%.
- Positive opportunity events last 20% longer.

### Final ability - Awaken Cthulhu

- **Prerequisite:** Reach required Global Influence.
- **Cost:** 100 Dread.
- **Effect:** Starts the 45-second final response phase.
- **During final phase:** Dread generation stops, hostile event rate doubles, and all cooldowns recover 30% faster.
- **Result:** Win if influence remains above the target and suppression stays below 100.

### Ability feedback

Each activation must provide:

- Immediate map pulse on the target.
- Floating numeric deltas.
- A one-line result in the action log.
- A short, distinct sound placeholder.
- A cooldown ring or progress bar.
- Tooltip text explaining modifiers and risks before activation.

---

## 9. Event Director

The source documents ask for event timing that keeps the player engaged without overwhelming them. The MVP should use a transparent rule-based director, not generative AI.

### Director goals

- First event occurs between 60 and 90 seconds.
- Normal cadence is one event every 45-75 seconds.
- Never exceed three active events globally.
- Never place a second event in a region that already has one.
- Avoid repeating the same event within the last three spawns.
- Alternate pressure and opportunity when possible.

### Pressure score

Every five seconds, calculate:

```text
pressure =
  elapsedMinutes * 5
  + globalInfluence * 0.55
  + averageRegionalExposure * 0.35
  + difficultyModifier
  - activeEventCount * 12
```

When pressure exceeds the next spawn threshold, schedule an event after a random 5-12 second warning.

Initial threshold: 35. After each spawn, add a seeded random value between 22 and 32.

### Event severity

- Severity 1 before 25 Global Influence.
- Severity 2 after 25 Global Influence.
- Severity 3 after 50 Global Influence or during Awakening.
- Hard difficulty treats pressure as 10 points higher for severity checks.

### Event structure

Each event contains:

- `id`
- `name`
- `type`: hostile or opportunity
- `validRegions`
- `weight`
- `minimumInfluence`
- `durationSeconds`
- `severityEffects`
- `onStart`
- `onTick`
- `onExpire`
- `abilityInteractions`
- `headlineTemplates`

### Hostile events

#### 1. Investigators Arrive

- **Trigger weight increases with:** Exposure and Cult Strength.
- **Duration:** 45 sec.
- **Effect:** Resistance +10/20/30 by severity. On expiration, Cult Strength -8/-16/-24.
- **Counters:** Rewrite the Headline; Secret Gathering after Hidden Cells.
- **Headline:** "Unmarked task force follows reports of impossible coastal chants."

#### 2. Government Crackdown

- **Valid where:** Institutional Resistance is 60 or higher.
- **Duration:** 60 sec.
- **Effect:** Dread generation from the region reduced by 50%; Cult Strength decays 0.15/0.25/0.35 per second.
- **Counter:** Build cult strength before it expires or reduce severity.

#### 3. Religious Revival

- **Duration:** 50 sec.
- **Effect:** Regional Influence decays 0.08/0.14/0.20 per second.
- **Interaction:** Impossible Omen converts it into Apocalyptic Schism, ending the decay and adding +8 Influence but +10 Exposure.

#### 4. Secret Society Intervention

- **Valid where:** Influence is 35 or higher.
- **Duration:** 55 sec.
- **Effect:** Ability costs in the region increase by 20/35/50%.
- **Expiration:** Global Suppression +3/6/9.

### Opportunity events

#### 5. Shared Nightmare

- **Duration:** 35 sec.
- **Effect:** Madness abilities cost 30% less in the region.
- **On use of Madness ability:** Adjacent regions gain +2 Influence.

#### 6. Solar Eclipse

- **Duration:** 40 sec.
- **Effect:** Ritual effects are 35% stronger and produce 25% more Exposure.

#### 7. Political Scandal

- **Duration:** 45 sec.
- **Effect:** Institutional Resistance -20. First ability used adds no Exposure.
- **Fictional framing only:** No real politicians or parties.

#### 8. Deep-Sea Signal

- **Valid where:** Oceania or a coastal-tagged region.
- **Duration:** 30 sec.
- **Effect:** Dread generation +100%. If the player spends 25 Dread during the event, all regions gain +1 Influence.

### Event outcomes

Events should not require a separate "solve" button. The player's ability use and the passage of time determine the outcome. When an event ends, the news ticker explains what happened and why.

---

## 10. Global Suppression System

Global Suppression represents coordinated human resistance.

### Suppression growth

Every five seconds:

```text
suppressionGain =
  max(0, averageExposure - 15) * 0.025
  + hostileEventSeveritySum * 0.10
  + highInfluenceRegionCount * 0.05
```

Apply the difficulty multiplier. During Awakening, multiply the result by 2.

### Suppression reduction

- Rewrite the Headline reduces local Exposure, indirectly slowing growth.
- Completing an opportunity event objective reduces Global Suppression by 1.
- When a hostile event is reduced to severity 0 before expiring, reduce Global Suppression by 2.
- Suppression cannot fall below 0.

### Feedback thresholds

- 0-24: Humanity is unaware.
- 25-49: Rumors and investigations spread.
- 50-74: Governments coordinate.
- 75-99: Emergency global response.
- 100: Cult network is suppressed; game over.

The map color grading, music intensity, and headline tone change at each threshold.

---

## 11. Cascading Systems

The game needs a small number of visible interactions rather than a large simulation hidden from the player.

### Core cascades

1. **Influence -> Dread:** More influenced population increases passive resource generation.
2. **Cult Strength -> resilience:** Strong cults survive crackdowns and generate more Dread.
3. **Powerful action -> Exposure:** Fast gains make hostile events and suppression more likely.
4. **Instability -> opportunity and danger:** Unstable regions amplify Madness but attract hostile events.
5. **Regional spread -> global attention:** Wide influence creates more global pressure than one hidden stronghold.
6. **Events -> ability value:** Events temporarily change which branch is most efficient.

### Adjacency graph

Use an explicit data file rather than geographic calculation:

```text
North America: Latin America, Europe, East Asia
Latin America: North America, Africa, Oceania
Europe: North America, Africa, Middle East
Africa: Latin America, Europe, Middle East, South Asia
Middle East: Europe, Africa, South Asia, East Asia
South Asia: Africa, Middle East, East Asia, Oceania
East Asia: North America, Middle East, South Asia, Oceania
Oceania: Latin America, South Asia, East Asia
```

This graph powers Madness cascades and event selection.

---

## 12. User Interface

### Primary screen layout

```text
+---------------------------------------------------------------+
| Global Influence | Suppression | Dread | Insight | Pause/Speed |
+-------------------+---------------------------+-----------------+
| Ability panel     |                           | Region details  |
| - Rituals         |       SVG WORLD MAP       | Stats           |
| - Madness         |                           | Active event    |
| - Omens           |                           | Modifiers       |
+-------------------+---------------------------+-----------------+
| News ticker / action log / tutorial hints                       |
+---------------------------------------------------------------+
```

### Top bar

- Global Influence meter with target marker.
- Global Suppression meter.
- Dread and generation rate.
- Insight points.
- Current phase.
- Pause, 1x, and 2x speed.

### Ability panel

Each ability card shows:

- Name and branch icon.
- Cost.
- Cooldown.
- Valid/invalid target state.
- Short effect summary.
- Upgrade state.

Clicking an ability enters targeting mode. Valid regions glow. Clicking the selected ability again cancels targeting.

### Region detail panel

- Region name.
- Population weight.
- Influence, Cult Strength, Resistance, Exposure, Instability.
- Static traits.
- Current event and remaining time.
- Predicted result of the selected ability.

The predicted result is important: it lets the player understand the system without revealing every random event probability.

### News ticker

Headlines communicate consequences and provide lightweight narrative.

Examples:

- "Dreams of a sunken city spread across South Asia. Influence +6."
- "Investigators dismantle a hidden cell in Europe. Cult Strength -16."
- "An impossible eclipse fractures a revival movement in Africa. Influence +8, Exposure +10."

The log keeps the latest 30 entries and supports pause-on-hover.

### Tutorial

A five-step overlay:

1. Place the first foothold.
2. Use Whispered Dreams.
3. Explain Dread and Influence.
4. Show the first event and ability interaction.
5. Open the upgrade panel.

The tutorial can be dismissed and remains dismissed in local storage.

---

## 13. Visual and Audio Direction

### Visual style

- Dark 8-bit/pixel-inspired occult interface, as proposed in the original high-level design.
- Deep navy, black, desaturated teal, bone, and restrained crimson.
- World map remains legible before atmosphere.
- Eldritch effects grow with Global Influence.
- UI uses simple CSS borders, icons, and subtle noise textures for MVP.
- Avoid depending on AI-generated final art during implementation. Use replaceable placeholders and a documented asset folder.

### Audio

MVP audio can be minimal:

- Low ambient drone.
- One click sound.
- One sound per ability branch.
- Warning sting for hostile events.
- Rising pulse at high Suppression.
- Awakening climax loop.

All audio must have a mute toggle and volume stored locally.

---

## 14. Difficulty and Pacing

### Flow target

The player should oscillate between control and danger:

- **Low pressure:** Build resources and plan.
- **Rising pressure:** A meaningful event appears.
- **Action peak:** The player commits an ability or combo.
- **Feedback:** The map and headlines explain the result.
- **Recovery:** Brief time to reassess and unlock something.

### Anti-overwhelm rules

- No hostile events in the first 60 seconds.
- Maximum two hostile events before 40 Global Influence.
- Maximum three events total.
- At least 20 seconds between event spawns.
- Tutorial pauses the simulation.
- A new player cannot trigger a Severity 3 event before minute 8 on Easy or Normal.

### Anti-boredom rules

- If no ability has been used for 45 seconds, increase Dread generation by 20% until the next use.
- If no event has spawned for 80 seconds, force an opportunity event.
- If the player has 100 or more Dread, pulse the resource counter and highlight affordable abilities.

### Rubber-band limits

The game should not secretly cancel good play. Director assistance may adjust event type and timing, but never directly remove Influence because the player is winning.

---

## 15. Data Model

Suggested TypeScript interfaces:

```ts
export type RegionId =
  | "north_america"
  | "latin_america"
  | "europe"
  | "africa"
  | "middle_east"
  | "south_asia"
  | "east_asia"
  | "oceania";

export type AbilityBranch = "ritual" | "madness" | "omen";
export type EventType = "hostile" | "opportunity";

export interface RegionDefinition {
  id: RegionId;
  name: string;
  populationWeight: number;
  skepticism: number;
  institutionalResistance: number;
  mysticism: number;
  adjacent: RegionId[];
  tags: string[];
}

export interface RegionState {
  influence: number;
  cultStrength: number;
  resistanceModifier: number;
  exposure: number;
  instability: number;
  activeEventId: string | null;
  milestoneFlags: string[];
}

export interface AbilityDefinition {
  id: string;
  name: string;
  branch: AbilityBranch;
  cost: number;
  cooldownSeconds: number;
  unlockCost: number;
  prerequisites: string[];
  targetRule: string;
  tags: string[];
}

export interface ActiveEvent {
  instanceId: string;
  eventId: string;
  regionId: RegionId;
  severity: 1 | 2 | 3;
  remainingSeconds: number;
  resolved: boolean;
}

export interface GameState {
  status: "setup" | "playing" | "awakening" | "won" | "lost";
  difficulty: "easy" | "normal" | "hard";
  seed: string;
  elapsedSeconds: number;
  speed: 0 | 1 | 2;
  dread: number;
  insight: number;
  globalInfluence: number;
  suppression: number;
  awakeningSecondsRemaining: number | null;
  regions: Record<RegionId, RegionState>;
  unlockedAbilities: string[];
  cooldowns: Record<string, number>;
  activeEvents: ActiveEvent[];
  recentEventIds: string[];
  log: GameLogEntry[];
}
```

### State rules

- All simulation changes occur through pure engine functions.
- UI components do not directly modify game values.
- Clamp every regional value to 0-100.
- Recalculate derived global values after each command and simulation tick.
- Use a seeded random number generator so bugs and balance cases can be reproduced.

---

## 16. Technical Architecture

### Recommended stack

- React + TypeScript + Vite.
- SVG for the world map.
- CSS Modules or plain scoped CSS.
- Zustand or a small custom reducer for application state.
- Vitest for unit tests.
- Playwright for end-to-end tests.
- LocalStorage for settings and one suspended run.

Avoid a server, database, canvas engine, or WebGL for the MVP.

### Suggested project structure

```text
src/
  app/
    App.tsx
    routes.ts
  components/
    AbilityCard.tsx
    AbilityPanel.tsx
    GameHud.tsx
    NewsTicker.tsx
    RegionPanel.tsx
    TutorialOverlay.tsx
    WorldMap.tsx
  data/
    abilities.ts
    difficulties.ts
    events.ts
    regions.ts
  game/
    commands.ts
    derived.ts
    director.ts
    engine.ts
    events.ts
    random.ts
    reducer.ts
    selectors.ts
    simulation.ts
    types.ts
  hooks/
    useGameLoop.ts
    useLocalSave.ts
  styles/
  tests/
```

### Simulation loop

- Use a 250 ms browser interval for responsiveness.
- Accumulate elapsed time and process simulation in fixed one-second steps.
- At 2x speed, process two simulation seconds per real second.
- Pause sets speed to zero.
- Never make balance depend on rendering frame rate.

### Commands

The engine should support explicit commands:

```ts
startRun(config)
placeStartingFoothold(regionId)
selectAbility(abilityId)
castAbility(abilityId, regionId)
unlockAbility(abilityId)
setGameSpeed(speed)
advanceSimulation(seconds)
startAwakening()
restartRun(seed?)
```

Each command returns either a new state or a typed validation error.

### Persistence

Save:

- Current run state.
- Settings.
- Tutorial completion.
- Best result per difficulty.

Version the save object. Invalid or old saves should fail safely and offer a new run.

---

## 17. MVP Content Matrix

The Pacha spreadsheets use ecosystem-versus-event and ecosystem-versus-ability matrices. The Cthulhu MVP should use the same pattern through tags and modifiers.

Example ability modifiers:

| Region trait | Rituals | Madness | Omens |
|---|---:|---:|---:|
| Mysticism 60+ | 1.25x cult gain | 1.0x | 1.1x duration |
| Skepticism 70+ | 0.85x influence | 0.9x influence | 1.2x resistance reduction |
| Institutional Resistance 70+ | 0.9x | 0.9x | 1.25x |
| Coastal | 1.1x | 1.0x | Deep-Sea Signal eligible |
| Population Weight 15+ | 1.0x | +1 cascade influence | 1.0x |

Do not create a large spreadsheet-driven editor for the MVP. Encode these values in typed data files, but keep the schema ready for later migration to JSON or a content tool.

---

## 18. Balance Targets

### Normal difficulty target curve

| Minute | Expected Global Influence | Expected Suppression | Expected unlocked abilities |
|---:|---:|---:|---:|
| 2 | 8-15 | 0-8 | 2 |
| 5 | 22-35 | 10-25 | 3-4 |
| 8 | 40-55 | 25-45 | 5-6 |
| 11 | 58-68 | 45-70 | 6-8 |
| 14 | 70+ | 60-90 | 8-10 |

### Desired outcomes across 20 internal runs

- Easy win rate: 70-85% after tutorial.
- Normal win rate: 45-65%.
- Hard win rate: 20-40%.
- Average Normal run: 12-16 minutes.
- No single ability used for more than 45% of all casts.
- At least two branches used in 80% of winning runs.
- Players can explain at least one cause of Suppression growth after one run.

### Initial tuning levers

1. Dread generation.
2. Ability costs and cooldowns.
3. Exposure generated by abilities.
4. Event cadence.
5. Hostile event severity.
6. Suppression formula.
7. Influence target.

Change one category at a time during early testing.

---

## 19. Testing Strategy

### Unit tests

- Global Influence calculation uses population weights correctly.
- All values clamp to 0-100.
- Ability validation blocks illegal targets and insufficient resources.
- Cooldowns recover correctly at 0x, 1x, and 2x speed.
- Seeded random sequences are repeatable.
- Director respects active-event and repetition limits.
- Win and loss conditions trigger exactly once.
- Save/load round trip preserves state.

### Deterministic scenario tests

Create fixtures:

- `early_game_safe.json`
- `high_exposure_crackdown.json`
- `awakening_near_loss.json`
- `all_regions_influenced.json`

Advance each fixture for fixed time using a known seed and assert expected ranges.

### End-to-end tests

- Start a run and place a foothold.
- Cast Whispered Dreams and observe Dread reduction and Influence increase.
- Unlock an ability.
- Pause and verify the simulation stops.
- Save, refresh, and resume.
- Reach a scripted win.
- Reach a scripted loss.

### Playtest questions

- Did you understand what Global Influence and Suppression represented?
- Did you know why a hostile event appeared?
- Did you feel you had a meaningful response to it?
- Which ability felt most and least useful?
- At what moment did the game feel too slow or too chaotic?
- Would you start another run with a different strategy?

Record run length, casts per ability, event outcomes, win/loss, and final meters in a downloadable debug JSON file.

---

## 20. Development Plan

### Milestone 1 - Playable map shell

- Vite/React/TypeScript scaffold.
- Main layout and responsive minimum width.
- SVG map with eight clickable regions.
- Region detail panel.
- Start/restart and difficulty selection.

**Exit criteria:** The player can select regions and begin a run with a seeded state.

### Milestone 2 - Core simulation

- Dread generation.
- Regional state.
- Global Influence and Suppression.
- Fixed-step simulation and speed controls.
- Starting foothold.

**Exit criteria:** A run progresses over time and can reach a temporary debug win/loss.

### Milestone 3 - Abilities and progression

- Six abilities.
- Cooldowns and target validation.
- Insight milestones.
- Upgrade panel.
- Floating deltas and log messages.

**Exit criteria:** Multiple strategies are possible and all actions have clear feedback.

### Milestone 4 - Event director

- Eight events.
- Event timing and severity.
- Ability-event interactions.
- News ticker.
- Awakening phase.

**Exit criteria:** A full 12-18 minute run has pressure, variation, and a climax.

### Milestone 5 - UX, save, and tests

- Tutorial.
- Audio placeholders and mute.
- Local save and settings.
- Unit and Playwright coverage.
- Debug overlay and telemetry export.
- Balance pass.

**Exit criteria:** A new tester can play without developer help, complete a run, and report actionable feedback.

---

## 21. Definition of Done for the MVP

The MVP is complete when:

- It runs locally with a documented `npm install` and `npm run dev` flow.
- A full run is playable using only the mouse.
- Easy, Normal, and Hard presets work.
- The player can use all abilities, encounter all events, win, and lose.
- The game is deterministic when given the same seed and command sequence.
- The interface clearly communicates costs, effects, cooldowns, and state changes.
- No runtime API keys or backend are required.
- Refreshing can resume a saved run.
- Core tests pass in one command.
- The README explains architecture, controls, rules, testing, and balance constants.

---

# Codex Implementation Prompt

Copy the prompt below into Codex at the root of a new repository.

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

---

## 22. First Playable Build Checklist

Before adding final art or more content, verify this exact slice:

- [ ] Start Normal difficulty with a fixed seed.
- [ ] Place a foothold in Oceania.
- [ ] Dread begins increasing.
- [ ] Cast Whispered Dreams on South Asia.
- [ ] Region and global meters update with visible deltas.
- [ ] First event appears between 60 and 90 seconds.
- [ ] Use an ability that interacts with the event.
- [ ] Earn Insight at a regional milestone.
- [ ] Unlock one new ability.
- [ ] Pause and resume without state drift.
- [ ] Force Awakening through the debug panel.
- [ ] Complete both a win and a loss.
- [ ] Export the run state as JSON.
- [ ] Reload and resume a saved run.

This is the vertical slice. Do not expand the region count, ability count, or event count until this sequence is stable and enjoyable.
