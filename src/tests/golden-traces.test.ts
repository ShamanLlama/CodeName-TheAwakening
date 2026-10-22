import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { gunzipSync, gzipSync } from "node:zlib";
import { describe, expect, it } from "vitest";
import * as engine from "../game/engine";
import { awarenessDrag, calendar, cultShare, influencePerMonth, readySites, recruitmentPerMonth, regionIncome, siteLabel, siteRequirement, totalFollowers } from "../game/derived";
import { ritualForecast } from "../game/forecast";
import { nextRandom, seedToState } from "../game/random";
import { describeThreat, threatConsequences } from "../game/threats";
import { ACTION_IDS, REGION_IDS, type ActionId, type ActionPreview, type CommandResult, type Difficulty, type GameState, type RegionId, type Specialization } from "../game/types";
import { play, type PlayApi, type Strategy } from "./playthrough";

// Golden traces for the Godot port. Each trace records every command a policy issues and a
// snapshot after every simulation step; godot/tests/test_golden.gd replays the commands and
// requires the same state, derived values, previews and log text.
// Record with `npm run golden`. Without RECORD_GOLDEN this test fails if the files are stale.
const RECORD = process.env.RECORD_GOLDEN === "1";
const OUT = new URL("../../godot/tests/golden/", import.meta.url);

type Outcome = { ok: boolean; message?: string };
type Command = Outcome & (
  | { op: "place"; region: RegionId }
  | { op: "action"; id: ActionId; region: RegionId; preview: ActionPreview }
  | { op: "doctrine"; choice: Specialization }
  | { op: "awaken" });

function snapshot(state: GameState, logFrom: number, previews: boolean) {
  const { log, ...rest } = state;
  return {
    state: rest,
    newLog: log.filter(entry => entry.id >= logFrom),
    derived: {
      totalFollowers: totalFollowers(state), influencePerMonth: influencePerMonth(state), cultShare: cultShare(state),
      awarenessDrag: awarenessDrag(state), readySites: readySites(state), forecast: ritualForecast(state),
      awakeningReason: engine.awakeningReason(state), calendar: calendar(state.elapsedMonths),
      regions: Object.fromEntries(REGION_IDS.map(id => {
        const r = state.regions[id];
        return [id, { recruitment: recruitmentPerMonth(state, r), income: regionIncome(r), siteRequirement: siteRequirement(r),
          siteLabel: siteLabel(r), threat: describeThreat(r), consequences: r.threat ? threatConsequences(r) : null }];
      })),
    },
    ...(previews ? { previews: Object.fromEntries(REGION_IDS.map(id => [id, Object.fromEntries(ACTION_IDS.map(a => [a, engine.previewAction(state, a, id)]))])) } : {}),
  };
}

class Recorder {
  initial: ReturnType<typeof snapshot> | null = null;
  steps: { commands: Command[]; months: number; after: ReturnType<typeof snapshot> }[] = [];
  private commands: Command[] = [];
  private logFrom = 1;
  constructor(readonly name: string, readonly seed: string, readonly difficulty: Difficulty) {}
  private outcome(result: CommandResult): Outcome { return result.ok ? { ok: true } : { ok: false, message: result.message }; }
  private snap(state: GameState) {
    // Every sixth step also records all 96 action previews.
    const shot = snapshot(state, this.logFrom, this.steps.length % 6 === 0);
    this.logFrom = state.nextLogId;
    return shot;
  }
  readonly api: PlayApi = {
    createGameState: config => { const state = engine.createGameState(config); this.initial = this.snap(state); return state; },
    placeStartingFoothold: (state, region) => { const result = engine.placeStartingFoothold(state, region); this.commands.push({ op: "place", region, ...this.outcome(result) }); return result; },
    previewAction: engine.previewAction,
    performAction: (state, id, region) => {
      const preview = engine.previewAction(state, id, region), result = engine.performAction(state, id, region);
      this.commands.push({ op: "action", id, region, preview, ...this.outcome(result) }); return result;
    },
    chooseSpecialization: (state, choice) => { const result = engine.chooseSpecialization(state, choice); this.commands.push({ op: "doctrine", choice, ...this.outcome(result) }); return result; },
    startAwakening: state => { const result = engine.startAwakening(state); this.commands.push({ op: "awaken", ...this.outcome(result) }); return result; },
    advanceSimulation: (state, months) => { const next = engine.advanceSimulation(state, months); this.steps.push({ commands: this.commands, months, after: this.snap(next) }); this.commands = []; return next; },
  };
  finish(state: GameState): string {
    if (this.commands.length) this.api.advanceSimulation(state, 0);
    return JSON.stringify({ name: this.name, seed: this.seed, difficulty: this.difficulty, initial: this.initial, steps: this.steps, final: state.status });
  }
}

