# The Awakening of Cthulhu — Game Design Document

**Status:** Design baseline for the next prototype revision; gameplay balance is unvalidated.

**Updated:** 2026-09-05

**Audience:** The project owner and the developer implementing the next revision.

**Canonical document:** This file supersedes the previous MVP GDD for future design work.

**Implementation update (2026-09-06):** The user authorized code changes after reviewing the plan. The Alignment 1 playtest build implements this baseline using the documented prototype defaults. See [implementation status](docs/implementation-checklist.md) for verification and remaining playtest work.

The previous design is preserved in [the legacy prototype GDD](docs/archive/GDD_legacy_prototype.md). Its old completion results describe that earlier game. The revised build has separate verification; neither set of automated tests proves gameplay quality.

## 1. Purpose and design status

Build a single-player, Plague Inc.-inspired world-map strategy game in which the player is Cthulhu, still asleep, spreading cults and supernatural influence until the conditions for awakening are fulfilled.

The central experience is choosing how to grow, when and where to become visible, what to sacrifice, and how to respond as humanity fights back. Replayability should come from different viable strategies and changing circumstances.

This document distinguishes:

- **Agreed:** Explicit decisions or direction established in the design conversation.
- **Proposed:** A concrete design recommendation consistent with that direction, ready to test but not separately approved in every detail.
- **Tuning / open:** Values or rules that still need a decision or evidence.

No mechanic is labeled “proven fun.” Existing code can provide a technical foundation; player choices, balance, pacing, and replayability require playtests.

## 2. Agreed decisions

| Area | Decision |
|---|---|
| Player role | The player is the awakening old god, initially Cthulhu. |
| Main interaction | Manage cults, resources, and conflict with humanity on a world map, in the spirit of Plague Inc. |
| Design priority | Interesting choices and replayability take priority over adding large amounts of content. |
| Economy | Manage three main resources. The proposed resource definitions are in section 5. |
| Sacrifices | Consume the player's own followers, trading the cult's future capacity for immediate benefit. |
| Concealment | Staying hidden all game should not be the universally best strategy. Visibility needs worthwhile rewards. |
| Population response | Disbelief and belief matter to recruitment and the world's reaction. Exact transitions remain proposed. |
| Prominent people | Converting famous people or politicians can help awakening but can expose the cult and provoke recruitment backlash. |
| Character scope | Detailed named cultists and their personal stories come later. |
| Run clock | Time advances in months across years toward a celestial alignment. |
| Deadline | The player must awaken within the alignment opportunity or lose the run. |
| Calendar flexibility | Dates can change. Fifty years is a suggested starting duration, not a locked requirement. |
| Strategic variety | Support early public expansion, a late coordinated revelation, and public strongholds supported by hidden cells as strategy targets. |
| Future expansion | Other old gods can be added after Cthulhu's game works. |
| Immediate task | Write this GDD first; revise game code only after the user's subsequent instruction. |

The three strategy targets are hypotheses to validate, not a promise that they are already equally viable.

## 3. Player fantasy and design pillars

> I am an ancient intelligence beneath the sea. Through dreams, worship, influence, and sacrifice, I prepare humanity for my return before the stars move beyond reach.

1. **World-scale decisions:** Regions, cult networks, and institutions are the focus.
2. **Power has a cost:** Growth, concealment, sacrifice, and revelation compete for resources and time.
3. **Escalation is a choice:** The player chooses where to expose power, while humanity can also discover hidden activity.
4. **Readable consequences:** Players can understand likely gains, costs, threats, and the reason for a changed outcome.
5. **Replayable strategies:** Different starting conditions and responses change which decisions are useful.
6. **Cosmic horror through play:** Dreams and rituals gradually become visible changes to the world.

## 4. Scope of the next prototype

### Proposed retained foundation

Reuse the existing browser-based, single-player project and its eight-region map as the starting point. Retain pause, speed controls, regional inspection, seeded events, a news log, and local saving where compatible.

These are economical implementation defaults from the existing project, not new platform commitments from the conversation.

### Core systems to validate

- Three-resource economy.
- Recruitment and cult growth.
- Sacrifice of existing followers.
- Regional concealment and public activity.
- Belief, denial, and recruitment backlash.
- Regional opposition and growing worldwide awareness.
- Monthly progression toward the alignment.
- Ritual preparation and an awakening finale.
- Limited progression choices and events that support the three strategy targets.

