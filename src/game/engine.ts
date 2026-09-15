import { ABILITIES, UPGRADES } from "../data/abilities";
import { DIFFICULTIES } from "../data/difficulties";
import { EVENTS } from "../data/events";
import { REGIONS } from "../data/regions";
import type { DebugPatch, EngineContract } from "./contracts";
import { spawnEvent, suppressionGain, tickActiveEvents, tickDirector } from "./director";
import { clamp, effectiveResistance, withDerived } from "./derived";
import { nextRandom, seedToState } from "./random";
import type {
  AbilityId, ActiveEvent, CommandResult, EventId, GameLogEntry, GameState, RegionId, RegionState, Speed,
  StartRunConfig, UpgradeId, ValidationCode,
} from "./types";

const REGION_IDS: RegionId[] = ["north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"];
const ABILITY_IDS: AbilityId[] = ["secret_gathering", "sacrament_of_the_deep", "whispered_dreams", "mass_delusion", "impossible_omen", "rewrite_headline"];
const BASE_SPREAD_PER_SECOND = 0.02;

const blankRegion = (): RegionState => ({
  influence: 0, cultStrength: 0, resistanceModifier: 10, exposure: 0, instability: 0,
  activeEventId: null, milestoneFlags: [], timedResistance: [],
});

const initialRegions = (): Record<RegionId, RegionState> => ({
  north_america: blankRegion(), latin_america: blankRegion(), europe: blankRegion(), africa: blankRegion(),
  middle_east: blankRegion(), south_asia: blankRegion(), east_asia: blankRegion(), oceania: blankRegion(),
});

const log = (state: GameState, kind: GameLogEntry["kind"], message: string): GameState => ({
  ...state,
  log: [...state.log, { id: state.log.length + 1, elapsedSeconds: state.elapsedSeconds, kind, message }].slice(-100),
});

const failure = (state: GameState, code: ValidationCode, message: string): CommandResult => ({ ok: false, code, message, state });
const success = (state: GameState): CommandResult => ({ ok: true, state: withDerived(state) });
const isActive = (state: GameState) => state.status === "playing" || state.status === "awakening";
const hostileEvent = (event: ActiveEvent | undefined): event is ActiveEvent => event !== undefined && EVENTS[event.eventId].type === "hostile";
const isAbilityId = (id: AbilityId | UpgradeId): id is AbilityId => ABILITY_IDS.some((abilityId) => abilityId === id);

function updateRegion(state: GameState, regionId: RegionId, update: (region: RegionState) => RegionState): GameState {
  return { ...state, regions: { ...state.regions, [regionId]: update(state.regions[regionId]) } };
}

function rewardMilestones(state: GameState, regionId: RegionId, previousInfluence: number): GameState {
  let result = state;
  for (const threshold of [25, 50, 75]) {
    const flag = `influence_${threshold}`;
    const region = result.regions[regionId];
    if (previousInfluence < threshold && region.influence >= threshold && !region.milestoneFlags.includes(flag)) {
      result = updateRegion(result, regionId, (current) => ({ ...current, milestoneFlags: [...current.milestoneFlags, flag] }));
      result = log({ ...result, insight: result.insight + 1 }, "milestone", `${REGIONS[regionId].name} reached ${threshold} Influence.`);
    }
  }
  return result;
}

function addInfluence(state: GameState, regionId: RegionId, amount: number): GameState {
  const previous = state.regions[regionId].influence;
  const result = updateRegion(state, regionId, (region) => ({ ...region, influence: clamp(region.influence + amount) }));
  return rewardMilestones(result, regionId, previous);
}

function addCascadeInfluence(state: GameState, regionId: RegionId, amount: number): GameState {
  const wasUninfluenced = state.regions[regionId].influence === 0;
  const result = addInfluence(state, regionId, amount);
  return wasUninfluenced && result.regions[regionId].influence > 0
    ? log({ ...result, insight: result.insight + 1 }, "milestone", `A chain reaction reached ${REGIONS[regionId].name}.`)
    : result;
}

