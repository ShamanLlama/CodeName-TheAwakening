export const REGION_IDS = ["north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"] as const;
export type RegionId = typeof REGION_IDS[number];
export type Difficulty = "easy" | "normal" | "hard";
export type Speed = 0 | 1 | 2;
export type GameStatus = "placing" | "playing" | "alignment" | "awakening" | "won" | "lost";
export type Specialization = "recruitment" | "infiltration" | "terror";
export const ACTION_IDS = ["recruit", "expand", "sacrifice", "reveal", "hide", "conceal", "manifest", "prepare", "counter", "exploit", "convert", "reveal_figure"] as const;
export type ActionId = typeof ACTION_IDS[number];
export interface RegionDefinition {
  id: RegionId; name: string; populationWeight: number; skepticism: number;
  institutionalResistance: number; mysticism: number; adjacent: readonly RegionId[]; tags: readonly string[];
}
export interface Population { unaware: number; deniers: number; believers: number; followers: number; losses: number }
export interface Threat {
  kind: "investigation" | "raid" | "occult"; dueMonth: number; severity: number;
}
export interface Opportunity {
  kind: "nightmare" | "eclipse" | "celebrity" | "politician"; expiresMonth: number;
}
export interface RegionState {
  population: Population; public: boolean; exposure: number; madness: number;
  backlashUntil: number; cooldowns: Partial<Record<ActionId, number>>;
  site: { invested: boolean; progress: number };
  threat: Threat | null; opportunity: Opportunity | null;
  figure: { kind: "celebrity" | "politician"; revealed: boolean } | null;
}
export interface GameLogEntry { id: number; month: number; message: string }
export interface Telemetry {
  actions: Partial<Record<ActionId, number>>; sacrificed: number; raidLosses: number;
  influenceEarned: number; influenceSpent: number; dreadEarned: number; dreadSpent: number;
  threatsCountered: number; revelations: Partial<Record<RegionId, number>>;
}
export interface GameState {
  version: 2; ruleset: "alignment-1"; status: GameStatus; difficulty: Difficulty;
  seed: string; rngState: number; elapsedMonths: number; speed: Speed;
  influence: number; dread: number; awareness: number; specialization: Specialization | null;
  finalProgress: number; regions: Record<RegionId, RegionState>;
  log: GameLogEntry[]; nextLogId: number; telemetry: Telemetry; resultReason: string | null;
}
export interface StartRunConfig { difficulty: Difficulty; seed: string }
export interface ActionDefinition { name: string; description: string; influence: number; dread: number; followers: number; cooldown: number }
export interface ActionPreview {
  ok: boolean; reason: string; influence: number; dread: number; followers: number;
  effect: string; risk: string;
}
export type CommandResult = { ok: true; state: GameState } | { ok: false; message: string; state: GameState };
