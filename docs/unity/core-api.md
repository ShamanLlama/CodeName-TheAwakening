# Awakening.Core: API contract for the Unity port

The interface (`Awakening.UI`), the platform layer and the game controller are written against this contract while the rules are ported, so the names below are fixed. Everything here lives under `unity/Assets/Awakening/Core/` in the `Awakening.Core` assembly, which has **no `UnityEngine` reference** and compiles as netstandard2.1 / C# 9 (`core-tests/Awakening.Core.csproj`).

The source of truth for behaviour is the Godot build in `godot/` (0.4.0, ruleset `alignment-3`). Port it **1:1**: same order of operations, same random draws, same arithmetic in `double`, same text. GDScript `snake_case` becomes C# `PascalCase`; the mapping is mechanical so a reader can put the two side by side.

## General rules

- Region iteration always follows `GameData.RegionIds`. Floating-point sums depend on that order.
- All numbers that GDScript keeps as `float` are `double`; GDScript `int` values are `int` (`uint` only for `RngState`). No `float` (single) anywhere in Core.
- All formatting and parsing uses `CultureInfo.InvariantCulture`. `Js.Num`, `Js.Fixed` and `Js.RoundHalfUp` reproduce `godot/core/js.gd` (JavaScript `Number.toString`, `toFixed`, `Math.round`). `Js.Num` must never produce exponent notation for values the game produces, and must give the same text on .NET 8, Mono and IL2CPP: do not rely on `"R"`.
- Commands never mutate their input. They deep-copy (`GameState.Clone()`) and return the copy, or return the untouched input when refused.
- IDs (regions, actions, statuses, difficulties, doctrines, threat and opportunity kinds) stay `string`, exactly as in the JSON save and the playtest wire format.
- JSON goes through `Awakening.Core.Json.JsonReader` / `JsonWriter` (objects are `Dictionary<string, object>`, arrays `List<object>`, numbers `double`). Newtonsoft is not available to game assemblies.

## Rules: `Awakening.Core.Rules`

| Godot | C# |
| --- | --- |
| `balance.gd` constants | `static class Balance` (`Ruleset = "alignment-3"`, `PreparationMonths`, `SiteDreadCosts` as `int[]`, …, `Deadline`, `LatestSiteStart`, `LatestRiteStart`) |
| `game_data.gd` | `static class GameData`: `RegionIds`, `ActionIds`, `Statuses`, `DifficultyIds`, `SpecializationIds`, `ThreatKinds`, `OpportunityKinds` (`string[]`); `Regions` (`RegionDefinition`: `Id`, `Name`, `PopulationWeight`, `Skepticism`, `InstitutionalResistance`, `Mysticism`, `Adjacent`, `Tags`); `Actions` (`ActionDefinition`: `Id`, `Name`, `Description`, `Influence`, `Dread`, `Followers`, `Cooldown`); `Specializations` (`Name`, `Description`); `SpecializationCost`; `Difficulties` (`Name`, `Recruitment`, `Pressure`, `Warning`); `ThreatNames`; `OpportunityNames`; `PopulationWeightTotal`; `WorldPopulation`. Lookups are `IReadOnlyDictionary<string, …>` |
| `rng.gd` | `Rng` (already ported: `SeedToState`, `Next`, `NextInt`) |
| `js.gd` | `static class Js`: `RoundHalfUp(double)`, `Num(double)`, `Fixed(double, int)` |
| state dictionaries | `GameState`, `RegionState`, `Population`, `Site`, `Threat`, `Opportunity`, `Figure`, `LogEntry`, `Telemetry` (below) |
| `game_engine.gd` | `static class GameEngine`: `IsActive`, `IsOver`, `CreateGameState(difficulty, seed)`, `PlaceStartingFoothold(state, regionId)`, `CounterCost(state, region)`, `SilenceCost`, `AwarenessCost(actionId, region)`, `ConcealCost(state)`, `SiteDreadCost(state)`, `PreviewAction(state, actionId, regionId)`, `PerformAction(state, actionId, regionId)`, `ChooseSpecialization(state, choice)`, `SetGameSpeed(state, speed)`, `AwakeningReason(state)`, `StartAwakening(state)`, `AdvanceSimulation(state, months)` |
| `derived.gd` | `static class Derived`: `ClampPct`, `TotalFollowers`, `LivePopulation`, `Recruitable`, `Capacity`, `SiteRequirement`, `Supported`, `CultShare`, `AwarenessDrag`, `ReadySites`, `Calendar`, `RegionIncome`, `InfluencePerMonth`, `RecruitmentPerMonth`, `SiteUpkeep`, `NetInfluencePerMonth`, `SiteLabel`, `AddLog`, `Recruit`, `LoseFollowers` |
| `director.gd` | `static class Director`: `Random`, `ScheduleThreat`, `ResolveWorld`, `SiteHunt`, `Tick` |
| `threats.gd` | `static class Threats`: `Consequences(region)` → `ThreatConsequences` (`Followers`, `Remaining`, `DestroysCell`, `PreparationLost`, `IncomeLost`, `ExposureAdded`, `InterruptsSite`), `Describe(region)` |
| `forecast.gd` | `static class Forecast`: `Ritual(state)` → `RitualForecast` (`Urgent`, `Message`) |

