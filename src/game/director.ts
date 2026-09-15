import { DIFFICULTIES } from "../data/difficulties";
import { EVENTS } from "../data/events";
import { REGIONS } from "../data/regions";
import { clamp } from "./derived";
import { nextRandom, randomInt } from "./random";
import type { CommandResult, EventId, GameLogEntry, GameState, RegionId } from "./types";
import { EVENT_IDS, REGION_IDS } from "./types";

const MAX_ACTIVE_EVENTS = 3;
const CHECK_INTERVAL_SECONDS = 5;
const MIN_SECONDS_BETWEEN_SPAWNS = 20;

type Candidate = { eventId: EventId; regionId: RegionId; weight: number };

function log(state: GameState, message: string): GameState {
  const last = state.log[state.log.length - 1];
  const entry: GameLogEntry = {
    id: (last?.id ?? 0) + 1,
    elapsedSeconds: state.elapsedSeconds,
    kind: "event",
    message,
  };
  return { ...state, log: [...state.log, entry] };
}

function severityValue(severity: 1 | 2 | 3, one: number, two: number, three: number): number {
  return severity === 1 ? one : severity === 2 ? two : three;
}

function regionIsEligible(state: GameState, eventId: EventId, regionId: RegionId): boolean {
  const event = EVENTS[eventId];
  const region = state.regions[regionId];
  const definition = REGIONS[regionId];
  return region.activeEventId === null
    && region.influence >= event.minimumInfluence
    && (event.minimumInstitutionalResistance === undefined || definition.institutionalResistance >= event.minimumInstitutionalResistance)
    && (event.validTags === undefined || event.validTags.some((tag) => definition.tags.includes(tag)));
}

function hostileLimitReached(state: GameState): boolean {
  return state.globalInfluence < 40
    && state.activeEvents.filter((event) => EVENTS[event.eventId].type === "hostile").length >= 2;
}

function eventIsEligible(state: GameState, eventId: EventId, regionId: RegionId, enforceHistory: boolean): boolean {
  return state.activeEvents.length < MAX_ACTIVE_EVENTS
    && regionIsEligible(state, eventId, regionId)
    && (!enforceHistory || !state.recentEventIds.includes(eventId))
    && (EVENTS[eventId].type !== "hostile" || !hostileLimitReached(state));
}

function currentSeverity(state: GameState): 1 | 2 | 3 {
  if (state.status === "awakening") return 3;
  const influence = state.globalInfluence + (state.difficulty === "hard" ? 10 : 0);
  const severity = influence >= 50 ? 3 : influence >= 25 ? 2 : 1;
  return severity === 3 && state.difficulty !== "hard" && state.elapsedSeconds < 480 ? 2 : severity;
}

function candidates(state: GameState, opportunityOnly: boolean): Candidate[] {
  const lastEvent = state.recentEventIds[state.recentEventIds.length - 1];
  const result: Candidate[] = [];
  for (const eventId of EVENT_IDS) {
    const event = EVENTS[eventId];
    if (opportunityOnly && event.type !== "opportunity") continue;
    for (const regionId of REGION_IDS) {
      if (!eventIsEligible(state, eventId, regionId, true)) continue;
      const region = state.regions[regionId];
      let weight = event.weight;
      if (eventId === "investigators_arrive") weight *= 0.3 + (region.exposure + region.cultStrength) / 50;
      else if (event.type === "hostile") weight *= 0.5 + region.influence / 50;
      else weight *= 0.5 + region.influence / 100;
      if (event.type === "hostile") weight *= 1 + region.instability / 100;
      if (lastEvent !== undefined && EVENTS[lastEvent].type !== event.type) weight *= 1.5;
      if (state.status === "awakening" && event.type === "hostile") weight *= 2;
      if (weight > 0) result.push({ eventId, regionId, weight });
    }
  }
  return result;
}

function pickCandidate(state: GameState, available: Candidate[]): { candidate: Candidate; rngState: number } | null {
  const total = available.reduce((sum, candidate) => sum + candidate.weight, 0);
  if (total <= 0) return null;
  const random = nextRandom(state.rngState);
  let roll = random.value * total;
  for (const candidate of available) {
    roll -= candidate.weight;
    if (roll <= 0) return { candidate, rngState: random.state };
  }
  const fallback = available[available.length - 1];
  return fallback === undefined ? null : { candidate: fallback, rngState: random.state };
}