function eventForRegion(state: GameState, regionId: RegionId): ActiveEvent | undefined {
  return state.activeEvents.find((event) => event.regionId === regionId && !event.resolved);
}

export function effectiveAbilityCost(state: GameState, abilityId: AbilityId, regionId: RegionId): number {
  const event = eventForRegion(state, regionId);
  let cost = ABILITIES[abilityId].cost;
  if (event?.eventId === "shared_nightmare" && ABILITIES[abilityId].branch === "madness") cost *= 0.7;
  if (event?.eventId === "secret_society_intervention") cost *= event.severity === 1 ? 1.2 : event.severity === 2 ? 1.35 : 1.5;
  return cost;
}

function ritualMultiplier(state: GameState, abilityId: AbilityId, regionId: RegionId): { effect: number; exposure: number } {
  if (ABILITIES[abilityId].branch !== "ritual" || eventForRegion(state, regionId)?.eventId !== "solar_eclipse") return { effect: 1, exposure: 1 };
  return { effect: 1.35, exposure: 1.25 };
}

function regionalModifiers(state: GameState, abilityId: AbilityId, regionId: RegionId) {
  const definition = REGIONS[regionId];
  const branch = ABILITIES[abilityId].branch;
  let influence = 1;
  let cult = 1;
  let omenDuration = 1;
  let resistanceReduction = 1;
  if (branch === "ritual") {
    if (definition.mysticism >= 60) cult *= abilityId === "secret_gathering" ? 4 / 3 : 1.25;
    if (definition.tags.includes("coastal")) { influence *= 1.1; cult *= 1.1; }
    if (definition.skepticism >= 70) influence *= 0.85;
    if (effectiveResistance(state, regionId) >= 70) { influence *= 0.9; cult *= 0.9; }
  } else if (branch === "madness") {
    if (definition.skepticism >= 70) influence *= 0.9;
    if (effectiveResistance(state, regionId) >= 70) influence *= 0.9;
    influence *= 1 + state.regions[regionId].instability / 500;
  } else {
    if (definition.mysticism >= 60) omenDuration *= 1.1;
    if (definition.skepticism >= 70) resistanceReduction *= 1.2;
    if (effectiveResistance(state, regionId) >= 70) omenDuration *= 1.25;
  }
  return { influence, cult, omenDuration, resistanceReduction, cascadeBonus: branch === "madness" && definition.populationWeight >= 15 ? 1 : 0 };
}

function applyEventAbilityInteractions(state: GameState, abilityId: AbilityId, regionId: RegionId, cost: number): GameState {
  const event = eventForRegion(state, regionId);
  if (!event) return state;
  let result = state;
  if (event.eventId === "shared_nightmare" && ABILITIES[abilityId].branch === "madness") {
    const amount = 2 + regionalModifiers(state, abilityId, regionId).cascadeBonus;
    for (const adjacent of REGIONS[regionId].adjacent) result = addCascadeInfluence(result, adjacent, amount);
  }
  if (event.eventId === "deep_sea_signal") {
    result = {
      ...result,
      activeEvents: result.activeEvents.map((current) => current.instanceId === event.instanceId
        ? { ...current, spentDread: current.spentDread + cost } : current),
    };
    const updated = result.activeEvents.find((current) => current.instanceId === event.instanceId);
    if (updated !== undefined && event.spentDread < 25 && updated.spentDread >= 25) {
      for (const id of REGION_IDS) result = addInfluence(result, id, 1);
      result = {
        ...result,
        activeEvents: result.activeEvents.map((current) => current.instanceId === event.instanceId ? { ...current, resolved: true } : current),
      };
      result = log(result, "event", "The Deep-Sea Signal carried Cthulhu's influence worldwide.");
    }
  }
  return {
    ...result,
    activeEvents: result.activeEvents.map((current) => current.instanceId === event.instanceId
      ? { ...current, firstAbilityUsed: true } : current),
  };
}

