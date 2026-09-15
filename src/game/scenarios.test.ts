import { describe, expect, it } from "vitest";
import allRegions from "../tests/fixtures/all_regions_influenced.json";
import awakeningNearLoss from "../tests/fixtures/awakening_near_loss.json";
import earlyGame from "../tests/fixtures/early_game_safe.json";
import crackdown from "../tests/fixtures/high_exposure_crackdown.json";
import { advanceSimulation, createGameState, debugPatchState, placeStartingFoothold, startAwakening } from "./engine";
import { spawnEvent } from "./director";
import type { CommandResult, Difficulty, GameState, RegionId } from "./types";

function done(result: CommandResult): GameState {
  if (!result.ok) throw new Error(result.message);
  return result.state;
}

function playing(seed: string, difficulty: string, foothold: string): GameState {
  return done(placeStartingFoothold(createGameState({ seed, difficulty: difficulty as Difficulty }), foothold as RegionId));
}

describe("deterministic GDD scenarios", () => {
  it("keeps the early game safe and bounded", () => {
    const state = advanceSimulation(playing(earlyGame.seed, earlyGame.difficulty, earlyGame.foothold), earlyGame.advanceSeconds);
    expect(state.status).toBe("playing");
    expect(state.dread).toBeGreaterThanOrEqual(earlyGame.dreadRange[0] ?? 0);
    expect(state.dread).toBeLessThanOrEqual(earlyGame.dreadRange[1] ?? 150);
    expect(state.activeEvents).toHaveLength(0);
  });

  it("applies a crackdown to a high-exposure region", () => {
    let state = playing(crackdown.seed, crackdown.difficulty, crackdown.foothold);
    state = { ...state, regions: { ...state.regions, north_america: { ...state.regions.north_america, exposure: crackdown.exposure } } };
    const before = state.regions.north_america.cultStrength;
    state = done(spawnEvent(state, "government_crackdown", "north_america", 2));
    state = advanceSimulation(state, crackdown.advanceSeconds);
    expect(state.regions.north_america.cultStrength).toBeLessThan(before);
    expect(state.activeEvents[0]?.eventId).toBe(crackdown.eventId);
  });

  it("keeps a near-loss awakening deterministic", () => {
    let state = playing(awakeningNearLoss.seed, awakeningNearLoss.difficulty, awakeningNearLoss.foothold);
    state = debugPatchState(state, { allRegionInfluence: awakeningNearLoss.influence, dread: awakeningNearLoss.dread, suppression: awakeningNearLoss.suppression });
    state = advanceSimulation(done(startAwakening(state)), awakeningNearLoss.advanceSeconds);
    expect(state.status).toBe("awakening");
    expect(state.suppression).toBeGreaterThanOrEqual(awakeningNearLoss.suppressionRange[0] ?? 0);
    expect(state.suppression).toBeLessThan(awakeningNearLoss.suppressionRange[1] ?? 100);
  });

  it("advances a run with every region influenced", () => {
    let state = playing(allRegions.seed, allRegions.difficulty, allRegions.foothold);
    state = debugPatchState(state, { allRegionInfluence: allRegions.influence });
    state = advanceSimulation(state, allRegions.advanceSeconds);
    expect(state.globalInfluence).toBeGreaterThanOrEqual(allRegions.globalRange[0] ?? 0);
    expect(state.globalInfluence).toBeLessThanOrEqual(allRegions.globalRange[1] ?? 100);
    expect(state.insight).toBe(8);
  });
});
