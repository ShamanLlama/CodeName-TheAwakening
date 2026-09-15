import { describe, expect, it } from "vitest";
import { createGameState } from "./engine";
import { decodeSave, defaultSave, LEGACY_SAVE_KEY, loadRun, RECOVERY_KEY, retainRecovery, SAVE_KEY, saveRun } from "./persistence";
function storage(): Storage {
  const data = new Map<string, string>();
  return { get length() { return data.size; }, clear: () => data.clear(), key: i => [...data.keys()][i] ?? null,
    getItem: k => data.get(k) ?? null, setItem: (k, v) => { data.set(k, v); }, removeItem: k => { data.delete(k); } };
}
describe("version 2 save compatibility", () => {
  it("round trips a complete run and settings", () => {
    const save = { ...defaultSave(), state: createGameState({ seed: "save", difficulty: "normal" }) };
    expect(decodeSave(JSON.stringify(save))).toEqual(save);
  });
  it("retains the legacy run and imports only valid settings", () => {
    const s = storage(), legacy = JSON.stringify({ version: 1, state: { oldRun: true }, settings: { muted: false, volume: 20 }, tutorialComplete: true });
    s.setItem(LEGACY_SAVE_KEY, legacy);
    const loaded = loadRun(s); expect(loaded.save.settings.volume).toBe(20);
    expect(loaded.save.state).toBeNull(); expect(loaded.save.tutorialComplete).toBe(false);
    saveRun(s, loaded.save); expect(s.getItem(LEGACY_SAVE_KEY)).toBe(legacy);
  });
  it("does not delete corrupted data and retains a recovery copy before replacing it", () => {
    const s = storage(); s.setItem(SAVE_KEY, "corrupted");
    expect(loadRun(s).blocked).toBe(true); expect(s.getItem(SAVE_KEY)).toBe("corrupted");
    expect(retainRecovery(s)).toBe(true); saveRun(s, defaultSave());
    expect(s.getItem(RECOVERY_KEY)).toBe("corrupted");
  });
  it("rejects malformed numbers, population, clocks and nested action data", () => {
    const state = createGameState({ seed: "invalid", difficulty: "normal" });
    for (const bad of [
      { ...state, elapsedMonths: 1.2 }, { ...state, dread: -1 }, { ...state, version: 1 },
      { ...state, telemetry: { ...state.telemetry, actions: { unknown: 2 } } },
      { ...state, regions: { ...state.regions, africa: { ...state.regions.africa, population: { ...state.regions.africa.population, followers: 999 } } } },
    ]) expect(decodeSave(JSON.stringify({ ...defaultSave(), state: bad }))).toBeNull();
  });
  it("continues in memory when browser storage throws", () => {
    const s = storage(); s.getItem = () => { throw new Error("blocked"); }; s.setItem = () => { throw new Error("full"); };
    expect(loadRun(s).blocked).toBe(true); expect(saveRun(s, defaultSave())).toBe(false);
  });
});
