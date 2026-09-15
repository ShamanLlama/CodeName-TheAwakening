import { describe, expect, it } from "vitest";
import { advanceSimulation, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, startAwakening } from "../game/engine";
import { BALANCE as B } from "../data/balance";
import { deadline, influencePerMonth, readySites, siteRequirement, totalFollowers } from "../game/derived";
import type { ActionId, Difficulty, GameState, RegionId, Specialization } from "../game/types";

// Test-only policies: each attempted choice is guarded by the public command preview.
// The test prints a deterministic data set used by docs/playtests/endgame-economy.json.
const TARGETS: RegionId[] = ["africa", "europe", "latin_america"];
// Even fifths of the preparation calendar, plus the earliest legal win month.
const SNAPSHOT_MONTHS = [0.2, 0.4, 0.6, 0.8, 1].map(f => Math.round(f * B.preparationMonths)).concat(B.preparationMonths + B.finalMonths);
type PolicyName = "public-recruitment" | "hidden-infiltration" | "sacrifice-terror" | "late-public-recruitment" | "unmanaged-public";
type Snapshot = { month: number; followers: number; influence: number; dread: number; sites: number; siteProgress: number; sacrificed: number; countered: number; influenceSpent: number; dreadSpent: number };
type Run = { seed: string; difficulty: Difficulty; policy: PolicyName; doctrine: Specialization; status: string; finishMonth: number; sitePreparedMonths: Record<string, number>; trueReadinessMonth: number | null; influenceCapMonths: number; influenceForegoneAtCap: number; snapshots: Snapshot[]; telemetry: GameState["telemetry"]; final: Snapshot };

function snapshot(state: GameState): Snapshot {
  return { month: state.elapsedMonths, followers: Number(totalFollowers(state).toFixed(2)), influence: Number(state.influence.toFixed(2)), dread: Number(state.dread.toFixed(2)), sites: readySites(state), siteProgress: TARGETS.reduce((n, id) => n + state.regions[id].site.progress, 0), sacrificed: state.telemetry.sacrificed, countered: state.telemetry.threatsCountered, influenceSpent: state.telemetry.influenceSpent, dreadSpent: state.telemetry.dreadSpent };
}

function run(policy: PolicyName, doctrine: Specialization, seed: string, difficulty: Difficulty): Run {
  let state = placeStartingFoothold(createGameState({ seed, difficulty }), "africa").state;
  const snapshots: Snapshot[] = [];
  const prepared: Record<string, number> = {};
  let trueReadinessMonth: number | null = null;
  let influenceCapMonths = 0;
  let influenceForegoneAtCap = 0;
  const act = (id: ActionId, region: RegionId) => {
    if (previewAction(state, id, region).ok) state = performAction(state, id, region).state;
  };
  for (let turns = 0; turns < deadline && !["won", "lost"].includes(state.status); turns++) {
    if (SNAPSHOT_MONTHS.includes(state.elapsedMonths)) snapshots.push(snapshot(state));
    for (const id of TARGETS) {
      const region = state.regions[id];
      if (policy !== "unmanaged-public" && region.threat && region.threat.dueMonth - state.elapsedMonths <= 3) act("counter", id);
      if (region.opportunity && (region.opportunity.kind === "nightmare" || (region.opportunity.kind === "eclipse" && region.site.invested))) act("exploit", id);
      // Prominent figures are only used by the hidden policy; revelation is deliberately avoided.
      if (policy === "hidden-infiltration" && !region.figure && (region.opportunity?.kind === "celebrity" || region.opportunity?.kind === "politician")) act("convert", id);
    }
    for (const id of TARGETS) if (!state.regions[id].population.followers) act("expand", id);
    if (!state.specialization && state.elapsedMonths >= 20 && state.influence >= 70) state = chooseSpecialization(state, doctrine).state;
    for (const id of TARGETS) {
      const region = state.regions[id];
      const revealAt = policy === "late-public-recruitment" ? 420 : 24;
      const shouldReveal = policy === "public-recruitment" || policy === "late-public-recruitment" || policy === "unmanaged-public" || (policy === "sacrifice-terror" && id === "africa");
      if (shouldReveal && state.elapsedMonths >= revealAt && region.population.followers >= 70 && !region.public) act("reveal", id);
      if (region.population.followers > 0 && region.population.followers < siteRequirement(region) + 160) act("recruit", id);
      const missingSites = TARGETS.filter(other => !state.regions[other].site.invested).length;
      const reserve = missingSites * 160 + B.finalDread;
      if (region.population.followers - B.sacrificeFollowers >= siteRequirement(region) + 120 && state.dread < reserve) act("sacrifice", id);
      if (!region.site.invested) act("prepare", id);
    }
    if (state.status === "alignment" && readySites(state) >= 3) state = startAwakening(state).state;
    TARGETS.forEach(id => { if (!prepared[id] && state.regions[id].site.progress >= 24) prepared[id] = state.elapsedMonths; });
    if (trueReadinessMonth === null && readySites(state) >= 3 && state.dread >= 100) trueReadinessMonth = state.elapsedMonths;
    if (state.influence >= 500) {
      influenceCapMonths++;
      influenceForegoneAtCap += influencePerMonth(state);
    }
    const before = TARGETS.map(id => state.regions[id].site.progress);
    state = advanceSimulation(state, 1);
    TARGETS.forEach((id, index) => { if (!prepared[id] && before[index]! < 24 && state.regions[id].site.progress >= 24) prepared[id] = state.elapsedMonths; });
  }
  return { seed, difficulty, policy, doctrine, status: state.status, finishMonth: state.elapsedMonths, sitePreparedMonths: prepared, trueReadinessMonth, influenceCapMonths, influenceForegoneAtCap: Number(influenceForegoneAtCap.toFixed(1)), snapshots, telemetry: state.telemetry, final: snapshot(state) };
}