// Random command policy with its own generator, so it never consumes the game's RNG.
function fuzz(api: PlayApi, seed: string, difficulty: Difficulty, start: RegionId, greedy: boolean): GameState {
  let rng = seedToState("fuzz:" + seed);
  const random = () => { const next = nextRandom(rng); rng = next.state; return next.value; };
  const pick = <T,>(items: readonly T[]): T => items[Math.floor(random() * items.length)] as T;
  let state = api.createGameState({ difficulty, seed });
  // Commands before placement must be refused, and waiting must do nothing.
  state = api.performAction(state, "recruit", start).state;
  state = api.advanceSimulation(state, 1);
  state = api.placeStartingFoothold(state, start).state;
  state = api.placeStartingFoothold(state, start).state;
  for (let guard = 0; guard < 400 && !["won", "lost"].includes(state.status); guard++) {
    if (greedy) {
      // Tries most legal actions in occupied regions: an aggressive player who over-extends.
      for (const region of REGION_IDS) for (const id of ACTION_IDS) {
        if (state.regions[region].population.followers > 0 || id === "expand") {
          if (random() < 0.3 && engine.previewAction(state, id, region).ok) state = api.performAction(state, id, region).state;
        }
      }
    }
    const attempts = Math.floor(random() * 4);
    for (let i = 0; i < attempts; i++) state = api.performAction(state, pick(ACTION_IDS), pick(REGION_IDS)).state;
    if (!state.specialization && random() < 0.04) state = api.chooseSpecialization(state, pick(["recruitment", "infiltration", "terror"] as const)).state;
    if (state.status !== "playing" && random() < 0.5) state = api.startAwakening(state).state;
    const roll = random();
    state = api.advanceSimulation(state, roll < 0.85 ? 1 : roll < 0.95 ? 2 + Math.floor(random() * 5) : roll < 0.99 ? 12 : 60);
  }
  return state;
}

const TRACES: { name: string; seed: string; difficulty: Difficulty; run: (api: PlayApi, seed: string, difficulty: Difficulty) => GameState }[] = [
  ...([["early", "rlyeh"], ["late", "dagon"], ["hub", "the-deep"], ["early", "resume"]] as [Strategy, string][]).map(([strategy, seed]) => ({
    name: "strategy-" + strategy + "-" + seed, seed, difficulty: "normal" as Difficulty, run: (api: PlayApi, s: string) => play(strategy, s, undefined, api),
  })),
  { name: "fuzz-easy-oceania", seed: "fuzz-a", difficulty: "easy", run: (api, s, d) => fuzz(api, s, d, "oceania", false) },
  { name: "fuzz-hard-east-asia", seed: "fuzz-b", difficulty: "hard", run: (api, s, d) => fuzz(api, s, d, "east_asia", false) },
  { name: "greedy-normal-europe", seed: "greedy-c", difficulty: "normal", run: (api, s, d) => fuzz(api, s, d, "europe", true) },
  { name: "greedy-hard-south-asia", seed: "greedy-d", difficulty: "hard", run: (api, s, d) => fuzz(api, s, d, "south_asia", true) },
  { name: "greedy-easy-unicode-seed", seed: "ルルイエ·ñ-𓆏", difficulty: "easy", run: (api, s, d) => fuzz(api, s, d, "latin_america", true) },
];

function record(trace: typeof TRACES[number]): { json: string; final: string } {
  const recorder = new Recorder(trace.name, trace.seed, trace.difficulty);
  const final = trace.run(recorder.api, trace.seed, trace.difficulty);
  return { json: recorder.finish(final), final: final.status + " at month " + final.elapsedMonths + (final.resultReason ? ": " + final.resultReason : "") };
}

describe("golden traces for the Godot port", () => {
  it("covers wins and both kinds of loss", () => {
    const results = TRACES.map(trace => record(trace).final);
    console.info("GOLDEN_RESULTS=" + JSON.stringify(results, null, 1));
    expect(results.some(r => r.startsWith("won"))).toBe(true);
    expect(results.some(r => r.includes("The world knows"))).toBe(true);
    expect(results.some(r => r.includes("The alignment has passed"))).toBe(true);
  });
  for (const trace of TRACES) {
    it((RECORD ? "records " : "has a current recording of ") + trace.name, () => {
      const { json } = record(trace), file = new URL(trace.name + ".json.gz", OUT);
      if (RECORD) { mkdirSync(OUT, { recursive: true }); writeFileSync(file, gzipSync(json, { level: 9 })); return; }
      expect(existsSync(file), "Run npm run golden to record " + trace.name).toBe(true);
      expect(gunzipSync(readFileSync(file)).toString("utf8") === json, "Engine output changed; re-record with npm run golden").toBe(true);
    });
  }
});
