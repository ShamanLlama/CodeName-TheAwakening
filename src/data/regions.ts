import type { RegionDefinition, RegionId } from "../game/types";

export const REGIONS: Record<RegionId, RegionDefinition> = {
  north_america: { id: "north_america", name: "North America", populationWeight: 14, skepticism: 65, institutionalResistance: 75, mysticism: 35, adjacent: ["latin_america", "europe", "east_asia"], tags: ["coastal", "media"] },
  latin_america: { id: "latin_america", name: "Latin America", populationWeight: 12, skepticism: 45, institutionalResistance: 45, mysticism: 65, adjacent: ["north_america", "africa", "oceania"], tags: ["coastal", "mystical"] },
  europe: { id: "europe", name: "Europe", populationWeight: 13, skepticism: 70, institutionalResistance: 80, mysticism: 30, adjacent: ["north_america", "africa", "middle_east"], tags: ["coastal", "institutional"] },
  africa: { id: "africa", name: "Africa", populationWeight: 15, skepticism: 40, institutionalResistance: 35, mysticism: 70, adjacent: ["latin_america", "europe", "middle_east", "south_asia"], tags: ["coastal", "mystical", "populous"] },
  middle_east: { id: "middle_east", name: "Middle East", populationWeight: 10, skepticism: 55, institutionalResistance: 70, mysticism: 60, adjacent: ["europe", "africa", "south_asia", "east_asia"], tags: ["religious", "institutional"] },
  south_asia: { id: "south_asia", name: "South Asia", populationWeight: 18, skepticism: 50, institutionalResistance: 50, mysticism: 65, adjacent: ["africa", "middle_east", "east_asia", "oceania"], tags: ["coastal", "mystical", "populous"] },
  east_asia: { id: "east_asia", name: "East Asia", populationWeight: 15, skepticism: 75, institutionalResistance: 85, mysticism: 35, adjacent: ["north_america", "middle_east", "south_asia", "oceania"], tags: ["coastal", "institutional", "populous"] },
  oceania: { id: "oceania", name: "Oceania", populationWeight: 3, skepticism: 50, institutionalResistance: 45, mysticism: 55, adjacent: ["latin_america", "south_asia", "east_asia"], tags: ["coastal", "rlyeh"] },
};

export const POPULATION_WEIGHT_TOTAL = Object.values(REGIONS).reduce((sum, region) => sum + region.populationWeight, 0);
