import { BALANCE } from "./balance";
import type { ActionDefinition, ActionId, Specialization } from "../game/types";
export const ACTIONS: Record<ActionId, ActionDefinition> = {
  recruit: { name: "Secret gathering", description: "Recruit up to 35 unaware people or believers.", influence: 18, dread: 0, followers: 0, cooldown: 12 },
  expand: { name: "Establish a cell", description: "Send the dream to an adjacent region. Rebuild anywhere if all cells are lost.", influence: 30, dread: 0, followers: 0, cooldown: 0 },
  sacrifice: { name: "Sacrifice followers", description: "Consume 40 followers for 25 Dread. Their future income is lost.", influence: 0, dread: 0, followers: BALANCE.sacrificeFollowers, cooldown: 6 },
  reveal: { name: "Begin public worship", description: "Double ongoing recruitment. Invite scrutiny and backlash.", influence: 10, dread: 0, followers: 0, cooldown: 6 },
  hide: { name: "Return underground", description: "End public recruitment. Existing evidence and rituals remain visible.", influence: 0, dread: 0, followers: 0, cooldown: 6 },
  conceal: { name: "Conceal the evidence", description: "Remove up to 25 Exposure and 1 world awareness. Public worship and rituals retain an evidence floor.", influence: 20, dread: 0, followers: 0, cooldown: 12 },
  manifest: { name: "Impossible manifestation", description: "Turn deniers and unaware people into believers. Belief also fuels resistance.", influence: 0, dread: 20, followers: 0, cooldown: 18 },
  prepare: { name: "Prepare a ritual site", description: "Commit Dread and 40 Influence, then maintain local support for 24 months. Each further site costs more.", influence: 40, dread: 60, followers: 0, cooldown: 0 },
  counter: { name: "Obstruct the investigation", description: "Burn your local connections to neutralize this region's threat. They take 12 months to rebuild.", influence: 30, dread: 0, followers: 0, cooldown: 12 },
  exploit: { name: "Exploit the omen", description: "Claim a shared nightmare's Dread or accelerate a ritual during an eclipse.", influence: 10, dread: 0, followers: 0, cooldown: 0 },
  convert: { name: "Convert a prominent figure", description: "A hidden celebrity earns Influence; a politician slows investigations.", influence: 45, dread: 0, followers: 0, cooldown: 0 },
  reveal_figure: { name: "Reveal their allegiance", description: "Recruit up to 60 people and advance the local site by 8 months once. Ends the secret benefit.", influence: 0, dread: 0, followers: 0, cooldown: 0 },
};
export const SPECIALIZATIONS: Record<Specialization, { name: string; description: string }> = {
  recruitment: { name: "Congregation", description: "+35% automatic recruitment. Build a broad following." },
  infiltration: { name: "Hidden hand", description: "Threats arrive 2 months later; obstructing them costs a third less." },
  terror: { name: "Harbinger", description: "Sacrifices yield 35 Dread; manifestations cost 15 Dread." },
};
