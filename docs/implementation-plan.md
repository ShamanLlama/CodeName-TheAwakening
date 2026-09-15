# Implementation plan — revised Cthulhu GDD

**Status:** Original code assessment and implementation sequence, followed by the user-authorized Alignment 1 build. See [implementation status](implementation-checklist.md) for what is now implemented and verified. The baseline measurements below describe the earlier assessment.

**Design authority:** [GDD](gdd/README.md). This plan explains how to implement it; proposed defaults below do not silently become approved GDD decisions.

## 1. Recommendation

Keep the React/TypeScript application, the eight-region map, the pure simulation boundary, seeded randomness, and existing test tools. Replace the legacy game rules in coordinated stages.

The current architecture is sufficient for this revision. No new engine, backend, state-management library, or general-purpose simulation framework is needed.

The major work is the meaning and progression of state: three resources, a monthly clock, population response, regional publicity, opposition, ritual preparation, and deadline-based victory. Renaming labels or retuning old costs will not implement the new game.

## 2. Verified baseline

During this planning pass:

| Check | Result | What it establishes |
|---|---|---|
| npm test | 29 tests passed across 5 files | Existing engine, director, persistence, and legacy scenarios satisfy their current assertions |
| npm run build | Passed, including TypeScript checking | Current source compiles and produces a browser bundle |
| npm run lint | Passed | Existing lint checks pass |
| npm run e2e | 5 Chromium tests passed | Setup, casting, Insight unlocking, pause/reload, and scripted end screens work under the old rules |
| Browser screenshot | Inspected at 1280 × 720 | Current three-panel layout and eight-region schematic can serve as the starting UI |

The browser win/loss tests use development controls. They do not prove that an ordinary player can win, that the economy is balanced, or that any of the proposed strategies is viable.

Tests regenerated ignored build/test artifacts. Application source, dependencies, and the existing design edits were preserved.

## 3. Code-to-design assessment

| Area and current source | Current behavior | Planned treatment |
|---|---|---|
| [types.ts](../src/game/types.ts), [contracts.ts](../src/game/contracts.ts) | Version-1 state contains seconds, Insight, regional/global influence, Suppression, and a 45-second finale | Replace obsolete fields with explicit monthly state and the new resource/population/ritual contracts |
| [engine.ts](../src/game/engine.ts), advanceSimulation | Pure whole-second loop; browser supplies speed-adjusted steps | Keep the pure stepping boundary; use whole months and a documented update order |
| engine.ts, dreadPerSecond | Cults and influence generate Dread automatically; inactivity after 45 seconds increases income by 20% | Cults generate spendable Influence; sacrifices and eligible events generate Dread. Remove the inactivity bonus |
| engine.ts, castAbility | Sacrament of the Deep spends Dread and adds cult strength, influence, instability, and exposure | Replace its economic role with sacrifice that removes local followers and grants Dread |
| engine.ts, rewardMilestones/unlockAbility/unlockUpgrade | Influence milestones and event outcomes award Insight; Insight buys abilities and upgrades | Remove Insight and all its award paths; use existing resources and limited specialization |
| [derived.ts](../src/game/derived.ts) | Population-weighted influence is both an important display value and victory gate | Derive total followers, income, live population shares, awareness effects, and ritual readiness |
| [director.ts](../src/game/director.ts) | Seeded weighted events; pressure and severity depend on seconds and global influence; Exposure contributes to Suppression | Retain seeded selection and event lifecycle patterns; retarget pressure using local evidence, public activity, worldwide awareness, and ritual preparation |
| engine.ts, startAwakening | Requires target influence and 100 Dread; resolves after 45 seconds | Replace with ritual-network readiness and alignment-window rules |
| [abilities.ts](../src/data/abilities.ts) | Single Dread cost per ability and Insight unlock costs | Support Influence/Dread/local-follower costs where relevant; replace percentage-growth effects with actual recruitment and belief transitions |
| [regions.ts](../src/data/regions.ts) | Eight region IDs, adjacency, weights, and traits | Keep IDs, map connections, and data-driven traits; add initial population composition and balance-specific recruitment/opposition modifiers |
| [events.ts](../src/data/events.ts), [difficulties.ts](../src/data/difficulties.ts) | Second-based timers, influence eligibility, 60/70/80% victory targets | Define month-based durations, evidence/publicity eligibility, and response/recruitment tuning. Remove legacy victory thresholds |
| [App.tsx](../src/App.tsx) | 250 ms interval, old HUD/tutorial, map colored by influence, inline action rendering | Update calendar, resource HUD, map states, action previews, threats, rituals, tutorial, and result explanations |
| App.tsx, abilityPreview and map validity | Calls the full pure cast function for previews and every target; simulated RNG results are discarded | Keep preview free of state/RNG consumption and avoid disclosing hidden random results; share validation and deterministic cost/effect helpers |
| [persistence.ts](../src/game/persistence.ts) | Strict version-1 schema; invalid saves are deleted; results measured in seconds/influence/suppression | Introduce version-2 storage without deleting the version-1 save; preserve compatible settings and separate old results |
| [game tests](../src/game/engine.test.ts), [scenario policies](../src/tests/playthrough.ts), [browser tests](../e2e/game.spec.ts) | Earlier expectations intentionally encoded legacy rules | Retain meaningful determinism/validation coverage, replace outdated rule assertions, and add new scenario and browser journeys |

