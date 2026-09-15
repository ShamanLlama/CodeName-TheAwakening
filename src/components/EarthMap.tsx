import earth from "../assets/earth-regions.json";
import { BALANCE } from "../data/balance";
import { REGIONS } from "../data/regions";
import { threatMapPosition } from "./threat-markers";
import { describeThreat } from "../game/threats";
import { REGION_IDS, type GameState, type RegionId } from "../game/types";

const LABELS: Record<RegionId, [number, number]> = {
  north_america: [150, 77], latin_america: [220, 205], europe: [400, 53],
  africa: [396, 165], middle_east: [451, 112], south_asia: [514, 156],
  east_asia: [591, 83], oceania: [635, 235],
};

export function EarthMap({ game, selected, onSelect }: { game: GameState; selected: RegionId; onSelect: (id: RegionId) => void }) {
  return <svg viewBox="0 0 720 300" className="world-map earth-map" aria-label="Earth map with eight selectable cult regions">
    <g className="graticule" aria-hidden="true">
      {[50, 110, 170, 230, 290].map(y => <line key={"lat" + y} x1="0" y1={y} x2="720" y2={y} />)}
      {[60, 180, 300, 420, 540, 660].map(x => <line key={"lon" + x} x1={x} y1="0" x2={x} y2="290" />)}
    </g>
    {REGION_IDS.map(id => {
      const region = game.regions[id];
      return <g key={id} role="button" tabIndex={0} data-testid={"region-" + id}
        aria-label={REGIONS[id].name + ", " + Math.floor(region.population.followers) + " followers, " + (region.public ? "public" : "hidden")}
        aria-pressed={selected === id}
        className={"region " + (selected === id ? "selected " : "") + (region.public ? "public " : "") + (region.population.followers > 0 ? "occupied" : "")}
        onClick={() => onSelect(id)} onKeyDown={e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onSelect(id); } }}>
        <title>{REGIONS[id].name} · click land to direct this cult</title>
        {earth[id].map(country => <path key={country.name} data-country={country.name} d={country.d} fillRule="evenodd"><title>{country.name} · {REGIONS[id].name}</title></path>)}
      </g>;
    })}
    <g className="earth-labels" aria-hidden="true">
      {REGION_IDS.map(id => {
        const region = game.regions[id], [x, y] = LABELS[id];
        return <g key={id} transform={"translate(" + x + " " + y + ")"}>
          <text>{REGIONS[id].name}</text>
          <text className="map-value" y="14">{Math.floor(region.population.followers)} followers</text>
          {region.site.invested && <text className="map-symbol" y="27">{region.site.progress >= BALANCE.siteMonths ? "◇ SITE" : "◇ PREPARING"}</text>}
        </g>;
      })}
    </g>
    <g className="threat-markers">
      {REGION_IDS.map(id => {
        const region = game.regions[id], threat = region.threat;
        if (!threat || game.status === "won" || game.status === "lost") return null;
        const [x, y] = threatMapPosition(game.seed, id, threat), months = threat.dueMonth - game.elapsedMonths;
        return <g key={id} role="button" tabIndex={0} data-testid={"threat-marker-" + id}
          className={"threat-marker " + (months <= 3 ? "urgent" : "")}
          transform={"translate(" + x + " " + y + ")"}
          aria-label={REGIONS[id].name + ": " + threat.kind + " in " + months + " months. " + describeThreat(region)}
          onClick={() => onSelect(id)} onKeyDown={e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onSelect(id); } }}>
          <title>{REGIONS[id].name} · {months} months to respond. At current strength: {describeThreat(region)}</title>
          <circle className="marker-target" r="11" /><circle className="marker-ring" r="8" />
          <circle className="marker-core" r="4" /><text y="-12">{months}m</text>
        </g>;
      })}
    </g>
  </svg>;
}
