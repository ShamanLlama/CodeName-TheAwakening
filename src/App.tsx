/*
THESIS: A world-map strategy table for choosing where the sleeping god reveals itself.
OWN-WORLD: Preserve the existing deep-sea teal, aged gold, serif headings, and regional map.
STORY: Build a cult, weigh sacrifice against growth, expose ritual sites, survive the alignment.
FIRST VIEWPORT: Resource/calendar header; contextual actions left, world and ritual readiness center, selected region right.
FORM: Extend the existing three-panel game surface; no replacement visual identity.
*/
import { useEffect, useRef, useState } from "react";
import { EarthMap } from "./components/EarthMap";
import { Chronicle } from "./components/Chronicle";
import { describeThreat } from "./game/threats";
import { ritualForecast } from "./game/forecast";
import { ACTIONS, SPECIALIZATIONS } from "./data/abilities";
import { BALANCE as B } from "./data/balance";
import { DIFFICULTIES } from "./data/difficulties";
import { OPPORTUNITY_NAMES, THREAT_NAMES } from "./data/events";
import { REGIONS } from "./data/regions";
import { advanceSimulation, awakeningReason, chooseSpecialization, createGameState, performAction, placeStartingFoothold, previewAction, setGameSpeed, startAwakening } from "./game/engine";
import { awarenessDrag, cultShare, calendar, deadline, influencePerMonth, livePopulation, readySites, recruitmentPerMonth, regionIncome, siteLabel, totalFollowers } from "./game/derived";
import { defaultSave, exportTelemetry, loadRun, retainRecovery, resultFor, saveRun, type LoadResult } from "./game/persistence";
import { REGION_IDS, type ActionId, type CommandResult, type Difficulty, type GameState, type RegionId, type Specialization } from "./game/types";

