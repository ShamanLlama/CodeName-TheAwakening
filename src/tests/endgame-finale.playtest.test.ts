import { describe, expect, it } from "vitest";
import { advanceSimulation, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, startAwakening } from "../game/engine";
import { BALANCE as B } from "../data/balance";
import { siteRequirement } from "../game/derived";
import { threatConsequences } from "../game/threats";
import { deadline, readySites } from "../game/derived";
import { latestSiteStart } from "../game/forecast";
import type { ActionId, Difficulty, GameState, RegionId, RegionState } from "../game/types";

// Endgame playtest harness. Runs begin from a new game and use only engine commands.
// The two forced-threshold cases below are labelled controlled: they isolate the 24-month
// alignment maths rather than making a claim about the director's random threat frequency.
// Small regions host the sites; Africa stays the recruiting ground that feeds sacrifices.
const targets: RegionId[] = ["latin_america", "middle_east", "oceania"];
const FARM: RegionId = "africa";
// The synthetic controls pin their own trio: they measure alignment arithmetic, not site choice.
const controlSites: RegionId[] = ["latin_america", "europe", "middle_east"];
type Response = "three-month" | "one-month" | "ignore";
type Sample = { seed: string; response: Response; status: string; month: number; ready: number; finalProgress: number; dread: number; raidLosses: number; counters: number; invested: number; prepared: number };
type LateSample = Sample & { fundingGate: number; fundedMonths: Partial<Record<RegionId, number>>; readyMonths: Partial<Record<RegionId, number>>; eclipseAccelerations: number };
// Synthetic-control helpers: a follower count automatic recruitment cannot lift to support
// within one month, and a monthly tick with the director silenced.
function setFollowers(r: RegionState, value: number): void {
  r.population.unaware -= value - r.population.followers;
  r.population.followers = value;
}
// A count automatic recruitment cannot lift back to support within one month.
const starve = (r: RegionState) => setFollowers(r, siteRequirement(r) - 60);
const restore = (r: RegionState) => setFollowers(r, siteRequirement(r));
const quietMonth = (state: GameState): GameState => {
  const quiet = structuredClone(state);
  for (const id of Object.keys(quiet.regions) as RegionId[]) quiet.regions[id].threat = null;
  return advanceSimulation(quiet, 1);
};
const lateSeeds = ["rlyeh", "dagon", "the-deep", "finale-a", "finale-b", "late-01", "late-02", "late-03", "late-04", "late-05", "late-06", "late-07"];

function run(seed: string, response: Response, difficulty: Difficulty = "normal", fundingGate = 0, notes?: { fundedMonths: Partial<Record<RegionId, number>>; readyMonths: Partial<Record<RegionId, number>>; eclipseAccelerations: number }): GameState {
  let state = placeStartingFoothold(createGameState({ seed, difficulty }), "africa").state;
  const act = (id: ActionId, region: RegionId) => {
    if (previewAction(state, id, region).ok) state = performAction(state, id, region).state;
  };
  for (let step = 0; step < deadline && !["won", "lost"].includes(state.status); step++) {
    for (const id of targets) {
      const threat = state.regions[id].threat;
      // Due-month reactions are impossible: resolveWorld applies them before this decision point.
      if (threat && (response === "three-month" ? threat.dueMonth - state.elapsedMonths <= 3 : response === "one-month" ? threat.dueMonth - state.elapsedMonths <= 1 : false)) act("counter", id);
      const opportunity = state.regions[id].opportunity;
      if (opportunity?.kind === "nightmare" || opportunity?.kind === "eclipse" && state.regions[id].site.invested) {
        const exploit = previewAction(state, "exploit", id);
        if (exploit.ok) {
          state = performAction(state, "exploit", id).state;
          if (opportunity.kind === "eclipse" && notes) notes.eclipseAccelerations++;
        }
      }
    }
    for (const id of targets) if (!state.regions[id].population.followers) act("expand", id);
    if (state.regions[FARM].population.followers < 700) act("recruit", FARM);
    if (state.dread < 420 && state.regions[FARM].population.followers - 40 >= 260) act("sacrifice", FARM);
    if (state.regions[FARM].exposure > 30) act("conceal", FARM);
    if (!state.specialization && state.elapsedMonths >= 20 && state.influence >= 70) state = chooseSpecialization(state, "recruitment").state;
    for (const id of targets) {
      const region = state.regions[id];
      if (region.population.followers > 0 && region.population.followers < siteRequirement(region) + 160) act("recruit", id);
      // Public worship is a growth phase only: staying public feeds exposure, severity and awareness.
      if (state.elapsedMonths >= 8 && !region.public && region.population.followers < siteRequirement(region)) act("reveal", id);
      if (region.public && region.population.followers >= siteRequirement(region) + 160) act("hide", id);
      if (region.exposure > 30) act("conceal", id);
      const missingSites = targets.filter(other => !state.regions[other].site.invested).length;
      if (region.population.followers - B.sacrificeFollowers >= siteRequirement(region) + 120 && state.dread < missingSites * 160 + B.finalDread) act("sacrifice", id);
      if (state.elapsedMonths >= fundingGate && !region.site.invested) {
        act("prepare", id);
        if (state.regions[id].site.invested && notes && notes.fundedMonths[id] === undefined) notes.fundedMonths[id] = state.elapsedMonths;
      }
    }
    if (state.status === "alignment" && readySites(state) >= 3) state = startAwakening(state).state;
    state = advanceSimulation(state, 1);
    if (notes) for (const id of targets) {
      if (state.regions[id].site.progress >= 24 && notes.readyMonths[id] === undefined) notes.readyMonths[id] = state.elapsedMonths;
    }
  }
  return state;
}

