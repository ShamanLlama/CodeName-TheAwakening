# The Awakening of Cthulhu: Unity build

The game since 0.5.0: a Unity 6000.3.25f1 (6.3 LTS) port of the Godot 0.4.0 build with the same rules (`alignment-3`), playtest adapter (`awakening-2`) and save. Plan in [docs/unity/plan-migracion.md](../docs/unity/plan-migracion.md); what was verified and how to repeat it in [docs/unity/verificacion.md](../docs/unity/verificacion.md). `godot/` stays as the reference until it is retired; the `godot` branch keeps that version.

## Layout

| Path | Contents |
|---|---|
| `Assets/Awakening/Core/` | Rules, saves and playtest capture. `Awakening.Core` has no `UnityEngine` reference, so it also builds as a plain .NET library |
| `Assets/Awakening/Core/Session/` | `GameSession`: commands, save envelope, consent, playtest emission and the clock partition (port of main.gd's logic) |
| `Assets/Awakening/Platform/` | IndexedDB bridge (`Plugins/WebGL`, shared with the Godot build's save), native save with a lock, playtest transport |
| `Assets/Awakening/UI/` | `GameController` (Unity side of main.gd), screens, dialogs, the Painter2D map, theme and sound |
| `Assets/Awakening/Editor/` | Batchmode builds |
| `Assets/Awakening/Resources/` | Map geometry and fonts loaded at runtime |
| `Assets/Awakening/Licenses/` | Natural Earth provenance and the SIL Open Font Licenses (not shipped in builds) |
| `Assets/Awakening/Core/Json/` | Reflection-free JSON. Newtonsoft is kept out of the game assemblies (`overrideReferences`) because it added 2.5 MB to the web build |
| `../core-tests/` | `dotnet` project that compiles `Core/` as netstandard2.1 / C# 9 and tests it |

## Test the rules without Unity

```bash
dotnet test core-tests/Tests
```

`core-tests/Awakening.Core.csproj` uses the API surface and language version Unity 6 compiles with, so code that passes there also compiles in the editor.

## Build

Unity needs an active licence: sign in to Unity Hub first. Batchmode fails with "No valid Unity Editor license found" otherwise.

```bash
UNITY=~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity
$UNITY -batchmode -quit -projectPath unity -executeMethod Awakening.Editor.Builds.WebGL -out Build/web -compression none -logFile -
$UNITY -batchmode -quit -projectPath unity -executeMethod Awakening.Editor.Builds.Linux -out Build/linux -logFile -
```

Batchmode fails while an editor has the project open ("another Unity instance is running"); close it, or build through the open editor with the `unity` CLI / MCP (`com.unity.pipeline`).

`-compression` takes `none`, `gzip` or `brotli`; compressed builds include Unity's decompression fallback because itch.io does not always send `Content-Encoding`. `-version` sets `PlayerSettings.bundleVersion`. Only the Mono Linux player is installed on this machine.

## Layout regression check

Run the windowed development-player walk against the same 60 combinations as Godot 0.4.0:

```bash
DISPLAY=:0 unity/layout-check.sh /tmp/awakening-layout
# Reuse unity/Build/linux-dev after a successful development build:
SKIP_BUILD=1 unity/layout-check.sh /tmp/awakening-layout-repeat
```

The script builds this checkout once, then runs early, mid, late, alignment and awakening, normal and crowded, in six profiles: 1600×900 at 100%; 1280×720 at 130%; 1600×900 at 160% touch; and 844×390 at 100%, 160% and 200% touch. `UNITY` overrides the editor executable; `LAYOUT_TIMEOUT` overrides the 120-second limit per player. `LAYOUT_JOBS=4` runs at most four independent player windows concurrently (default: one); each keeps its own log and result. Each case must exit zero and print `LAYOUT_CHECK_PASSED`; missing markers, timeouts and exceptions fail the matrix. Per-case logs and the build log go in the selected directory.

For one case, without building or writing a screenshot:

```bash
unity/Build/linux-dev/the-awakening.x86_64 -screen-width 844 -screen-height 390 -screen-fullscreen 0 \
  -logFile /tmp/layout.log --layout-check 1 --scenario late --crowd 1 --touch 1 --scale 2
```

`Screens/LayoutCheck.cs` uses the screenshot scenario staging, then walks every region, tab, visible action explanation and applicable dialog. It checks ledger items, case files, modal boxes, and all four chart overlays with UI Toolkit `worldBound` rectangles. Crowded mode adds threats to every held region, clears seen notes, zooms the chart and expands the timetable. These checks run only in the editor or development players, use an in-memory session, and never access the real save or send playtest data. V-sync is disabled during development harness runs so unfocused windows do not stall the walk.

The walk, one-unit canvas tolerance, overlay intersections and ledger boundary match `godot/dev/layout_check.gd`, including its deliberate omission of the top-edge overflow test. Toolkit has no `get_combined_minimum_size()`: the case-file width assertion instead checks the horizontal extent of its visible laid-out descendants, including content a scroll viewport would clip. Unresolved geometry also fails explicitly. Scrollable content may extend vertically inside its viewport, as in Godot.

For screenshot evidence, `shots.sh` also accepts `--crowd 1` and `--choose action` (with `--select region`). The latter opens the action's tab and explanation without performing it. UI integration checks remain available through `--shot /tmp/ui-check.png --check-ui 1`; a successful run prints `UI_CHECKS_PASSED`.

## Screenshots and checks (development players)

```bash
unity/shots.sh /tmp/shots "mid --select africa" "late --dialog menu" "start --touch 1 --scale 1.6"   # WIDTH/HEIGHT set the window
unity/shots.sh /tmp/checks "start --check-ui 1"   # interaction checks: UI_CHECKS_PASSED
unity/layout-check.sh /tmp/layout                 # the 60 layout combinations of 0.4.0
```

Scenarios match the Godot harness: start, consent, continue, retired, placing, guide, early, mid, late, alignment, awakening, won, lost.

## Measure web loading

```bash
PLAYWRIGHT_CHROMIUM_EXECUTABLE=~/.cache/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-linux64/chrome-headless-shell \
  node scripts/measure-web-load.mjs unity/Build/web unity --mbps=12,4 --runs=2
```

Emulated phone (844×390, 4× slower CPU), first visit, local server gzipping like itch.io. The same script measures a Godot export with `godot` as the second argument.