function clearEvent(state: GameState, instanceId: string): GameState {
  const event = state.activeEvents.find((active) => active.instanceId === instanceId);
  if (event === undefined) return state;
  const region = state.regions[event.regionId];
  const regions = region.activeEventId === instanceId
    ? { ...state.regions, [event.regionId]: { ...region, activeEventId: null, timedResistance: region.timedResistance.filter((modifier) => modifier.sourceEventId !== instanceId) } }
    : state.regions;
  return { ...state, regions, activeEvents: state.activeEvents.filter((active) => active.instanceId !== instanceId) };
}

export function suppressionGain(state: GameState): number {
  const averageExposure = REGION_IDS.reduce((sum, id) => sum + state.regions[id].exposure, 0) / REGION_IDS.length;
  const exposureGain = Math.max(0, averageExposure - 15) * 0.025
    * (state.unlockedUpgrades.includes("false_consensus") ? 0.85 : 1);
  const hostileSeverity = state.activeEvents
    .filter((event) => !event.resolved && EVENTS[event.eventId].type === "hostile")
    .reduce((sum, event) => sum + event.severity, 0);
  const highInfluenceRegions = REGION_IDS.filter((id) => state.regions[id].influence >= 60).length;
  const multiplier = DIFFICULTIES[state.difficulty].suppressionMultiplier * (state.status === "awakening" ? 2 : 1);
  return (exposureGain + hostileSeverity * 0.1 + highInfluenceRegions * 0.05) * multiplier;
}

export function spawnEvent(state: GameState, eventId: EventId, regionId: RegionId, severity = currentSeverity(state)): CommandResult {
  if ((state.status !== "playing" && state.status !== "awakening") || !eventIsEligible(state, eventId, regionId, true)) {
    return { ok: false, code: "INVALID_TARGET", message: "That event cannot occur in this region now.", state };
  }
  const definition = EVENTS[eventId];
  const duration = definition.durationSeconds * (definition.type === "opportunity" && state.unlockedUpgrades.includes("false_consensus") ? 1.2 : 1);
  const instanceId = `event_${state.elapsedSeconds}_${eventId}_${regionId}_${state.log.length}`;
  const activeEvent = { instanceId, eventId, regionId, severity, remainingSeconds: Math.round(duration), resolved: false, spentDread: 0, firstAbilityUsed: false };
  const region = state.regions[regionId];
  const timedResistance = [...region.timedResistance];
  if (eventId === "investigators_arrive") timedResistance.push({ amount: severity * 10, remainingSeconds: activeEvent.remainingSeconds, sourceEventId: instanceId });
  if (eventId === "political_scandal") timedResistance.push({ amount: -20, remainingSeconds: activeEvent.remainingSeconds, sourceEventId: instanceId });
  const regions = { ...state.regions, [regionId]: { ...region, activeEventId: instanceId, timedResistance } };
  const next = log({
    ...state,
    regions,
    activeEvents: [...state.activeEvents, activeEvent],
    recentEventIds: [...state.recentEventIds, eventId].slice(-3),
    lastEventSpawnAt: state.elapsedSeconds,
  }, definition.headline);
  return { ok: true, state: next };
}

