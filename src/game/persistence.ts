import { BALANCE as B } from "../data/balance";
import { REGIONS } from "../data/regions";
import { deadline, readySites, totalFollowers } from "./derived";
import { ACTION_IDS, REGION_IDS, type GameState } from "./types";
export const SAVE_KEY = "cthulhu-awakening-save-v2";
export const LEGACY_SAVE_KEY = "cthulhu-awakening-save";
export const RECOVERY_KEY = "cthulhu-awakening-save-v2-recovery";
export const SAVE_VERSION = 2;
export interface GameSettings { muted: boolean; volume: number }
export interface RunResult { won: boolean; month: number; followers: number; sites: number; seed: string }
export interface SaveEnvelope { version: 2; state: GameState | null; settings: GameSettings; tutorialComplete: boolean; lastResult: RunResult | null }
export interface LoadResult { save: SaveEnvelope; notice: string; blocked: boolean }
export const defaultSave = (): SaveEnvelope => ({ version: 2, state: null, settings: { muted: true, volume: 40 }, tutorialComplete: false, lastResult: null });
const record = (v: unknown): v is Record<string, unknown> => typeof v === "object" && v !== null && !Array.isArray(v);
const number = (v: unknown, max = Number.MAX_SAFE_INTEGER): v is number => typeof v === "number" && Number.isFinite(v) && v >= 0 && v <= max;
const integer = (v: unknown, max = Number.MAX_SAFE_INTEGER): v is number => number(v, max) && Number.isInteger(v);
const oneOf = (v: unknown, choices: readonly string[]) => typeof v === "string" && choices.includes(v);
function settings(v: unknown): v is GameSettings { return record(v) && typeof v.muted === "boolean" && number(v.volume, 100); }
export function isGameState(v: unknown): v is GameState {
  if (!record(v) || v.version !== 2 || v.ruleset !== "alignment-1" || typeof v.seed !== "string"
    || !integer(v.rngState, 4294967295) || !integer(v.elapsedMonths, deadline)
    || !oneOf(v.status, ["placing", "playing", "alignment", "awakening", "won", "lost"])
    || !oneOf(v.difficulty, ["easy", "normal", "hard"]) || ![0, 1, 2].includes(v.speed as number)
    || !number(v.influence, B.influenceCap) || !number(v.dread, B.dreadCap) || !number(v.awareness, 100)
    || (v.specialization !== null && !oneOf(v.specialization, ["recruitment", "infiltration", "terror"]))
    || !integer(v.finalProgress, B.finalMonths) || !integer(v.nextLogId)
    || (v.resultReason !== null && typeof v.resultReason !== "string") || !record(v.regions)) return false;
  if (v.status === "placing" && v.elapsedMonths !== 0 || v.status === "playing" && v.elapsedMonths >= B.preparationMonths
    || ["alignment", "awakening", "won"].includes(String(v.status)) && v.elapsedMonths < B.preparationMonths
    || v.status === "won" && v.finalProgress !== B.finalMonths || v.status === "lost" && v.elapsedMonths !== deadline
    || ["won", "lost"].includes(String(v.status)) && v.speed !== 0) return false;
  for (const id of REGION_IDS) {
    const r = v.regions[id];
    if (!record(r) || !record(r.population) || typeof r.public !== "boolean" || !number(r.exposure, 100)
      || !number(r.madness, 100) || !integer(r.backlashUntil) || !record(r.cooldowns) || !record(r.site)
      || typeof r.site.invested !== "boolean" || !integer(r.site.progress, B.siteMonths)
      || !r.site.invested && r.site.progress !== 0) return false;
    const population = r.population;
    const keys = ["unaware", "deniers", "believers", "followers", "losses"];
    if (keys.some(k => !number(population[k]))) return false;
    const sum = keys.reduce((s, k) => s + (population[k] as number), 0);
    if (Math.abs(sum - REGIONS[id].populationWeight * B.populationScale) > 0.00001) return false;
    if (Object.entries(r.cooldowns).some(([key, val]) => !ACTION_IDS.includes(key as typeof ACTION_IDS[number]) || !integer(val))) return false;
    if (r.threat !== null && (!record(r.threat) || !oneOf(r.threat.kind, ["investigation", "raid", "occult"])
      || !integer(r.threat.dueMonth) || r.threat.dueMonth <= v.elapsedMonths || !integer(r.threat.severity, 3) || r.threat.severity < 1)) return false;
    if (r.opportunity !== null && (!record(r.opportunity) || !oneOf(r.opportunity.kind, ["nightmare", "eclipse", "celebrity", "politician"])
      || !integer(r.opportunity.expiresMonth) || r.opportunity.expiresMonth <= v.elapsedMonths)) return false;
    if (r.figure !== null && (!record(r.figure) || !oneOf(r.figure.kind, ["celebrity", "politician"]) || typeof r.figure.revealed !== "boolean")) return false;
  }
  if (!Array.isArray(v.log) || v.log.length > 100) return false;
  let lastId = 0;
  for (const entry of v.log) {
    if (!record(entry) || !integer(entry.id) || entry.id <= lastId || entry.id >= v.nextLogId
      || !integer(entry.month, v.elapsedMonths) || typeof entry.message !== "string") return false;
    lastId = entry.id;
  }
  const t = v.telemetry;
  if (!record(t) || !record(t.actions) || !record(t.revelations)
    || ["sacrificed", "raidLosses", "influenceEarned", "influenceSpent", "dreadEarned", "dreadSpent", "threatsCountered"].some(k => !number(t[k]))) return false;
  if (Object.entries(t.actions).some(([k, n]) => !ACTION_IDS.includes(k as typeof ACTION_IDS[number]) || !integer(n))) return false;
  const elapsedMonths = v.elapsedMonths;
  if (Object.entries(t.revelations).some(([k, n]) => !REGION_IDS.includes(k as typeof REGION_IDS[number]) || !integer(n, elapsedMonths))) return false;
  return true;
}
export function decodeSave(raw: string | null): SaveEnvelope | null {
  if (raw === null) return defaultSave();
  try {
    const v: unknown = JSON.parse(raw);
    if (!record(v) || v.version !== 2 || !settings(v.settings) || typeof v.tutorialComplete !== "boolean"
      || (v.state !== null && !isGameState(v.state))) return null;
    const r = v.lastResult;
    if (r !== null && (!record(r) || typeof r.won !== "boolean" || !integer(r.month, deadline)
      || !number(r.followers, 10000) || !integer(r.sites, 8) || typeof r.seed !== "string")) return null;
    return v as unknown as SaveEnvelope;
  } catch { return null; }
}
export function loadRun(storage: Storage): LoadResult {
  try {
    const raw = storage.getItem(SAVE_KEY);
    if (raw !== null) {
      const decoded = decodeSave(raw);
      return decoded ? { save: decoded, notice: "", blocked: false }
        : { save: defaultSave(), notice: "This save could not be read. Starting fresh will retain a recovery copy.", blocked: true };
    }
    const save = defaultSave(), legacy = storage.getItem(LEGACY_SAVE_KEY);
    if (legacy) {
      try {
        const old: unknown = JSON.parse(legacy);
        if (record(old) && old.version === 1 && settings(old.settings)) save.settings = old.settings;
      } catch { /* Retain unreadable old data without importing it. */ }
      return { save, notice: "Your earlier run is preserved. The revised rules start a new campaign.", blocked: false };
    }
    return { save, notice: "", blocked: false };
  } catch { return { save: defaultSave(), notice: "Storage is unavailable. You can play, but this session will not save.", blocked: true }; }
}
export function retainRecovery(storage: Storage): boolean {
  try {
    const raw = storage.getItem(SAVE_KEY);
    if (raw !== null && !decodeSave(raw)) storage.setItem(RECOVERY_KEY, raw);
    return true;
  } catch { return false; }
}
export function saveRun(storage: Storage, save: SaveEnvelope): boolean {
  try { storage.setItem(SAVE_KEY, JSON.stringify(save)); return true; } catch { return false; }
}
export function resultFor(state: GameState): RunResult {
  return { won: state.status === "won", month: state.elapsedMonths, followers: totalFollowers(state), sites: readySites(state), seed: state.seed };
}
export const exportTelemetry = (state: GameState): string => JSON.stringify({
  ruleset: state.ruleset, seed: state.seed, difficulty: state.difficulty, month: state.elapsedMonths,
  status: state.status, specialization: state.specialization, resultReason: state.resultReason,
  followers: totalFollowers(state), readySites: readySites(state), finalProgress: state.finalProgress,
  resources: { influence: state.influence, dread: state.dread }, awareness: state.awareness,
  ...state.telemetry,
  logCoverage: { retained: state.log.length, total: state.nextLogId - 1, firstMonth: state.log[0]?.month ?? null, lastMonth: state.log.at(-1)?.month ?? null, truncated: state.nextLogId - 1 > state.log.length },
  regions: Object.fromEntries(Object.entries(state.regions).map(([id, r]) => [id, { population: r.population, public: r.public, exposure: r.exposure, site: r.site, threat: r.threat }])),
  log: state.log,
}, null, 2);