Results:

```csharp
public sealed class CommandResult { public bool Ok; public string Message; public GameState State; }
public sealed class ActionPreview { public bool Ok; public string Reason; public int Influence; public int Dread; public int Followers; public double Awareness; public string Effect; public string Risk; }
```

State (mutable classes with a deep `Clone()`; field names are the JSON keys in PascalCase):

```csharp
public sealed class GameState {
    public int Version = 2; public string Ruleset; public string Status; public string Difficulty; public string Seed;
    public uint RngState; public int ElapsedMonths; public int Speed;
    public double Influence, Dread, Awareness; public string Specialization; public int FinalProgress;
    public RegionMap Regions; public List<LogEntry> Log; public int NextLogId; public string ResultReason; public Telemetry Telemetry;
    public GameState Clone();
}
// RegionMap: indexer by region id, enumerates in GameData.RegionIds order.
public sealed class RegionState {
    public Population Population; public bool Public, Revealed, Scattered; public int ScatteredUntil;
    public double Exposure, Madness; public int BacklashUntil; public Dictionary<string, int> Cooldowns;
    public Site Site; public Threat Threat; public Opportunity Opportunity; public Figure Figure;
}
public sealed class Population { public double Unaware, Deniers, Believers, Followers, Losses; }
public sealed class Site { public bool Invested; public int Progress; public bool Dormant; }
public sealed class Threat { public string Kind; public int DueMonth; public int Severity; }
public sealed class Opportunity { public string Kind; public int ExpiresMonth; }
public sealed class Figure { public string Kind; public bool Revealed; }
public sealed class LogEntry { public int Id; public int Month; public string Message; }
public sealed class Telemetry {
    public Dictionary<string, int> Actions; public double Sacrificed, RaidLosses, InfluenceEarned, InfluenceSpent, DreadEarned, DreadSpent, UpkeepPaid;
    public int ThreatsCountered; public Dictionary<string, int> Revelations;
}
```

If the Godot code shows a field is fractional where this table says `int` (or the reverse), follow Godot and update this file.

`GameStateJson.ToJson(GameState) → Dictionary<string, object>` and `GameStateJson.FromJson(object) → GameState` (null when invalid) use exactly the Godot save keys (`elapsedMonths`, `rngState`, `scatteredUntil`, `backlashUntil`, `nextLogId`, `resultReason`, …).

## Saves: `Awakening.Core.Save`

`static class SaveData`, from `godot/core/save_data.gd`: `SaveVersion`, `RetiredRulesets`, `RetiredNotice`, `DefaultSave()`, `IsGameState(object)`, `DecodeSave(string)` → `SaveEnvelope` or null, `EncodeSave(SaveEnvelope)` → string, `TakeRetiredNotice(SaveEnvelope)`, `WithDefaultSettings`. `SaveEnvelope` holds `Version`, `State` (`GameState` or null), `Settings` (`Muted`, `Volume`, `UiScale`, `ReducedMotion`, `Hints`), `TutorialComplete`, `LastResult` and `Playtest` (the raw JSON object owned by `PlaytestCapture`, kept verbatim). A save written by the Godot build must decode, and an encoded save must decode back to an equal state. File and IndexedDB access is **not** in Core; the platform layer passes strings in and out.

## Playtest: `Awakening.Core.Playtest`

`PlaytestCapture`, from `godot/core/playtest_capture.gd`, with the same public methods in PascalCase (`Configure`, `Configured`, `Accept`, `Restore`, `Answered`, `Decline`, `Withdraw`, `Snapshot`, `Active`, `Capturing`, `StartRun`, `Resume`, `Tick`, `EmitEvent`, `Checkpoint`, `StopCapture`, `SealBatch`, `QueueComment`, `NextJob`, `FindJob`, `Attempted`, `Postpone`, `Reject`, `SplitJob`, `ConfirmJob`, `UndoConfirmation`, `Expire`, `PendingEvents`) and constants (`Adapter = "awakening-2"`, `StoredAdapters`, limits). It reads the game through `CaptureState` (`Seed`, `Difficulty`, `Ruleset`, `Status`, `ElapsedMonths`, `NextLogId`, `IsOver`), so it does not depend on the rules. Wall-clock time and random identifiers come from injectable `Func<double>` (Unix seconds) and `Func<byte[]>` (16 random bytes) so tests are deterministic. Its stored record is plain JSON objects, byte-compatible with what Godot keeps in the save's `playtest` field.

## Development tools: `Awakening.Core.Dev`

`Playthrough` (scripted policy, `godot/dev/playthrough.gd`), `Measure` (`godot/dev/measure.gd`) and `GoldenTraces` (record/replay of `godot/tests/golden/*.json.gz`). A console project under `core-tests/` runs `Measure` and regenerates goldens without Unity.