function sample(seed: string, response: Response): Sample {
  const state = run(seed, response);
  const sites = targets.map(id => state.regions[id].site);
  return { seed, response, status: state.status, month: state.elapsedMonths, ready: readySites(state), finalProgress: state.finalProgress, dread: state.dread,
    raidLosses: state.telemetry.raidLosses, counters: state.telemetry.threatsCountered,
    invested: sites.filter(site => site.invested).length, prepared: sites.filter(site => site.progress >= 24).length };
}

function lateSample(seed: string, fundingGate: number): LateSample {
  const notes = { fundedMonths: {} as Partial<Record<RegionId, number>>, readyMonths: {} as Partial<Record<RegionId, number>>, eclipseAccelerations: 0 };
  const state = run(seed, "three-month", "normal", fundingGate, notes);
  const base = sampleFromState(seed, "three-month", state);
  return { ...base, fundingGate, ...notes };
}

function sampleFromState(seed: string, response: Response, state: GameState): Sample {
  const sites = targets.map(id => state.regions[id].site);
  return { seed, response, status: state.status, month: state.elapsedMonths, ready: readySites(state), finalProgress: state.finalProgress, dread: state.dread,
    raidLosses: state.telemetry.raidLosses, counters: state.telemetry.threatsCountered,
    invested: sites.filter(site => site.invested).length, prepared: sites.filter(site => site.progress >= 24).length };
}

