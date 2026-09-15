import { describe, expect, it } from "vitest";
import { play, type Strategy } from "../tests/playthrough";
import { decodeSave, defaultSave, isGameState } from "./persistence";
import { advanceSimulation } from "./engine";
describe("legal strategy playthroughs", () => {
  for (const strategy of ["early", "late", "hub"] as Strategy[]) {
    for (const seed of ["rlyeh", "dagon", "the-deep"]) {
      it(strategy + " can awaken through legal commands: " + seed, () => {
        const state = play(strategy, seed);
        expect(state.status, JSON.stringify({ strategy, seed, month: state.elapsedMonths, followers: Object.values(state.regions).map(r => r.population.followers), dread: state.dread, log: state.log.slice(-6) })).toBe("won");
        expect(state.telemetry.sacrificed).toBeGreaterThan(0);
        expect(isGameState(state)).toBe(true);
      });
    }
  }
  it("save/load preserves a real campaign's continuation and population accounting", () => {
    const state = play("hub", "resume", 420);
    const decoded = decodeSave(JSON.stringify({ ...defaultSave(), state }));
    expect(decoded?.state).toEqual(state);
    if (!decoded?.state) throw new Error("Campaign failed validation");
    expect(advanceSimulation(decoded.state, 60)).toEqual(advanceSimulation(state, 60));
  });
});
