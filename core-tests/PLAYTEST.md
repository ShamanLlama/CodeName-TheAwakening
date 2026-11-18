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
  belongs to `GameSession`; the controller supplies monotonic time and visibility observations.
- `Ready`, `Writable`, and `Failed` gate all sends. The extra explicit `Ready` guard is
  redundant with Godot's platform (which cannot be writable until ready).
- On Stop, call `session.SetParticipation(false)` to account time, clear capture records,
  cancel transport and persist the remembered no. `outbox.Cancel()` alone does not change
  participation. Outbox-only callers can still use `outbox.Withdraw()`.
  On opening, call `Restore()` only when the save is writable, as Godot's controller does.

The capture defaults to HTTPS-only endpoint validation. Pass `debugBuild: true` only for debug
builds to allow the same `http://127.0.0.1:` exception as Godot. UUIDs use 16 cryptographic random
bytes in network order; injectable bytes and backoff jitter allow deterministic tests.

## Reproduce

From this worktree root:

```sh
~/.dotnet/dotnet test core-tests/Tests
core-tests/check-playtest-contract.sh /mnt/1tb-p/Proyects/GTC-page/gtc-web /tmp/awakening-playtest-batches
~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity -batchmode -nographics \
  -projectPath "$PWD/unity" -runTests -testPlatform EditMode \
  -testResults /tmp/editmode.xml -logFile /tmp/editmode.log
```

The contract script exports the exact sealed bodies from all nine command traces plus four
synthetic bodies: start, decisions including scatter and
silence, resumed session and terminal result, and a comment reusing the terminal event. It
runs the authoritative GTC validator separately on every body (the synthetic bodies have
2/21/4/1 events), then requires exit 1 for an `awakening-1` copy of the decision batch.
The synthetic stream has 27 unique events across two sessions; its comment duplicates the final event. Each golden run also checks per-command
spending-counter differences, refused commands, every engine snapshot (including logs, previews
and derived values), transitions and the final outcome. Its terminal comment is validated too.
The current corpus produces 29 session bodies plus four synthetic bodies, all `awakening-2`.
No Django database or receiver is used.

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

## Session integration and coverage

`Awakening.Core.Session.GameSession` ports the controller's engine-free run logic. Construct it
once platform loading has completed, with the decoded envelope, collection options,
`Storage.Now` and `envelope => Storage.Persist(SaveData.EncodeSave(envelope))`. Supply blocked
when the platform is not ready/writable or the save cannot safely be replaced. Configure/restore
is performed by the session; do not configure its capture again after construction. Inject the
capture itself for deterministic wall time/UUIDs or to select the debug endpoint policy.

The controller delegates commands/settings, `ShowStart`/`ShowGame`, `Hold`/`Release`, tutorial and
participation methods. Each Update polls storage and the outbox, then calls
`session.Process(Time.unscaledDeltaTime, Storage.Failed)`. Forward native focus through
`session.OnApplicationFocus(focused)`; pass `web: true` for Unity web notifications (which only
auto-pause, because browser observations own the capture clock). Supply a visibility callback
that drains `Storage.VisibilityChanges()` in timestamp order and maps to
`SessionVisibilityChange(at, focused, visible)`. The injected clock and observations must share
the same monotonic origin. Screens read `session.Screen` (`Start` / `Game`) and session state.

Subscribe `Changed` for redraw and `Events` for chronicle entries/refusals (the session's
`GameEvent` type lives in `Awakening.Core.Session`). Audio and Unity screen construction remain
in the controller. Point `PlaytestStorageAdapter` at `session.Commit`, and pass the outbox's
`Cancel` method as `cancelPendingRequests` (a closure is useful while wiring construction).
`Envelope()` supplies the complete capture-bearing save for exports; serialize it immediately.
Recovery copies must still run in the controller before `Begin`. Dispose storage/outbox there.
The session preserves unknown loaded envelope/settings fields through the existing save codec.

All seventeen original Godot playtest cases now have ports: the prior thirteen capture/outbox
cases, three at the session boundary, and the native writer lease in Unity EditMode. Session
coverage adds the four clock partitions, queued visibility observations across suspended
frames, the strict >30 s foreground gap, five-second saving, twenty-second checkpoints,
nested dialog holds, terminal closure, resume, storage failures, frame simulation and settings.
The native test uses a unique temporary directory, real atomic save files and the actual
`GameStorage` lease; it also checks a non-owner cannot release it and a new writer can recover
after the owner closes. It models two windows in one process, as the original Godot test did;
it does not prove simultaneous cross-process acquisition is race-free.

Two controller differences from Godot were verified with a separate temporary Godot 4.7.2
script (no changes to `godot/`) and corrected in the session:

- `SetSpeed` uses reference equality on an engine-produced clone in the current controller.
  Selecting the existing speed therefore resets fractional simulation, emits `menu_changed`
  and persists again. Godot compares dictionaries by value and does none of those things.
- `Begin` currently uses .NET `Trim()`, which removes NBSP and other Unicode whitespace but
  retains some ASCII controls. Godot `strip_edges()` removes characters <= U+0020 and keeps
  Unicode whitespace, so these seeds otherwise identify different runs. The session uses
  the existing Godot-compatible edge trimming helper.

The session uses double precision for fractional simulation, following Godot and Core's API;
the current controller uses a float accumulator. UI delegation remains for the controller
integration change; this work does not edit `UI/`. HTTP delivery, browser ownership and real
browser storage durability remain platform integration checks.

No intended changes to the valid wire contract, consent, capture policy or retry semantics.
The shared strict JSON writer escapes control characters as valid JSON rather than emitting
Godot's nonstandard `\v`; malformed/non-finite JSON is rejected by the existing strict reader.