function finishNeutralizedHostile(state: GameState, instanceId: string): GameState {
  const event = state.activeEvents.find((current) => current.instanceId === instanceId && !current.resolved);
  if (!event || EVENTS[event.eventId].type !== "hostile") return state;
  const result = {
    ...state,
    insight: state.insight + 1,
    activeEvents: state.activeEvents.map((current) => current.instanceId === instanceId ? { ...current, resolved: true } : current),
  };
  return log(result, "event", `${EVENTS[event.eventId].name} was neutralized before it could escalate.`);
}

function cooldownsAfterSecond(state: GameState): GameState {
  const recovery = state.status === "awakening" ? 1.3 : 1;
  const cooldowns = { ...state.cooldowns };
  for (const id of ABILITY_IDS) cooldowns[id] = Math.max(0, cooldowns[id] - recovery);
  return { ...state, cooldowns };
}

function passiveSpread(state: GameState): GameState {
  let result = state;
  for (const id of REGION_IDS) {
    const region = result.regions[id];
    if (region.cultStrength <= 0) continue;
    const spread = BASE_SPREAD_PER_SECOND * (0.5 + region.cultStrength / 100) * (1.25 - REGIONS[id].skepticism / 200);
    result = addInfluence(result, id, spread);
  }
  return result;
}

function contagiousDreams(state: GameState): GameState {
  if (!state.unlockedUpgrades.includes("contagious_dreams") || state.contagiousDreamsElapsed < 20) return state;
  let source: RegionId = "north_america";
  for (const id of REGION_IDS) if (state.regions[id].influence > state.regions[source].influence) source = id;
  const targets = REGIONS[source].adjacent.filter((id) => state.regions[id].influence < 30);
  let result = { ...state, contagiousDreamsElapsed: state.contagiousDreamsElapsed - 20 };
  if (targets.length === 0) return result;
  const random = nextRandom(result.rngState);
  result = { ...result, rngState: random.state };
  const target = targets.find((_, index) => index === Math.floor(random.value * targets.length));
  if (target === undefined) return result;
  return addCascadeInfluence(result, target, 1);
}

export function dreadPerSecond(state: GameState): number {
  if (state.status !== "playing") return 0;
  let value = 0.2;
  for (const id of REGION_IDS) {
    const region = state.regions[id];
    let modifier = 1;
    const event = eventForRegion(state, id);
    if (event?.eventId === "government_crackdown") modifier *= 0.5;
    if (event?.eventId === "deep_sea_signal") modifier *= 2;
    value += (region.cultStrength * REGIONS[id].populationWeight / 9000 + REGIONS[id].populationWeight * region.influence / 100 / 250) * modifier;
  }
  return value * (state.elapsedSeconds - state.lastAbilityUsedAt >= 45 ? 1.2 : 1);
}

export function createGameState(config: StartRunConfig): GameState {
  return withDerived({
    version: 1, status: "placing", difficulty: config.difficulty, seed: config.seed, rngState: seedToState(config.seed),
    elapsedSeconds: 0, speed: 1, dread: 20, insight: 0, globalInfluence: 0, suppression: 0,
    awakeningSecondsRemaining: null, regions: initialRegions(), unlockedAbilities: ["secret_gathering", "whispered_dreams"],
    unlockedUpgrades: [], cooldowns: { secret_gathering: 0, sacrament_of_the_deep: 0, whispered_dreams: 0, mass_delusion: 0, impossible_omen: 0, rewrite_headline: 0 },
    activeEvents: [], recentEventIds: [], log: [], startingFootholdPlaced: false, selectedRegionId: null, selectedAbilityId: null,
    nextEventThreshold: 35, pendingEventSeconds: null, lastAbilityUsedAt: 0, lastEventSpawnAt: 0, contagiousDreamsElapsed: 0,
    telemetry: { casts: { secret_gathering: 0, sacrament_of_the_deep: 0, whispered_dreams: 0, mass_delusion: 0, impossible_omen: 0, rewrite_headline: 0 }, eventsResolved: 0, eventsExpired: 0 },
  });
}

