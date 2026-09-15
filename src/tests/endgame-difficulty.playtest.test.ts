import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { advanceSimulation, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, startAwakening } from "../game/engine";
import { BALANCE as B } from "../data/balance";
import { deadline, readySites, siteRequirement, totalFollowers } from "../game/derived";
import type { ActionId, Difficulty, GameState, RegionId, Specialization } from "../game/types";

// Deterministic, legal-command policy sweep. This is intentionally separate from the
// smaller regression playthroughs: its purpose is balance telemetry, not unit coverage.
const targets: RegionId[] = ["africa", "latin_america", "europe"];
const policies: Record<string, { doctrine: Specialization; revealMonth: number; publicTargets: RegionId[] }> = {
  early_public: { doctrine: "recruitment", revealMonth: 8, publicTargets: targets },
  late_public: { doctrine: "infiltration", revealMonth: 160, publicTargets: targets },
  africa_hub: { doctrine: "terror", revealMonth: 8, publicTargets: ["africa"] },
};

type Snapshot = {
  month: number; status: string; readySites: number; fundedSites: number; siteProgress: number[];
  dread: number; influence: number; followers: number; raidLosses: number; countered: number;
};
type Run = { difficulty: Difficulty; policy: string; seed: string; firstEndgameReadyMonth: number | null; alignment: Snapshot; final: Snapshot };

function snapshot(state: GameState): Snapshot {
  return {
    month: state.elapsedMonths,
    status: state.status,
    readySites: readySites(state),
    fundedSites: targets.filter(id => state.regions[id].site.invested).length,
    siteProgress: targets.map(id => state.regions[id].site.progress),
    dread: state.dread,
    influence: state.influence,
    followers: totalFollowers(state),
    raidLosses: state.telemetry.raidLosses,
    countered: state.telemetry.threatsCountered,
  };
}

function run(difficulty: Difficulty, policyName: string, seed: string): Run {
  const policy = policies[policyName]!;
  let state = placeStartingFoothold(createGameState({ difficulty, seed }), "africa").state;
  const act = (id: ActionId, region: RegionId) => {
    if (previewAction(state, id, region).ok) state = performAction(state, id, region).state;
  };
  let alignment: Snapshot | undefined;
  let firstEndgameReadyMonth: number | null = null;
  for (let step = 0; step < deadline && !["won", "lost"].includes(state.status); step++) {
    if (state.status === "alignment" && !alignment) alignment = snapshot(state);
    if (firstEndgameReadyMonth === null && readySites(state) >= 3 && state.dread >= 100) firstEndgameReadyMonth = state.elapsedMonths;
    for (const id of targets) {
      const region = state.regions[id];
      if (region.threat && region.threat.dueMonth - state.elapsedMonths <= 3) act("counter", id);
      if (region.opportunity?.kind === "nightmare" || (region.opportunity?.kind === "eclipse" && region.site.invested)) act("exploit", id);
    }
    for (const id of targets) if (!state.regions[id].population.followers) act("expand", id);
    if (!state.specialization && state.elapsedMonths >= 20 && state.influence >= 70) {
      state = chooseSpecialization(state, policy.doctrine).state;
    }
    for (const id of targets) {
      const region = state.regions[id];
      if (region.population.followers > 0 && region.population.followers < siteRequirement(region) + 160) act("recruit", id);
      if (state.elapsedMonths >= policy.revealMonth && region.population.followers >= 70 && !region.public && policy.publicTargets.includes(id)) act("reveal", id);
      const missing = targets.filter(other => !state.regions[other].site.invested).length;
      if (region.population.followers - B.sacrificeFollowers >= siteRequirement(region) + 120 && state.dread < missing * 160 + B.finalDread) act("sacrifice", id);
      if (!region.site.invested) act("prepare", id);
    }
    if (state.status === "alignment" && readySites(state) >= 3) state = startAwakening(state).state;
    state = advanceSimulation(state, 1);
  }
  return { difficulty, policy: policyName, seed, firstEndgameReadyMonth, alignment: alignment ?? snapshot(state), final: snapshot(state) };
}

function summarize(runs: Run[]) {
  const mean = (values: number[]) => Number((values.reduce((a, b) => a + b, 0) / values.length).toFixed(2));
  const readyRuns = runs.filter(r => r.firstEndgameReadyMonth !== null);
  return {
    runs: runs.length,
    wins: runs.filter(r => r.final.status === "won").length,
    winRate: Number((runs.filter(r => r.final.status === "won").length / runs.length).toFixed(4)),
    alignment: {
      readySites: mean(runs.map(r => r.alignment.readySites)),
      fundedSites: mean(runs.map(r => r.alignment.fundedSites)),
      dread: mean(runs.map(r => r.alignment.dread)),
      influence: mean(runs.map(r => r.alignment.influence)),
      followers: mean(runs.map(r => r.alignment.followers)),
      readyRate: Number((runs.filter(r => r.alignment.readySites >= 3 && r.alignment.dread >= 100).length / runs.length).toFixed(4)),
    },
    preparationSlack: {
      readinessRate: Number((readyRuns.length / runs.length).toFixed(4)),
      firstEndgameReadyMonth: readyRuns.length ? mean(readyRuns.map(r => r.firstEndgameReadyMonth!)) : null,
      monthsBeforeAlignment: readyRuns.length ? mean(readyRuns.map(r => B.preparationMonths - r.firstEndgameReadyMonth!)) : null,
    },
    final: {
      month: mean(runs.map(r => r.final.month)),
      readySites: mean(runs.map(r => r.final.readySites)),
      dread: mean(runs.map(r => r.final.dread)),
      influence: mean(runs.map(r => r.final.influence)),
      followers: mean(runs.map(r => r.final.followers)),
      raidLosses: mean(runs.map(r => r.final.raidLosses)),
      countered: mean(runs.map(r => r.final.countered)),
    },
  };
}

describe("endgame difficulty playtest sweep", () => {
  it("runs 120 seeds for each difficulty and legal policy", () => {
    const seeds = Array.from({ length: 120 }, (_, index) => `endgame-${String(index).padStart(3, "0")}`);
    const runs = (["easy", "normal", "hard"] as Difficulty[]).flatMap(difficulty =>
      Object.keys(policies).flatMap(policy => seeds.map(seed => run(difficulty, policy, seed))),
    );
    const report = {
      generatedBy: "src/tests/endgame-difficulty.playtest.test.ts",
      seedFormat: "endgame-000 through endgame-119",
      firstEndgameReadyMonth: "First observed month with at least 3 ready sites and at least 100 Dread, measured before that month's legal actions.",
      policies,
      groups: Object.fromEntries((["easy", "normal", "hard"] as Difficulty[]).flatMap(difficulty => [
        [difficulty, summarize(runs.filter(r => r.difficulty === difficulty))],
        ...Object.keys(policies).map(policy => [`${difficulty}/${policy}`, summarize(runs.filter(r => r.difficulty === difficulty && r.policy === policy))]),
      ])),
      runs,
    };
    const directory = resolve(process.cwd(), "docs/playtests");
    mkdirSync(directory, { recursive: true });
    writeFileSync(resolve(directory, "endgame-difficulty.json"), JSON.stringify(report, null, 2) + "\n");
    expect(runs).toHaveLength(1080);
  }, 120_000);
});
