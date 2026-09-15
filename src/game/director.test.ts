import { describe, expect, it } from "vitest";
import { EVENT_IDS, REGION_IDS } from "./types";
import type { EventId, GameState, RegionId, RegionState } from "./types";
import { spawnEvent, suppressionGain, tickActiveEvents, tickDirector } from "./director";

const region = (): RegionState => ({
  influence: 0,
  cultStrength: 0,
  resistanceModifier: 0,
  exposure: 0,
  instability: 0,
  activeEventId: null,
  milestoneFlags: [],
  timedResistance: [],
});

const state = (): GameState => ({
  version: 1,
  status: "playing",
  difficulty: "normal",
  seed: "rlyeh",
  rngState: 12345,
  elapsedSeconds: 0,
  speed: 1,
  dread: 50,
  insight: 0,
  globalInfluence: 0,
  suppression: 10,
  awakeningSecondsRemaining: null,
  regions: {
    north_america: region(), latin_america: region(), europe: region(), africa: region(),
    middle_east: region(), south_asia: region(), east_asia: region(), oceania: region(),
  },
  unlockedAbilities: [],
  unlockedUpgrades: [],
  cooldowns: { secret_gathering: 0, sacrament_of_the_deep: 0, whispered_dreams: 0, mass_delusion: 0, impossible_omen: 0, rewrite_headline: 0 },
  activeEvents: [],
  recentEventIds: [],
  log: [],
  startingFootholdPlaced: true,
  selectedRegionId: null,
  selectedAbilityId: null,
  nextEventThreshold: 35,
  pendingEventSeconds: null,
  lastAbilityUsedAt: 0,
  lastEventSpawnAt: 0,
  contagiousDreamsElapsed: 0,
  telemetry: { casts: { secret_gathering: 0, sacrament_of_the_deep: 0, whispered_dreams: 0, mass_delusion: 0, impossible_omen: 0, rewrite_headline: 0 }, eventsResolved: 0, eventsExpired: 0 },
});

function spawned(eventId: EventId, regionId: RegionId, severity: 1 | 2 | 3 = 1): GameState {
  const result = spawnEvent(state(), eventId, regionId, severity);
  if (!result.ok) throw new Error(result.message);
  return result.state;
}

describe("event director", () => {
  it("allows every event in a legal region and rejects invalid special regions", () => {
    const legalRegions: Record<EventId, RegionId> = {
      investigators_arrive: "north_america",
      government_crackdown: "north_america",
      religious_revival: "north_america",
      secret_society_intervention: "europe",
      shared_nightmare: "north_america",
      solar_eclipse: "north_america",
      political_scandal: "north_america",
      deep_sea_signal: "north_america",
    };
    for (const eventId of EVENT_IDS) {
      const fixture = state();
      if (eventId === "secret_society_intervention") fixture.regions.europe.influence = 35;
      expect(spawnEvent(fixture, eventId, legalRegions[eventId]).ok).toBe(true);
    }
    expect(spawnEvent(state(), "government_crackdown", "latin_america").ok).toBe(false);
    expect(spawnEvent(state(), "deep_sea_signal", "middle_east").ok).toBe(false);
  });

  it("enforces region, active, and recent-event limits", () => {
    const first = spawned("shared_nightmare", "north_america");
    expect(spawnEvent(first, "solar_eclipse", "north_america").ok).toBe(false);
    expect(spawnEvent(first, "shared_nightmare", "latin_america").ok).toBe(false);
    const two = spawnEvent(first, "solar_eclipse", "latin_america");
    if (!two.ok) throw new Error(two.message);
    const three = spawnEvent(two.state, "political_scandal", "europe");
    if (!three.ok) throw new Error(three.message);
    expect(spawnEvent(three.state, "deep_sea_signal", "africa").ok).toBe(false);
  });

  it("is seeded, warns before the first event, and spawns by 90 seconds", () => {
    const run = (): GameState => {
      let current = { ...state(), elapsedSeconds: 75, globalInfluence: 70 };
      current = tickDirector(current);
      for (let second = 76; second <= 91; second += 1) current = tickDirector({ ...current, elapsedSeconds: second });
      return current;
    };
    const left = run();
    const right = run();
    expect(left.activeEvents).toEqual(right.activeEvents);
    expect(left.rngState).toBe(right.rngState);
    expect(left.activeEvents).toHaveLength(1);
    expect(left.lastEventSpawnAt).toBeGreaterThanOrEqual(80);
    expect(left.lastEventSpawnAt).toBeLessThanOrEqual(90);
  });

  it("calculates suppression including False Consensus", () => {
    const base = state();
    for (const regionId of REGION_IDS) base.regions[regionId].exposure = 20;
    const hostile = spawned("investigators_arrive", "north_america", 2);
    const withExposure = { ...hostile, regions: base.regions };
    expect(suppressionGain(withExposure)).toBeCloseTo(0.325);
    expect(suppressionGain({ ...withExposure, unlockedUpgrades: ["false_consensus"] })).toBeCloseTo(0.30625);
  });

  it("applies tick and expiry effects, then resolves event outcomes once", () => {
    const crackdown = spawned("government_crackdown", "north_america", 2);
    const ticking = { ...crackdown, regions: { ...crackdown.regions, north_america: { ...crackdown.regions.north_america, cultStrength: 10 } } };
    expect(tickActiveEvents(ticking).regions.north_america.cultStrength).toBeCloseTo(9.75);

    const investigator = spawned("investigators_arrive", "north_america", 1);
    const active = investigator.activeEvents.find((event) => event.eventId === "investigators_arrive");
    if (active === undefined) throw new Error("missing investigator event");
    const expiring = {
      ...investigator,
      regions: { ...investigator.regions, north_america: { ...investigator.regions.north_america, cultStrength: 30 } },
      activeEvents: [{ ...active, remainingSeconds: 1 }],
    };
    const expired = tickActiveEvents(expiring);
    expect(expired.regions.north_america.cultStrength).toBe(22);
    expect(expired.activeEvents).toHaveLength(0);

    const nightmare = spawned("shared_nightmare", "north_america");
    const resolved = tickActiveEvents({ ...nightmare, activeEvents: nightmare.activeEvents.map((event) => ({ ...event, resolved: true })) });
    expect(resolved.suppression).toBe(9);
    expect(resolved.telemetry.eventsResolved).toBe(1);
  });
});