### Deferred scope

- Additional playable gods.
- Individual cultist simulation, character relationships, and named-character stories.
- Multiplayer, accounts, backend services, or live operations.
- Country-by-country simulation, full diplomacy, and detailed historical change.
- A large campaign, extensive upgrade trees, or production-quality cinematics.

Prominent-person conversion belongs to the design, but the smallest test can use a generic “celebrity” or “politician” opportunity. It does not require the deferred character-management system.

## 5. Proposed three-resource economy

The resource count is agreed. These names and detailed relationships are the proposed baseline.

| Resource | Meaning and acquisition | Main uses | Strategic tension |
|---|---|---|---|
| Cult strength | The local pool of committed followers, increased through recruitment | Maintains cells, supports recruitment and rituals, generates Influence, and is consumed by sacrifices | Preserve future capacity, expand, or spend followers for immediate power |
| Influence | Human connections and organizational reach, generated by functioning cult networks | Establish or expand cells, infiltrate institutions, convert prominent people, and interfere with investigations | Growth versus protection |
| Dread | Usable supernatural power, obtained through eligible supernatural events and sacrifices | Nightmares, manifestations, supernatural interventions, and awakening preparation | Immediate advantage versus progress toward victory |

### Representation

**Proposed:** Cult strength is regional. Influence and Dread are global spendable pools. The HUD shows total cult strength, while every sacrifice identifies the region whose followers will be lost.

Cult strength represents abstract follower units in the prototype, not an accurately simulated human headcount. Its display, recruitment rules, and sacrifice costs must use a consistent scale.

A region with more followers can support more activity. Exact income formulas, caps, recruitment rates, and costs remain tuning decisions.

### Values that are not additional currencies

- **Awakening progress:** Completion of the requirements for victory.
- **Regional Exposure:** Evidence and attention surrounding a local cult.
- **Global Awareness:** Humanity's recognition of a worldwide supernatural threat.
- **Belief composition:** Regional population categories that affect behavior.
- **Madness:** A regional supernatural condition affecting recruitment, rituals, and potentially control.
- **Time remaining:** Months until the alignment and its closing deadline.

Keep these values readable without presenting them as six more resources to collect.

### Economic constraints

- Sacrifices remove followers immediately and reduce their future contribution.
- No action can spend more followers or currency than available.
- A player must have a feasible starting route into recruitment and resource generation.
- Dread-funded manifestations must not create an unlimited self-funding power loop.
- Preparation consumes time; preserving every resource cannot be the best answer indefinitely.
- Avoid adding a fourth progression currency. The legacy Insight system is not part of this baseline.
- “Influence” now means spendable human connections. The legacy global Influence percentage must not retain the same name with a conflicting meaning.

## 6. Core loop and sacrifice

### Monthly decision loop

1. Inspect the countdown, regional opportunities, and active threats.
2. Choose whether to recruit, expand, infiltrate, hide activity, reveal power, sacrifice, or prepare a ritual.
3. See costs, expected benefits, timing, and exposure consequences before committing.
4. Advance time and receive regional income, action results, and world responses.
5. Adapt to recruitment changes, discoveries, lost followers, and new opportunities.
6. Convert the surviving network's resources into readiness for the alignment.

The proposed presentation is continuous play with pause and speed controls; game rules resolve in months. Whether any actions also resolve immediately is an implementation decision to specify consistently.

### Sacrifice

**Agreed:** Sacrifices consume the player's own followers.

**Proposed baseline:** A sacrifice spends local cult strength to generate Dread and adds local Exposure. Dread can then fund awakening preparations or other supernatural actions.

The UI must explain the immediate gain and the resulting loss of productive cult strength. Sacrifice should be useful in some situations and damaging in others.

Example: a cult about to be raided might sacrifice followers to complete a distant ritual, giving up any chance to preserve that local network. A safe, growing cult might be more valuable alive.

Sacrificing the final followers must have an explicit result: the cell becomes inactive, its ongoing support ends, and any dependent ritual shows whether it is interrupted. The exact minimum-garrison rule remains open.

## 7. Proposed population model: disbelief, belief, and worship

The user's proposed distinction is that some people accept the old god's reality and may convert, while deniers resist conversion. The following model adds a way for belief to strengthen opposition as well.

