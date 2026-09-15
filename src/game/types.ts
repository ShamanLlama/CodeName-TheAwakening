export const REGION_IDS = [
  "north_america", "latin_america", "europe", "africa",
  "middle_east", "south_asia", "east_asia", "oceania",
] as const;
export type RegionId = (typeof REGION_IDS)[number];

export const ABILITY_IDS = [
  "secret_gathering", "sacrament_of_the_deep", "whispered_dreams",
  "mass_delusion", "impossible_omen", "rewrite_headline",
] as const;
export type AbilityId = (typeof ABILITY_IDS)[number];

export const UPGRADE_IDS = ["hidden_cells", "contagious_dreams", "false_consensus"] as const;
export type UpgradeId = (typeof UPGRADE_IDS)[number];

export const EVENT_IDS = [
  "investigators_arrive", "government_crackdown", "religious_revival",
  "secret_society_intervention", "shared_nightmare", "solar_eclipse",
  "political_scandal", "deep_sea_signal",
] as const;
export type EventId = (typeof EVENT_IDS)[number];

export type Difficulty = "easy" | "normal" | "hard";
export type Speed = 0 | 1 | 2;
export type GameStatus = "setup" | "placing" | "playing" | "awakening" | "won" | "lost";
export type AbilityBranch = "ritual" | "madness" | "omen";
export type EventType = "hostile" | "opportunity";

export interface RegionDefinition {
  id: RegionId;
  name: string;
  populationWeight: number;
  skepticism: number;
  institutionalResistance: number;
  mysticism: number;
  adjacent: readonly RegionId[];
  tags: readonly string[];
}

export interface TimedResistanceModifier { amount: number; remainingSeconds: number; sourceEventId?: string }

export interface RegionState {
  influence: number;
  cultStrength: number;
  resistanceModifier: number;
  exposure: number;
  instability: number;
  activeEventId: string | null;
  milestoneFlags: string[];
  timedResistance: TimedResistanceModifier[];
}

export interface AbilityDefinition {
  id: AbilityId;
  name: string;
  branch: AbilityBranch;
  cost: number;
  cooldownSeconds: number;
  unlockCost: number;
  prerequisites: readonly (AbilityId | UpgradeId)[];
  targetRule: string;
  summary: string;
}

export interface UpgradeDefinition {
  id: UpgradeId;
  name: string;
  branch: AbilityBranch;
  unlockCost: number;
  prerequisite: AbilityId;
  summary: string;
}

export interface EventDefinition {
  id: EventId;
  name: string;
  type: EventType;
  durationSeconds: number;
  minimumInfluence: number;
  weight: number;
  validTags?: readonly string[];
  minimumInstitutionalResistance?: number;
  headline: string;
}

export interface ActiveEvent {
  instanceId: string;
  eventId: EventId;
  regionId: RegionId;
  severity: 1 | 2 | 3;
  remainingSeconds: number;
  resolved: boolean;
  spentDread: number;
  firstAbilityUsed: boolean;
}

export interface GameLogEntry {
  id: number;
  elapsedSeconds: number;
  kind: "system" | "ability" | "event" | "milestone" | "result";
  message: string;
}

export interface Telemetry {
  casts: Record<AbilityId, number>;
  eventsResolved: number;
  eventsExpired: number;
}

export interface GameState {
  version: 1;
  status: GameStatus;
  difficulty: Difficulty;
  seed: string;
  rngState: number;
  elapsedSeconds: number;
  speed: Speed;
  dread: number;
  insight: number;
  globalInfluence: number;
  suppression: number;
  awakeningSecondsRemaining: number | null;
  regions: Record<RegionId, RegionState>;
  unlockedAbilities: AbilityId[];
  unlockedUpgrades: UpgradeId[];
  cooldowns: Record<AbilityId, number>;
  activeEvents: ActiveEvent[];
  recentEventIds: EventId[];
  log: GameLogEntry[];
  startingFootholdPlaced: boolean;
  selectedRegionId: RegionId | null;
  selectedAbilityId: AbilityId | null;
  nextEventThreshold: number;
  pendingEventSeconds: number | null;
  lastAbilityUsedAt: number;
  lastEventSpawnAt: number;
  contagiousDreamsElapsed: number;
  telemetry: Telemetry;
}

export type ValidationCode =
  | "INVALID_STATUS" | "INVALID_TARGET" | "INSUFFICIENT_DREAD" | "INSUFFICIENT_INSIGHT"
  | "LOCKED" | "COOLDOWN" | "FOOTHOLD_ALREADY_PLACED" | "INFLUENCE_REQUIRED";

export type CommandResult =
  | { ok: true; state: GameState }
  | { ok: false; code: ValidationCode; message: string; state: GameState };

export interface StartRunConfig { difficulty: Difficulty; seed: string }
