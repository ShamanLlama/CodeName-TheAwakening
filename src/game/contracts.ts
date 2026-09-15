import type {
  AbilityId, CommandResult, EventId, GameState, RegionId, Speed, StartRunConfig, UpgradeId,
} from "./types";

export interface DebugPatch {
  dread?: number;
  insight?: number;
  suppression?: number;
  allRegionInfluence?: number;
}

export interface EngineContract {
  createGameState(config: StartRunConfig): GameState;
  placeStartingFoothold(state: GameState, regionId: RegionId): CommandResult;
  castAbility(state: GameState, abilityId: AbilityId, regionId: RegionId): CommandResult;
  unlockAbility(state: GameState, abilityId: AbilityId): CommandResult;
  unlockUpgrade(state: GameState, upgradeId: UpgradeId): CommandResult;
  setGameSpeed(state: GameState, speed: Speed): GameState;
  advanceSimulation(state: GameState, seconds: number): GameState;
  startAwakening(state: GameState): CommandResult;
  forceEvent(state: GameState, eventId: EventId, regionId: RegionId): CommandResult;
  debugPatchState(state: GameState, patch: DebugPatch): GameState;
}

export interface DirectorContract {
  tickDirector(state: GameState): GameState;
  tickActiveEvents(state: GameState): GameState;
  spawnEvent(state: GameState, eventId: EventId, regionId: RegionId, severity?: 1 | 2 | 3): CommandResult;
  suppressionGain(state: GameState): number;
}
