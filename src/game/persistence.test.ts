import { describe, expect, it } from "vitest";
import { decodeSave, DEFAULT_SAVE, encodeSave, type SaveEnvelope } from "./persistence";
import { createGameState, placeStartingFoothold } from "./engine";
import { spawnEvent } from "./director";

describe("versioned persistence", () => {
  it("round trips settings and metadata", () => {
    const save: SaveEnvelope = { ...DEFAULT_SAVE, settings: { muted: true, volume: 25 }, tutorialComplete: true };
    expect(decodeSave(encodeSave(save))).toEqual(save);
  });

  it("fails invalid and obsolete saves safely", () => {
    expect(decodeSave("not-json")).toBeNull();
    expect(decodeSave('{"version":0}')).toBeNull();
  });

  it("rejects malformed nested run data", () => {
    const state = createGameState({ difficulty: "normal", seed: "nested" });
    const malformedRegion = { ...DEFAULT_SAVE, state: { ...state, regions: { ...state.regions, africa: { ...state.regions.africa, timedResistance: "broken" } } } };
    const malformedEvent = { ...DEFAULT_SAVE, state: { ...state, activeEvents: [{ eventId: "unknown" }] } };
    expect(decodeSave(JSON.stringify(malformedRegion))).toBeNull();
    expect(decodeSave(JSON.stringify(malformedEvent))).toBeNull();
  });

  it("rejects broken event linkage and oversized director history", () => {
    const placed = placeStartingFoothold(createGameState({ difficulty: "normal", seed: "links" }), "africa");
    if (!placed.ok) throw new Error(placed.message);
    const spawned = spawnEvent(placed.state, "shared_nightmare", "africa");
    if (!spawned.ok) throw new Error(spawned.message);
    const unlinked = { ...DEFAULT_SAVE, state: { ...spawned.state, regions: { ...spawned.state.regions, africa: { ...spawned.state.regions.africa, activeEventId: null } } } };
    const longHistory = { ...DEFAULT_SAVE, state: { ...placed.state, recentEventIds: ["shared_nightmare", "solar_eclipse", "political_scandal", "deep_sea_signal"] } };
    expect(decodeSave(JSON.stringify(unlinked))).toBeNull();
    expect(decodeSave(JSON.stringify(longHistory))).toBeNull();
  });

  it("returns defaults when no save exists", () => {
    expect(decodeSave(null)).toEqual(DEFAULT_SAVE);
  });
});