export function tickActiveEvents(state: GameState): GameState {
  if (state.status !== "playing" && state.status !== "awakening") return state;
  let regions = state.regions;
  for (const regionId of REGION_IDS) {
    const region = regions[regionId];
    regions = { ...regions, [regionId]: {
      ...region,
      timedResistance: region.timedResistance
        .map((modifier) => ({ ...modifier, remainingSeconds: modifier.remainingSeconds - 1 }))
        .filter((modifier) => modifier.remainingSeconds > 0),
    } };
  }

  let next: GameState = { ...state, regions, activeEvents: state.activeEvents.map((event) => ({ ...event })) };
  for (const event of [...next.activeEvents]) {
    if (event.resolved) {
      const reduction = EVENTS[event.eventId].type === "hostile" ? 2 : 1;
      next = clearEvent({ ...next, suppression: clamp(next.suppression - reduction) }, event.instanceId);
      next = log({ ...next, telemetry: { ...next.telemetry, eventsResolved: next.telemetry.eventsResolved + 1 } }, `${EVENTS[event.eventId].name} is resolved.`);
      continue;
    }
    const region = next.regions[event.regionId];
    if (event.eventId === "government_crackdown") {
      const decay = severityValue(event.severity, 0.15, 0.25, 0.35);
      const protectedDecay = next.unlockedUpgrades.includes("hidden_cells") ? decay * 0.75 : decay;
      next = { ...next, regions: { ...next.regions, [event.regionId]: { ...region, cultStrength: clamp(region.cultStrength - protectedDecay) } } };
    }
    if (event.eventId === "religious_revival") {
      const decay = severityValue(event.severity, 0.08, 0.14, 0.20);
      next = { ...next, regions: { ...next.regions, [event.regionId]: { ...region, influence: clamp(region.influence - decay) } } };
    }
    const updated = { ...event, remainingSeconds: event.remainingSeconds - 1 };
    next = { ...next, activeEvents: next.activeEvents.map((active) => active.instanceId === event.instanceId ? updated : active) };
    if (updated.remainingSeconds > 0) continue;
    let expired = next;
    const expiredRegion = expired.regions[event.regionId];
    if (event.eventId === "investigators_arrive") {
      const loss = severityValue(event.severity, 8, 16, 24) * (expired.unlockedUpgrades.includes("hidden_cells") ? 0.75 : 1);
      expired = { ...expired, regions: { ...expired.regions, [event.regionId]: { ...expiredRegion, cultStrength: clamp(expiredRegion.cultStrength - loss) } } };
    }
    if (event.eventId === "secret_society_intervention") expired = { ...expired, suppression: clamp(expired.suppression + severityValue(event.severity, 3, 6, 9)) };
    next = clearEvent(expired, event.instanceId);
    next = log({ ...next, telemetry: { ...next.telemetry, eventsExpired: next.telemetry.eventsExpired + 1 } }, `${EVENTS[event.eventId].name} ends in ${REGIONS[event.regionId].name}.`);
  }
  return next;
}

export function tickDirector(state: GameState): GameState {
  if (state.status !== "playing" && state.status !== "awakening") return state;
  if (state.pendingEventSeconds !== null) {
    const pending = state.pendingEventSeconds - 1;
    if (pending > 0) return { ...state, pendingEventSeconds: pending };
    const forcedOpportunity = state.elapsedSeconds - state.lastEventSpawnAt >= 80;
    const available = candidates(state, forcedOpportunity);
    const choice = pickCandidate(state, available.length > 0 ? available : candidates(state, false));
    if (choice === null) return { ...state, pendingEventSeconds: null };
    const spawned = spawnEvent({ ...state, rngState: choice.rngState, pendingEventSeconds: null }, choice.candidate.eventId, choice.candidate.regionId);
    if (!spawned.ok) return spawned.state;
    const increment = randomInt(spawned.state.rngState, 22, 32);
    return { ...spawned.state, rngState: increment.state, nextEventThreshold: Math.max(spawned.state.nextEventThreshold, 35) + increment.value };
  }
  if (state.elapsedSeconds % CHECK_INTERVAL_SECONDS !== 0 || state.elapsedSeconds < 60 || state.activeEvents.length >= MAX_ACTIVE_EVENTS) return state;
  if (state.recentEventIds.length > 0 && state.elapsedSeconds - state.lastEventSpawnAt < MIN_SECONDS_BETWEEN_SPAWNS) return state;
  const averageExposure = REGION_IDS.reduce((sum, id) => sum + state.regions[id].exposure, 0) / REGION_IDS.length;
  const difficulty = DIFFICULTIES[state.difficulty];
  const pressure = ((state.elapsedSeconds / 60) * 5 + state.globalInfluence * 0.55 + averageExposure * 0.35 + difficulty.pressureModifier - state.activeEvents.length * 12) * difficulty.eventPressureMultiplier;
  const sinceLast = state.elapsedSeconds - state.lastEventSpawnAt;
  const shouldSchedule = pressure > Math.max(state.nextEventThreshold, 35) || state.recentEventIds.length === 0 && state.elapsedSeconds >= 75 || sinceLast >= 60;
  if (!shouldSchedule) return state;
  const warning = randomInt(state.rngState, 5, 12);
  return { ...state, rngState: warning.state, pendingEventSeconds: warning.value };
}
