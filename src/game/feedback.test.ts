import { describe, expect, it } from "vitest";
import { BALANCE as B } from "../data/balance";
import { createGameState, performAction, placeStartingFoothold, previewAction } from "./engine";
import { deadline, recruit, siteRequirement } from "./derived";
import { resolveWorld } from "./director";
import { describeThreat, threatConsequences } from "./threats";
import { latestSiteStart, ritualForecast } from "./forecast";
import { threatMapPosition } from "../components/threat-markers";
import { exportTelemetry } from "./persistence";

const start = () => placeStartingFoothold(createGameState({ seed: "feedback", difficulty: "normal" }), "africa").state;
describe("playtest feedback and threat forecasts", () => {
  it("projects exactly the losses that an unresolved threat applies", () => {
    const state = start(), region = state.regions.africa;
    region.site = { invested: true, progress: 24 };
    region.threat = { kind: "occult", dueMonth: 1, severity: 3 };
    const before = threatConsequences(region);
    // Severity three takes a share of the cell, floored by the small flat minimum.
    expect(before.followers).toBe(Math.max(3 * B.threatLossFloor, 100 * 3 * B.threatLossShare)); expect(before.preparationLost).toBe(6); expect(before.interruptsSite).toBe(true);
    expect(describeThreat(region)).toContain("income −" + (before.followers * B.influencePerFollower).toFixed(2));
    state.elapsedMonths = 1; resolveWorld(state);
    expect(region.population.followers).toBe(before.remaining);
    expect(region.site.progress).toBe(18); expect(state.telemetry.raidLosses).toBe(before.followers);
  });
  it("caps losses at the cell population and includes loss of celebrity income on destruction", () => {
    const s = start(), r = s.regions.africa;
    r.population.unaware += 80; r.population.followers = 20; r.figure = { kind: "celebrity", revealed: false };
    r.threat = { kind: "raid", severity: 3, dueMonth: 1 };
    expect(threatConsequences(r)).toMatchObject({ followers: 20, remaining: 0, destroysCell: true });
    expect(threatConsequences(r).incomeLost).toBeCloseTo(20 * B.influencePerFollower + 0.6);
  });
  it("varies marker locations between investigations but remains stable across reload and does not use simulation RNG", () => {
    const s = start(), threat = { kind: "investigation" as const, dueMonth: 15, severity: 1 };
    const first = threatMapPosition(s.seed, "africa", threat);
    expect(threatMapPosition(s.seed, "africa", structuredClone(threat))).toEqual(first);
    expect(threatMapPosition(s.seed, "africa", { ...threat, dueMonth: 27 })).not.toEqual(first);
    expect(s).toEqual(start());
  });
  it("warns about the user's late third site rather than implying readiness guarantees a win", () => {
    const s = start(); s.elapsedMonths = B.preparationMonths; s.status = "alignment"; s.influence = 100; s.dread = 400;
    recruit(s.regions.africa, siteRequirement(s.regions.africa));
    for (const id of ["africa", "europe", "north_america"] as const) s.regions[id].site = { invested: true, progress: id === "north_america" ? 0 : 24 };
    expect(latestSiteStart).toBe(B.preparationMonths + B.alignmentMonths - B.finalMonths - B.siteMonths);
    expect(ritualForecast(s).message).toContain("12 months too soon");
    expect(ritualForecast(s).urgent).toBe(true);
    s.regions.africa.site.invested = false;
    const preview = performAction(s, "prepare", "africa");
    expect(preview.ok).toBe(false);
    expect(preview.state).toBe(s);
    expect(previewAction(s, "prepare", "africa").reason).toContain("Too late");
    // Four months earlier the site is still refused, but an eclipse's eight months of
    // acceleration brings it back inside the window.
    const earlier = structuredClone(s); earlier.status = "playing"; earlier.elapsedMonths = latestSiteStart + 4;
    expect(performAction(earlier, "prepare", "africa").ok).toBe(false);
    earlier.regions.africa.opportunity = { kind: "eclipse", expiresMonth: deadline };
    expect(performAction(earlier, "prepare", "africa").ok).toBe(true);
  });
  it("exports log coverage and regional state without pretending the retained log is complete", () => {
    const s = start(); s.nextLogId = 150;
    const report = JSON.parse(exportTelemetry(s));
    expect(report.logCoverage).toMatchObject({ retained: 1, total: 149, truncated: true });
    expect(report.regions.africa.population.followers).toBe(100);
  });
});
