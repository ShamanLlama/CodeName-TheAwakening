import { REGIONS } from "../data/regions";
import type { GameState, RegionId, RegionState } from "./types";

export const clamp = (value: number, minimum = 0, maximum = 100) => Math.min(maximum, Math.max(minimum, value));

export function globalInfluence(regions: Record<RegionId, RegionState>): number {
  return Object.values(REGIONS).reduce((sum, definition) => sum + definition.populationWeight * regions[definition.id].influence / 100, 0);
}

export function averageExposure(state: GameState): number {
  return Object.values(state.regions).reduce((sum, region) => sum + region.exposure, 0) / 8;
}

export function effectiveResistance(state: GameState, regionId: RegionId): number {
  const region = state.regions[regionId];
  return clamp(REGIONS[regionId].institutionalResistance + region.resistanceModifier
    + region.timedResistance.reduce((sum, modifier) => sum + modifier.amount, 0));
}

export function withDerived(state: GameState): GameState {
  return { ...state, globalInfluence: clamp(globalInfluence(state.regions)), dread: clamp(state.dread, 0, 150), suppression: clamp(state.suppression) };
}