| Group | Behavior |
|---|---|
| Unaware | Has not recognized the threat; available to hidden recruitment and first revelations |
| Deniers | Dismisses the cult's claims and resists ordinary recruitment; may also undermine warnings about the cult |
| Believers | Accepts the supernatural threat; may join the cult or support resistance |
| Cult followers | Worships the god and supplies the local cult network |

Belief is not loyalty. Revealing Cthulhu can create recruits and determined enemies in the same region.

### Proposed transition rules

- Quiet recruitment can move some unaware people into the cult.
- Revelations can move unaware people and some deniers into belief.
- Cult activity can convert some believers into followers.
- Hostile responses and backlash can reduce recruitment and cult strength.
- Believers outside the cult can strengthen opposition without becoming a fifth population category.
- Deniers obstruct recruitment but can help hidden cells by dismissing evidence.

**Unresolved:** The user initially suggested deniers never convert. The recommended prototype rule is that they resist ordinary recruitment but some can change after an unmistakable manifestation. Permanent denial versus difficult conversion needs validation and owner confirmation.

If shares are used, they must remain nonnegative and total 100%. Sacrifices reduce cult strength; the later implementation must define how abstract follower units correspond to these shares without inventing population from rounding or counting casualties as converts.

## 8. Concealment, revelation, and the world response

### Regional exposure and global awareness

**Proposed:** Track local Exposure separately from Global Awareness.

A cult can act openly in one region and remain hidden in another. Major revelations increase worldwide awareness, making other regions more alert, but do not automatically disclose every hidden cell.

Concealment is a temporary strategic advantage. Hidden cells can still be discovered through investigations and accumulated evidence.

### Rewards for visibility

| Action | Reward | Cost or risk |
|---|---|---|
| Public worship | Faster recruitment and greater organizational reach | Easier targeting, raids, and recruitment backlash |
| Manifestation | Breaks some disbelief and creates opportunities for recruitment and rituals | Creates believers who resist, increases awareness, and consumes Dread |
| Great ritual | Substantial progress toward awakening | Reveals its location, commits resources, and must survive interruption |
| Public conversion of a prominent person | Recruitment or awakening benefit from public influence | Scrutiny, polarization, and resistance among parts of the population |

These are proposed mechanics. Their benefits must justify the additional danger; cosmetic exposure penalties alone do not create a strategy.

### Opposition

Humanity should respond to observable player behavior:

- Investigators follow evidence and discover cells.
- Authorities disrupt public cult activity and raid exposed networks.
- Occult organizations interfere with manifestations and major rituals.

The prototype can implement these as regional threats and events rather than independent faction simulations.

Threats need visible targets, consequences, and response opportunities. Possible player responses include hiding activity, spending Influence, relocating investment, countering a threat with Dread, or accepting a local loss.

Discovery should not automatically end the run. A public strategy must be survivable when managed well.

### Avoiding a dominant “hide until the final moment” strategy

Use a combination of:

- Real rewards from public activity.
- Preparations that require visible actions and take time.
- Opportunities that expire.
- Investigations that can progress despite concealment.
- The fixed alignment deadline.

Do not make late revelation impossible. A planned late surge should work when preparation supports it; passive hiding followed by an instant victory button should not.

## 9. Prominent-person conversion

**Agreed concept:** Converting famous people or politicians can help awakening, but visibility and backlash can make recruitment harder.

**Proposed minimal implementation:** Generic opportunities for a celebrity or politician, without individual character simulation. Fictional figures are the recommended content default; specific real people are not required.

| Use of a converted figure | Proposed benefit | Proposed consequence |
|---|---|---|
| Keep allegiance secret | Ongoing Influence or help obstructing investigations | Discovery risk; no large public recruitment benefit |
| Reveal allegiance | A recruitment opportunity and a bounded awakening contribution | Local backlash and broader attention |
| Sacrifice the converted figure | A one-time Dread benefit | Permanent loss of their ongoing benefit and a stronger response if exposed |

A prominent sacrifice is a later extension of the agreed follower-sacrifice rule, not essential to the first economy test.

An ordinary converted figure must not award the same awakening benefit repeatedly. The secret-versus-public choice, eligibility, costs, discovery rules, and effect sizes are still proposed.

The UI should distinguish “more potential recruits” from “everyone becomes easier to recruit.” A public endorsement can attract one group while hardening another against the cult.

## 10. Monthly calendar and celestial alignment

### Agreed structure

