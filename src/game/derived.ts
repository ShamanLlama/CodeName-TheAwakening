import { BALANCE as B } from "../data/balance";
import { DIFFICULTIES } from "../data/difficulties";
import { POPULATION_WEIGHT_TOTAL } from "../data/regions";
import type { GameState, RegionState } from "./types";
export const clamp = (value: number, minimum = 0, maximum = 100) => Math.min(maximum, Math.max(minimum, value));
export const totalFollowers = (state: GameState) => Object.values(state.regions).reduce((sum, r) => sum + r.population.followers, 0);
export const livePopulation = (r: RegionState) => r.population.unaware + r.population.deniers + r.population.believers + r.population.followers;
export const recruitable = (r: RegionState) => r.population.unaware + r.population.believers;
// Population is conserved, so the five buckets still sum to the region's original capacity.
export const capacity = (r: RegionState) => r.population.unaware + r.population.deniers + r.population.believers + r.population.followers + r.population.losses;
export const siteRequirement = (r: RegionState) => Math.max(B.siteFollowers, Math.round(capacity(r) * B.siteSupportShare));
export const supported = (r: RegionState) => r.population.followers >= siteRequirement(r);
// A watched world gives less and listens less. At the awareness cap the run is already lost.
export const worldPopulation = POPULATION_WEIGHT_TOTAL * B.populationScale;
export const cultShare = (state: GameState) => totalFollowers(state) / worldPopulation;
export const awarenessDrag = (state: GameState) => Math.max(0, 1 - state.awareness / B.awarenessDrag);
export const readySites = (state: GameState) => Object.values(state.regions).filter(r => r.site.invested && r.site.progress >= B.siteMonths && supported(r)).length;
export const deadline = B.preparationMonths + B.alignmentMonths;
export const calendar = (month: number) => "Year " + (Math.floor(month / 12) + 1) + " · Month " + (month % 12 + 1);
export function regionIncome(r: RegionState): number {
  return r.population.followers > 0 ? r.population.followers * B.influencePerFollower + (r.figure?.kind === "celebrity" && !r.figure.revealed ? 0.6 : 0) : 0;
}
export const influencePerMonth = (state: GameState) => Object.values(state.regions).reduce((sum, r) => sum + regionIncome(r), 0) * awarenessDrag(state);
export function recruitmentPerMonth(state: GameState, r: RegionState): number {
  if (r.population.followers <= 0) return 0;
  return Math.min(recruitable(r), (B.recruitmentBase + r.population.followers * B.recruitmentRate)
    * (r.public ? B.publicRecruitment : 1)
    * (state.specialization === "recruitment" ? 1.35 : 1)
    * (state.elapsedMonths < r.backlashUntil ? 0.55 : 1)
    * (1 + r.madness / 500) * DIFFICULTIES[state.difficulty].recruitment * awarenessDrag(state));
}
export function siteLabel(r: RegionState): string {
  if (!r.site.invested) return "No ritual site";
  if (!supported(r)) return "Interrupted · needs " + siteRequirement(r) + " followers";
  return r.site.progress >= B.siteMonths ? "Ritual prepared" : "Preparing · " + r.site.progress + "/" + B.siteMonths + " months";
}
export function addLog(state: GameState, message: string): void {
  state.log.push({ id: state.nextLogId++, month: state.elapsedMonths, message });
  state.log = state.log.slice(-100);
}
// Transfers never create population; beliefs and membership use one source of truth.
export function recruit(r: RegionState, amount: number): number {
  const fromBelievers = Math.min(r.population.believers, amount);
  const fromUnaware = Math.min(r.population.unaware, Math.max(0, amount - fromBelievers));
  r.population.believers -= fromBelievers; r.population.unaware -= fromUnaware;
  r.population.followers += fromBelievers + fromUnaware;
  return fromBelievers + fromUnaware;
}
export function loseFollowers(r: RegionState, amount: number): number {
  const removed = Math.min(r.population.followers, amount);
  r.population.followers -= removed; r.population.losses += removed;
  if (r.population.followers <= 0) { r.public = false; r.site = { invested: false, progress: 0 }; }
  return removed;
}
