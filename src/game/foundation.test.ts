import { describe, expect, it } from "vitest";
import { createGameState, placeStartingFoothold } from "./engine";
import { calendar, clamp, livePopulation } from "./derived";
import { REGIONS, POPULATION_WEIGHT_TOTAL } from "../data/regions";
import { REGION_IDS } from "./types";
import { nextRandom, seedToState } from "./random";
describe("shared foundation", () => {
  it("keeps the abstract world population and adjacency valid", () => {
    const state = placeStartingFoothold(createGameState({ seed: "population", difficulty: "normal" }), "oceania").state;
    expect(POPULATION_WEIGHT_TOTAL).toBe(100);
    expect(REGION_IDS.reduce((sum, id) => sum + livePopulation(state.regions[id]), 0)).toBe(10000);
    for (const id of REGION_IDS) for (const adjacent of REGIONS[id].adjacent) expect(REGIONS[adjacent].adjacent).toContain(id);
  });
  it("formats calendar rollovers and clamps finite values", () => {
    expect(calendar(0)).toBe("Year 1 · Month 1");
    expect(calendar(12)).toBe("Year 2 · Month 1");
    expect(clamp(-10)).toBe(0); expect(clamp(110)).toBe(100);
  });
  it("repeats seeded random sequences", () => {
    expect(nextRandom(seedToState("same"))).toEqual(nextRandom(seedToState("same")));
  });
});