Time progresses in months across years. The alignment provides a deadline for awakening. Calendar years are flexible.

### Proposed starting configuration

- Preparation lasts **50 years / 600 months**.
- The display shows the current year and month and the number of months remaining.
- The start date is configurable flavor; 1890 and 2000 are not fixed requirements.
- Pause stops the simulation clock and all simulation-based timers.
- Speed controls change real-time playback, not game rules.
- A short alignment window follows preparation; its duration is still open.

The 600-month value is a test configuration. Real seconds per month and target real-world session length are not yet decided. They must be tested together so the game does not require hundreds of repetitive clicks.

The calendar describes strategic time. It does not require simulating every historical change over fifty years.

### Proposed run phases

1. **Whispers:** Establish a hidden foothold and a functioning economy.
2. **Expansion:** Build networks, choose investments, and exploit regional opportunities.
3. **Revelation:** Commit to public power and defend visible preparations.
4. **Alignment:** Complete and protect the final awakening effort before the window closes.

These are an intended experience, not mandatory fixed-year gates. Early revelation and late surges must remain possible.

## 11. Awakening, victory, and defeat

### Agreed outcome

Win by awakening Cthulhu during the celestial opportunity. Lose if that opportunity closes without awakening.

### Proposed victory mechanism

Build and maintain a network of prepared ritual sites. Sites require local cult support, resources, and time, and expose themselves during major preparation.

At alignment, surviving prepared sites and sufficient committed power allow the final ritual. The player must protect the required support until awakening completes.

A smaller, well-prepared network should be a plausible alternative to widespread public worship. Global recruitment percentage alone is not the proposed victory condition.

**Open:** Number and placement of sites, preparation cost, minimum support, resource commitments, final ritual duration, and the alignment window length.

### Proposed failure and interruption rules

- Losing support interrupts the affected ritual, with a clear explanation.
- Interrupted preparations should have a defined recovery path before the deadline; refund and progress-retention rules remain open.
- Loss of one region does not automatically defeat the entire cult.
- Closing the alignment window without completing awakening ends the run.
- Total cult elimination may provide an earlier defeat condition if no recovery action remains; this needs a precise rule.
- Global Awareness is not an automatic defeat meter in this proposal.
- There must be a documented resolution order when ritual completion and the deadline occur in the same month.

The legacy rule of reaching 60/70/80% global Influence, spending 100 Dread, and surviving 45 seconds is superseded as future design. Do not combine it silently with the monthly ritual system.

## 12. Strategy and replayability targets

| Strategy | Investment | Advantage | Weakness |
|---|---|---|---|
| Early public expansion | Early revelation and recruitment | Larger network and more time to benefit from visible power | Long exposure to opposition |
| Late coordinated revelation | Hidden growth and advance preparation | Lower early pressure and a concentrated push | Little recovery time if the surge fails |
| Public stronghold with hidden support | An exposed productive center plus concealed regional cells | Public power with reserve networks | Dependence on a defended hub and its support |

All three should be feasible in suitable conditions. Equal win rates in every seed are not required.

### Proposed replayability sources

- Starting regions with different growth opportunities and opposition.
- Event sequences that change the value and timing of actions.
- Limited upgrade choices that encourage specialization.
- World responses tied to the player's behavior.
- Different timings and locations for revelation and sacrifice.

Potential specializations are recruitment, infiltration, and supernatural terror. The exact upgrade structure remains open. Avoid allowing every run to acquire all benefits without tradeoffs.

Use seeded randomness for repeatable testing. Signal major threats and opportunities so a changed outcome can be explained by choices and risk, rather than an unseen random punishment.

## 13. Interface, feedback, and atmosphere

### Proposed main screen

- World map with selectable regions.
- Three-resource HUD.
- Calendar and clear alignment countdown.
- Regional detail panel showing cult strength, population response, Exposure, active threats, and ritual readiness.
- Actions with costs, timing, expected effects, and eligibility.
- Global Awareness and awakening readiness indicators.
- News and cult reports explaining important state changes.
- Pause and speed controls.

Unavailable actions must explain why. Actions that fail validation must not consume resources or advance their own progress.

Before a sacrifice, show both the gain and remaining followers. Before revelation, show known benefits and backlash risks. If outcomes are uncertain, show the uncertainty rather than a misleading exact promise.

### Atmosphere

