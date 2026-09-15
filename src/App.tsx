import { useEffect, useMemo, useRef, useState } from "react";
import {
  createGameState, advanceSimulation, castAbility, debugPatchState, forceEvent,
  dreadPerSecond, effectiveAbilityCost, placeStartingFoothold, setGameSpeed, startAwakening, unlockAbility, unlockUpgrade,
} from "./game/engine";
import { ABILITIES, UPGRADES } from "./data/abilities";
import { DIFFICULTIES } from "./data/difficulties";
import { EVENTS } from "./data/events";
import { REGIONS } from "./data/regions";
import { ABILITY_IDS, EVENT_IDS, REGION_IDS, type AbilityBranch, type AbilityId, type Difficulty, type EventId, type GameState, type RegionId } from "./game/types";
import { effectiveResistance } from "./game/derived";
import { exportTelemetry, loadRun, resultFor, saveRun, SAVE_VERSION, type GameSettings } from "./game/persistence";

const BRANCHES: readonly AbilityBranch[] = ["ritual", "madness", "omen"];
const TUTORIAL = [
  "Choose a vulnerable region and establish your first foothold.",
  "Use Whispered Dreams to turn unease into influence.",
  "Influence grows Dread. Dread powers your darker interventions.",
  "Events change a region's conditions. Work with opportunities and contain hostile attention.",
  "Spend Insight to unlock abilities and their branch upgrades.",
];
const MAP: Record<RegionId, { x: number; y: number; w: number; h: number }> = {
  north_america: { x: 38, y: 66, w: 118, h: 74 }, latin_america: { x: 118, y: 160, w: 78, h: 120 },
  europe: { x: 265, y: 70, w: 76, h: 52 }, africa: { x: 262, y: 136, w: 86, h: 122 },
  middle_east: { x: 360, y: 122, w: 74, h: 58 }, south_asia: { x: 430, y: 166, w: 104, h: 82 },
  east_asia: { x: 485, y: 74, w: 122, h: 102 }, oceania: { x: 564, y: 225, w: 90, h: 53 },
};