describe("endgame finale playtests", () => {
  it("records response-delay samples from legal new-game runs", () => {
    const samples = lateSeeds.flatMap(seed =>
      (["three-month", "one-month", "ignore"] as Response[]).map(response => sample(seed, response)));
    console.info("ENDGAME_FINALE_SAMPLES=" + JSON.stringify(samples));
    const winRate = (response: Response) => {
      const runs = samples.filter(s => s.response === response);
      return runs.filter(s => s.status === "won").length / runs.length;
    };
    // Responding beats ignoring. Which response is better is no longer obvious: with a
    // cooldown on obstruction, spending it early can leave the next threat undefended,
    // so the three-month and one-month rates are recorded rather than ranked.
    expect(winRate("three-month")).toBeGreaterThan(winRate("ignore"));
    expect(winRate("one-month")).toBeGreaterThan(winRate("ignore"));
  });

  it("records actual late-site funding outcomes from legal new-game runs", () => {
    const samples = [-4, 0, 1, 4].map(offset => latestSiteStart + offset).flatMap(fundingGate => lateSeeds.map(seed => lateSample(seed, fundingGate)));
    console.info("ENDGAME_LATE_FUNDING_SAMPLES=" + JSON.stringify(samples));
    expect(samples).toHaveLength(48);
    expect(samples.every(s => s.status === "won" || s.status === "lost")).toBe(true);
  });

  it("has a twelve-month recovery budget at the alignment boundary (synthetic timing control)", () => {
    // Synthetic control: this deliberately removes economy and director variance to measure only
    // the documented 24-month alignment window. Natural behavior is sampled in the test above.
    const state = placeStartingFoothold(createGameState({ seed: "boundary-lineage", difficulty: "normal" }), "africa").state;
    state.status = "alignment"; state.elapsedMonths = B.preparationMonths; state.dread = 100; state.influence = 0;
    for (const id of controlSites) {
      restore(state.regions[id]);
      state.regions[id].site = { invested: true, progress: 24 };
    }
    const started = startAwakening(state); expect(started.ok).toBe(true);
    if (!started.ok) return;
    // Twelve unavailable months still leave exactly twelve supported monthly ticks (613..624).
    const twelveLost = structuredClone(started.state);
    starve(twelveLost.regions.europe);
    let recovered = twelveLost;
    for (let month = 0; month < 12; month++) {
      recovered = quietMonth(recovered);
      // Keep this deliberately unsupported despite automatic recruitment.
      starve(recovered.regions.europe);
    }
    restore(recovered.regions.europe);
    for (let month = 0; month < 12; month++) recovered = quietMonth(recovered);
    expect(recovered.status).toBe("won"); expect(recovered.elapsedMonths).toBe(deadline);
    // A thirteenth unavailable month leaves only eleven productive ticks and loses at month 624.
    const thirteenLost = structuredClone(started.state);
    starve(thirteenLost.regions.europe);
    let missed = thirteenLost;
    for (let month = 0; month < 13; month++) {
      missed = quietMonth(missed);
      starve(missed.regions.europe);
    }
    restore(missed.regions.europe);
    for (let month = 0; month < 11; month++) missed = quietMonth(missed);
    expect(missed.status).toBe("lost"); expect(missed.finalProgress).toBe(11);
  });

  it("shows a missed severity-three occult hit breaks a minimum-supported ready site (synthetic control)", () => {
    // Synthetic control, not a natural director sample: pin the threat to make its finale impact exact.
    const state = placeStartingFoothold(createGameState({ seed: "threat-lineage", difficulty: "normal" }), "africa").state;
    state.status = "alignment"; state.elapsedMonths = B.preparationMonths; state.dread = 100;
    for (const id of controlSites) { restore(state.regions[id]); state.regions[id].site = { invested: true, progress: 24 }; }
    state.regions.europe.threat = { kind: "occult", severity: 3, dueMonth: B.preparationMonths + 1 };
    const projected = threatConsequences(state.regions.europe);
    const afterHit = advanceSimulation(state, 1);
    // The hit lands as projected, then a single month of recruitment cannot restore support.
    expect(afterHit.regions.europe.population.followers).toBeGreaterThanOrEqual(projected.remaining);
    expect(afterHit.regions.europe.population.followers).toBeLessThan(siteRequirement(afterHit.regions.europe));
    expect(afterHit.regions.europe.site.progress).toBe(18);
    expect(readySites(afterHit)).toBe(2);
  });

  it("requires exactly one hundred Dread to start and a site needs exactly sixty followers (synthetic reserve control)", () => {
    const state = placeStartingFoothold(createGameState({ seed: "reserve-boundary", difficulty: "normal" }), "africa").state;
    state.status = "alignment"; state.elapsedMonths = B.preparationMonths;
    for (const id of controlSites) { restore(state.regions[id]); state.regions[id].site = { invested: true, progress: 24 }; }
    state.dread = 99; expect(startAwakening(state).ok).toBe(false);
    state.dread = 100; expect(startAwakening(state).ok).toBe(true);
    setFollowers(state.regions.europe, siteRequirement(state.regions.europe) - 1);
    expect(startAwakening(state).ok).toBe(false);
  });
});
