import { describe, expect, it } from "vitest";
import { POPULATION_WEIGHT_TOTAL, REGIONS } from "../data/regions";
import { clamp, globalInfluence } from "./derived";
import { nextRandom, seedToState } from "./random";
import type { RegionId, RegionState } from "./types";

const blankRegion = (): RegionState => ({ influence: 0, cultStrength: 0, resistanceModifier: 0, exposure: 0, instability: 0, activeEventId: null, milestoneFlags: [], timedResistance: [] });

describe("shared foundation", () => {
  it("keeps population weights and weighted influence exact", () => {
    expect(POPULATION_WEIGHT_TOTAL).toBe(100);
    const regions = Object.fromEntries(Object.keys(REGIONS).map((id) => [id, blankRegion()])) as Record<RegionId, RegionState>;
    regions.south_asia.influence = 50;
    expect(globalInfluence(regions)).toBe(9);
  });

  it("clamps bounded values", () => {
    expect(clamp(-3)).toBe(0);
    expect(clamp(103)).toBe(100);
  });

  it("repeats seeded random sequences", () => {
    const seed = seedToState("rlyeh");
    expect(nextRandom(seed)).toEqual(nextRandom(seed));
  });
});
