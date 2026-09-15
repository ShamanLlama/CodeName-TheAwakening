import type { ActionId, ActionPreview, CommandResult, GameState, RegionId, StartRunConfig } from "./types";
export interface EngineContract {
  createGameState(config: StartRunConfig): GameState;
  placeStartingFoothold(state: GameState, regionId: RegionId): CommandResult;
  previewAction(state: GameState, actionId: ActionId, regionId: RegionId): ActionPreview;
  performAction(state: GameState, actionId: ActionId, regionId: RegionId): CommandResult;
  advanceSimulation(state: GameState, months: number): GameState;
  startAwakening(state: GameState): CommandResult;
}
