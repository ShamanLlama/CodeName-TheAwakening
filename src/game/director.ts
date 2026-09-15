import { BALANCE as B } from "../data/balance";
import { REGIONS } from "../data/regions";
import { DIFFICULTIES } from "../data/difficulties";
import { OPPORTUNITY_NAMES, THREAT_NAMES } from "../data/events";
import { addLog, clamp, livePopulation, loseFollowers } from "./derived";
import { nextRandom } from "./random";
import { describeThreat, threatConsequences } from "./threats";
import { REGION_IDS, type GameState, type RegionId, type Opportunity } from "./types";

function random(state: GameState): number {
  const value = nextRandom(state.rngState); state.rngState = value.state; return value.value;
}
// Mutates only an engine-owned clone. A threat is separate from the opportunity slot.
export function scheduleThreat(state: GameState, id: RegionId, ritual = false): void {
  const r = state.regions[id];
  if (r.threat || r.population.followers <= 0) return;
  const warning = B.threatWarningMonths + DIFFICULTIES[state.difficulty].warning
    + (state.specialization === "infiltration" ? 2 : 0)
    + (r.figure?.kind === "politician" && !r.figure.revealed ? 2 : 0);
  const kind = ritual || r.site.invested ? "occult" : r.public ? "raid" : "investigation";
  r.threat = { kind, dueMonth: state.elapsedMonths + warning, severity: clamp(1 + Math.floor((r.exposure + state.awareness) / 60), 1, 3) };
  addLog(state, THREAT_NAMES[kind] + " in " + REGIONS[id].name + ". Respond within " + warning + " months. If ignored: " + describeThreat(r));
}
export function resolveWorld(state: GameState): void {
  for (const id of REGION_IDS) {
    const r = state.regions[id];
    if (r.opportunity && r.opportunity.expiresMonth <= state.elapsedMonths) {
      addLog(state, OPPORTUNITY_NAMES[r.opportunity.kind] + " passes in " + REGIONS[id].name + ".");
      r.opportunity = null;
    }
    if (r.threat && r.threat.dueMonth <= state.elapsedMonths) {
      const threat = r.threat;
      const consequences = threatConsequences(r);
      const loss = loseFollowers(r, consequences.followers);
      state.telemetry.raidLosses += loss;
      if (threat.kind === "occult" && r.site.invested) r.site.progress -= consequences.preparationLost;
      r.exposure = clamp(r.exposure + 5);
      r.threat = null;
      addLog(state, REGIONS[id].name + " loses " + loss.toFixed(0) + " followers to " + threat.kind + "; income −" + consequences.incomeLost.toFixed(2) + "/month."
        + (consequences.destroysCell ? " The cell and any ritual site are destroyed." : consequences.preparationLost ? " Ritual loses " + consequences.preparationLost + " preparation months." : "")
        + (!consequences.destroysCell && consequences.interruptsSite ? " Ritual support is interrupted." : ""));
    }
  }
}
export function tickDirector(state: GameState): void {
  if (state.elapsedMonths % 4 === 0) {
    for (const id of REGION_IDS) {
      const r = state.regions[id];
      if (r.population.followers <= 0 || r.threat) continue;
      const believers = r.population.believers / Math.max(1, livePopulation(r));
      const pressure = (0.02 + r.exposure / 500 + (r.public ? 0.1 : 0) + (r.site.invested ? 0.12 : 0)
        + state.awareness / 1500 + believers * 0.15 + r.madness / 2000)
        * DIFFICULTIES[state.difficulty].pressure * (0.6 + REGIONS[id].institutionalResistance / 150);
      if (random(state) < pressure) scheduleThreat(state, id);
    }
  }
  if (state.elapsedMonths % 12 === 0) {
    const available = REGION_IDS.filter(id => !state.regions[id].opportunity && state.regions[id].population.followers > 0);
    const id = available[Math.floor(random(state) * available.length)];
    if (!id) return;
    const r = state.regions[id];
    const kinds: Opportunity["kind"][] = r.figure ? ["nightmare", "eclipse"] : ["nightmare", "eclipse", "celebrity", "politician"];
    const kind = kinds[Math.floor(random(state) * kinds.length)] ?? "nightmare";
    r.opportunity = { kind, expiresMonth: state.elapsedMonths + 8 };
    addLog(state, OPPORTUNITY_NAMES[kind] + " in " + REGIONS[id].name + ". Opportunity lasts 8 months.");
  }
}