// A bounded analogue of the documented human eight-region public run. It still
// funds only the three ritual targets, but recruitment, threats, and counters span all regions.
function runEightRegion(seed: string): { status: string; finishMonth: number; trueReadinessMonth: number | null; capMonths: number; foregone: number; publicRegionsAt600: number; followersAt600: number; counters: number; raidLosses: number; influenceSpent: number; sacrificed: number } {
  let state = placeStartingFoothold(createGameState({ seed, difficulty: "normal" }), "africa").state;
  let readiness: number | null = null, capMonths = 0, foregone = 0, publicAt600 = 0, followersAt600 = 0;
  const act = (id: ActionId, region: RegionId) => { if (previewAction(state, id, region).ok) state = performAction(state, id, region).state; };
  for (let turns = 0; turns < deadline && !["won", "lost"].includes(state.status); turns++) {
    for (const id of Object.keys(state.regions) as RegionId[]) {
      const region = state.regions[id];
      if (region.threat && region.threat.dueMonth - state.elapsedMonths <= 3) act("counter", id);
      if (region.opportunity && (region.opportunity.kind === "nightmare" || (region.opportunity.kind === "eclipse" && region.site.invested))) act("exploit", id);
    }
    for (const id of Object.keys(state.regions) as RegionId[]) if (!state.regions[id].population.followers) act("expand", id);
    if (!state.specialization && state.elapsedMonths >= 20 && state.influence >= 70) state = chooseSpecialization(state, "recruitment").state;
    for (const id of Object.keys(state.regions) as RegionId[]) {
      const region = state.regions[id];
      if (state.elapsedMonths >= 8 && region.population.followers >= 70 && !region.public) act("reveal", id);
      if (region.population.followers > 0 && region.population.followers < (TARGETS.includes(id) && region.site.invested ? 105 : 150)) act("recruit", id);
      const missingSites = TARGETS.filter(other => !state.regions[other].site.invested).length;
      if (region.population.followers - B.sacrificeFollowers >= siteRequirement(region) + 120 && state.dread < missingSites * 160 + B.finalDread) act("sacrifice", id);
      if (TARGETS.includes(id) && !region.site.invested) act("prepare", id);
    }
    if (state.status === "alignment" && readySites(state) >= 3) state = startAwakening(state).state;
    if (readiness === null && readySites(state) >= 3 && state.dread >= 100) readiness = state.elapsedMonths;
    if (state.influence >= 500) { capMonths++; foregone += influencePerMonth(state); }
    state = advanceSimulation(state, 1);
    if (state.elapsedMonths === B.preparationMonths) { publicAt600 = Object.values(state.regions).filter(region => region.public).length; followersAt600 = totalFollowers(state); }
  }
  return { status: state.status, finishMonth: state.elapsedMonths, trueReadinessMonth: readiness, capMonths, foregone: Number(foregone.toFixed(1)), publicRegionsAt600: publicAt600, followersAt600: Number(followersAt600.toFixed(1)), counters: state.telemetry.threatsCountered, raidLosses: state.telemetry.raidLosses, influenceSpent: state.telemetry.influenceSpent, sacrificed: state.telemetry.sacrificed };
}

