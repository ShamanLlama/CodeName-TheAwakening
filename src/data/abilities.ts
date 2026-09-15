import type { AbilityDefinition, AbilityId, UpgradeDefinition, UpgradeId } from "../game/types";

export const ABILITIES: Record<AbilityId, AbilityDefinition> = {
  secret_gathering: { id: "secret_gathering", name: "Secret Gathering", branch: "ritual", cost: 12, cooldownSeconds: 20, unlockCost: 0, prerequisites: [], targetRule: "Influence above 0", summary: "+15 Cult, +4 Influence, +4 Exposure" },
  sacrament_of_the_deep: { id: "sacrament_of_the_deep", name: "Sacrament of the Deep", branch: "ritual", cost: 28, cooldownSeconds: 45, unlockCost: 2, prerequisites: [], targetRule: "Cult Strength at least 25", summary: "+14 Influence, +8 Cult, +10 Instability, +12 Exposure" },
  whispered_dreams: { id: "whispered_dreams", name: "Whispered Dreams", branch: "madness", cost: 10, cooldownSeconds: 15, unlockCost: 0, prerequisites: [], targetRule: "Any region", summary: "+6 Influence, +5 Instability, +3 Exposure" },
  mass_delusion: { id: "mass_delusion", name: "Mass Delusion", branch: "madness", cost: 30, cooldownSeconds: 50, unlockCost: 2, prerequisites: [], targetRule: "Influence at least 20", summary: "+16 Influence, +18 Instability, +15 Exposure; adjacent +3" },
  impossible_omen: { id: "impossible_omen", name: "Impossible Omen", branch: "omen", cost: 18, cooldownSeconds: 35, unlockCost: 1, prerequisites: [], targetRule: "Any region", summary: "Resistance -10 for 45s, +5 Influence, +8 Instability" },
  rewrite_headline: { id: "rewrite_headline", name: "Rewrite the Headline", branch: "omen", cost: 22, cooldownSeconds: 40, unlockCost: 2, prerequisites: [], targetRule: "Hostile event or Exposure above 20", summary: "-15 Exposure, -1 event severity, -2 Influence" },
};

export const UPGRADES: Record<UpgradeId, UpgradeDefinition> = {
  hidden_cells: { id: "hidden_cells", name: "Hidden Cells", branch: "ritual", unlockCost: 3, prerequisite: "secret_gathering", summary: "Secret Gathering adds 3 less Exposure; crackdowns remove 25% less Cult." },
  contagious_dreams: { id: "contagious_dreams", name: "Contagious Dreams", branch: "madness", unlockCost: 3, prerequisite: "whispered_dreams", summary: "Every 20s the strongest region spreads +1 Influence to an adjacent region below 30." },
  false_consensus: { id: "false_consensus", name: "False Consensus", branch: "omen", unlockCost: 3, prerequisite: "impossible_omen", summary: "Exposure suppression -15%; opportunity events last 20% longer." },
};