### Dependencies that make this a coordinated change

- Renaming Influence affects the engine, event eligibility, difficulty, previews, map, tutorial, telemetry, save validation, and tests.
- Converting seconds affects every cooldown, event, resistance modifier, log entry, save field, debug control, and end screen.
- Removing Suppression changes both event outcomes and defeat handling. Global Awareness cannot inherit the same automatic-loss behavior.
- Ritual state must be independent of temporary event slots. A positive event must not occupy a region's slot and prevent the required response to a public ritual.
- The current UI mostly lives in App.tsx. Extract only the components actually being changed, such as the HUD, region panel, and action panel, as their complexity warrants it.

## 4. Proposed defaults for the first coherent build

These recommendations resolve enough ambiguity to plan implementation. They are test settings, not claims about final balance.

### Calendar and clock

- Preparation: 600 months, corresponding to the GDD's provisional 50 years.
- Alignment window: propose 24 additional months.
- Final ritual: propose 12 supported months within that window.
- Playback: begin testing at 2 real seconds per month, with pause, 1×, and 2×. A full 624-month run takes about 20.8 minutes at 1× before pauses.
- Display “Year 1, Month 1” initially. Keep the historical start year optional.
- Auto-pause when the alignment opens so the player can inspect readiness. This does not change the simulation deadline.
- Pause on a hidden browser tab. No offline income or background catch-up for the first build.

Use elapsedMonths as the simulation authority. Calendar labels are derived from it. Browser timing belongs outside game rules; use elapsed real time rather than assuming every interval callback arrives exactly on schedule.

Reset the browser's fractional tick remainder on a new run. A paused state still permits valid immediate player commands, as in the current prototype; only time-based effects stop. Large step requests stop at terminal states and at the alignment-opening pause boundary.

### Population and follower accounting

Recommend absolute abstract counts per region:

- unaware
- deniers
- believers
- followers
- cumulative losses, used only for accounting

Use a fixed initial population capacity based on existing population weights. A concrete test scale is populationWeight × 100, giving 10,000 abstract population units worldwide. This is a game abstraction, not a demographic estimate.

Cult strength is the follower count, exposed through a selector rather than stored a second time. The global cult-strength resource is the sum of regional followers.

Recruitment transfers population into followers. Sacrifice removes followers into cumulative losses. Live shares are calculated from the remaining live population; living groups plus cumulative losses equal initial capacity. No births or demographic simulation in this prototype. An empty region displays zero live population without division by zero.

Distinguish casualties from departures: a raid's explicitly lost followers enter losses; a defection returns to the defined living group. Never silently turn sacrificed followers into believers or refill a depleted region.

Recommend that ordinary recruitment cannot recruit deniers, while a costly manifestation can transfer some into belief. This remains the main design alternative to the user's original permanent-denial idea; keep its conversion rate independently tunable, including zero.

### Economy and actions

- Global Influence and Dread; regional followers.
- Followers generate Influence each month. No baseline passive Dread or inactivity bonus.
- Start with a functioning hidden cell and enough initial resources for a recruitment or expansion choice.
- Low-rate automatic recruitment operates in functioning cults; player actions accelerate it. The player should not have to press Recruit every month.
- Sacrifice immediately consumes local followers, provides Dread, and raises local Exposure.
- Manifestations consume Dread and shift belief; they do not automatically refund Dread.
- Use Influence for recruitment campaigns, spreading to an adjacent region, concealment, and institutional intervention.
- Action previews show local followers remaining and monthly income changes after sacrifice.
- Core actions are available from the start. Unlock only advanced options; use existing resources and one committed specialization choice rather than introducing a replacement Insight currency.

Keep all prices, income coefficients, recruitment rates, and caps in a small balance configuration. Choose them together with a resource budget for a complete run; independent guessed costs can make the awakening mathematically unreachable.

### Visibility, belief, and opposition

Separate deliberate public activity from observed Exposure:

