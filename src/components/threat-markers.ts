import { seedToState } from "../game/random";
import type { RegionId, Threat } from "../game/types";

// Land locations in longitude/latitude. Visual variation does not consume game RNG.
const LOCATIONS: Record<RegionId, [number, number][]> = {
  north_america: [[-123,53],[-102,39],[-101,24],[-80,43],[-150,64],[-98,60]],
  latin_america: [[-74,-10],[-52,-10],[-65,-32],[-66,7],[-86,14],[-69,-22]],
  europe: [[2,47],[14,51],[26,62],[40,56],[-4,40],[20,45]],
  africa: [[-10,12],[18,5],[30,-6],[23,-23],[25,26],[10,-1]],
  middle_east: [[44,33],[54,32],[45,23],[34,39],[44,16],[46,25]],
  south_asia: [[76,23],[69,30],[89,24],[81,8],[67,34],[82,27]],
  east_asia: [[105,36],[99,47],[104,17],[136,36],[117,-2],[95,60]],
  oceania: [[135,-25],[146,-22],[122,-22],[145,-6],[174,-39],[130,-18]],
};
export function threatMapPosition(seed: string, id: RegionId, threat: Threat): [number, number] {
  const points = LOCATIONS[id];
  const index = (seedToState(seed + id + threat.kind) + Math.floor(threat.dueMonth / 12)) % points.length;
  const [lon, lat] = points[index]!;
  return [(lon + 180) * 2, (85 - lat) * 2];
}
