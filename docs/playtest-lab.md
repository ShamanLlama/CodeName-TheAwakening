# The Awakening — GTC Playtest Lab adapter

Phase E4 is implemented and verified locally (2026-10-31). Collection is **disabled** in the checked-in build. This is the Godot adapter for this game, not a general SDK. Rules, balance, deterministic outcomes and the existing summary export remain compatible.

The [GTC wire contract](../../../GTC-page/gtc-web/contracts/playtests/README.md) is authoritative. The neighboring checkout is required for that link and the receiver integration tests.

## Rulesets

The adapter reports the save's ruleset in `run_started`/`capture_started` (`Balance.RULESET`). Since 0.3.0 (2026-11-02) that is `alignment-2`; the GTC schema accepts `alignment-1` and `alignment-2`. A save holding an unfinished `alignment-1` run loads without that run: settings, tutorial and playtest records stay, and the start screen explains why. Every build is listed with its ruleset and date in [the version log](playtests/versiones.md); a ruleset change needs a new GTC build and round.

## Player flow

The title screen offers an unchecked participation box when a collection is configured. Its notice identifies difficulty, choices, actual resource spending, world seed, progress, session times, a random round ID and six-month detailed retention. Players should keep personal details out of the seed. Playing without checking it creates no tester/run/event IDs and sends no telemetry.

Participation is renewed on each opening. Pending records stay paused until the player joins again; they are not silently uploaded merely by opening a save. Continue preserves an observed run and creates a new session. An old save, a changed build/round or progress made without observation starts **partial** coverage. Unknown initial choices and previous time are not reconstructed. Tutorial completion and the first observed ready site are checkpoints, not inferences from log text.

The in-game menu → **GTC Playtest Lab** shows pending/receipt/error status, exports the complete local save and playtest record, and stops participation. Stopping cancels requests and clears local unsent records and identity. A request already committed by the server can still exist there; the operator manages received records separately. There is no remote deletion claim. The end dialog accepts up to ten optional comments per captured run, each up to 1,000 characters. A comment reuses the exact terminal event in another batch, without adding an event after the ending. Exports remain under the downloader's control.

## Build configuration

`godot/data/playtest-config.json` contains `enabled`, `endpoint`, `collection_id`, `build_id` and `round_id`. Defaults are false and empty strings. IDs come from the actual opened GTC round/build; they are public collection identifiers, not reading credentials. Use the exact HTTPS `/api/playtests/v1/batches/` endpoint. Debug native builds additionally accept loopback HTTP for isolated tests. No production identifiers are checked in.

E6 operation and restoration passed locally. Before enabling a real build, finish E7: configure the exact iframe origin in the GTC round and global origin allowlist, then exercise itch.io and a physical phone. An exported file or local HTTPS test does not establish the itch.io origin. Keep GTC collection paused until the operation/retention checks pass. No deployment is part of E4.

## Capture and clock

- Accepted place/action/doctrine/rite commands emit structured events. Refused actions produce no decision event. Costs are differences in engine spending counters, including sacrificed followers, rather than advertised button prices.
- Phase transitions, known won/lost endings and explicit abandon/replacement are recorded once. A closed tab or network silence is not an abandonment.
- Monotonic time is partitioned into simulation, planning (paused simulation in the game, including open menus/guide), background (lost focus or hidden document), and excluded (outside an active game or after its ending). Pausing alone does not establish what the player is thinking.
- Browser focus/visibility callbacks retain monotonic timestamps while rendering pauses; polling also reconciles current document state. Native focus loss pauses simulation. Clock snapshots persist every five seconds and checkpoints are emitted every twenty seconds. Closed intervals are never charged. Resume records an unknown-time gap because a crash can lose a tail after the last checkpoint. A foreground stall above thirty seconds is excluded and marked unknown; debug time jumps also mark unknown coverage.

## Persistence and transport

`PlaytestCapture` owns consent, IDs, sequence, clocks and outbox. `PlaytestPlatform` owns the save and single-writer lease. `PlaytestClient` owns transport and server receipts. Optional unreadable playtest metadata is retained for export and stops capture; it never invalidates an otherwise valid v2 game save.

Native saves use a temporary file, flush and atomic rename. A lock directory next to the save prevents another process from writing/sending. Linux detects a dead PID through `/proc`; another OS fails closed on a stale lock. On those systems, close all game processes before manually recovering/removing only the matching `.lock` directory. Native Windows/macOS recovery has not been validated here.

