# 04 — Visibility and awareness

Two separate meters. Confusing them is the most common way to misread the game.

| | **Exposure** | **Awareness** |
| --- | --- | --- |
| Scope | Per region | Global |
| Represents | Local evidence | What humanity collectively knows |
| Range | 0–100 | 0–100 |
| Drives | Threat frequency and severity in that region | Income, recruitment, threat severity — and defeat |
| Reduced by | `conceal` | `conceal`, and shrinking the cult |
| Fatal? | No | **Yes, at 100** |

## Exposure

Regional evidence. It rises with what you do there and drifts with what you are.

**Floors.** Exposure cannot be concealed below a floor set by what is visibly happening:
a funded ritual site imposes a high floor, public worship a lower one, and an underground
cell none. This is why `conceal` reports "public worship and rituals retain an evidence
floor" — **a ritual site is permanently conspicuous** and there is no way to hide one.

**Drift.** A public cell accumulates exposure every month; a hidden one slowly sheds it,
down to its floor (`advanceSimulation`, `src/game/engine.ts:186`).

**Actions add exposure** roughly in proportion to how brazen they are: recruiting a little,
sacrifice more, manifesting and revealing a lot, unmasking a converted figure most.

Exposure feeds two things: the director's per-region threat pressure, and **threat
severity**, which is what sets the price of obstructing it.

## Awareness — the losing clock

Global, and the run's second failure condition. It has three inputs:

1. **Your share of humanity.** Every month awareness rises by
   `cultShare × BALANCE.awarenessPerCapita`, where `cultShare` is total followers over
   world population (`src/game/engine.ts:196`, `cultShare` at `derived.ts:15`). **This is
   the dominant term in any large cult** and it cannot be avoided by being careful. A cult
   holding half the world is loudly, structurally visible.
2. **Conspicuous acts.** Revealing, manifesting, funding a site, and unmasking a figure
   each add a one-off amount.
3. **Standing visibility.** Each region that is public or hosts a site adds a small amount
   every month.

### Effects

**The drag** (`awarenessDrag`, `src/game/derived.ts:16`) multiplies **both Influence income
and recruitment**:

```
drag = max(0, 1 − awareness / BALANCE.awarenessDrag)
```

The constant is above 100, so the drag at maximum awareness is severe but not total — a
dying run still functions, it just cannot grow out of trouble. Because the drag hits income
and recruitment together, a noticed cult gets poorer and smaller at the same time. It is a
soft spiral, and it is meant to be felt well before it is fatal.

**The loss.** At 100, the run ends immediately: *"The world knows. Every cell is hunted and
the rite dies with them."* Checked every month, before the calendar deadline
(`src/game/engine.ts:202`).

### The only brake

`conceal` removes a small fixed amount of awareness in addition to regional exposure. It
has a long per-region cooldown, so with eight regions there is a hard ceiling on how much
awareness you can scrub per month.

**That ceiling is the design.** Below a certain cult size your concealment capacity
outpaces the per-capita gain and awareness is manageable. Above it, awareness wins no
matter how diligently you play. The game is asking: *how much of humanity can you afford to
own?*

## Public worship

`reveal` doubles the region's automatic recruitment. It also raises exposure sharply, adds
awareness, sets an exposure floor, upgrades that region's future threats from
investigations to raids, and applies a recruitment **backlash** penalty for a window
afterwards.

`hide` ends public worship. It does **not** remove the evidence already created — exposure
stays where it is and decays naturally.

The intended pattern is **public worship as a growth phase, not a resting state**: reveal
to climb toward a site's support requirement, then go underground and let exposure decay.
Staying public indefinitely feeds exposure, severity, and awareness simultaneously. Both AI
playtest harnesses model it this way, with hysteresis so they do not thrash the toggle
(`src/tests/playthrough.ts`) — each toggle costs awareness.
