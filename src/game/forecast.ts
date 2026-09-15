import { BALANCE as B } from "../data/balance";
import { deadline, readySites, supported } from "./derived";
import type { GameState } from "./types";

export const latestSiteStart = deadline - B.finalMonths - B.siteMonths;
export const latestRiteStart = deadline - B.finalMonths;
export function ritualForecast(state: GameState): { urgent: boolean; message: string } {
  if (state.status === "won" || state.status === "lost") return { urgent: false, message: state.resultReason ?? "Run complete." };
  if (state.status === "awakening") {
    const remaining = B.finalMonths - state.finalProgress;
    return { urgent: readySites(state) < B.requiredSites || deadline - state.elapsedMonths <= remaining,
      message: "Final rite: " + remaining + " supported months needed; " + (deadline - state.elapsedMonths) + " months remain." + (readySites(state) < B.requiredSites ? " Restore three supported sites now." : "") };
  }
  const sites = Object.values(state.regions).filter(r => r.site.invested);
  const remainingPreparations = sites.map(r => B.siteMonths - r.site.progress).sort((a,b) => a-b);
  const preparation = remainingPreparations[B.requiredSites - 1];
  if (preparation !== undefined) {
    const earliest = Math.max(B.preparationMonths, state.elapsedMonths + preparation) + B.finalMonths;
    const unsupported = sites.filter(r => !supported(r)).length;
    return { urgent: earliest > deadline || unsupported > 0 || state.elapsedMonths >= B.preparationMonths,
      message: earliest > deadline
        ? "Preparation is too late: these sites need " + preparation + " months, then the final rite needs 12. Accelerate preparation or the window closes " + (earliest - deadline) + " months too soon."
        : (state.elapsedMonths >= B.preparationMonths && readySites(state) >= 3 ? "The stars are right — begin the final rite. " : "Funded sites need up to " + preparation + " more preparation months. ")
          + "The final rite needs 12 months and 100 Dread." + (unsupported ? " Restore follower support at " + unsupported + " site(s)." : "") };
  }
  const left = latestSiteStart - state.elapsedMonths;
  return { urgent: left <= 24, message: "Fund " + (B.requiredSites - sites.length) + " more ritual site(s). "
    + (left >= 0 ? (left <= 24 ? "Only " : "") + left + " months left to start new sites without acceleration. " : "New sites now need acceleration to finish in time. ")
    + "Allow 24 months of preparation plus 12 for the final rite." };
}