- A regional hidden/public stance controls ongoing worship and recruitment benefits.
- Exposure is evidence, and Global Awareness affects response intensity.
- Returning to hidden activity ends its public benefit, but does not erase evidence or public knowledge.
- Revealed ritual preparations remain targetable. Closing public worship cannot conceal an ongoing great ritual.
- Public worship should begin testing with roughly twice hidden recruitment, plus increased evidence and threat pressure; the multiplier is a hypothesis.
- Non-cult believers contribute to opposition using a regional resistance coefficient. Belief alone is not equivalent to cult loyalty.
- Madness provides a bounded supernatural/recruitment modifier and can increase hostile response. Defer random autonomous cult behavior until its value is demonstrated.

Use targeted, warned threats with actionable deadlines. Select and display a threat's target when it is scheduled, rather than waiting until its impact.

Give hidden cells a discoverable evidence path without making them automatically known worldwide. Keep major ritual responses outside optional opportunity-event capacity, with a bounded queue or reserved threat slot so event congestion cannot grant immunity.

### Rituals, recovery, and final resolution

Propose three prepared sites in distinct regions. Initially any three eligible regions work; use the existing adjacency graph for expansion without adding a second mandatory graph puzzle.

Each site requires local followers, an upfront resource commitment, and sustained preparation. Start testing with 24 preparation months and a tunable minimum follower support. Site count, costs, and required followers belong in balance data.

Site state: unprepared → preparing → prepared, with interrupted as a recoverable state. Losing support suspends progress; restoring it resumes progress. No automatic refund or repeated reward when starting/stopping. Destroyed sites lose their local preparation, while losing one cell leaves the rest of the network intact.

At alignment the player starts the final ritual if enough prepared sites are supported and the final Dread cost can be paid. Deduct that cost once. The final ritual counts supported months; interruption freezes progress while calendar time keeps advancing. Unsupported sites can be replaced by another prepared site.

Recommended monthly resolution order:

1. Advance the month and expire cooldowns/timed effects.
2. Resolve effects of existing threats and losses.
3. Resolve income, recruitment, belief, and exposure changes.
4. Recheck local support and advance site/final-ritual progress.
5. Resolve legal completion as victory.
6. If still active, resolve the deadline as defeat.
7. Schedule future threats and opportunities, which cannot damage the cult in the month they are created.

At elapsed month 600, the alignment opens after preparation processing; final-ritual ticks run during months 601–624. Completion on month 624 wins if support survived that month's threats; otherwise the run loses. No actions are accepted after the terminal state.

Use deadline defeat only in the first build. A wiped-out cult can establish a new cell if Influence and eligibility permit; otherwise offer restart without claiming a formally proven early defeat. An automatic elimination rule can follow once all recovery paths are defined.

### Prominent people

Add generic fictional celebrity and politician opportunities after the main loop works:

- Influence pays conversion.
- A secret celebrity provides ongoing Influence; a secret politician reduces local investigative pressure.
- Public revelation ends the secret benefit and provides a recruitment opportunity plus backlash.
- Represent the awakening contribution as a one-time grant of preparation progress to a selected eligible site; it cannot remove the need for supported sites and the final ritual.
- Track converted/revealed/rewarded state so repeated clicks cannot repeat grants.
- Defer sacrificing prominent figures and named stories; ordinary follower sacrifices already test the core cost.

These effects distinguish prominent-person conversion from an ordinary recruitment button without building a character simulation.

## 5. Implementation sequence and completion gates

Every stage includes its necessary UI and tests. Stage 1 is a partial development build; stage 3 is the first complete new-rules run. Do not present intermediate builds as GDD-complete.

### Stage 1 — Monthly economy and safe state replacement

**Depends on:** The population representation and initial clock/economy defaults above.

**Change:** State types/contracts; calendar and browser stepping; derived resource selectors; starting foothold; recruitment/expansion/sacrifice actions; version-2 persistence; minimum HUD and region-panel changes. Remove Insight awards, the old influence percentage, idle Dread bonus, and conflicting legacy finale in the same coordinated revision.

**Gate:** Start, grow, expand, sacrifice, pause, save, and reload a monthly run. Sacrifice measurably reduces subsequent income; no action overspends. The countdown expires correctly. Old data remains intact.

**Evidence:** Population accounting tests, action failure immutability, income-after-sacrifice scenario, year rollover, pause/speed checks, and save/resume continuity.

### Stage 2 — Belief, revelation, and humanity's response

**Depends on:** Stage 1.

**Change:** Hidden/public stance; population transitions; public recruitment; manifestations; regional Exposure; Global Awareness; targeted investigations/raids/occult interference; threat warnings, map indicators, and reaction logs.

**Gate:** A public region gains a real benefit and a visible threat. Another hidden region remains concealed unless its own discovery conditions are met. The player can counter or absorb a threat. A denier-heavy region behaves differently from an unaware one.