export function placeStartingFoothold(state: GameState, regionId: RegionId): CommandResult {
  if (state.status !== "placing") return failure(state, state.startingFootholdPlaced ? "FOOTHOLD_ALREADY_PLACED" : "INVALID_STATUS", "A foothold can only be placed at the start of a run.");
  let result = updateRegion(state, regionId, (region) => ({ ...region, influence: clamp(region.influence + 8), cultStrength: clamp(region.cultStrength + 20), exposure: clamp(region.exposure + 5) }));
  result = rewardMilestones(result, regionId, 0);
  result = log({ ...result, status: "playing", startingFootholdPlaced: true, selectedRegionId: regionId }, "system", `A Cult Foothold was established in ${REGIONS[regionId].name}.`);
  return success(result);
}

export function castAbility(state: GameState, abilityId: AbilityId, regionId: RegionId): CommandResult {
  if (!isActive(state)) return failure(state, "INVALID_STATUS", "Abilities are unavailable before the foothold is placed or after the run ends.");
  if (!state.unlockedAbilities.includes(abilityId)) return failure(state, "LOCKED", "This ability has not been unlocked.");
  if (state.cooldowns[abilityId] > 0) return failure(state, "COOLDOWN", "This ability is cooling down.");
  const region = state.regions[regionId];
  if ((abilityId === "secret_gathering" && region.influence <= 0) || (abilityId === "sacrament_of_the_deep" && region.cultStrength < 25) || (abilityId === "mass_delusion" && region.influence < 20) || (abilityId === "rewrite_headline" && !hostileEvent(eventForRegion(state, regionId)) && region.exposure <= 20)) {
    return failure(state, "INFLUENCE_REQUIRED", "This ability's target requirement is not met.");
  }
  const cost = effectiveAbilityCost(state, abilityId, regionId);
  if (state.dread < cost) return failure(state, "INSUFFICIENT_DREAD", "Not enough Dread.");
  const event = eventForRegion(state, regionId);
  const scandalFreeExposure = event?.eventId === "political_scandal" && !event.firstAbilityUsed;
  const ritual = ritualMultiplier(state, abilityId, regionId);
  const regional = regionalModifiers(state, abilityId, regionId);
  let result: GameState = {
    ...state, dread: state.dread - cost, cooldowns: { ...state.cooldowns, [abilityId]: ABILITIES[abilityId].cooldownSeconds },
    selectedAbilityId: abilityId, selectedRegionId: regionId, lastAbilityUsedAt: state.elapsedSeconds,
    telemetry: { ...state.telemetry, casts: { ...state.telemetry.casts, [abilityId]: state.telemetry.casts[abilityId] + 1 } },
  };
  const alter = (influence: number, cult: number, instability: number, exposure: number) => {
    const oldInfluence = result.regions[regionId].influence;
    result = updateRegion(result, regionId, (current) => ({ ...current, influence: clamp(current.influence + influence * regional.influence), cultStrength: clamp(current.cultStrength + cult * regional.cult), instability: clamp(current.instability + instability), exposure: clamp(current.exposure + (scandalFreeExposure && exposure > 0 ? 0 : exposure)) }));
    result = rewardMilestones(result, regionId, oldInfluence);
  };
  if (abilityId === "secret_gathering") {
    const hiddenCells = result.unlockedUpgrades.includes("hidden_cells") ? 3 : 0;
    alter(4 * ritual.effect, 15 * ritual.effect, 0, Math.max(0, 4 - hiddenCells) * ritual.exposure);
  } else if (abilityId === "sacrament_of_the_deep") {
    alter(14 * ritual.effect, 8 * ritual.effect, 10 * ritual.effect, 12 * ritual.exposure);
    const random = nextRandom(result.rngState);
    result = { ...result, rngState: random.state };
    if (random.value < 0.2 + result.regions[regionId].exposure / 500 && !eventForRegion(result, regionId)) {
      const spawned = spawnEvent(result, "investigators_arrive", regionId);
      if (spawned.ok) result = spawned.state;
    }
  } else if (abilityId === "whispered_dreams") {
    alter(6, region.cultStrength <= 0 ? 5 : 0, 5, 3);
  } else if (abilityId === "mass_delusion") {
    alter(16, 0, 18, 15);
    for (const adjacent of REGIONS[regionId].adjacent) result = addCascadeInfluence(result, adjacent, 3 + regional.cascadeBonus);
  } else if (abilityId === "impossible_omen") {
    alter(5, 0, 8, 0);
    const revival = eventForRegion(result, regionId);
    if (revival?.eventId === "religious_revival") {
      result = addInfluence(result, regionId, 8);
      result = updateRegion(result, regionId, (current) => ({ ...current, exposure: clamp(current.exposure + 10) }));
      result = finishNeutralizedHostile(result, revival.instanceId);
      result = log(result, "event", "Religious Revival fractured into an Apocalyptic Schism.");
    }
    result = updateRegion(result, regionId, (current) => ({ ...current, timedResistance: [...current.timedResistance, { amount: -10 * regional.resistanceReduction, remainingSeconds: Math.round(45 * regional.omenDuration) }] }));
  } else {
    alter(-2, 0, 0, -15);
    const targetEvent = eventForRegion(result, regionId);
    if (hostileEvent(targetEvent)) {
      const reduced = targetEvent.severity - 1;
      if (reduced === 0) result = finishNeutralizedHostile(result, targetEvent.instanceId);
      else {
        const severity = targetEvent.severity === 3 ? 2 : 1;
        result = updateRegion(result, regionId, (current) => ({ ...current, timedResistance: current.timedResistance.map((modifier) => modifier.sourceEventId === targetEvent.instanceId ? { ...modifier, amount: severity * 10 } : modifier) }));
        result = { ...result, activeEvents: result.activeEvents.map((current) => current.instanceId === targetEvent.instanceId ? { ...current, severity } : current) };
      }
    }
  }
  result = applyEventAbilityInteractions(result, abilityId, regionId, cost);
  const finalRegion = result.regions[regionId];
  return success(log(result, "ability", `${ABILITIES[abilityId].name} in ${REGIONS[regionId].name}: ${cost.toFixed(1)} Dread, Influence ${region.influence.toFixed(1)}→${finalRegion.influence.toFixed(1)}, Exposure ${region.exposure.toFixed(1)}→${finalRegion.exposure.toFixed(1)}.`));
}

