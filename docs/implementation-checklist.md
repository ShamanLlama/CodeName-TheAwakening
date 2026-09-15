# Alignment 1 — implementation and playtest status

The [GDD](gdd/README.md) and [implementation plan](implementation-plan.md) are the design references.

## Implemented

- Monthly calendar: 600 preparation months, 24-month alignment window, 12-month final rite.
- Regional followers, global Influence and Dread; permanent follower sacrifices.
- Conserved abstract population: unaware, deniers, believers, followers, and losses.
- Hidden/public worship, backlash, manifestations, regional Exposure, Global Awareness.
- Targeted, warned investigations, raids, and occult threats independent of opportunity slots.
- Three supported ritual sites, interruption/recovery, final-month completion, deadline defeat.
- Generic celebrity/politician conversions and one-time public ritual contributions.
- One permanent recruitment, infiltration, or terror doctrine.
- Guide, pause/speed controls, hidden-tab pause, regional actions, live reports, mobile region selector.
- Separate v2 saves, retained legacy runs, corrupt-save recovery, optional audio, report export.
- Development-only time advances; no resource-grant or instant-win controls.
- Visible desktop chronicle and worldwide threat watch with countdowns, projected follower/income losses, ritual disruption, and counter actions.
- Varied land positions for map threats, stable for each active threat; ritual timing forecast before the alignment deadline.
- Exported regional snapshots and explicit retained-log coverage.

## Verified

- 38 engine/director/persistence/scenario/feedback tests pass, including nine legal winning campaigns across three strategies and three shared seeds.
- Type checking, lint, and production build pass.
- Seven Chromium journeys pass: actions/save/pause, legal-checkpoint finale, deadline loss, legacy preservation, responsive layout, clock/guide behavior, and visible chronicle/threat consequences at 2560×1440 and 1280×720.
- Desktop and mobile screenshots inspected. No horizontal overflow at 1440 px or 390 px.
- Production bundle excludes playtest time controls.

The legal player-policy tests demonstrate reachable wins, not balanced or equally enjoyable strategies. The browser finale resumes a checkpoint generated through legal engine actions; it is not a human playthrough of the entire campaign.

## Needs human playtesting

- Real session length, waiting time, recruitment repetition, and warning readability.
- Strength of public worship versus concealed growth.
- Sacrifice timing and the value of preserving followers.
- Difficulty and usefulness of deniers, manifestations, doctrines, and prominent conversions.
- Distinctiveness of early-public, late-surge, and public-hub strategies.
- Amount and timing of available resources and opposition.

Additional gods, detailed named characters, historical simulation, and richer art/audio remain deferred.

## Human playtest — seed ff53a4ca, 2026-09-06

The player reported having fun despite losing. Their exported Normal/recruitment run ended at month 624 with 6,566 followers, three supported sites, 500 Influence, 100 Dread, and zero final-rite progress. The loss was a ritual scheduling failure; resources and site count alone did not guarantee enough time to awaken.

The retained log shows Africa funded at month 538 and initially ready at 562; an occult threat at 567 delayed it again until 572. East Asia was funded at 584 and ready at 608. North America was funded at 600 and only ready at the closing month, 624. Without acceleration or interruptions, a new site must start by month 588 and the final rite by month 612.

Across the run, the player performed 181 tracked actions, countered 54 threats, sacrificed 960 followers, and lost 736 followers to opposition. Their first public worship occurred at month 103; all eight regions had been revealed by month 338. This supports investigating public expansion and late ritual planning in the next test, but does not establish overall balance from one campaign.

Feedback led to the visible chronicle, varied threat markers, explicit consequences, and ritual forecast above. Threat damage and counter costs remain at their prior values for comparison. One balance question remains: the screenshot shows about 52.53 Influence/month at the end, exceeding the 30 Influence cost of one counter. Late opposition may become inexpensive to neutralize; test whether players make meaningful choices once they understand its consequences before changing costs.

Evidence limits: the original export retains only the last 100 log entries (IDs 207–306, months 491–624), plus campaign totals. It does not record real elapsed play time, so session duration and the full early-game action sequence cannot be reconstructed. New exports identify log truncation and include regional state snapshots; they still do not measure real session duration.
