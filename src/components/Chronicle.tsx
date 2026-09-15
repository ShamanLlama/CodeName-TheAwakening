import { REGIONS } from "../data/regions";
import { THREAT_NAMES } from "../data/events";
import { calendar } from "../game/derived";
import { previewAction } from "../game/engine";
import { describeThreat } from "../game/threats";
import { REGION_IDS, type GameState, type RegionId } from "../game/types";

export function Chronicle({ game, onSelect, onCounter, disabled }: {
  game: GameState; onSelect: (id: RegionId) => void; onCounter: (id: RegionId) => void; disabled: boolean;
}) {
  const ended = game.status === "won" || game.status === "lost";
  const threatened = ended ? [] : REGION_IDS.filter(id => game.regions[id].threat).sort((a,b) => game.regions[a].threat!.dueMonth - game.regions[b].threat!.dueMonth);
  const totalCost = threatened.reduce((sum,id) => sum + previewAction(game, "counter", id).influence, 0);
  return <footer className="chronicle-dock" data-testid="chronicle-dock">
    <section className="chronicle-pane" aria-labelledby="chronicle-title">
      <div className="dock-heading"><h2 id="chronicle-title">World chronicle</h2><span>Latest first · seed {game.seed}</span></div>
      <ol className="chronicle-list" tabIndex={0} aria-label="World event history">
        {game.log.slice().reverse().map(entry => <li key={entry.id}><time>{calendar(entry.month)}</time><span>{entry.message}</span></li>)}
        {game.log.length === 0 && <li>The world has not yet heard your name. Choose a starting region.</li>}
      </ol>
    </section>
    <section className="threat-watch" aria-labelledby="threat-watch-title">
      <div className="dock-heading"><h2 id="threat-watch-title">{threatened.length} approaching threats</h2><span>{totalCost > 0 ? totalCost + " Influence to counter all · " + Math.floor(game.influence) + " available" : ended ? "Run complete" : "Watch the world"}</span></div>
      <div className="threat-list" tabIndex={0} aria-label="Approaching world threats">
        {threatened.map(id => {
          const r = game.regions[id], threat = r.threat!, months = threat.dueMonth - game.elapsedMonths;
          const preview = previewAction(game, "counter", id);
          return <article key={id} className={months <= 3 ? "urgent" : ""} data-testid={"threat-watch-" + id}>
            <div><button className="threat-region" onClick={() => onSelect(id)}>{REGIONS[id].name} · {months} months</button><span>{THREAT_NAMES[threat.kind]}</span></div>
            <p>At current strength: {describeThreat(r)}</p>
            <button disabled={disabled || !preview.ok} onClick={() => onCounter(id)}>Counter · {preview.influence} Influence</button>
            {!preview.ok && <small>{preview.reason}</small>}
          </article>;
        })}
        {threatened.length === 0 && <p>{ended ? "Review the chronicle to plan your next awakening." : "No active investigations. New threats appear here with their deadlines and consequences."}</p>}
      </div>
    </section>
  </footer>;
}
