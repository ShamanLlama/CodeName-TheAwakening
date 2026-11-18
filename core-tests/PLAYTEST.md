# Unity playtest core

`PlaytestCapture` ports `godot/core/playtest_capture.gd` without a rules or Unity dependency.
`CaptureState` supplies only the seven fields capture reads. The saved `playtest` value stays a
plain JSON tree; existing serialized job bodies are never rewritten on load or retry. New jobs
use the Godot adapter's sorted keys and default number precision through an optional overload
of the shared `JsonWriter`; its existing default API is unchanged.

`PlaytestOutbox` ports the state machine from `playtest_client.gd`. The controller calls
`Process()` each frame and supplies these platform implementations:

- `IPlaytestStorage.Persist()` saves the **entire game envelope**, including
  `capture.Snapshot(currentCaptureState)`, and returns its generation (negative on failure).
  `SavedGeneration` advances only after durable completion. `Poll()` updates storage state.
- `IPlaytestTransport` sends the exact body string, polls responses, and cancels outstanding
  work. The platform enforces the 10-second timeout, 16-KiB response cap, JSON content type,
  no redirects, and omitted browser credentials. A send failure must return status `0`.
- Inject the same Unix clock into capture/outbox, and the monotonic millisecond clock into
  outbox. Wall time controls TTL/backoff; monotonic time controls 20-second flushes and
  5-second send spacing. Clock partitioning into simulation/planning/background/excluded
  remains the controller's responsibility; feed observed millisecond deltas to `Tick`.
- `Ready`, `Writable`, and `Failed` gate all sends. The extra explicit `Ready` guard is
  redundant with Godot's platform (which cannot be writable until ready).
- On Stop, call `outbox.Withdraw()` to clear capture records, cancel transport and persist
  the remembered no. `Cancel()` alone mirrors Godot and does not change participation.
  On opening, call `Restore()` only when the save is writable, as Godot's controller does.

The capture defaults to HTTPS-only endpoint validation. Pass `debugBuild: true` only for debug
builds to allow the same `http://127.0.0.1:` exception as Godot. UUIDs use 16 cryptographic random
bytes in network order; injectable bytes and backoff jitter allow deterministic tests.

## Reproduce

From this worktree root:

```sh
~/.dotnet/dotnet test core-tests/Tests
core-tests/check-playtest-contract.sh /mnt/1tb-p/Proyects/GTC-page/gtc-web /tmp/awakening-playtest-batches
~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath "$PWD/unity" -logFile /tmp/codex-playtest-unity.log
```

The contract script exports four exact sealed bodies: start, decisions including scatter and
silence, resumed session and terminal result, and a comment reusing the terminal event. It
runs the authoritative GTC validator separately on every body (2/21/4/1 events), then requires
exit 1 for an `awakening-1` copy of the decision batch. There are 27 unique events across two
sessions; the comment duplicates the final event. No Django database or receiver is used.

`Tests/Fixtures/godot-playtest.json` was generated with **Godot 4.7.2** from an unmodified copy
of `godot/core/playtest_capture.gd` in a temporary project outside `godot/`. Its only engine
stub was `GameEngine.is_over(state) = state.status in ["won", "lost"]`. The generator accepted
participation, started `normal/godot-stored-fixture` (placing, month 0, nextLogId 1), ticked
1,200 ms planning, emitted `before_save`, sealed/attempted/postponed the job, ticked 800 ms
background, and emitted `visibility_changed`. It set queue timestamps to 1,800,000,000 and
next_retry to 1,800,000,600 for deterministic expiry. UUIDs and the serialized body were
produced by Godot. Tests load the whole tree unchanged, preserve its exact body and signature,
and resume its run with a new session at zero clock. An `awakening-1` record is also accepted
without rewriting its adapter. Numeric token fixtures were independently queried from
Godot's `JSON.stringify`, including its 32-decimal cap and negative rounded zero.

## Coverage and deferred integration

Thirteen of the seventeen Godot test cases are covered at the capture/outbox boundary,
including the expiry-on-opening client case. The corrupt optional metadata test covers
capture's raw preservation and refusal to capture; decoding the surrounding save belongs to
the separate Save port. Additional cases cover persisted generations, receipt rollback,
status handling, flush/spacing, Unicode limits and malformed stores.

The following original tests need the controller/platform and are intentionally deferred:

- `test_game_commands_emit_actual_costs_and_preserve_golden_outcomes`: calls `main.gd`
  commands and checks real engine costs/outcomes against golden traces.
- `test_second_native_window_cannot_write_the_same_save`: real files and native writer lease.
- `test_open_game_menu_counts_planning_not_simulation_or_excluded`: UI, holds, focus and
  the controller's clock-mode selection.
- `test_start_screen_asks_only_when_a_writable_round_has_no_answer`: start-screen prompt
  policy and writable-save/controller integration.

The synthetic contract run supplies state/spending explicitly; it proves the capture wire
contract, not the future controller's placement, costs, or timing integration. HTTP, real
storage durability, browser ownership and delivery remain platform integration checks.

No intended changes to the valid wire contract, consent, capture policy or retry semantics.
The shared strict JSON writer escapes control characters as valid JSON rather than emitting
Godot's nonstandard `\v`; malformed/non-finite JSON is rejected by the existing strict reader.
