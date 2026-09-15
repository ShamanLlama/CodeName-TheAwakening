// First-playtest values. Calendar and population scale are game abstractions.
export const BALANCE = {
  preparationMonths: 200, alignmentMonths: 24, finalMonths: 12, secondsPerMonth: 2,
  populationScale: 100, influenceCap: 4000, dreadCap: 400,
  startingFollowers: 100, startingInfluence: 35, startingDread: 20,
  influencePerFollower: 0.014, recruitmentBase: 0.6, recruitmentRate: 0.0075,
  publicRecruitment: 2, recruitAmount: 35, expandAmount: 35,
  sacrificeFollowers: 40, sacrificeDread: 25,
  manifestationDeniers: 65, manifestationUnaware: 50,
  siteMonths: 24, siteFollowers: 60, requiredSites: 3, finalDread: 100,
  // A ritual site is supported by a share of its region's whole population, so where you
  // build it matters and sacrificing followers can starve it.
  siteSupportShare: 0.25, siteDreadCosts: [60, 100, 160],
  // Threats take a share of the local cell, so a large cell is not automatically safe.
  // The flat minimum only bites small cells, so it stays low: a proportional loss must stay
  // proportional, or a small region is punished twice for being small.
  raidLossShare: 0.12, threatLossShare: 0.08, raidLossFloor: 8, threatLossFloor: 6,
  // Obstructing a threat is a scarce favour, not a purchase: one region at a time.
  counterSeverityCost: 30, awarenessDrag: 150, concealAwareness: 1,
  // A cult this large cannot stay secret: awareness climbs with the share of the world
  // that follows you, so hoarding followers is itself a risk.
  awarenessPerCapita: 1.5,
  figureSiteProgress: 8, threatWarningMonths: 5,
} as const;
