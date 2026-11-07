# The Awakening of Cthulhu: Godot build

Godot 4.7 (GDScript, Compatibility renderer) port of the web prototype, built for itch.io's browser player. The same project also exports Linux and Windows desktop builds.

The rules in `core/` mirror `src/game/` line for line. Recorded TypeScript runs check that they match (see [Rule parity](#rule-parity)). The interface is new: a cult's archive on a lamp-lit desk around 1908–1926, with the world as an old survey chart and every panel a document (case file, press clipping, telegram, ledger). Everything is drawn in code; the only assets are three fonts.

## Run

```bash
godot --path godot            # play
godot --editor --path godot   # open the editor
```

Keys: Space pauses, 1 and 2 set the speed, Tab picks the next region, ↑ ↓ choose an action in the open file, Enter performs it, ← → change its tab, C opens the chronicle, D the doctrines, H the guide, R shows the whole map, and Esc closes the region panel or opens the menu. The wheel or a pinch zooms the map and a drag pans it. The menu sets the interface size (100–200 %) and reduced motion. Debug builds add playtest controls to the menu: advance 1, 12 or 120 months.

## Test

```bash
npm run godot:test            # import, then every tests/test_*.gd
godot --headless --path godot -s res://tests/run_tests.gd -- save   # filter by file or test name
godot --headless --path godot -s res://dev/check_scripts.gd         # parse-check every script
```

Run the import step after adding or renaming a `class_name` script. Otherwise Godot's class cache is stale and scripts fail to resolve.

## Rule parity

`src/tests/golden-traces.test.ts` records nine seeded runs from the TypeScript engine into `tests/golden/*.json.gz`:

- four runs of the scripted playtest strategy;
- two random-command runs;
- three aggressive random runs that over-extend.

Between them they cover wins, deadline losses, awareness losses and a non-ASCII seed. Every command is recorded with its result and preview, and a snapshot is taken after every step.

`tests/test_golden.gd` replays every command and requires the same:

- state, previews and command results;
- derived values;
- log text.

It also checks that every snapshot survives a save and reload.

- If you change the TypeScript rules, re-record with `npm run golden`, port the change to `core/`, then run `npm run godot:test`. `npm test` fails while the recordings are stale.
- If you change a Godot rule on its own, the parity test is meant to fail. Decide whether the TypeScript version is still the reference.

The port keeps JavaScript's number behaviour where it shows:

- `Js.fixed` rounds like `toFixed` (printf rounds 0.125 to 0.12);
- `Js.round_half_up` matches `Math.round`;
- `Rng` masks to 32 bits;
- `Js.num` prints the shortest round-trip decimal.

## Differences from the web prototype

- Saves live in `user://save-v2.json` (IndexedDB on the web build). Browser LocalStorage saves can't carry over, so the legacy-save import is gone.
- A run lost to world awareness before the deadline now reloads. The web validator rejected it and discarded the whole save.
- Sound is on by default (procedural effects in `ui/sfx.gd`), and can be muted from the menu.
- Starting the final rite resumes the clock at normal speed.
- Explanations moved out of the layout: hover an action for its effect, risk and why it's unavailable. Hover a readout for what it means.

## Screenshots

Debug builds have a screenshot harness that builds a realistic state with the scripted strategy. It opens a window briefly:

```bash
godot --path godot --resolution 1600x900 -- --shot=/tmp/mid.png --scenario=mid --select=africa
```

Scenarios: `start`, `placing`, `guide`, `early`, `mid`, `late`, `alignment`, `awakening`, `won`, `lost`. Optional flags: `--select=<region_id>`, `--choose=<action_id>`, `--dialog=menu|doctrine|chronicle|guide`, `--scale=1.6` (interface size), `--touch=1`, `--motion=reduced`, `--hints=1` (show first-time notes).

Layout check (needs a window): stages a scenario, walks every region, tab, action explanation and dialog, and exits 1 if anything leaves the canvas, hides under the region file, or two overlays (timetable, threat clippings, note, whole-world button) cover each other. `--crowd=1` adds a threat to every held region, an open note and a zoomed chart. Pass `--disable-vsync`, or an unfocused window throttles it.

```bash
godot --path godot --resolution 1688x780 --disable-vsync --script res://dev/layout_check.gd -- --shot=/tmp/unused.png --wait=300 --scenario=late --touch=1 --scale=2
```

## Export

The Web preset uses a smaller custom template built by `scripts/build-web-template.sh` (Godot 4.7.2 source, Emscripten 4.0.11, SCons; it drops 3D, physics, navigation, XR, advanced GUI, SVG and unused importers) and a custom loading screen in `web/shell.html`. Build the template once per machine before exporting; Linux and Windows still use the official templates.

The export templates for 4.7.2 must be installed (Editor → Manage Export Templates, or into `~/.local/share/godot/export_templates/4.7.2.stable/`).

```bash
npm run godot:web                                                      # godot/build/web/
godot --headless --path godot --export-release Linux build/linux/the-awakening.x86_64
godot --headless --path godot --export-release Windows build/windows/TheAwakening.exe
```

The web preset uses the no-threads template, so itch.io needs no SharedArrayBuffer setting.

### itch.io upload

1. Zip the contents of `godot/build/web/` with `index.html` at the zip root:
   `cd godot/build/web && zip -9 ../the-awakening-web.zip *`
2. On the itch.io project page, set the kind of project to **HTML** and upload the zip. Mark it **This file will be played in the browser**.
3. Under embed options, set the viewport to 1600 × 900 (or 1280 × 720; the interface scales), and enable the fullscreen button.

The Windows build disables `application/modify_resources` because it needs rcedit. Configure rcedit in Editor Settings to embed an icon and version info.

## Layout

| Path | Contents |
|---|---|
| `core/` | Rules: balance, data, RNG, engine, director, threats, forecast, saves |
| `map/` | Natural Earth projection, geometry loading, the map control |
| `ui/` | Theme and palette, screens, region panel, dialogs, gauges, sound |
| `dev/` | Scripted playtest strategy, script checker |
| `tests/` | Headless runner, rule, save and parity tests, golden traces |
| `data/earth_regions.json` | Country rings by region, from `node scripts/build-earth-map.mjs` |
| `fonts/` | IM Fell English, Old Standard TT and Courier Prime (SIL Open Font License, `OFL-*.txt`) |
