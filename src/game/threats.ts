import { BALANCE } from "../data/balance";
import { regionIncome, siteRequirement } from "./derived";
import type { RegionState } from "./types";

// Both the preview and resolution use this calculation. Forecasts use current population.
export function threatConsequences(region: RegionState) {
  const threat = region.threat;
  // A share of the cell, floored by the old flat loss so small cells can still be wiped out.
  const share = threat ? threat.severity * (threat.kind === "raid" ? BALANCE.raidLossShare : BALANCE.threatLossShare) : 0;
  const followers = threat
    ? Math.min(region.population.followers, Math.max(
        threat.severity * (threat.kind === "raid" ? BALANCE.raidLossFloor : BALANCE.threatLossFloor),
        region.population.followers * share))
    : 0;
  const remaining = region.population.followers - followers;
  const destroysCell = !!threat && region.population.followers > 0 && remaining <= 0;
  const preparationLost = threat?.kind === "occult" && region.site.invested
    ? Math.min(region.site.progress, 6) : 0;
  return {
    followers, remaining, destroysCell, preparationLost,
    incomeLost: destroysCell ? regionIncome(region) : followers * BALANCE.influencePerFollower,
    exposureAdded: threat ? Math.min(5, 100 - region.exposure) : 0,
    interruptsSite: !!threat && region.site.invested && (remaining < siteRequirement(region) || region.site.progress >= BALANCE.siteMonths && region.site.progress - preparationLost < BALANCE.siteMonths),
  };
}

export function describeThreat(region: RegionState): string {
  const c = threatConsequences(region);
  if (!region.threat) return "No approaching threat.";
  const effects = ["Lose " + Math.ceil(c.followers) + " followers", "income −" + c.incomeLost.toFixed(2) + " Influence/month"];
  if (c.preparationLost) effects.push("ritual −" + c.preparationLost + " preparation months");
  if (c.destroysCell) effects.push(region.site.invested ? "cell and ritual site destroyed" : "cell destroyed");
  else if (c.interruptsSite) effects.push("site cannot support the final rite");
  if (c.exposureAdded) effects.push("Exposure +" + c.exposureAdded);
  return effects.join(" · ") + ".";
}