Cthulhu's approach should change headlines, map treatment, sound, and environmental signs: shared dreams, strange tides, impossible coastal geometry, and altered stars.

Keep the strategic information readable as the world becomes more uncanny. Sacrifice can be communicated through ritual imagery and reports; graphic animation is not required for the mechanic.

## 14. Relationship to the existing prototype

The existing project provides a React/TypeScript browser implementation with a regional map, resource and ability logic, seeded events, local persistence, and tests. It was built against the archived design.

| Legacy concept | New design direction |
|---|---|
| Second-based progression and finale | Monthly simulation and a celestial deadline |
| Dread and Insight with global Influence percentage | Cult strength, spendable Influence, and Dread |
| Ritual that adds cult strength | Sacrifice explicitly consumes followers |
| Suppression reaching 100 causes immediate loss | Regional threats, global awareness, and deadline-driven defeat; early elimination rule open |
| Global Influence threshold opens awakening | Proposed prepared ritual network and alignment conditions |
| Exposure mainly contributes to pressure | Exposure also enables high-value public strategies |
| Broad regional influence | Proposed differentiated belief and recruitment responses |

Reuse suitable infrastructure after reviewing it. Do not rewrite the entire project solely because the GDD changed.

During the later code revision, review save compatibility and provide an explicit reset or migration path for old runs. Old completion claims and automated tests must be reevaluated against the new rules. This document does not authorize those implementation changes yet.

## 15. Acceptance criteria and evidence

### Functional acceptance for the revised prototype

- The game presents exactly three primary managed resources with distinct roles.
- Sacrificing followers reduces the selected region's cult strength and its future contribution.
- Recruitment, sacrifice, and supernatural spending form a feasible starting economy.
- The player can benefit materially from public activity and suffer understandable consequences.
- Local publicity does not automatically reveal every hidden network.
- The calendar advances correctly across months and years; pause stops progression.
- The alignment and deadline are visible before the player commits to a strategy.
- Victory, interrupted rituals, and deadline defeat have explicit, reproducible rules.
- Invalid actions cannot overspend resources or partially mutate the game.
- Save/resume preserves calendar, resources, threats, and randomness consistently.
- If prominent conversions are included, secret and public uses differ, and one-time benefits cannot be duplicated.

Use targeted automated tests for arithmetic, state transitions, timer boundaries, action validation, and deterministic save/resume. These tests cannot establish replayability or atmosphere.

### Human playtest questions

- Can the player explain a choice between preserving followers and sacrificing them?
- Does the player choose when and where to reveal a cult for an identifiable benefit?
- Can the player understand why a revelation creates recruits and opposition?
- Can a setback be recovered from without making the deadline irrelevant?
- Do starting conditions and events change the player's preferred strategy?
- Does the calendar create meaningful urgency without excessive repetition?
- After losing, can the player identify a different plan worth trying?

Compare the three strategies across multiple shared seeds. Record outcomes, revelation timing, sacrifices, lost cells, awakening readiness, and player explanations.

A design failure is indicated if hiding until the same final moment dominates every run, if public play is always punished more than rewarded, if sacrifice is always correct or never useful, or if one action sequence solves every seed.

Do not set claimed success rates before observing a baseline.

## 16. Open decisions and next step

The project owner decides design direction; implementation details can follow the chosen prototype defaults after code work is authorized.

| Decision | Current position |
|---|---|
| Resource names and exact income rules | Proposed: Cult strength, Influence, Dread |
| Deniers | Difficult conversion recommended; permanent non-conversion remains unresolved |
| Population shares versus follower units | Requires one consistent abstraction before implementation |
| Calendar duration | Proposed: 600 preparation months; configurable |
| Real-time pacing | Open; choose with a short playtest |
| Alignment window and final ritual timing | Open |
| Ritual site count and support requirements | Proposed system; exact requirements open |
| Madness effects and loss of control | Regional condition proposed; detailed rules open |
| Backlash and discovery formulas | Open tuning |
| Prominent figures in first test | Generic opportunity recommended; full character system deferred |
| Early defeat and recovery rules | Deadline loss agreed; elimination rule open |
| Upgrade choices | Limited specialization proposed; exact structure open |

**Current next step:** Playtest the user-authorized Alignment 1 implementation, using the prototype defaults documented in the implementation plan. Record pacing, clarity, and strategy feedback before expanding content or treating balance assumptions as final.
