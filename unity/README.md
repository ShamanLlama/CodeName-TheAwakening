# The Awakening of Cthulhu: Unity build

Unity 6000.3.25f1 port of the Godot 0.4.0 game, in progress. The plan, phases and decisions are in [docs/unity/plan-migracion.md](../docs/unity/plan-migracion.md). Until the port reaches parity, `godot/` stays the shipped build and the reference.

## Layout

| Path | Contents |
|---|---|
| `Assets/Awakening/Core/` | Rules, saves and playtest capture. `Awakening.Core` has no `UnityEngine` reference, so it also builds as a plain .NET library |
| `Assets/Awakening/UI/` | UI Toolkit interface and the Painter2D map |
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

## Measure web loading

```bash
PLAYWRIGHT_CHROMIUM_EXECUTABLE=~/.cache/ms-playwright/chromium_headless_shell-1243/chrome-headless-shell-linux64/chrome-headless-shell \
  node scripts/measure-web-load.mjs unity/Build/web unity --mbps=12,4 --runs=2
```

Emulated phone (844×390, 4× slower CPU), first visit, local server gzipping like itch.io. The same script measures a Godot export with `godot` as the second argument.
