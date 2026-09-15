# Endgame balance playtest — 2026-09-06

Three GPT-5.6 Terra agents, each using medium reasoning, ran 1,236 deterministic campaign simulations through legal game commands. The strongest finding is a large gap between early preparation and late planning: managed strategies usually satisfy the ritual requirements hundreds of months before alignment, while delaying all site investments from month 588 to 589 changed the late-funding sample from 12/12 wins to 0/12.

This measures scripted strategy performance. Agents inspect the full state and act every simulated month; these are not 1,236 human or browser playthroughs. The browser separately passed the existing prepared-finale victory and missed-deadline checks. No production code or balance values were changed.

## Difficulty and preparation

The main sweep used 120 seeds (`endgame-000` through `endgame-119`), three difficulties, and three policies: early public worship with Recruitment, late public worship with Infiltration, and an Africa public hub with Terror. All start in Africa and expand into Latin America and Europe. Policies counter urgent threats, maintain followers, exploit legal omens, and reserve Dread for three sites and the final rite.

| Difficulty | Wins / runs | Observed win rate | Mean first readiness month | Mean months before alignment | Mean Influence at alignment | Mean Dread at alignment |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Easy | 360 / 360 | 100% | 236.15 | 363.85 | 500 | 229.22 |
| Normal | 360 / 360 | 100% | 257.99 | 342.01 | 500 | 223.61 |
| Hard | 358 / 360 | 99.44% | 297.24 | 302.76 | 486 | 215.56 |

Readiness means three completed sites supported by at least 60 followers each, plus at least 100 Dread. It is measured before monthly actions; means exclude runs that never reach readiness. It is a first crossing of the requirements, not proof that readiness remains uninterrupted until alignment. Alignment opens at month 600.

Both Hard losses used early public worship. Seed `endgame-019` first met the requirements at month 613, one month after the latest unaccelerated final-rite start. Seed `endgame-092` had no surviving followers or sites at alignment. Hard early-public averaged 533 opposition losses versus 123 for Hard late-public. These are policy comparisons: visibility timing and doctrine change together, so the data does not isolate doctrine strength.

Source: [full difficulty results](endgame-difficulty.json), [harness](../../src/tests/endgame-difficulty.playtest.test.ts).

## Economy and threat response

The economy agent tested six Normal policy bundles over 12 shared seeds (`endgame-0` through `endgame-11`): five focused three-region strategies and a public eight-region strategy. All 72 runs won. The 60 managed runs finished at month 612; the 12 runs ignoring counters finished between months 612 and 622, averaging 617.8.

| Normal policy | Mean first readiness month | Mean followers at month 600 | Mean opposition losses | Mean counters | Mean months already at the Influence cap after actions |
| --- | ---: | ---: | ---: | ---: | ---: |
| Early public / Recruitment | 265.9 | 2,295.7 | 262.7 | 29.8 | 219.5 |
| Hidden / Infiltration with secret figures | 243.9 | 1,785.7 | 86.7 | 20.8 | 219.2 |
| Africa public hub / Terror | 258.8 | 1,870.5 | 154.7 | 24.2 | 209.4 |
| Public from month 420 / Recruitment | 243.1 | 2,479.0 | 82.7 | 26.8 | 208.8 |
| Early public / never counter | 264.3 | 1,113.0 | 1,517.3 | 0 | 278.5 |
| Eight public regions / Recruitment | 287.9 | 6,329.7 | 427.3 | 68.8 | 239.6 |

The eight-region sample supports the prior human playtest's concern about inexpensive late counters: mean follower income at month 600 was approximately 50.64 Influence/month, while a counter costs 30. This policy still spent roughly 240 months at the 500 cap despite countering about 69 threats. These cap observations count checks after actions; they do not measure human idle time or exact discarded income.

An independent finale sample of 12 seeds also won 12/12 when ignoring threats, averaging 1,440.67 opposition losses and finishing at month 617.83. Responding with three months left averaged 228 losses and finished at month 612. One month left also won 12/12. Waiting until the due month is too late because monthly threat resolution precedes the next decision.

Source: [economy results and policy definitions](endgame-economy.json), [economy harness](../../src/tests/endgame-economy.playtest.test.ts), [finale results](endgame-finale.json).

## Late preparation and tuning implications

The finale agent built resources normally but withheld all three site investments until each gate below. Every site was funded exactly at its gate. Counters and available eclipse actions remained enabled. Each gate used the same 12 Normal seeds.

| Site funding month | All three sites ready | Wins / runs | Outcome month |
| --- | ---: | ---: | ---: |
| 576 | 600 | 12 / 12 | Win at 612 |
| 588 | 612 | 12 / 12 | Win at 624 |
| 589 | 613 | 0 / 12 | Lose at 624 |
| 600 | 624 | 0 / 12 | Lose at 624 |

Month 588 is the latest funding time without acceleration: 24 preparation months plus 12 final-rite months consume the remaining 36 months. Six eclipse uses across the month-589 sample did not accelerate all three sites enough to rescue a run. Additional eclipses or figure contributions can change this boundary; it is not an unconditional game rule forbidding later funding.

Separate synthetic controls verified that a severity-three occult hit removes 48 followers and six preparation months. A site with exactly 60 followers is consequently fragile. A buffer of 108 followers preserves the 60-follower support threshold through that hit, but the preparation setback still interrupts readiness. These controls are excluded from the campaign counts.

The next balance experiments should address the long maintenance period and the value of responding to threats, while keeping the late-planning failure visible to players. Compare modest, separately tested changes to ongoing site costs or late opposition against these baselines; also test whether players understand the preparation forecast. A blanket increase in site costs or preparation time could worsen the documented human scheduling loss. Doctrine changes need a separate experiment holding the other policy choices constant. These are hypotheses for follow-up, not tested balance fixes.

The existing [human playtest record](../implementation-checklist.md#human-playtest--seed-ff53a4ca-2026-09-06) remains important: that run lost with 6,566 followers and 500 Influence because its final site completed at month 624. The automated economy is forgiving; the calendar can still be unforgiving to an uninformed player.

## Reproduction and validation

Run from the repository root with the installed Node dependencies:

```powershell
npm test
npm run typecheck
npx eslint src/tests/endgame-difficulty.playtest.test.ts src/tests/endgame-economy.playtest.test.ts src/tests/endgame-finale.playtest.test.ts
npm run e2e -- --grep 'finishes a legally prepared campaign|misses the alignment deadline'
```

The difficulty harness regenerates its full JSON dataset. Economy and finale harnesses print their measured summaries and samples; their companion JSON files document this session's results. The three suites represent 1,080 difficulty campaigns, 72 economy campaigns, and 84 finale campaigns (36 response comparisons plus 48 late-funding comparisons). They reuse seeds across policies and should not be pooled into a single population win-rate estimate.

Validation: 45 Vitest tests, repository typecheck, lint on the three new harnesses, and both selected Chromium journeys passed. The sampled game inputs were unchanged during the difficulty sweep. Existing user modifications were preserved.
