import type { Opportunity, Threat } from "../game/types";
export const THREAT_NAMES: Record<Threat["kind"], string> = { investigation: "Investigators follow the dream", raid: "Authorities prepare a raid", occult: "An occult society targets the rite" };
export const OPPORTUNITY_NAMES: Record<Opportunity["kind"], string> = { nightmare: "A shared nightmare", eclipse: "A lightless eclipse", celebrity: "A celebrated artist seeks the truth", politician: "A minister studies forbidden texts" };
