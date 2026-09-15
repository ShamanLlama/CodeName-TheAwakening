import { ABILITY_IDS, EVENT_IDS, REGION_IDS, UPGRADE_IDS, type ActiveEvent, type Difficulty, type GameState } from "./types";

export const SAVE_KEY = "cthulhu-awakening-save";
export const SAVE_VERSION = 1;

export interface GameSettings {
  muted: boolean;
  volume: number;
}

export interface BestResult {
  won: boolean;
  elapsedSeconds: number;
  finalInfluence: number;
  finalSuppression: number;
}

export interface SaveEnvelope {
  version: 1;
  state: GameState | null;
  settings: GameSettings;
  tutorialComplete: boolean;
  bestResults: Partial<Record<Difficulty, BestResult>>;
}

export const DEFAULT_SAVE: SaveEnvelope = {
  version: SAVE_VERSION,
  state: null,
  settings: { muted: false, volume: 55 },
  tutorialComplete: false,
  bestResults: {},
};

const isRecord = (value: unknown): value is Record<string, unknown> => typeof value === "object" && value !== null && !Array.isArray(value);
const isNumber = (value: unknown): value is number => typeof value === "number" && Number.isFinite(value);
const inRange = (value: unknown, minimum: number, maximum: number) => isNumber(value) && value >= minimum && value <= maximum;
const isId = (value: unknown, ids: readonly string[]) => typeof value === "string" && ids.some((id) => id === value);

function isRegion(value: unknown): boolean {
  if (!isRecord(value) || !inRange(value.influence, 0, 100) || !inRange(value.cultStrength, 0, 100)
    || !inRange(value.exposure, 0, 100) || !inRange(value.instability, 0, 100) || !inRange(value.resistanceModifier, -100, 100)) return false;
  if (value.activeEventId !== null && typeof value.activeEventId !== "string") return false;
  if (!Array.isArray(value.milestoneFlags) || value.milestoneFlags.some((flag) => typeof flag !== "string")) return false;
  return Array.isArray(value.timedResistance) && value.timedResistance.every((modifier) => isRecord(modifier)
    && inRange(modifier.amount, -100, 100) && inRange(modifier.remainingSeconds, 0, 10_000)
    && (modifier.sourceEventId === undefined || typeof modifier.sourceEventId === "string"));
}

function isActiveEvent(value: unknown): value is ActiveEvent {
  return isRecord(value) && typeof value.instanceId === "string" && isId(value.eventId, EVENT_IDS)
    && isId(value.regionId, REGION_IDS) && [1, 2, 3].includes(Number(value.severity))
    && inRange(value.remainingSeconds, 0, 10_000) && typeof value.resolved === "boolean"
    && inRange(value.spentDread, 0, 10_000) && typeof value.firstAbilityUsed === "boolean";
}

function isLogEntry(value: unknown): boolean {
  return isRecord(value) && inRange(value.id, 0, Number.MAX_SAFE_INTEGER) && inRange(value.elapsedSeconds, 0, Number.MAX_SAFE_INTEGER)
    && ["system", "ability", "event", "milestone", "result"].includes(String(value.kind)) && typeof value.message === "string";
}

function isSettings(value: unknown): value is GameSettings {
  return isRecord(value) && typeof value.muted === "boolean" && isNumber(value.volume) && value.volume >= 0 && value.volume <= 100;
}

function isBestResult(value: unknown): value is BestResult {
  return isRecord(value) && typeof value.won === "boolean" && inRange(value.elapsedSeconds, 0, Number.MAX_SAFE_INTEGER)
    && inRange(value.finalInfluence, 0, 100) && inRange(value.finalSuppression, 0, 100);
}