export function unlockAbility(state: GameState, abilityId: AbilityId): CommandResult {
  if (!isActive(state)) return failure(state, "INVALID_STATUS", "Abilities can only be unlocked during a run.");
  if (state.unlockedAbilities.includes(abilityId)) return failure(state, "LOCKED", "This ability is already unlocked.");
  const ability = ABILITIES[abilityId];
  if (!ability.prerequisites.every((prerequisite) => isAbilityId(prerequisite)
    ? state.unlockedAbilities.includes(prerequisite) : state.unlockedUpgrades.includes(prerequisite))) return failure(state, "LOCKED", "This ability's prerequisites are not met.");
  if (state.insight < ability.unlockCost) return failure(state, "INSUFFICIENT_INSIGHT", "Not enough Insight.");
  return success(log({ ...state, insight: state.insight - ability.unlockCost, unlockedAbilities: [...state.unlockedAbilities, abilityId] }, "system", `${ability.name} was unlocked.`));
}

export function unlockUpgrade(state: GameState, upgradeId: UpgradeId): CommandResult {
  if (!isActive(state)) return failure(state, "INVALID_STATUS", "Upgrades can only be unlocked during a run.");
  if (state.unlockedUpgrades.includes(upgradeId)) return failure(state, "LOCKED", "This upgrade is already unlocked.");
  const upgrade = UPGRADES[upgradeId];
  if (!state.unlockedAbilities.includes(upgrade.prerequisite)) return failure(state, "LOCKED", "This upgrade's prerequisite is not unlocked.");
  if (state.insight < upgrade.unlockCost) return failure(state, "INSUFFICIENT_INSIGHT", "Not enough Insight.");
  return success(log({ ...state, insight: state.insight - upgrade.unlockCost, unlockedUpgrades: [...state.unlockedUpgrades, upgradeId], contagiousDreamsElapsed: upgradeId === "contagious_dreams" ? 0 : state.contagiousDreamsElapsed }, "system", `${upgrade.name} was unlocked.`));
}