describe("endgame economy playtest", () => {
  it("runs 12 seeds per doctrine policy using only legal engine commands", () => {
    const policies: Array<[PolicyName, Specialization]> = [["public-recruitment", "recruitment"], ["hidden-infiltration", "infiltration"], ["sacrifice-terror", "terror"], ["late-public-recruitment", "recruitment"], ["unmanaged-public", "recruitment"]];
    const results = policies.flatMap(([policy, doctrine]) => Array.from({ length: 12 }, (_, i) => run(policy, doctrine, "endgame-" + i, "normal")));
    const mean = (values: number[]) => Number((values.reduce((sum, value) => sum + value, 0) / values.length).toFixed(1));
    const summary = Object.fromEntries(policies.map(([policy]) => {
      const runs = results.filter(result => result.policy === policy);
      const thirdSite = runs.map(result => Math.max(...TARGETS.map(id => result.sitePreparedMonths[id] ?? B.preparationMonths)));
      return [policy, {
        wins: runs.filter(result => result.status === "won").length + "/" + runs.length,
        finishMonth: { min: Math.min(...runs.map(result => result.finishMonth)), mean: mean(runs.map(result => result.finishMonth)), max: Math.max(...runs.map(result => result.finishMonth)) },
        meanThirdSiteMonth: mean(thirdSite), latestThirdSiteMonth: Math.max(...thirdSite),
        trueReadinessN: runs.filter(result => result.trueReadinessMonth !== null).length + "/" + runs.length,
        meanTrueReadinessMonth: mean(runs.flatMap(result => result.trueReadinessMonth === null ? [] : [result.trueReadinessMonth])),
        latestTrueReadinessMonth: Math.max(...runs.flatMap(result => result.trueReadinessMonth === null ? [] : [result.trueReadinessMonth])),
        meanFollowersAt600: mean(runs.map(result => result.snapshots.find(s => s.month === B.preparationMonths)?.followers ?? 0)),
        meanInfluenceCapMonths: mean(runs.map(result => result.influenceCapMonths)),
        meanForegoneInfluenceAtCap: mean(runs.map(result => result.influenceForegoneAtCap)),
        meanInfluenceSpent: mean(runs.map(result => result.telemetry.influenceSpent)),
        meanSacrificed: mean(runs.map(result => result.telemetry.sacrificed)),
        meanCounters: mean(runs.map(result => result.telemetry.threatsCountered)),
        meanRaidLosses: mean(runs.map(result => result.telemetry.raidLosses)),
        meanFinalDread: mean(runs.map(result => result.final.dread)),
      }];
    }));
    const wide = Array.from({ length: 12 }, (_, i) => runEightRegion("endgame-" + i));
    console.log(JSON.stringify({ seeds: Array.from({ length: 12 }, (_, i) => "endgame-" + i), summary, eightRegionPublicRecruitment: {
      wins: wide.filter(result => result.status === "won").length + "/" + wide.length,
      finishMonth: { min: Math.min(...wide.map(result => result.finishMonth)), mean: mean(wide.map(result => result.finishMonth)), max: Math.max(...wide.map(result => result.finishMonth)) },
      trueReadinessN: wide.filter(result => result.trueReadinessMonth !== null).length + "/" + wide.length,
      meanTrueReadinessMonth: mean(wide.flatMap(result => result.trueReadinessMonth === null ? [] : [result.trueReadinessMonth])), latestTrueReadinessMonth: Math.max(...wide.flatMap(result => result.trueReadinessMonth === null ? [] : [result.trueReadinessMonth])),
      meanPublicRegionsAt600: mean(wide.map(result => result.publicRegionsAt600)), meanFollowersAt600: mean(wide.map(result => result.followersAt600)),
      meanInfluenceCapMonths: mean(wide.map(result => result.capMonths)), meanForegoneInfluenceAtCap: mean(wide.map(result => result.foregone)), meanCounters: mean(wide.map(result => result.counters)), meanRaidLosses: mean(wide.map(result => result.raidLosses)), meanInfluenceSpent: mean(wide.map(result => result.influenceSpent)), meanSacrificed: mean(wide.map(result => result.sacrificed)),
    }}));
    expect(results).toHaveLength(60);
    expect(results.filter(result => result.status === "won").length).toBeGreaterThan(0);
  });
});