function isGameState(value: unknown): value is GameState {
  if (!isRecord(value) || value.version !== 1 || typeof value.seed !== "string") return false;
  if (!["setup", "placing", "playing", "awakening", "won", "lost"].includes(String(value.status))) return false;
  if (!["easy", "normal", "hard"].includes(String(value.difficulty))) return false;
  if (![0, 1, 2].includes(Number(value.speed))) return false;
  const numericKeys = ["rngState", "elapsedSeconds", "insight", "nextEventThreshold", "lastAbilityUsedAt", "lastEventSpawnAt", "contagiousDreamsElapsed"];
  if (numericKeys.some((key) => !inRange(value[key], 0, Number.MAX_SAFE_INTEGER))) return false;
  if (!inRange(value.dread, 0, 150) || !inRange(value.globalInfluence, 0, 100) || !inRange(value.suppression, 0, 100)) return false;
  if (value.awakeningSecondsRemaining !== null && !inRange(value.awakeningSecondsRemaining, 0, 45)) return false;
  if (value.pendingEventSeconds !== null && !inRange(value.pendingEventSeconds, 0, 10_000)) return false;
  const regions = value.regions;
  if (!isRecord(regions) || REGION_IDS.some((id) => !isRegion(regions[id]))) return false;
  if (!Array.isArray(value.unlockedAbilities) || value.unlockedAbilities.some((valueId) => !isId(valueId, ABILITY_IDS))) return false;
  if (!Array.isArray(value.unlockedUpgrades) || value.unlockedUpgrades.some((valueId) => !isId(valueId, UPGRADE_IDS))) return false;
  const cooldowns = value.cooldowns;
  if (!isRecord(cooldowns) || ABILITY_IDS.some((id) => !inRange(cooldowns[id], 0, 10_000))) return false;
  if (!Array.isArray(value.activeEvents) || value.activeEvents.length > 3) return false;
  const activeEvents: ActiveEvent[] = [];
  for (const event of value.activeEvents) {
    if (!isActiveEvent(event) || activeEvents.some((active) => active.instanceId === event.instanceId || active.regionId === event.regionId)) return false;
    const eventRegion = regions[event.regionId];
    if (!isRecord(eventRegion) || eventRegion.activeEventId !== event.instanceId) return false;
    activeEvents.push(event);
  }
  if (!Array.isArray(value.recentEventIds) || value.recentEventIds.length > 3 || value.recentEventIds.some((id) => !isId(id, EVENT_IDS))) return false;
  if (!Array.isArray(value.log) || !value.log.every(isLogEntry)) return false;
  if ((value.selectedRegionId !== null && !isId(value.selectedRegionId, REGION_IDS)) || (value.selectedAbilityId !== null && !isId(value.selectedAbilityId, ABILITY_IDS))) return false;
  const telemetry = value.telemetry;
  if (!isRecord(telemetry)) return false;
  const casts = telemetry.casts;
  if (!isRecord(casts) || ABILITY_IDS.some((id) => !inRange(casts[id], 0, Number.MAX_SAFE_INTEGER))
    || !inRange(telemetry.eventsResolved, 0, Number.MAX_SAFE_INTEGER) || !inRange(telemetry.eventsExpired, 0, Number.MAX_SAFE_INTEGER)) return false;
  if (REGION_IDS.some((id) => {
    const region = regions[id];
    const linked = isRecord(region) && region.activeEventId !== null
      ? activeEvents.find((event) => event.instanceId === region.activeEventId) : undefined;
    return linked !== undefined && linked.regionId !== id || isRecord(region) && region.activeEventId !== null && linked === undefined;
  })) return false;
  return typeof value.startingFootholdPlaced === "boolean";
}

function isSaveEnvelope(value: unknown): value is SaveEnvelope {
  if (!isRecord(value) || value.version !== SAVE_VERSION || !isSettings(value.settings)) return false;
  if (typeof value.tutorialComplete !== "boolean" || !isRecord(value.bestResults)
    || Object.keys(value.bestResults).some((key) => !["easy", "normal", "hard"].includes(key))) return false;
  if (value.state !== null && !isGameState(value.state)) return false;
  for (const result of Object.values(value.bestResults)) if (!isBestResult(result)) return false;
  return true;
}

export function decodeSave(raw: string | null): SaveEnvelope | null {
  if (raw === null) return DEFAULT_SAVE;
  try {
    const parsed: unknown = JSON.parse(raw);
    return isSaveEnvelope(parsed) ? parsed : null;
  } catch {
    return null;
  }
}

export function encodeSave(save: SaveEnvelope): string {
  return JSON.stringify(save);
}

export function saveRun(storage: Storage, save: SaveEnvelope): void {
  storage.setItem(SAVE_KEY, encodeSave(save));
}

export function loadRun(storage: Storage): SaveEnvelope {
  const decoded = decodeSave(storage.getItem(SAVE_KEY));
  if (decoded) return decoded;
  storage.removeItem(SAVE_KEY);
  return DEFAULT_SAVE;
}

export function resultFor(state: GameState): BestResult {
  return { won: state.status === "won", elapsedSeconds: state.elapsedSeconds, finalInfluence: state.globalInfluence, finalSuppression: state.suppression };
}

export function exportTelemetry(state: GameState): string {
  return JSON.stringify({ seed: state.seed, difficulty: state.difficulty, elapsedSeconds: state.elapsedSeconds, status: state.status, finalInfluence: state.globalInfluence, suppression: state.suppression, ...state.telemetry }, null, 2);
}