**Evidence:** Shared-seed hidden/public comparisons, denial conversion on/off, no publicity-toggle evidence reset, and no event-slot immunity for mandatory responses.

### Stage 3 — Ritual preparation and a complete awakening run

**Depends on:** Stages 1–2.

**Change:** Site actions and support checks; interruption/resumption; alignment transition; final ritual; victory/deadline results; calendar tutorial; debug controls for boundary scenarios.

**Gate:** A normal command sequence can build a supported network and win. An unsupported or late attempt loses for a stated reason. A damaged site can recover if enough time remains. No global Influence threshold or Suppression=100 loss remains.

**Evidence:** Full engine journey using legal actions, three-site support scenarios, resource affordability, deadline equality, loss of support in the final month, and a browser journey through the same action flow. Debug-generated ready states are useful boundary tests but are insufficient evidence of a playable economy.

### Stage 4 — Prominent figures, specialization, and replayability

**Depends on:** A complete Stage 3 run.

**Change:** Generic conversion events; secret/public outcomes; one-time site contribution; a small mutually exclusive specialization choice; rewritten regional/event balance; run summary and telemetry.

**Gate:** The early-public, late-surge, and public-hub strategies each have a plausible successful command sequence under suitable shared test conditions. The player has a meaningful reason to preserve or sacrifice followers and to reveal at different times.

**Evidence:** Deterministic strategy scenarios and human playtests. Record revelation timing by region, follower sacrifices/losses, resource income/spending, threat responses, site readiness, and reason for defeat. Include seed and ruleset version in exports.

After the common Normal baseline works, tune Easy and Hard through recruitment, threat pressure, and warning time. Do not quietly shorten the calendar while comparing strategies.

## 6. Save compatibility and rollback

The affected persistent data is the browser's LocalStorage envelope. Inspected source routes load/save through persistence.ts; the current browser test confirms local save/resume. No backend migration is present in this project.

**Observed hazard:** loadRun deletes SAVE_KEY when decoding fails. A version bump on the same key could remove both the old run and the settings/results in that envelope.

**Recommended change:**

1. Write new runs under a new key, such as cthulhu-awakening-save-v2, with version 2.
2. Leave cthulhu-awakening-save unchanged.
3. If no v2 save exists, read the v1 envelope only to copy validated audio settings. Reset tutorial completion because the rules changed.
4. Do not reinterpret an active v1 run as a v2 run or compare its best result with new-rules results.
5. Explain once in setup that the earlier run is retained but cannot resume under the revised rules.
6. On malformed v2 data, preserve the raw value and show recovery/new-run behavior before replacing it. Storage-access failures should fall back to an in-memory session with an explicit unsaved notice.

Verify using an in-memory Storage stub and isolated browser contexts. Never clear the user's real browser storage to test this migration.

Rollback is restoring the earlier application revision, which continues using the preserved v1 key. Keep v2 data separate for a later retry. No package changes or dependency upgrades are required by this plan.

## 7. Validation and implementation boundaries

For each gameplay stage, run focused tests for changed rules. At each integrated milestone, run the existing test, build/typecheck, lint, and browser commands.

Prioritize observable invariants:

- Deterministic seed plus command sequence, including save/load.
- One population source of truth and no sacrificed followers reappearing.
- No stale seconds-based timers after the calendar migration.
- Legal actions only; failed actions change neither resources nor RNG.
- Previewing/highlighting does not advance simulation or reveal hidden RNG.
- Public benefits, pressure, interruption, and recovery remain legible.
- No repeated grants from conversion, ritual resumption, or event resolution.
- Consistent final-month completion and deadline resolution.
- No old save deletion and no old statistics presented as new-rules results.
- No debug controls in the production build.

During playtests, record idle periods and action frequency as well as wins. A 600-month run with repetitive waiting or constant Recruit clicks fails the intended experience even if its tests pass.

Do not build more gods, detailed characters, historical simulation, or a large content library to compensate for a weak core loop. Tune the cost of growth, visibility, sacrifice, and preparation first.

## 8. Decisions still needing design validation

The main recommendations for owner review are:

- Absolute population counts, with followers directly representing cult strength.
- Deniers change only through manifestations, versus remaining permanently unconvertible.
- Two real seconds per month and a 24-month alignment window.
- Three supported ritual sites, with interrupted progress retained.
- Deadline-only automatic defeat for the initial revision.
- Generic prominent people after the complete core loop.

Exact costs, recruitment coefficients, support thresholds, and opposition rates are implementation tuning work, guided by a feasible run budget and playtest evidence.

The user subsequently authorized implementation. Stages 1–4 are available in the Alignment 1 playtest build; human balance and experience validation remains open. See the implementation status linked above.