function isEventId(value: string): value is EventId { return EVENT_IDS.some((id) => id === value); }
function isDifficulty(value: string): value is Difficulty { return value === "easy" || value === "normal" || value === "hard"; }
function meter(value: number, label: string, max = 100) {
  return <div className="meter" aria-label={`${label}: ${Math.round(value)} of ${max}`}><span>{label}</span><div><i style={{ width: `${Math.max(0, Math.min(100, (value / max) * 100))}%` }} /></div><b>{Math.round(value)}{max === 100 ? "%" : `/${max}`}</b></div>;
}
function duration(seconds: number) { return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`; }

export function App() {
  const [initialSave] = useState(() => loadRun(localStorage));
  const [game, setGame] = useState<GameState | null>(initialSave.state);
  const [difficulty, setDifficulty] = useState<Difficulty>("normal");
  const [seed, setSeed] = useState("");
  const [selectedRegion, setSelectedRegion] = useState<RegionId>("north_america");
  const [selectedAbility, setSelectedAbility] = useState<AbilityId | null>(null);
  const [showUpgrades, setShowUpgrades] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [settings, setSettings] = useState<GameSettings>(initialSave.settings);
  const [tutorialComplete, setTutorialComplete] = useState(initialSave.tutorialComplete);
  const [tutorialStep, setTutorialStep] = useState<number | null>(null);
  const [notice, setNotice] = useState("");
  const [debugEvent, setDebugEvent] = useState<EventId>("investigators_arrive");
  const bestResults = useRef(initialSave.bestResults);
  const gameRef = useRef(game);
  const elapsed = useRef(0);

  useEffect(() => { gameRef.current = game; }, [game]);
  useEffect(() => {
    if (game && (game.status === "won" || game.status === "lost")) {
      const candidate = resultFor(game);
      const previous = bestResults.current[game.difficulty];
      const better = !previous || (candidate.won && !previous.won) || (candidate.won === previous.won && candidate.elapsedSeconds < previous.elapsedSeconds);
      if (better) bestResults.current = { ...bestResults.current, [game.difficulty]: candidate };
    }
    saveRun(localStorage, { version: SAVE_VERSION, state: game, settings, tutorialComplete, bestResults: bestResults.current });
  }, [game, settings, tutorialComplete]);
  useEffect(() => {
    const timer = window.setInterval(() => {
      const current = gameRef.current;
      if (!current || current.status === "setup" || current.status === "won" || current.status === "lost" || tutorialStep !== null || current.speed === 0) return;
      elapsed.current += current.speed * 0.25;
      const seconds = Math.floor(elapsed.current);
      if (seconds) {
        elapsed.current -= seconds;
        setGame((previous) => previous ? advanceSimulation(previous, seconds) : previous);
      }
    }, 250);
    return () => window.clearInterval(timer);
  }, [tutorialStep]);

  const selected = game ? game.regions[selectedRegion] : null;
  const selectedEvent = game?.activeEvents.find((event) => event.regionId === selectedRegion && !event.resolved);
  const abilityPreview = game && selectedAbility ? castAbility(game, selectedAbility, selectedRegion) : null;
  const previewText = !abilityPreview ? "" : !abilityPreview.ok ? abilityPreview.message : `Influence ${selected?.influence.toFixed(1)}→${abilityPreview.state.regions[selectedRegion].influence.toFixed(1)}, Cult ${selected?.cultStrength.toFixed(1)}→${abilityPreview.state.regions[selectedRegion].cultStrength.toFixed(1)}, Exposure ${selected?.exposure.toFixed(1)}→${abilityPreview.state.regions[selectedRegion].exposure.toFixed(1)}`;
  const target = game ? DIFFICULTIES[game.difficulty].influenceTarget : 100;
  const mostUsed = useMemo(() => {
    if (!game) return null;
    return ABILITY_IDS.reduce((best, id) => game.telemetry.casts[id] > game.telemetry.casts[best] ? id : best, ABILITY_IDS[0]);
  }, [game]);

  function begin() {
    const next = createGameState({ difficulty, seed: seed.trim() || crypto.randomUUID().slice(0, 8) });
    setGame(next); setSelectedAbility(null); setNotice(""); elapsed.current = 0;
    if (!tutorialComplete) setTutorialStep(0);
  }
  function apply(result: ReturnType<typeof castAbility>) {
    setGame(result.state);
    setNotice(result.ok ? result.state.log.at(-1)?.message ?? "The world shifts." : result.message);
  }
  function chooseRegion(regionId: RegionId) {
    if (!game) return;
    setSelectedRegion(regionId);
    if (game.status === "placing") { apply(placeStartingFoothold(game, regionId)); return; }
    if (selectedAbility) {
      const result = castAbility(game, selectedAbility, regionId);
      apply(result);
      if (result.ok) { playTone(ABILITIES[selectedAbility].branch); setSelectedAbility(null); }
    }
  }
  function selectAbility(id: AbilityId) {
    if (!game) return;
    if (!game.unlockedAbilities.includes(id)) { apply(unlockAbility(game, id)); return; }
    setSelectedAbility((current) => current === id ? null : id);
    setNotice(currentNotice(id, game, selectedRegion));
  }
  function currentNotice(id: AbilityId, state: GameState, region: RegionId) {
    const result = castAbility(state, id, region);
    return result.ok ? `Target ${REGIONS[region].name}. Click it to cast.` : result.message;
  }
  function closeTutorial() { setTutorialStep(null); setTutorialComplete(true); }
  function playTone(branch: AbilityBranch) {
    if (settings.muted) return;
    const context = new AudioContext();
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = branch === "ritual" ? "sine" : branch === "madness" ? "sawtooth" : "triangle";
    oscillator.frequency.value = branch === "ritual" ? 140 : branch === "madness" ? 210 : 310;
    gain.gain.setValueAtTime(Math.max(0.01, settings.volume / 500), context.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, context.currentTime + 0.16);
    oscillator.connect(gain).connect(context.destination);
    oscillator.start(); oscillator.stop(context.currentTime + 0.16);
    oscillator.addEventListener("ended", () => void context.close());
  }
  function exportRun() {
    if (!game) return;
    const link = document.createElement("a");
    link.href = URL.createObjectURL(new Blob([exportTelemetry(game)], { type: "application/json" }));
    link.download = "cthulhu-run.json"; link.click(); URL.revokeObjectURL(link.href);
  }
  function debugWin() {
    if (!game) return;
    const ready = debugPatchState(game, { dread: 150, allRegionInfluence: target });
    const awakening = startAwakening(ready);
    setGame(awakening.ok ? advanceSimulation(awakening.state, 45) : awakening.state);
  }

  if (!game) return <main className="app setup"><section className="modal" aria-labelledby="game-title"><p className="eyebrow">A world-map strategy game</p><h1 id="game-title">The Awakening<br />of Cthulhu</h1><p>Spread the dream. Evade the watchers. Open the way.</p><label>Difficulty<select data-testid="setup-difficulty" value={difficulty} onChange={(event) => { if (isDifficulty(event.target.value)) setDifficulty(event.target.value); }}>{Object.values(DIFFICULTIES).map((item) => <option key={item.id} value={item.id}>{item.name} — target {item.influenceTarget}%</option>)}</select></label><label>Seed <input data-testid="setup-seed" value={seed} onChange={(event) => setSeed(event.target.value)} placeholder="Leave blank for a new omen" maxLength={40} /></label><button className="primary" data-testid="start-run" onClick={begin}>Begin the rite</button></section></main>;

  const end = game.status === "won" || game.status === "lost";
  return <main className="app">
    <header className="hud"><div className="brand">THE AWAKENING <small>of CTHULHU</small></div>{meter(game.globalInfluence, "Influence", target)}{meter(game.suppression, "Suppression")}<div className="resource"><span>Dread</span><b>{Math.floor(game.dread)}</b><small>+{dreadPerSecond(game).toFixed(2)}/s</small></div><div className="resource"><span>Insight</span><b>{game.insight}</b></div><div className="phase">{game.status === "placing" ? "Choose a foothold" : game.status}</div><div className="speed" aria-label="Simulation speed"><button data-testid="speed-pause" onClick={() => setGame(setGameSpeed(game, 0))} aria-pressed={game.speed === 0}>Ⅱ</button><button data-testid="speed-1x" onClick={() => setGame(setGameSpeed(game, 1))} aria-pressed={game.speed === 1}>1×</button><button data-testid="speed-2x" onClick={() => setGame(setGameSpeed(game, 2))} aria-pressed={game.speed === 2}>2×</button></div><button className="icon" onClick={() => setShowSettings((value) => !value)} aria-label="Open settings">⚙</button></header>
    {showSettings && <aside className="settings"><label><input type="checkbox" checked={settings.muted} onChange={(event) => setSettings({ ...settings, muted: event.target.checked })} /> Mute audio</label><label>Volume <input type="range" min="0" max="100" value={settings.volume} onChange={(event) => setSettings({ ...settings, volume: Number(event.target.value) })} disabled={settings.muted} /></label><button onClick={() => setGame(null)}>Restart run</button></aside>}
    <section className="game-grid">
      <aside className="panel abilities"><div className="panel-title"><h2>Dark arts</h2><button data-testid="upgrade-panel" onClick={() => setShowUpgrades((value) => !value)} aria-expanded={showUpgrades}>Upgrades</button></div>{showUpgrades ? <div className="cards">{Object.values(UPGRADES).map((upgrade) => { const unlocked = game.unlockedUpgrades.includes(upgrade.id); const ready = game.unlockedAbilities.includes(upgrade.prerequisite) && game.insight >= upgrade.unlockCost; return <button data-testid={`upgrade-${upgrade.id}`} className="card" key={upgrade.id} disabled={unlocked || !ready} onClick={() => apply(unlockUpgrade(game, upgrade.id))}><span>{upgrade.branch} upgrade</span><strong>{upgrade.name}</strong><small>{upgrade.summary}</small><em>{unlocked ? "unlocked" : `${upgrade.unlockCost} insight`}</em></button>; })}</div> : BRANCHES.map((branch) => <div className="branch" key={branch}><h3>{branch}</h3>{ABILITY_IDS.filter((id) => ABILITIES[id].branch === branch).map((id) => { const ability = ABILITIES[id]; const unlocked = game.unlockedAbilities.includes(id); const cd = game.cooldowns[id]; const cost = effectiveAbilityCost(game, id, selectedRegion); const reason = !unlocked ? `${ability.unlockCost} insight to unlock` : cd > 0 ? `${Math.ceil(cd)}s cooldown` : game.dread < cost ? "not enough Dread" : ability.targetRule; return <button data-testid={`ability-${id}`} data-action={unlocked ? "cast" : "unlock"} key={id} className={`card ${selectedAbility === id ? "selected" : ""}`} onClick={() => selectAbility(id)} disabled={game.status !== "playing" && game.status !== "awakening" || (unlocked && cd > 0)}><span>{branch}</span><strong>{ability.name}</strong><small>{ability.summary}</small>{unlocked && cd > 0 && <progress aria-label={`${ability.name} cooldown`} max={ability.cooldownSeconds} value={ability.cooldownSeconds - cd} />}<em>{unlocked ? `${cost.toFixed(1)} dread · ${reason}` : reason}</em></button>; })}</div>)}</aside>
      <section className="map-panel" aria-label="World map"><div className="map-heading"><h2>Where the dream takes hold</h2><span>{notice || "Select a region to inspect it."}</span></div><svg viewBox="0 0 700 330" role="img" aria-label="Eight selectable world regions" className="world-map"><path className="ocean-lines" d="M0 100 C150 50 260 150 350 80 S550 70 700 140 M0 220 C150 160 250 250 380 190 S560 210 700 160" />{REGION_IDS.map((id) => { const point = MAP[id]; const region = game.regions[id]; const active = selectedRegion === id; const valid = selectedAbility ? castAbility(game, selectedAbility, id).ok : false; const eventType = game.activeEvents.find((event) => event.regionId === id && !event.resolved); const corruption = Math.min(4, Math.floor(region.influence / 20)); return <g data-testid={`region-${id}`} data-corruption={corruption} key={id} role="button" tabIndex={0} aria-label={`${REGIONS[id].name}, influence ${Math.round(region.influence)} percent`} aria-pressed={active} className={`region corruption-${corruption} ${eventType ? `event-${EVENTS[eventType.eventId].type}` : ""} ${active ? "active" : ""} ${valid ? "valid" : ""}`} onClick={() => chooseRegion(id)} onKeyDown={(event) => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); chooseRegion(id); } }}><rect x={point.x} y={point.y} width={point.w} height={point.h} rx="16" style={{ opacity: 0.38 + region.influence / 165 }} /><text x={point.x + point.w / 2} y={point.y + point.h / 2 - 8}>{REGIONS[id].name}</text><text className="map-value" x={point.x + point.w / 2} y={point.y + point.h / 2 + 16}>{Math.round(region.influence)}% influence</text></g>; })}</svg><div className="legend"><span className="legend-active" /> selected <span className="legend-valid" /> valid target</div></section>
      <aside className="panel detail"><h2>{REGIONS[selectedRegion].name}</h2><p className="traits">{REGIONS[selectedRegion].tags.join(" · ")}</p>{selected && <dl><div><dt>Influence</dt><dd>{Math.round(selected.influence)}%</dd></div><div><dt>Cult strength</dt><dd>{Math.round(selected.cultStrength)}</dd></div><div><dt>Resistance</dt><dd>{Math.round(effectiveResistance(game, selectedRegion))}</dd></div><div><dt>Exposure</dt><dd>{Math.round(selected.exposure)}%</dd></div><div><dt>Instability</dt><dd>{Math.round(selected.instability)}%</dd></div><div><dt>Population weight</dt><dd>{REGIONS[selectedRegion].populationWeight}</dd></div></dl>}{selectedEvent ? <section className={`event ${EVENTS[selectedEvent.eventId].type}`}><span>{EVENTS[selectedEvent.eventId].type} event</span><strong>{EVENTS[selectedEvent.eventId].name}</strong><p>{EVENTS[selectedEvent.eventId].headline}</p><b>{duration(selectedEvent.remainingSeconds)} remaining</b></section> : <p className="empty">No active event in this region.</p>}{selectedAbility && <section className="preview"><span>Predicted effect</span><strong>{ABILITIES[selectedAbility].name}</strong><p>{previewText}</p><small>{effectiveAbilityCost(game, selectedAbility, selectedRegion).toFixed(1)} Dread · {ABILITIES[selectedAbility].targetRule}</small></section>}<button data-testid="awakening-action" className="awakening" disabled={game.status !== "playing"} onClick={() => apply(startAwakening(game))}>Awaken Cthulhu <small>100 Dread + target Influence</small></button></aside>
    </section>
    <footer className="ticker" aria-live="polite"><b>Chronicle</b><div>{game.log.slice(-1)[0]?.message ?? "The deep waits beneath unquiet waters."}</div><span>{duration(game.elapsedSeconds)}</span><details className="action-log"><summary>Action log</summary><ol>{game.log.slice(-30).reverse().map((entry) => <li key={entry.id}><time>{duration(entry.elapsedSeconds)}</time> {entry.message}</li>)}</ol></details></footer>
    {import.meta.env.DEV && <details data-testid="debug-panel" className="debug"><summary>Development controls · seed <code>{game.seed}</code></summary><div><button data-testid="debug-dread" onClick={() => setGame(debugPatchState(game, { dread: game.dread + 25 }))}>+25 Dread</button><button data-testid="debug-insight" onClick={() => setGame(debugPatchState(game, { insight: game.insight + 1 }))}>+1 Insight</button><button data-testid="debug-influence" onClick={() => setGame(debugPatchState(game, { allRegionInfluence: 70 }))}>Set influence 70</button><button data-testid="debug-suppression" onClick={() => setGame(debugPatchState(game, { suppression: 50 }))}>Set suppression 50</button><button data-testid="debug-win" onClick={debugWin}>Script win</button><button data-testid="debug-loss" onClick={() => setGame(debugPatchState(game, { suppression: 100 }))}>Script loss</button><select data-testid="debug-event" value={debugEvent} onChange={(event) => { if (isEventId(event.target.value)) setDebugEvent(event.target.value); }} aria-label="Force event">{EVENT_IDS.map((id) => <option key={id} value={id}>{EVENTS[id].name}</option>)}</select><button data-testid="debug-force-event" onClick={() => apply(forceEvent(game, debugEvent, selectedRegion))}>Force event</button><button data-testid="debug-export" onClick={exportRun}>Export JSON</button></div></details>}
    {tutorialStep !== null && <section className="overlay" role="dialog" aria-modal="true" aria-labelledby="tutorial-title"><div className="tutorial"><p>Initiation {tutorialStep + 1} / {TUTORIAL.length}</p><h2 id="tutorial-title">{TUTORIAL[tutorialStep]}</h2><div><button data-testid="tutorial-dismiss" onClick={closeTutorial}>Dismiss</button><button data-testid="tutorial-next" className="primary" onClick={() => tutorialStep === TUTORIAL.length - 1 ? closeTutorial() : setTutorialStep(tutorialStep + 1)}>{tutorialStep === TUTORIAL.length - 1 ? "Enter the world" : "Next"}</button></div></div></section>}
    {end && <section className="overlay" role="dialog" aria-modal="true" aria-labelledby="end-title"><div className="tutorial end"><p>{game.status === "won" ? "The stars are right" : "The watchers prevailed"}</p><h2 id="end-title">{game.status === "won" ? "Cthulhu awakens" : "The rite is broken"}</h2><p>Run {duration(game.elapsedSeconds)} · Influence {Math.round(game.globalInfluence)}% · Suppression {Math.round(game.suppression)}%</p><p>Most used: {mostUsed ? ABILITIES[mostUsed].name : "None"}</p><button data-testid="new-run" className="primary" onClick={() => { setGame(null); setTutorialStep(null); }}>Begin again</button></div></section>}
  </main>;
}
