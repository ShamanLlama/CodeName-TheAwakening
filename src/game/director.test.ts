import { describe, expect, it } from "vitest";
import { BALANCE as B } from "../data/balance";
import { advanceSimulation, createGameState, performAction, placeStartingFoothold } from "./engine";
import { resolveWorld, scheduleThreat } from "./director";
import { recruit, siteRequirement } from "./derived";
const start = () => placeStartingFoothold(createGameState({ seed: "director", difficulty: "normal" }), "africa").state;
describe("regional world response", () => {
  it("schedules a warned ritual threat even when an opportunity occupies the region", () => {
    const s = start(); s.dread = 100; s.influence = 100;
    recruit(s.regions.africa, siteRequirement(s.regions.africa));
    s.regions.africa.opportunity = { kind: "nightmare", expiresMonth: 24 };
    const prepared = performAction(s, "prepare", "africa").state;
    expect(prepared.regions.africa.threat?.kind).toBe("occult");
    expect(prepared.regions.africa.threat?.dueMonth).toBe(B.threatWarningMonths);
    expect(prepared.regions.africa.opportunity).not.toBeNull();
    // Funding a site costs Influence and Dread, never followers.
    expect(prepared.regions.africa.population.followers).toBe(s.regions.africa.population.followers);
  });
  it("resolves threat losses once and allows Influence to prevent them", () => {
    const s = start(); scheduleThreat(s, "africa");
    const countered = performAction(s, "counter", "africa").state;
    expect(countered.regions.africa.threat).toBeNull();
    expect(countered.telemetry.threatsCountered).toBe(1);
    s.elapsedMonths = 15; resolveWorld(s); const losses = s.regions.africa.population.losses; resolveWorld(s);
    expect(losses).toBeGreaterThan(0); expect(s.regions.africa.population.losses).toBe(losses);
  });
  it("uses a secret politician and infiltration to extend newly scheduled warnings", () => {
    const s = start(); s.specialization = "infiltration"; s.regions.africa.figure = { kind: "politician", revealed: false };
    // Infiltration and a secret politician each add two months to the base warning.
    scheduleThreat(s, "africa"); expect(s.regions.africa.threat?.dueMonth).toBe(B.threatWarningMonths + 4);
  });
  it("repeats event outcomes for a shared seed and expires opportunities", () => {
    expect(advanceSimulation(start(), 120)).toEqual(advanceSimulation(start(), 120));
    const s = start(); s.regions.africa.opportunity = { kind: "nightmare", expiresMonth: 1 };
    expect(advanceSimulation(s, 1).regions.africa.opportunity).toBeNull();
  });
});
