import type { EventDefinition, EventId } from "../game/types";

export const EVENTS: Record<EventId, EventDefinition> = {
  investigators_arrive: { id: "investigators_arrive", name: "Investigators Arrive", type: "hostile", durationSeconds: 45, minimumInfluence: 0, weight: 10, headline: "An unmarked task force follows reports of impossible coastal chants." },
  government_crackdown: { id: "government_crackdown", name: "Government Crackdown", type: "hostile", durationSeconds: 60, minimumInfluence: 0, weight: 8, minimumInstitutionalResistance: 60, headline: "Authorities coordinate raids against suspected esoteric cells." },
  religious_revival: { id: "religious_revival", name: "Religious Revival", type: "hostile", durationSeconds: 50, minimumInfluence: 0, weight: 9, headline: "A revival movement rallies communities against the spreading dreams." },
  secret_society_intervention: { id: "secret_society_intervention", name: "Secret Society Intervention", type: "hostile", durationSeconds: 55, minimumInfluence: 35, weight: 7, headline: "An old society reveals itself to contain the impossible." },
  shared_nightmare: { id: "shared_nightmare", name: "Shared Nightmare", type: "opportunity", durationSeconds: 35, minimumInfluence: 0, weight: 10, headline: "Thousands describe the same black city beneath alien stars." },
  solar_eclipse: { id: "solar_eclipse", name: "Solar Eclipse", type: "opportunity", durationSeconds: 40, minimumInfluence: 0, weight: 8, headline: "A lightless noon draws hidden congregations into the streets." },
  political_scandal: { id: "political_scandal", name: "Political Scandal", type: "opportunity", durationSeconds: 45, minimumInfluence: 0, weight: 8, headline: "A fictional administration turns inward amid an invented scandal." },
  deep_sea_signal: { id: "deep_sea_signal", name: "Deep-Sea Signal", type: "opportunity", durationSeconds: 30, minimumInfluence: 0, weight: 7, validTags: ["coastal"], headline: "Hydrophones record a repeating signal from an impossible depth." },
};
