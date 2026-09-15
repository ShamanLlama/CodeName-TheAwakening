import { describe, expect, it } from "vitest";
import { BALANCE as B } from "../data/balance";
import { advanceSimulation, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, startAwakening } from "./engine";
import { deadline, influencePerMonth, readySites, recruit, siteRequirement, totalFollowers } from "./derived";
import { REGION_IDS, type GameState } from "./types";
function start() { return placeStartingFoothold(createGameState({ seed: "engine", difficulty: "normal" }), "africa").state; }
function ready(month: number = B.preparationMonths): GameState {
  const s = start(); s.elapsedMonths = month; s.status = "alignment"; s.dread = 200;
  for (const id of ["africa", "europe", "latin_america"] as const) { recruit(s.regions[id], siteRequirement(s.regions[id]) + 60); s.regions[id].site = { invested: true, progress: 24 }; }
  return s;
}
describe("monthly cult engine", () => {
  it("starts with one living cell and three managed resources", () => {
    const s = start(); expect(totalFollowers(s)).toBe(100); expect(s.influence).toBe(35); expect(s.dread).toBe(20);
    expect("insight" in s).toBe(false); expect("suppression" in s).toBe(false);
  });
  it("sacrifice permanently consumes followers and reduces future income", () => {
    const s = start(), before = influencePerMonth(s), snapshot = structuredClone(s);
    const result = performAction(s, "sacrifice", "africa");
    expect(result.ok).toBe(true); expect(s).toEqual(snapshot);
    expect(result.state.regions.africa.population.followers).toBe(60);
    expect(result.state.regions.africa.population.losses).toBe(40);
    expect(result.state.dread).toBe(45); expect(influencePerMonth(result.state)).toBeCloseTo(before - B.sacrificeFollowers * B.influencePerFollower);
  });
  it("rejects overspending, invalid targets and cooldowns without mutation or RNG changes", () => {
    const s = start();
    expect(performAction(s, "prepare", "africa").state).toBe(s);
    expect(performAction(s, "expand", "oceania").ok).toBe(false);
    const sacrificed = performAction(s, "sacrifice", "africa").state;
    expect(performAction(sacrificed, "sacrifice", "africa").state).toBe(sacrificed);
    expect(previewAction(s, "recruit", "africa").ok).toBe(true);
    expect(s).toEqual(start());
  });
  it("does not generate Dread by waiting and advances explicit months independently of speed", () => {
    const s = start(); expect(advanceSimulation(s, 1).dread).toBe(20);
    expect(advanceSimulation({ ...s, speed: 0 }, 12).elapsedMonths).toBe(12);
    expect(advanceSimulation({ ...s, speed: 2 }, 12).elapsedMonths).toBe(12);
    expect(advanceSimulation(s, Infinity)).toBe(s);
  });
  it("recruits people without converting deniers; manifestations create believers", () => {
    const s = start(), denied = s.regions.africa.population.deniers;
    const recruited = performAction(s, "recruit", "africa").state;
    expect(recruited.regions.africa.population.deniers).toBe(denied);
    const manifested = performAction(s, "manifest", "africa").state;
    expect(manifested.regions.africa.population.deniers).toBe(denied - B.manifestationDeniers);
    expect(manifested.regions.africa.population.believers).toBe(115);
    expect(manifested.dread).toBe(0); expect(manifested.awareness).toBe(8);
  });
  it("public worship improves growth after backlash but hiding does not erase evidence", () => {
    const s = start(); s.elapsedMonths = 100;
    const visible = performAction(s, "reveal", "africa").state;
    visible.regions.africa.backlashUntil = 0; visible.awareness = s.awareness;
    const publicNext = advanceSimulation(visible, 1), hiddenNext = advanceSimulation(s, 1);
    expect(publicNext.regions.africa.population.followers - 100).toBeCloseTo(2 * (hiddenNext.regions.africa.population.followers - 100));
    const hidden = performAction(visible, "hide", "africa").state;
    expect(hidden.regions.africa.exposure).toBe(visible.regions.africa.exposure);
    expect(hidden.regions.europe.exposure).toBe(0);
  });
  it("pauses large advances at alignment and ends unsupported attempts at the deadline", () => {
    const s = advanceSimulation(start(), deadline);
    expect(s.elapsedMonths).toBe(B.preparationMonths); expect(s.speed).toBe(0); expect(s.status).toBe("alignment");
    const lost = advanceSimulation(s, B.alignmentMonths); expect(lost.status).toBe("lost"); expect(lost.elapsedMonths).toBe(deadline);
    expect(advanceSimulation(lost, 10)).toBe(lost);
  });
  it("wins on the final legal month, with threat losses resolved before support", () => {
    const s = ready(B.preparationMonths + B.finalMonths);
    const final = startAwakening(s); expect(final.ok).toBe(true);
    // Synthetic control: the director is silenced so this measures only the alignment arithmetic.
    let run = structuredClone(final.state);
    for (let month = 0; month < B.finalMonths; month++) {
      for (const id of REGION_IDS) run.regions[id].threat = null;
      run = advanceSimulation(run, 1);
    }
    expect(run.status).toBe("won");
    const damaged = structuredClone(final.state); damaged.finalProgress = 11; damaged.elapsedMonths = deadline - 1;
    damaged.regions.europe.population.unaware += damaged.regions.europe.population.followers - 60;
    damaged.regions.europe.population.followers = 60;
    damaged.regions.europe.threat = { kind: "raid", severity: 3, dueMonth: deadline };
    expect(advanceSimulation(damaged, 1).status).toBe("lost");
  });
  it("interrupts and resumes a funded site without charging twice", () => {
    const s = ready(); s.regions.europe.site.progress = 5;
    const short = siteRequirement(s.regions.europe) - 20;
    s.regions.europe.population.unaware += s.regions.europe.population.followers - short; s.regions.europe.population.followers = short;
    const interrupted = advanceSimulation(s, 1);
    expect(interrupted.regions.europe.site.progress).toBe(5);
    const repaired = performAction(interrupted, "recruit", "europe").state;
    expect(advanceSimulation(repaired, 1).regions.europe.site.progress).toBe(6);
    expect(performAction(repaired, "prepare", "europe").ok).toBe(false);
  });
  it("cannot repeatedly obtain a figure's site contribution or switch doctrine", () => {
    const s = ready(); s.influence = 200; s.regions.africa.site.progress = 1;
    s.regions.africa.opportunity = { kind: "celebrity", expiresMonth: deadline };
    const converted = performAction(s, "convert", "africa").state;
    const revealed = performAction(converted, "reveal_figure", "africa").state;
    expect(revealed.regions.africa.site.progress).toBe(9);
    expect(performAction(revealed, "reveal_figure", "africa").state).toBe(revealed);
    const doctrine = chooseSpecialization(s, "terror").state;
    expect(chooseSpecialization(doctrine, "recruitment").state).toBe(doctrine);
  });
  it("destroys a site when its last followers are lost and does not recreate population", () => {
    const s = ready(); const r = s.regions.europe; r.population.unaware += r.population.followers - 40; r.population.followers = 40;
    const sacrificed = performAction(s, "sacrifice", "europe").state;
    expect(sacrificed.regions.europe.site.invested).toBe(false); expect(readySites(sacrificed)).toBe(2);
    for (const id of REGION_IDS) expect(Object.values(sacrificed.regions[id].population).reduce((a, b) => a + b, 0)).toBeCloseTo(Object.values(s.regions[id].population).reduce((a, b) => a + b, 0));
  });
});