const GROUPS: Record<string, ActionId[]> = {
  Cult: ["recruit", "expand", "reveal", "hide", "conceal", "counter"],
  Rituals: ["sacrifice", "manifest", "prepare"],
  Opportunities: ["exploit", "convert", "reveal_figure"],
};
const format = (n: number) => Math.floor(n).toLocaleString("en-US");
function initial(): LoadResult {
  try { return loadRun(window.localStorage); }
  catch { return { save: defaultSave(), notice: "Storage is unavailable. This session will not save.", blocked: true }; }
}
function download(text: string, filename: string) {
  const url = URL.createObjectURL(new Blob([text], { type: "application/json" }));
  const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
export function App() {
  const [loaded] = useState(initial);
  const [game, setGame] = useState<GameState | null>(loaded.save.state);
  const [settings, setSettings] = useState(loaded.save.settings);
  const [tutorialComplete, setTutorialComplete] = useState(loaded.save.tutorialComplete);
  const [help, setHelp] = useState(false);
  const [blocked, setBlocked] = useState(loaded.blocked);
  const [storageNotice, setStorageNotice] = useState(loaded.notice);
  const [notice, setNotice] = useState<{ message: string; logId: number } | null>(null);
  const [difficulty, setDifficulty] = useState<Difficulty>("normal");
  const [seed, setSeed] = useState("");
  const [regionId, setRegionId] = useState<RegionId>("africa");
  const [group, setGroup] = useState("Cult");
  const [showSettings, setShowSettings] = useState(false);
  const ref = useRef(game);
  const elapsed = useRef(0);
  useEffect(() => { ref.current = game; }, [game]);
  useEffect(() => {
    if (blocked) return;
    let saved = false;
    try {
      saved = saveRun(localStorage, { version: 2, state: game, settings, tutorialComplete,
        lastResult: game && ["won", "lost"].includes(game.status) ? resultFor(game) : loaded.save.lastResult });
    } catch { /* Access to storage itself may be blocked. */ }
    if (!saved) {
      const timer = window.setTimeout(() => { setStorageNotice("Saving is unavailable. This session continues in memory."); setBlocked(true); }, 0);
      return () => window.clearTimeout(timer);
    }
  }, [game, settings, tutorialComplete, blocked, loaded.save.lastResult]);
  useEffect(() => {
    let previous = performance.now();
    const timer = window.setInterval(() => {
      const now = performance.now(), delta = Math.min((now - previous) / 1000, 0.5); previous = now;
      const current = ref.current;
      if (!current || current.status === "placing" || ["won", "lost"].includes(current.status) || current.speed === 0 || help || document.hidden) return;
      elapsed.current += delta * current.speed / B.secondsPerMonth;
      const months = Math.floor(elapsed.current);
      if (months > 0) { elapsed.current -= months; setGame(s => s ? advanceSimulation(s, months) : s); }
    }, 100);
    const hide = () => {
      previous = performance.now();
      if (document.hidden) setGame(s => s ? setGameSpeed(s, 0) : s);
    };
    document.addEventListener("visibilitychange", hide);
    return () => { window.clearInterval(timer); document.removeEventListener("visibilitychange", hide); };
  }, [help]);
  function begin() {
    let writable = false;
    try { writable = retainRecovery(localStorage); } catch { /* Keep playing in memory. */ }
    setBlocked(!writable); setStorageNotice(writable ? "" : "This session is not saving.");
    elapsed.current = 0;
    setGame(createGameState({ difficulty, seed: seed.trim() || crypto.randomUUID().slice(0, 8) }));
    setHelp(!tutorialComplete); setNotice(null); setGroup("Cult");
  }
  function apply(result: CommandResult) {
    setGame(result.state); setNotice({ message: result.ok ? result.state.log.at(-1)?.message ?? "The world shifts." : result.message, logId: result.state.log.at(-1)?.id ?? 0 });
    if (result.ok && !settings.muted && settings.volume > 0) {
      try {
        const context = new AudioContext(), oscillator = context.createOscillator(), gain = context.createGain();
        oscillator.frequency.value = 150; gain.gain.value = settings.volume / 1000;
        gain.gain.exponentialRampToValueAtTime(0.001, context.currentTime + 0.15);
        oscillator.connect(gain).connect(context.destination); oscillator.start(); oscillator.stop(context.currentTime + 0.15);
        oscillator.onended = () => { void context.close(); };
      } catch { /* Audio is optional; actions and saving remain available. */ }
    }
  }
  function select(id: RegionId) {
    setRegionId(id);
    if (game?.status === "placing" && !help) apply(placeStartingFoothold(game, id));
  }
  function dismissHelp() { setHelp(false); setTutorialComplete(true); }
  if (!game) return <main className="setup">
    <section className="intro">
      <p className="eyebrow">A world-map strategy game</p>
      <h1>The Awakening<br /><span>of Cthulhu</span></h1>
      <p className="lead">The stars are moving.<br />Make the world ready.</p>
      <p>Build hidden cults. Trade followers for forbidden power. Decide where to reveal yourself before humanity closes the way.</p>
      <div className="setup-facts"><span>50 years to prepare</span><span>3 ritual sites</span><span>One sleeping god</span></div>
    </section>
    <section className="setup-form" aria-labelledby="new-run-title">
      <h2 id="new-run-title">Begin a new awakening</h2>
      {storageNotice && <p role="status" className="notice">{storageNotice}</p>}
      <label>Difficulty<select data-testid="setup-difficulty" value={difficulty} onChange={e => setDifficulty(e.target.value as Difficulty)}>
        {Object.entries(DIFFICULTIES).map(([id, d]) => <option key={id} value={id}>{d.name}</option>)}
      </select></label>
      <label>World seed <span className="muted">(optional)</span><input data-testid="setup-seed" value={seed} onChange={e => setSeed(e.target.value)} maxLength={40} placeholder="A new omen each run" /></label>
      <button className="primary" data-testid="start-run" onClick={begin}>Enter the dream</button>
      <p className="muted">About 21 minutes at 1×, plus pauses. Every game month takes 2 seconds. Pause whenever you need to plan.</p>
      <p className="build-label">Playtest build · Alignment 1</p>
    </section>
  </main>;
  const r = game.regions[regionId], population = livePopulation(r), end = game.status === "won" || game.status === "lost";
  const aligned = game.elapsedMonths >= B.preparationMonths;
  const remaining = (aligned ? deadline : B.preparationMonths) - game.elapsedMonths;
  const readiness = readySites(game);
  const forecast = ritualForecast(game);
  return <main className="app">
    <header className="hud">
      <div className="brand">THE AWAKENING<small>of Cthulhu</small></div>
      <div className="resource"><span>Cult strength</span><b data-testid="followers">{format(totalFollowers(game))}</b><small>living followers</small></div>
      <div className="resource"><span>Influence</span><b data-testid="influence">{format(game.influence)}</b><small>+{influencePerMonth(game).toFixed(2)} / month</small></div>
      <div className="resource"><span>Dread</span><b data-testid="dread">{format(game.dread)}</b><small>power for the rites</small></div>
      <div className="calendar"><b data-testid="calendar">{calendar(game.elapsedMonths)}</b><small data-testid="countdown">{remaining} months {aligned ? "until the window closes" : "until alignment"}</small></div>
      <div className="speed" aria-label="Simulation speed">{([0, 1, 2] as const).map(speed => <button key={speed} data-testid={"speed-" + (speed === 0 ? "pause" : speed + "x")} disabled={end} aria-pressed={game.speed === speed} onClick={() => setGame(setGameSpeed(game, speed))}>{speed === 0 ? "Pause" : speed + "×"}</button>)}</div>
      <button className="plain" onClick={() => setHelp(!help)} aria-expanded={help}>Guide</button>
      <button className="plain" aria-label="Settings" aria-expanded={showSettings} onClick={() => setShowSettings(!showSettings)}>Settings</button>
    </header>
    {storageNotice && <p className="storage-notice" role="status">{storageNotice}</p>}
    {showSettings && <section className="settings">
      <label><input type="checkbox" checked={settings.muted} onChange={e => setSettings({ ...settings, muted: e.target.checked })} /> Mute audio</label>
      <label>Volume <input aria-label="Volume" type="range" min="0" max="100" value={settings.volume} onChange={e => setSettings({ ...settings, volume: Number(e.target.value) })} /></label>
      <button onClick={() => download(exportTelemetry(game), "cthulhu-playtest.json")}>Export playtest report</button>
      <button onClick={() => { setGame(null); setHelp(false); }}>Abandon run and start over</button>
    </section>}
    {help && <section className="guide" aria-label="How to play">
      <div><h2>Your path to awakening</h2><p>Choose a starting region. Cults recruit slowly and earn Influence. Expand through adjacent regions, then sacrifice followers for Dread.</p>
      <p>Public worship doubles growth but attracts opposition. Keep 60 followers at each of 3 ritual sites. Prepare each for 24 months, reserve 100 Dread, then complete a 12-month final rite during the alignment.</p>
      <p>Actions can be used while paused. Deniers need a manifestation before recruitment; believers may also resist you. The clock is paused while this guide is open.</p></div>
      <button className="primary" data-testid="tutorial-dismiss" onClick={dismissHelp}>I understand</button>
    </section>}
    <div className="game-grid">
      <aside className="panel actions-panel" aria-label="Cult actions">
        <h2>Direct the cult</h2><p className="muted">Actions affect {REGIONS[regionId].name}.</p>
        <div className="action-tabs" aria-label="Action groups">{Object.keys(GROUPS).map(g => <button key={g} onClick={() => setGroup(g)} aria-pressed={group === g}>{g}</button>)}</div>
        <div className="actions">{GROUPS[group]?.map(id => {
          const preview = previewAction(game, id, regionId);
          return <section className="action" key={id}>
            <button data-testid={"action-" + id} disabled={!preview.ok || help} onClick={() => apply(performAction(game, id, regionId))}>{ACTIONS[id].name}</button>
            <p>{preview.effect}</p>
            <small className="cost">{preview.influence > 0 ? preview.influence + " Influence  " : ""}{preview.dread > 0 ? preview.dread + " Dread  " : ""}{preview.followers > 0 ? preview.followers + " followers" : ""}</small>
            {preview.risk && <p className="risk">{preview.risk}</p>}
            {!preview.ok && <small className="unavailable">{preview.reason}</small>}
          </section>;
        })}</div>
        <details className="doctrines"><summary>Choose your doctrine <span>{game.specialization ?? "40 Influence"}</span></summary>
          {(Object.entries(SPECIALIZATIONS) as [Specialization, { name: string; description: string }][]).map(([id, d]) => <div className="action" key={id}>
            <button data-testid={"doctrine-" + id} disabled={!!game.specialization || game.influence < 40 || !["playing", "alignment", "awakening"].includes(game.status) || help} onClick={() => apply(chooseSpecialization(game, id))}>{d.name}{game.specialization === id ? " · chosen" : ""}</button><p>{d.description}</p>
          </div>)}<small>One doctrine per run. The choice is permanent.</small>
        </details>
      </aside>
      <section className="map-panel" aria-label="World map">
        <div className="map-heading"><div><p className="eyebrow">{game.status === "placing" ? "The first whisper" : aligned ? "The stars are right" : "The world stirs"}</p><h2>{game.status === "placing" ? "Choose your first congregation" : "Where the dream takes hold"}</h2></div><span className="phase" data-testid="phase">{game.status}</span></div>
        <div className="world-status"><span>World awareness <b>{Math.round(game.awareness)}%</b></span><progress aria-label="World awareness" value={game.awareness} max="100" /><small>{Math.round(cultShare(game) * 100)}% of the world follows you, so awareness climbs {(cultShare(game) * B.awarenessPerCapita).toFixed(2)}/month. Income and recruitment are at {Math.round(awarenessDrag(game) * 100)}% of normal; at 100% awareness the world hunts every cell and the run ends.</small></div>
        {!end && <p className={"ritual-forecast " + (forecast.urgent ? "urgent" : "")} role="status" data-testid="ritual-forecast">{forecast.message}</p>}
        <div className="compact-regions"><label>Choose a region<select aria-label="Choose a region" value={regionId} onChange={e => select(e.target.value as RegionId)}>{REGION_IDS.map(id => <option key={id} value={id}>{REGIONS[id].name} · {format(game.regions[id].population.followers)} followers{game.regions[id].threat ? " · threat approaching" : ""}</option>)}</select></label>{game.status === "placing" && <button disabled={help} onClick={() => select(regionId)}>Establish foothold here</button>}</div>
        <EarthMap game={game} selected={regionId} onSelect={select} />
        <div className="legend"><span>Gold borders · selected region</span><span>Teal · public cult</span><span>Red marker · approaching threat</span></div>
        <p className="map-credit">Made with <a href="https://www.naturalearthdata.com/about/terms-of-use/" target="_blank" rel="noreferrer">Natural Earth</a> · public-domain map data</p>
        <section className={"awakening-panel " + (aligned ? "aligned" : "")}>
          <div className="rite-heading"><h2>{end ? game.status === "won" ? "Cthulhu awakens" : "The alignment has passed" : "Prepare the awakening"}</h2><b data-testid="site-count">{readiness} / 3 sites</b></div>
          <div className="site-list">{REGION_IDS.filter(id => game.regions[id].site.invested).map(id => <button key={id} onClick={() => setRegionId(id)}>{REGIONS[id].name}<small>{siteLabel(game.regions[id])}</small></button>)}
          {!REGION_IDS.some(id => game.regions[id].site.invested) && <p>No sites yet. Gather Dread and maintain 60 followers in each ritual region.</p>}</div>
          {game.status === "awakening" && <p role="status">Final rite: {game.finalProgress}/12 supported months. {readiness < 3 ? "Interrupted — restore support. Time is still passing." : "The way is opening."}</p>}
          {end ? <div className="result"><p role="status">{game.resultReason}</p><p>{format(game.telemetry.sacrificed)} sacrificed · {format(game.telemetry.raidLosses)} lost to opposition · {game.telemetry.threatsCountered} threats countered</p>
            <button className="primary" data-testid="new-run" onClick={() => setGame(null)}>Begin another awakening</button><button onClick={() => download(exportTelemetry(game), "cthulhu-playtest.json")}>Export this run</button></div>
            : <><button className="primary" data-testid="awakening-action" disabled={!!awakeningReason(game) || help} onClick={() => apply(startAwakening(game))}>Begin the final rite · 100 Dread</button><small>{awakeningReason(game) || "Ready. Keep three sites supported for 12 months."}</small></>}
        </section>
        <p className="action-notice" role="status">{notice && notice.logId === (game.log.at(-1)?.id ?? 0) ? notice.message : game.log.at(-1)?.message || "Select a region on the map."}</p>
      </section>
      <aside className="panel detail" aria-label="Region details">
        <p className="eyebrow">Selected region</p><h2>{REGIONS[regionId].name}</h2>
        <p className="region-stance">{r.population.followers > 0 ? r.public ? "Public congregation" : "Hidden cell" : "No cult presence"}</p>
        <dl><div><dt>Followers</dt><dd data-testid="regional-followers">{format(r.population.followers)}</dd></div><div><dt>Income / month</dt><dd>+{regionIncome(r).toFixed(2)} Influence</dd></div><div><dt>Recruitment / month</dt><dd>+{recruitmentPerMonth(game, r).toFixed(2)}</dd></div><div><dt>Exposure</dt><dd>{Math.round(r.exposure)}%</dd></div><div><dt>Madness</dt><dd>{Math.round(r.madness)}%</dd></div></dl>
        {r.backlashUntil > game.elapsedMonths && <p className="risk">Recruitment backlash: {r.backlashUntil - game.elapsedMonths} months remaining.</p>}
        <h3>Belief is not loyalty</h3>
        <div className="population">{(["unaware", "deniers", "believers", "followers"] as const).map(key => <div key={key}><label>{key}<span>{format(r.population[key])}</span></label><progress aria-label={key} max={Math.max(1, population)} value={r.population[key]} /></div>)}</div>
        <small>{format(r.population.losses)} permanently lost · abstract population units</small>
        <section className="regional-event"><h3>Opposition</h3>{r.threat && !end ? <><strong>{THREAT_NAMES[r.threat.kind]}</strong><p>Arrives in {r.threat.dueMonth - game.elapsedMonths} months · severity {r.threat.severity}</p><p className="risk">If ignored, at current strength: {describeThreat(r)}</p><button disabled={!previewAction(game, "counter", regionId).ok || help} onClick={() => apply(performAction(game, "counter", regionId))}>Counter · {previewAction(game, "counter", regionId).influence} Influence</button></> : <p>{end ? "The campaign has ended." : "No approaching threat. Hidden activity can still be discovered."}</p>}</section>
        <section className="regional-event opportunity"><h3>Opportunity</h3>{r.opportunity ? <><strong>{OPPORTUNITY_NAMES[r.opportunity.kind]}</strong><p>{r.opportunity.expiresMonth - game.elapsedMonths} months remaining</p><button onClick={() => setGroup("Opportunities")}>Inspect opportunity actions</button></> : <p>No active opportunity.</p>}
          {r.figure && <p>{r.figure.revealed ? "Public" : "Secret"} {r.figure.kind} · {r.figure.revealed ? "awakening contribution used" : r.figure.kind === "celebrity" ? "+0.6 Influence/month" : "+6 months to newly scheduled threats"}</p>}
        </section>
      </aside>
    </div>
    <Chronicle game={game} onSelect={setRegionId} onCounter={id => apply(performAction(game, "counter", id))} disabled={help} />
    {import.meta.env.DEV && <details className="debug" data-testid="debug-panel"><summary>Playtest controls</summary><div><button data-testid="debug-month" disabled={end || help} onClick={() => setGame(advanceSimulation(game, 1))}>Advance 1 month</button><button data-testid="debug-year" disabled={end || help} onClick={() => setGame(advanceSimulation(game, 12))}>Advance 1 year</button><button data-testid="debug-decade" disabled={end || help} onClick={() => setGame(advanceSimulation(game, 120))}>Advance 10 years</button><button onClick={() => download(exportTelemetry(game), "cthulhu-playtest.json")}>Export report</button></div><small>Time controls advance all costs, threats, and recruitment. They grant no resources.</small></details>}
  </main>;
}
