import { ACTIONS } from "../data/abilities";
import { BALANCE as B } from "../data/balance";
import { REGIONS } from "../data/regions";
import { addLog, clamp, cultShare, deadline, influencePerMonth, readySites, recruit, recruitable, recruitmentPerMonth, loseFollowers, siteRequirement, supported, totalFollowers } from "./derived";
import { resolveWorld, scheduleThreat, tickDirector } from "./director";
import { seedToState } from "./random";
import { REGION_IDS, type ActionId, type ActionPreview, type CommandResult, type GameState, type RegionId, type RegionState, type Specialization, type Speed, type StartRunConfig } from "./types";
import type { EngineContract } from "./contracts";

const active = (s: GameState) => s.status === "playing" || s.status === "alignment" || s.status === "awakening";
const fail = (state: GameState, message: string): CommandResult => ({ ok: false, message, state });
const success = (state: GameState): CommandResult => ({ ok: true, state });

export function createGameState(config: StartRunConfig): GameState {
  const regions = Object.fromEntries(REGION_IDS.map(id => {
    const capacity = REGIONS[id].populationWeight * B.populationScale;
    const deniers = Math.round(capacity * REGIONS[id].skepticism / 200);
    return [id, {
      population: { unaware: capacity - deniers, deniers, believers: 0, followers: 0, losses: 0 },
      public: false, exposure: 0, madness: 0, backlashUntil: 0, cooldowns: {},
      site: { invested: false, progress: 0 }, threat: null, opportunity: null, figure: null,
    } satisfies RegionState];
  })) as Record<RegionId, RegionState>;
  return {
    version: 2, ruleset: "alignment-1", status: "placing", ...config, rngState: seedToState(config.seed),
    elapsedMonths: 0, speed: 1, influence: B.startingInfluence, dread: B.startingDread, awareness: 0,
    specialization: null, finalProgress: 0, regions, log: [], nextLogId: 1, resultReason: null,
    telemetry: { actions: {}, sacrificed: 0, raidLosses: 0, influenceEarned: 0, influenceSpent: 0, dreadEarned: 0, dreadSpent: 0, threatsCountered: 0, revelations: {} },
  };
}
export function placeStartingFoothold(state: GameState, id: RegionId): CommandResult {
  if (state.status !== "placing" || !REGION_IDS.includes(id)) return fail(state, "Choose a region before the run begins.");
  const next = structuredClone(state);
  recruit(next.regions[id], B.startingFollowers); next.regions[id].exposure = 5; next.status = "playing";
  addLog(next, "A hidden congregation gathers in " + REGIONS[id].name + ". The alignment is " + B.preparationMonths + " months away.");
  return success(next);
}
// Obstruction is priced by the severity you let build up, so ignoring exposure gets expensive.
export function counterCost(state: GameState, r: RegionState): number {
  const base = B.counterSeverityCost * (r.threat?.severity ?? 1);
  return state.specialization === "infiltration" ? Math.round(base * 2 / 3) : base;
}
// Each further ritual site costs more Dread than the last.
export function siteDreadCost(state: GameState): number {
  const funded = Object.values(state.regions).filter(region => region.site.invested).length;
  const costs: readonly number[] = B.siteDreadCosts;
  return costs[Math.min(funded, costs.length - 1)] ?? B.finalDread;
}
export function previewAction(state: GameState, id: ActionId, regionId: RegionId): ActionPreview {
  const a = ACTIONS[id], r = state.regions[regionId];
  if (!a || !r) return { ok: false, reason: "Unknown action or region.", influence: 0, dread: 0, followers: 0, effect: "", risk: "" };
  const cost = {
    influence: id === "counter" ? counterCost(state, r) : a.influence,
    dread: id === "manifest" && state.specialization === "terror" ? 15 : id === "prepare" ? siteDreadCost(state) : a.dread,
    followers: a.followers,
  };
  let reason = !active(state) ? "Start a run before acting; completed runs cannot change." : "";
  const cooldown = (r.cooldowns[id] ?? 0) - state.elapsedMonths;
  if (!reason && cooldown > 0) reason = "Available in " + cooldown + " months.";
  if (!reason && state.influence < cost.influence) reason = "Need " + cost.influence + " Influence.";
  if (!reason && state.dread < cost.dread) reason = "Need " + cost.dread + " Dread.";
  if (!reason && r.population.followers < cost.followers) reason = "Need " + cost.followers + " local followers.";
  if (!reason && id !== "expand" && r.population.followers <= 0) reason = "Establish a cult here first.";
  if (!reason && (id === "recruit" || id === "expand") && recruitable(r) < 1) reason = "No recruits available. Use a manifestation to change disbelief.";
  if (!reason && id === "expand" && r.population.followers > 0) reason = "This region already has a cell.";
  if (!reason && id === "expand" && totalFollowers(state) > 0 && !REGIONS[regionId].adjacent.some(other => state.regions[other].population.followers > 0)) reason = "Requires a cult in an adjacent region.";
  if (!reason && id === "reveal" && r.public) reason = "Worship is already public.";
  if (!reason && id === "hide" && !r.public) reason = "This cult is already underground.";
  if (!reason && id === "conceal" && r.exposure <= (r.site.invested ? 40 : r.public ? 20 : 0)) reason = "No concealable evidence. Public rites remain visible.";
  if (!reason && id === "manifest" && r.population.deniers + r.population.unaware < 1) reason = "Everyone here already believes.";
  if (!reason && id === "prepare" && r.site.invested) reason = "This site's preparation is already funded.";
  if (!reason && id === "prepare" && !supported(r)) reason = "Maintain at least " + siteRequirement(r) + " local followers.";
  // An unfunded site can still be accelerated once funded: an eclipse exploit, or revealing a converted figure.
  const acceleration = (r.opportunity?.kind === "eclipse" ? 8 : 0) + (r.figure && !r.figure.revealed ? B.figureSiteProgress : 0);
  const preparationLeft = deadline - state.elapsedMonths - B.finalMonths;
  if (!reason && id === "prepare" && preparationLeft < B.siteMonths - acceleration)
    reason = "Too late: preparation needs " + (B.siteMonths - acceleration) + " more months and the final rite needs " + B.finalMonths + ", but only " + Math.max(0, deadline - state.elapsedMonths) + " remain.";
  if (!reason && id === "counter" && !r.threat) reason = "No approaching threat in this region.";
  if (!reason && id === "exploit" && (!r.opportunity || !["nightmare", "eclipse"].includes(r.opportunity.kind))) reason = "Requires an active nightmare or eclipse.";
  if (!reason && id === "exploit" && r.opportunity?.kind === "eclipse" && (!r.site.invested || r.site.progress >= B.siteMonths)) reason = "An eclipse needs an unfinished ritual site.";
  if (!reason && id === "convert" && (r.figure || !r.opportunity || !["celebrity", "politician"].includes(r.opportunity.kind))) reason = "Requires an unconverted prominent-person opportunity.";
  if (!reason && id === "reveal_figure" && (!r.figure || r.figure.revealed)) reason = "Requires a secretly converted figure.";
  if (!reason && id === "reveal_figure" && !r.site.invested) reason = "Prepare a local ritual site to receive their awakening contribution.";
  let effect = a.description, risk = "";
  if (id === "sacrifice") {
    const gain = state.specialization === "terror" ? 35 : B.sacrificeDread;
    effect = "+" + Math.min(gain, B.dreadCap - state.dread).toFixed(0) + " Dread. " + Math.max(0, r.population.followers - cost.followers).toFixed(0)
      + " followers remain; income falls by " + (cost.followers * B.influencePerFollower).toFixed(2) + "/month.";
    risk = "+12 Exposure. Fewer than " + siteRequirement(r) + " followers interrupts this site's ritual.";
  } else if (id === "reveal") risk = "+18 Exposure, +5 world awareness; recruitment backlash for 6 months, then full public growth.";
  else if (id === "manifest") risk = "+15 Exposure and +8 awareness. New believers can strengthen resistance.";
  else if (id === "prepare") risk = "Site remains exposed, even underground. An occult response is scheduled."
    + (preparationLeft < B.siteMonths ? " Needs acceleration to finish in time: " + (B.siteMonths - acceleration) + " preparation months, then a " + B.finalMonths + "-month final rite." : "");
  else if (id === "reveal_figure") risk = "+25 Exposure, +8 awareness; 12 months of recruitment backlash. Secret benefits end.";
  else if (id === "recruit") effect = "+" + Math.min(B.recruitAmount, recruitable(r)).toFixed(0) + " followers; +3 Exposure.";
  else if (id === "exploit") effect = r.opportunity?.kind === "nightmare" ? "+30 Dread once, up to the resource cap." : "+8 months of site preparation once.";
  return { ok: !reason, reason, ...cost, effect, risk };
}
export function performAction(state: GameState, id: ActionId, regionId: RegionId): CommandResult {
  const preview = previewAction(state, id, regionId);
  if (!preview.ok) return fail(state, preview.reason);
  const next = structuredClone(state), r = next.regions[regionId];
  next.influence -= preview.influence; next.dread -= preview.dread;
  next.telemetry.influenceSpent += preview.influence; next.telemetry.dreadSpent += preview.dread;
  next.telemetry.actions[id] = (next.telemetry.actions[id] ?? 0) + 1;
  r.cooldowns[id] = next.elapsedMonths + ACTIONS[id].cooldown;
  if (id === "recruit" || id === "expand") { recruit(r, id === "recruit" ? B.recruitAmount : B.expandAmount); r.exposure = clamp(r.exposure + 3); }
  if (id === "sacrifice") {
    next.telemetry.sacrificed += loseFollowers(r, preview.followers);
    grantDread(next, next.specialization === "terror" ? 35 : B.sacrificeDread);
    r.exposure = clamp(r.exposure + 12);
  }
  if (id === "reveal") {
    r.public = true; r.exposure = clamp(r.exposure + 18); next.awareness = clamp(next.awareness + 5);
    r.backlashUntil = Math.max(r.backlashUntil, next.elapsedMonths + 6);
    next.telemetry.revelations[regionId] ??= next.elapsedMonths;
  }
  if (id === "hide") r.public = false;
  if (id === "conceal") {
    r.exposure = Math.max(r.site.invested ? 40 : r.public ? 20 : 0, r.exposure - 25);
    next.awareness = clamp(next.awareness - B.concealAwareness);
  }
  if (id === "manifest") {
    const denied = Math.min(r.population.deniers, B.manifestationDeniers);
    const unaware = Math.min(r.population.unaware, B.manifestationUnaware);
    r.population.deniers -= denied; r.population.unaware -= unaware; r.population.believers += denied + unaware;
    r.madness = clamp(r.madness + 20); r.exposure = clamp(r.exposure + 15); next.awareness = clamp(next.awareness + 8);
  }
  if (id === "prepare") {
    r.site.invested = true; r.exposure = Math.max(40, r.exposure); next.awareness = clamp(next.awareness + 8);
    scheduleThreat(next, regionId, true);
  }
  if (id === "counter") { r.threat = null; next.telemetry.threatsCountered++; }
  if (id === "exploit") {
    if (r.opportunity?.kind === "nightmare") grantDread(next, 30);
    else r.site.progress = Math.min(B.siteMonths, r.site.progress + 8);
    r.opportunity = null;
  }
  if (id === "convert" && (r.opportunity?.kind === "celebrity" || r.opportunity?.kind === "politician")) {
    r.figure = { kind: r.opportunity.kind, revealed: false }; r.opportunity = null; r.exposure = clamp(r.exposure + 5);
  }
  if (id === "reveal_figure" && r.figure) {
    r.figure.revealed = true; recruit(r, 60); r.site.progress = Math.min(B.siteMonths, r.site.progress + B.figureSiteProgress);
    r.exposure = clamp(r.exposure + 25); next.awareness = clamp(next.awareness + 8); r.backlashUntil = next.elapsedMonths + 12;
  }
  addLog(next, ACTIONS[id].name + " · " + REGIONS[regionId].name + ". " + preview.effect);
  return success(next);
}
function grantDread(s: GameState, amount: number): void {
  const gain = Math.min(amount, B.dreadCap - s.dread); s.dread += gain; s.telemetry.dreadEarned += gain;
}
export function chooseSpecialization(state: GameState, choice: Specialization): CommandResult {
  if (!active(state) || state.specialization || !["recruitment", "infiltration", "terror"].includes(choice)) return fail(state, "Choose one doctrine per run.");
  if (state.influence < 40) return fail(state, "A doctrine requires 40 Influence.");
  const next = structuredClone(state); next.influence -= 40; next.telemetry.influenceSpent += 40; next.specialization = choice;
  addLog(next, "The cult commits to " + choice + ". Other doctrines are closed for this run."); return success(next);
}
export function setGameSpeed(state: GameState, speed: Speed): GameState {
  return state.status === "won" || state.status === "lost" || ![0, 1, 2].includes(speed) ? state : { ...state, speed };
}
export function awakeningReason(state: GameState): string {
  if (state.status !== "alignment") return state.status === "playing" ? "The stars align in " + (B.preparationMonths - state.elapsedMonths) + " months." : "The final rite is not available now.";
  if (readySites(state) < B.requiredSites) return "Need " + B.requiredSites + " prepared sites, each supported by a quarter of its region.";
  if (state.dread < B.finalDread) return "Reserve 100 Dread for the final rite.";
  if (deadline - state.elapsedMonths < B.finalMonths) return "Fewer than 12 months remain. The final rite can no longer finish in time.";
  return "";
}
export function startAwakening(state: GameState): CommandResult {
  const reason = awakeningReason(state); if (reason) return fail(state, reason);
  const next = structuredClone(state); next.dread -= B.finalDread; next.telemetry.dreadSpent += B.finalDread; next.status = "awakening";
  addLog(next, "The final rite begins. Maintain three supported sites for 12 months."); return success(next);
}
export function advanceSimulation(state: GameState, months: number): GameState {
  if (!active(state) || !Number.isFinite(months) || months < 1) return state;
  const next = structuredClone(state);
  for (let m = 0; m < Math.min(Math.floor(months), deadline); m++) {
    if (!active(next)) break;
    next.elapsedMonths++;
    resolveWorld(next);
    const income = Math.min(influencePerMonth(next), Math.max(0, B.influenceCap - next.influence));
    next.influence += income; next.telemetry.influenceEarned += income;
    for (const id of REGION_IDS) {
      const r = next.regions[id];
      recruit(r, recruitmentPerMonth(next, r));
      if (r.population.followers > 0) {
        const floor = r.site.invested ? 40 : r.public ? 20 : 0;
        r.exposure = clamp(Math.max(floor, r.exposure + (r.public ? 0.6 : -0.36)));
        if (r.public || r.site.invested) next.awareness = clamp(next.awareness + 0.075);
      }
      r.madness = Math.max(0, r.madness - 0.3);
      if (r.site.invested && supported(r) && r.site.progress < B.siteMonths) {
        r.site.progress++;
        if (r.site.progress === B.siteMonths) addLog(next, "The ritual site in " + REGIONS[id].name + " is prepared.");
      }
    }
    next.awareness = clamp(next.awareness + cultShare(next) * B.awarenessPerCapita);
    if (next.status === "awakening" && readySites(next) >= B.requiredSites) next.finalProgress++;
    if (next.finalProgress >= B.finalMonths) {
      next.status = "won"; next.speed = 0; next.resultReason = "Three surviving ritual sites opened the way. Cthulhu awakens.";
      addLog(next, next.resultReason); break;
    }
    if (next.awareness >= 100) {
      next.status = "lost"; next.speed = 0;
      next.resultReason = "The world knows. Every cell is hunted and the rite dies with them.";
      addLog(next, next.resultReason); break;
    }
    if (next.elapsedMonths >= deadline) {
      next.status = "lost"; next.speed = 0; next.resultReason = "The alignment has passed. " + readySites(next) + "/3 sites supported; final rite " + next.finalProgress + "/12 months.";
      addLog(next, next.resultReason); break;
    }
    tickDirector(next);
    if (next.elapsedMonths === B.preparationMonths) {
      next.status = "alignment"; next.speed = 0;
      addLog(next, "The stars align. Time is paused. You have " + B.alignmentMonths + " months to complete the awakening."); break;
    }
  }
  return next;
}
export const engine: EngineContract = { createGameState, placeStartingFoothold, previewAction, performAction, advanceSimulation, startAwakening };