export function setGameSpeed(state: GameState, speed: Speed): GameState {
  return state.status === "won" || state.status === "lost" ? state : { ...state, speed };
}

export function advanceSimulation(state: GameState, seconds: number): GameState {
  let result = state;
  for (let step = 0; step < Math.max(0, Math.floor(seconds)); step += 1) {
    if (!isActive(result)) break;
    result = { ...result, elapsedSeconds: result.elapsedSeconds + 1, contagiousDreamsElapsed: result.contagiousDreamsElapsed + 1 };
    result = cooldownsAfterSecond(result);
    result = passiveSpread(result);
    result = tickActiveEvents(result);
    result = contagiousDreams(result);
    result = withDerived(result);
    if (result.status === "playing" || result.status === "awakening") result = { ...result, dread: clamp(result.dread + dreadPerSecond(result), 0, 150) };
    if (result.elapsedSeconds % 5 === 0) {
      result = { ...result, suppression: clamp(result.suppression + suppressionGain(result)) };
    }
    result = tickDirector(result);
    if (result.suppression >= 100) {
      result = log({ ...result, status: "lost", speed: 0, awakeningSecondsRemaining: null }, "result", "Humanity's suppression network shattered the cult.");
      break;
    }
    if (result.status === "awakening") {
      const remaining = (result.awakeningSecondsRemaining ?? 0) - 1;
      if (remaining <= 0) {
        const target = DIFFICULTIES[result.difficulty].influenceTarget;
        result = log({ ...result, status: result.globalInfluence >= target && result.suppression < 100 ? "won" : "lost", speed: 0, awakeningSecondsRemaining: 0 }, "result", result.globalInfluence >= target && result.suppression < 100 ? "Cthulhu has awakened." : "The final response drove the cult back." );
        break;
      }
      result = { ...result, awakeningSecondsRemaining: remaining };
    }
    result = withDerived(result);
  }
  return withDerived(result);
}

export function startAwakening(state: GameState): CommandResult {
  if (state.status !== "playing") return failure(state, "INVALID_STATUS", "Awakening can only begin during an active run.");
  if (state.globalInfluence < DIFFICULTIES[state.difficulty].influenceTarget) return failure(state, "INFLUENCE_REQUIRED", "Global Influence has not reached the Awakening threshold.");
  if (state.dread < 100) return failure(state, "INSUFFICIENT_DREAD", "Awakening requires 100 Dread.");
  return success(log({ ...state, status: "awakening", dread: state.dread - 100, awakeningSecondsRemaining: 45 }, "system", "The Awakening has begun."));
}

export function forceEvent(state: GameState, eventId: EventId, regionId: RegionId): CommandResult {
  if (!isActive(state)) return failure(state, "INVALID_STATUS", "Events can only be forced during an active run.");
  return spawnEvent(state, eventId, regionId);
}

export function debugPatchState(state: GameState, patch: DebugPatch): GameState {
  let result: GameState = { ...state, dread: patch.dread === undefined ? state.dread : clamp(patch.dread, 0, 150), insight: patch.insight === undefined ? state.insight : Math.max(0, patch.insight), suppression: patch.suppression === undefined ? state.suppression : clamp(patch.suppression) };
  if (patch.allRegionInfluence !== undefined) {
    for (const id of REGION_IDS) result = addInfluence(result, id, clamp(patch.allRegionInfluence) - result.regions[id].influence);
  }
  result = withDerived(result);
  return result.suppression >= 100 && result.status !== "won"
    ? log({ ...result, status: "lost", speed: 0, awakeningSecondsRemaining: null }, "result", "Humanity's suppression network shattered the cult.")
    : result;
}

export const engine: EngineContract = { createGameState, placeStartingFoothold, castAbility, unlockAbility, unlockUpgrade, setGameSpeed, advanceSimulation, startAwakening, forceEvent, debugPatchState };
