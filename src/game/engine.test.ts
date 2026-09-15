import { describe, expect, it } from "vitest";
import {
  advanceSimulation, castAbility, createGameState, debugPatchState, dreadPerSecond, effectiveAbilityCost, placeStartingFoothold, setGameSpeed, startAwakening,
  unlockAbility, unlockUpgrade,
} from "./engine";
import { spawnEvent } from "./director";
import { effectiveResistance } from "./derived";
import type { CommandResult, GameState } from "./types";

function succeeded(result: CommandResult): GameState {
  if (!result.ok) throw new Error(result.message);
  return result.state;
}

function playing(seed = "test"): GameState {
  return succeeded(placeStartingFoothold(createGameState({ difficulty: "normal", seed }), "africa"));
}

describe("game engine", () => {
  it("validates ability targets, costs, and cooldowns without mutating failures", () => {
    const state = playing();
    const cast = castAbility(state, "secret_gathering", "africa");
    const afterCast = succeeded(cast);
    expect(afterCast.dread).toBe(8);
    expect(afterCast.cooldowns.secret_gathering).toBe(20);
    expect(afterCast.regions.africa.cultStrength).toBe(42);
    const cooldownFailure = castAbility(afterCast, "secret_gathering", "africa");
    expect(cooldownFailure.ok).toBe(false);
    expect(cooldownFailure.state).toBe(afterCast);
    const invalidTarget = castAbility(state, "secret_gathering", "europe");
    expect(invalidTarget.ok).toBe(false);
    expect(invalidTarget.state).toBe(state);
  });

  it("unlocks abilities and upgrades only when their Insight and prerequisites are present", () => {
    const state = debugPatchState(playing(), { insight: 5 });
    const sacrament = succeeded(unlockAbility(state, "sacrament_of_the_deep"));
    expect(sacrament.insight).toBe(3);
    expect(sacrament.unlockedAbilities).toContain("sacrament_of_the_deep");
    const hiddenCells = succeeded(unlockUpgrade(sacrament, "hidden_cells"));
    expect(hiddenCells.insight).toBe(0);
    expect(hiddenCells.unlockedUpgrades).toContain("hidden_cells");
  });

  it("treats advanceSimulation arguments as simulation seconds regardless of UI speed", () => {
    const paused = setGameSpeed(playing(), 0);
    const doubled = setGameSpeed(playing(), 2);
    expect(advanceSimulation(paused, 3).elapsedSeconds).toBe(3);
    expect(advanceSimulation(doubled, 3).elapsedSeconds).toBe(3);
  });

  it("awards influence milestones once and never farms them after influence falls", () => {
    const atTwentyFive = debugPatchState(playing(), { allRegionInfluence: 25 });
    expect(atTwentyFive.insight).toBe(8);
    const reset = debugPatchState(atTwentyFive, { allRegionInfluence: 0 });
    expect(debugPatchState(reset, { allRegionInfluence: 25 }).insight).toBe(8);
  });

  it("repeats the same command sequence from the same seed", () => {
    const run = (seed: string) => {
      let state = playing(seed);
      state = succeeded(castAbility(state, "whispered_dreams", "africa"));
      return advanceSimulation(state, 15);
    };
    expect(run("reproducible")).toEqual(run("reproducible"));
  });

  it("starts Awakening only at the threshold, then wins after exactly 45 seconds", () => {
    const ready = debugPatchState(playing(), { allRegionInfluence: 100, dread: 150 });
    const awakening = succeeded(startAwakening(ready));
    expect(awakening.awakeningSecondsRemaining).toBe(45);
    const outcome = advanceSimulation(awakening, 45);
    expect(outcome.status).toBe("won");
    expect(outcome.awakeningSecondsRemaining).toBe(0);
  });

  it("marks a suppression loss immediately, including through debug state", () => {
    const lost = debugPatchState(playing(), { suppression: 100 });
    expect(lost.status).toBe("lost");
    expect(lost.speed).toBe(0);
  });

  it("applies and removes event Resistance modifiers", () => {
    const state = playing();
    expect(effectiveResistance(state, "north_america")).toBe(85);
    const scandal = succeeded(spawnEvent(state, "political_scandal", "north_america"));
    expect(effectiveResistance(scandal, "north_america")).toBe(65);
    expect(effectiveResistance(advanceSimulation(scandal, 45), "north_america")).toBe(85);
  });

  it("awards cascade Insight for Mass Delusion and Shared Nightmare", () => {
    let mass = debugPatchState(playing(), { dread: 150, insight: 2 });
    mass = { ...mass, regions: { ...mass.regions, africa: { ...mass.regions.africa, influence: 20 } } };
    mass = succeeded(unlockAbility(mass, "mass_delusion"));
    mass = succeeded(castAbility(mass, "mass_delusion", "africa"));
    expect(mass.insight).toBe(5);

    let nightmare = succeeded(spawnEvent(playing("nightmare"), "shared_nightmare", "africa"));
    nightmare = succeeded(castAbility(nightmare, "whispered_dreams", "africa"));
    expect(nightmare.insight).toBe(4);
  });

  it("uses shared event-aware cost and Dread-rate selectors", () => {
    const base = playing();
    const nightmare = succeeded(spawnEvent(base, "shared_nightmare", "africa"));
    expect(effectiveAbilityCost(nightmare, "whispered_dreams", "africa")).toBe(7);
    const institutional = succeeded(placeStartingFoothold(createGameState({ difficulty: "normal", seed: "rate" }), "north_america"));
    const crackdown = succeeded(spawnEvent(institutional, "government_crackdown", "north_america"));
    expect(dreadPerSecond(crackdown)).toBeLessThan(dreadPerSecond(institutional));
    expect(dreadPerSecond({ ...base, status: "awakening" })).toBe(0);
    expect(dreadPerSecond(createGameState({ difficulty: "normal", seed: "placing" }))).toBe(0);
  });

  it("makes regional traits and instability change Madness outcomes", () => {
    const africa = succeeded(castAbility(playing("traits-a"), "whispered_dreams", "africa"));
    const eastAsia = succeeded(castAbility(playing("traits-b"), "whispered_dreams", "east_asia"));
    expect(africa.regions.africa.influence - 8).toBeGreaterThan(eastAsia.regions.east_asia.influence - 8);
    const unstable = { ...playing("traits-c"), regions: { ...playing("traits-c").regions, africa: { ...playing("traits-c").regions.africa, instability: 100 } } };
    expect(succeeded(castAbility(unstable, "whispered_dreams", "africa")).regions.africa.influence).toBeGreaterThan(africa.regions.africa.influence);
  });

  it("resets Contagious Dreams and resolves event interactions consistently", () => {
    let state = advanceSimulation(playing(), 45);
    state = debugPatchState(state, { insight: 3 });
    state = succeeded(unlockUpgrade(state, "contagious_dreams"));
    expect(state.contagiousDreamsElapsed).toBe(0);
    expect(advanceSimulation(state, 19).regions.europe.influence).toBe(0);

    let scandal = debugPatchState(playing("scandal"), { dread: 150, insight: 2 });
    scandal = { ...scandal, regions: { ...scandal.regions, africa: { ...scandal.regions.africa, exposure: 30 } } };
    scandal = succeeded(spawnEvent(scandal, "political_scandal", "africa"));
    scandal = succeeded(unlockAbility(scandal, "rewrite_headline"));
    expect(succeeded(castAbility(scandal, "rewrite_headline", "africa")).regions.africa.exposure).toBe(15);

    let revival = debugPatchState(playing("revival"), { dread: 150, insight: 1 });
    revival = { ...revival, regions: { ...revival.regions, africa: { ...revival.regions.africa, influence: 20 } } };
    revival = succeeded(spawnEvent(revival, "religious_revival", "africa"));
    revival = succeeded(unlockAbility(revival, "impossible_omen"));
    revival = succeeded(castAbility(revival, "impossible_omen", "africa"));
    expect(revival.activeEvents[0]?.resolved).toBe(true);
    expect(revival.regions.africa.timedResistance.some((modifier) => modifier.amount < 0)).toBe(true);
    expect(advanceSimulation(revival, 1).telemetry.eventsResolved).toBe(1);
  });
});
