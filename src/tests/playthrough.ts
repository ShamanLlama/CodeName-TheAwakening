import { BALANCE as B } from "../data/balance";
import { advanceSimulation, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, startAwakening } from "../game/engine";
import { deadline, readySites, siteRequirement } from "../game/derived";
import type { ActionId, GameState, RegionId } from "../game/types";
export type Strategy = "early" | "late" | "hub";
// Ritual support scales with regional population, so sites go in small regions.
// Africa stays the starting cell: populous and credulous, it is a recruiting ground, not a site.
export const TARGETS: RegionId[] = ["latin_america", "middle_east", "oceania"];
const FARM: RegionId = "africa";
// Dread still owed: every unfunded site plus the final rite.
function dreadOwed(state: GameState): number {
  const costs: readonly number[] = B.siteDreadCosts;
  const funded = TARGETS.filter(id => state.regions[id].site.invested).length;
  let owed = B.finalDread;
  for (let i = funded; i < TARGETS.length; i++) owed += costs[Math.min(i, costs.length - 1)] ?? B.finalDread;
  return owed;
}
// Test-only player policy. Every gain comes through normal actions and monthly simulation.
export function play(strategy: Strategy, seed: string, stopMonth: number = deadline): GameState {
  let state = placeStartingFoothold(createGameState({ seed, difficulty: "normal" }), FARM).state;
  const act = (id: ActionId, region: RegionId) => {
    if (previewAction(state, id, region).ok) state = performAction(state, id, region).state;
  };
  for (let step = 0; step < stopMonth && !["won", "lost"].includes(state.status); step++) {
    for (const id of TARGETS) {
      const r = state.regions[id];
      if (r.threat) act("counter", id);
      if (r.opportunity?.kind === "nightmare" || r.opportunity?.kind === "eclipse" && r.site.invested) act("exploit", id);
    }
    for (const id of TARGETS) if (!state.regions[id].population.followers) act("expand", id);
    if (!state.specialization && state.elapsedMonths >= 20 && state.influence >= 70) {
      state = chooseSpecialization(state, strategy === "early" ? "recruitment" : strategy === "late" ? "infiltration" : "terror").state;
    }
    // Funding a site outranks every other use of Influence: it is the only thing that wins.
    for (const id of TARGETS) if (!state.regions[id].site.invested) act("prepare", id);
    const owed = dreadOwed(state);
    // The farm holds no site, so its surplus is the Dread supply — but it is also the
    // income base, so it keeps a floor.
    const farm = state.regions[FARM];
    if (farm.population.followers < 700) act("recruit", FARM);
    if (state.dread < owed && farm.population.followers - B.sacrificeFollowers >= 260) act("sacrifice", FARM);
    if (farm.exposure > 30) act("conceal", FARM);
    for (const id of TARGETS) {
      const r = state.regions[id];
      if (!r.population.followers) continue;
      // The buffer absorbs a raid, which now takes a share of the cell rather than a flat count.
      const need = siteRequirement(r), buffer = need + 160;
      if (r.population.followers < buffer) act("recruit", id);
      // Public worship is a growth phase. Hysteresis keeps it from thrashing: each toggle
      // costs awareness, which is now fatal at 100.
      const revealMonth = strategy === "late" ? 160 : 8;
      const allowsPublic = state.elapsedMonths >= revealMonth && (strategy !== "hub" || id === FARM);
      if (allowsPublic && !r.public && r.population.followers < need) act("reveal", id);
      if (r.public && (!allowsPublic || r.population.followers >= buffer)) act("hide", id);
      if (r.exposure > 30) act("conceal", id);
      // Support comes first: only a comfortable surplus is worth burning for Dread.
      if (state.dread < owed && r.population.followers - B.sacrificeFollowers >= need + 120) act("sacrifice", id);
    }
    if (state.status === "alignment" && readySites(state) >= 3) state = startAwakening(state).state;
    state = advanceSimulation(state, 1);
  }
  return state;
}