Web saves use one completed IndexedDB transaction for the **whole v2 envelope**, including pending batches (`lucid-awakening-save-v1`, store `snapshots`, key = save path). The first load falls back to the original Godot filesystem save, without deleting it. Web Locks allow one writer per game/save; a second window plays in memory and cannot overwrite or send the owner's sequence. If locking/storage is unavailable, gameplay remains available in memory and capture is disabled.

The new IndexedDB envelope is authoritative after migration. A previous binary only knows its older filesystem copy. For rollback, first export the current full save, retain both stores and keep the reader/adapter or perform an explicit v2 restore; do not claim that simply serving the old binary preserves newer progress. Never clear browser storage as a rollback step.

Batches normally flush at twenty seconds or 32 events; start/end/comment events also request a flush. A serialized batch body and attempted state must be durably saved **before** any POST. Only HTTP 200/201 plus a validated receipt permits removing it; removal and receipt must also persist before the UI reports confirmation. Network errors, 429 and 5xx retain the body/IDs, use exponential backoff with jitter and honor `Retry-After`. Normal sends are spaced five seconds apart. Browser fetch omits credentials and rejects redirects; requests time out after ten seconds.

HTTP 413 splits a multi-event batch with new batch IDs and unchanged event IDs/queue age. Permanent errors stop that run and retain its local copy for export, rather than retry forever. Queue limits are 5,000 records or 8 MiB, with seven-day TTL from capture, not from retry. Overflow records a gap and stops capture; expiration is visible. Seven-day sessions, 64 sessions/run and 20,000 sequences are bounded. Reaching a session boundary may require a new partial observation. No delivery is promised while the browser is closed or after storage is cleared.

## Validation and reproduction

From the game checkout:

```sh
npm run godot:test
godot --headless --path godot -s res://dev/check_scripts.gd
npm run lint
npm run godot:web
```

`test_playtest.gd` covers consent, save/restore, partial coverage, clocks, expiry/full queue, immutable retries, split/receipt/rejection, comments, round identity and native writer exclusivity. It also replays three golden traces through `main.gd` commands and compares the complete engine outcomes and actual spending. Set `GTC_E4_TRACE_OUTPUT` to an explicit output path to regenerate those synthetic wire streams. GTC's `test_awakening_adapter.py` validates/replays the checked-in streams against its isolated receiver, including late gap fills and identical receipts.

For end-to-end tests, use a disposable PostgreSQL database and local Gunicorn at `127.0.0.1:58483`. Create a synthetic enabled project/build/open round with build `10000000-0000-4000-8000-000000000002`, round `10000000-0000-4000-8000-000000000003`, collection `10000000-0000-4000-8000-000000000001`, origin `https://127.0.0.1:58485`, a future close and collection unpaused. Enable collection and allow **only** that origin in this disposable service. Do not use a personal SQLite database or a real deployment for this fixture.

Native: set `GTC_E4_NATIVE_OUTPUT` to an evidence path and run `godot --headless --path godot -s res://tests/playtest_native.gd`. It writes only a uniquely named test save and cleans its file/lease.

Web: copy `godot/` to a temporary project, excluding `.godot/` and `build/`; set its main scene to `res://tests/playtest_probe.tscn`, remove `tests/*` from that copy's export exclusions, configure the three synthetic IDs above and HTTPS endpoint `https://127.0.0.1:58484/api/playtests/v1/batches/`, import and export Web. Do not modify the production config to run the probe. With a local test certificate/key, run:

```sh
node godot/tests/playtest-web.mjs /tmp/your-probe/build/web /tmp/key.pem /tmp/cert.pem /tmp/evidence.json
```

The script owns and closes local HTTPS ports 58484 (proxy) and 58485 (build), and its browser contexts. It verifies a legacy Godot filesystem save, consent, real document focus, two windows, a dropped response after database commit, offline continuation/reopen, bytes already present in IndexedDB before POST, restored session and storage failure. Playwright's forced-focus emulation is explicitly disabled for the focus test. `PLAYWRIGHT_CHROMIUM_EXECUTABLE` selects an existing Chromium; `GTC_E4_HEADED=1` enables headed execution under an isolated display such as Xvfb. Production exports exclude every probe/test file.

Local evidence lives in GTC's `contracts/playtests/evidence/phase-4/`. It covers Linux/Chromium and synthetic playthroughs, not real people, itch.io, physical phones, Safari, service capacity or deployment. E5 supplies private review screens. E6 supplies purge/operation and corrects menus/guide to planning, as originally specified in E1. Thirty Godot tests and the HTTPS Web probe pass with this policy. Earlier E4 synthetic exports retain their historical clock values; do not rewrite queued data or combine builds with different clock policies. Use a new identified build and round for E7, which exercises the real host/devices and pilot.
