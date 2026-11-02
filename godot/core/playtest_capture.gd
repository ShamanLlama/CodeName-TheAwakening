class_name PlaytestCapture
extends RefCounted
## Consent-aware capture and durable outbox. Never performs I/O or changes game rules.

const MAX_EVENTS := 5000
const MAX_BYTES := 8388608
const TTL := 604800
const CLOCK_KEYS := ["elapsed_ms", "simulation_ms", "planning_ms", "background_ms", "excluded_ms"]
var config := {}
var data := fresh()
# Consent is an explicit choice per round, remembered on this device until the player stops.
# configure() never confirms by itself; main restores a remembered "yes" only for a writable save.
var confirmed := false
var session_live := false
var warning := ""
var corrupt := false
var raw_stored = null
var urgent := false
var _count := 0
var _bytes := 0

static func identifier() -> String:
	var bytes := Crypto.new().generate_random_bytes(16)
	bytes[6] = (bytes[6] & 15) | 64
	bytes[8] = (bytes[8] & 63) | 128
	var s := bytes.hex_encode()
	return "%s-%s-%s-%s-%s" % [s.substr(0, 8), s.substr(8, 4), s.substr(12, 4), s.substr(16, 4), s.substr(20)]

static func valid_id(value) -> bool:
	if typeof(value) != TYPE_STRING: return false
	var regex := RegEx.new()
	regex.compile("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")
	return regex.search(value) != null

static func fresh() -> Dictionary:
	return {"version": 1, "choices": {}, "testers": {}, "runs": {}, "active": "", "signature": "", "staging": [], "jobs": [], "losses": 0, "receipt": {}}

static func empty_clock() -> Dictionary:
	return {"elapsed_ms": 0, "simulation_ms": 0, "planning_ms": 0, "background_ms": 0, "excluded_ms": 0}

static func signature(state) -> String:
	return "" if state == null else JSON.stringify([state.seed, state.difficulty, int(state.elapsedMonths), int(state.nextLogId), state.status])

static func valid_endpoint(value) -> bool:
	if typeof(value) != TYPE_STRING or not value.ends_with("/api/playtests/v1/batches/"): return false
	for forbidden in ["@", "?", "#", "\n", "\r"]:
		if value.contains(forbidden): return false
	return value.begins_with("https://") or (OS.is_debug_build() and value.begins_with("http://127.0.0.1:"))

func configure(options: Dictionary, stored = {}) -> void:
	config = options.duplicate(true)
	confirmed = false
	session_live = false
	corrupt = not _valid_store(stored)
	raw_stored = stored
	data = fresh() if corrupt or (typeof(stored) == TYPE_DICTIONARY and stored.is_empty()) else stored.duplicate(true)
	if corrupt: warning = "The saved playtest record is unreadable. Gameplay is available; export or clear the local record."
	_recount()
	if _count > MAX_EVENTS or _bytes > MAX_BYTES:
		corrupt = true
		warning = "The saved playtest queue exceeds its limit. Export or clear the local record."

func configured() -> bool:
	return config.get("enabled") == true and valid_endpoint(config.get("endpoint")) and valid_id(config.get("collection_id")) and valid_id(config.get("build_id")) and valid_id(config.get("round_id"))

func accept() -> bool:
	if not configured() or corrupt: return false
	expire()
	if not data.testers.has(config.round_id) and data.testers.size() >= 64:
		warning = "The local record has reached its round limit. Export and clear it before joining another round."
		return false
	confirmed = true
	if data.active.is_empty(): warning = ""
	data.choices[config.round_id] = true
	if not data.testers.has(config.round_id): data.testers[config.round_id] = identifier()
	return true

## A remembered "yes" for this round confirms again on opening; "no" or no answer does not.
func restore() -> bool:
	if not configured() or corrupt or data.choices.get(config.round_id, false) != true or not data.testers.has(config.round_id): return false
	return accept()

## True once the player has answered for this round on this device, either way.
func answered() -> bool:
	return configured() and data.choices.has(config.round_id)

## Remembers "play without sharing" for this round. No IDs are created.
func decline() -> void:
	if not configured() or corrupt or confirmed: return
	data.choices[config.round_id] = false

func withdraw() -> void:
	data = fresh()
	if configured(): data.choices[config.round_id] = false
	confirmed = false
	session_live = false
	corrupt = false
	raw_stored = null
	urgent = false
	warning = "Participation stopped. Unsent records were cleared. Records already received are managed by the GTC operator."
	_recount()

func snapshot(state = null):
	if corrupt: return raw_stored
	if session_live and confirmed and not active().get("stopped", true): data.signature = signature(state)
	return data.duplicate(true)

func active() -> Dictionary:
	return data.runs.get(data.active, {})

func capturing() -> bool:
	var run := active()
	return confirmed and session_live and not corrupt and not run.is_empty() and not run.closed and not run.stopped

func start_run(state: Dictionary, from_start := true) -> void:
	if not confirmed or not configured() or corrupt or GameEngine.is_over(state): return
	expire()
	_prune_runs()
	if data.runs.size() >= 64 or data.testers.size() > 64 or _count >= MAX_EVENTS - 4 or _bytes > MAX_BYTES - 8192:
		warning = "Playtest storage is full. Export the local record; gameplay continues without capture."
		session_live = false
		return
	if state.seed.length() > 64:
		warning = "This seed is too long for playtest capture. Gameplay is available."
		return
	var id := identifier()
	var context := {"schema_version": "1.0", "adapter": "awakening-1", "collection_id": config.collection_id,
		"build_id": config.build_id, "round_id": config.round_id, "tester_id": data.testers[config.round_id],
		"run_id": id, "participation_version": "playtests-v1", "coverage": "from_start" if from_start else "partial"}
	data.runs[id] = {"context": context, "endpoint": config.endpoint, "sequence": 0, "session_id": identifier(),
		"clock": empty_clock(), "sessions": 1, "comments": 0, "closed": false, "stopped": false, "seen": {}, "last_event": {}, "last_session_id": ""}
	data.active = id
	session_live = true
	emit_event("run_started" if from_start else "capture_started", {"difficulty": state.difficulty, "seed": state.seed, "ruleset": state.ruleset}, state)
	emit_event("session_started", {"mode": "new" if from_start else "partial"}, state)
	data.signature = signature(state)
	urgent = true

func resume(state: Dictionary) -> void:
	if not confirmed or not configured() or corrupt or session_live or GameEngine.is_over(state): return
	var run := active()
	if run.is_empty() or run.closed or run.stopped or run.context.round_id != config.round_id or run.context.build_id != config.build_id or data.signature != signature(state):
		start_run(state, false)
		return
	if int(run.sessions) >= 64:
		run.stopped = true
		start_run(state, false)
		return
	run.sessions = int(run.sessions) + 1
	run.session_id = identifier()
	run.clock = empty_clock()
	session_live = true
	emit_event("session_started", {"mode": "resume"}, state)
	# No closed interval is charged. A crash may have lost an unsaved tail.
	emit_event("capture_gap", {"reason": "unknown_time", "missing_events": null}, state)
	urgent = true

func tick(milliseconds: int, mode: String) -> void:
	if not capturing() or milliseconds <= 0: return
	var clock: Dictionary = active().clock
	if clock.elapsed_ms + milliseconds > 604800000:
		stop_capture("A capture session reached seven days. Reopen the game to start a new session.")
		return
	clock.elapsed_ms = int(clock.elapsed_ms) + milliseconds
	clock[mode + "_ms"] = int(clock[mode + "_ms"]) + milliseconds

func emit_event(kind: String, payload: Dictionary, state: Dictionary, phase := "", reserve := false) -> void:
	if not capturing(): return
	var run := active()
	var event := {"event_id": identifier(), "sequence": int(run.sequence) + 1, "type": kind,
		"phase": phase if phase else ("ended" if GameEngine.is_over(state) else state.status),
		"game_month": int(state.elapsedMonths), "clock": run.clock.duplicate(), "data": payload.duplicate(true)}
	var size := JSON.stringify(event).to_utf8_buffer().size()
	if not reserve and (_count >= MAX_EVENTS - 2 or _bytes + size > MAX_BYTES - 4096 or run.sequence >= 19998):
		emit_event("capture_gap", {"reason": "queue_full", "missing_events": null}, state, str(run.last_event.get("phase", state.status)), true)
		stop_capture("Playtest storage is full. Capture stopped; export the local record. Gameplay continues.")
		return
	data.staging.append({"run_id": data.active, "session_id": run.session_id, "event": event, "created_at": Time.get_unix_time_from_system()})
	run.sequence = event.sequence
	run.last_event = event.duplicate(true)
	run.last_session_id = run.session_id
	_count += 1
	_bytes += size
	if kind in ["run_finished", "run_abandoned"]:
		run.closed = true
		session_live = false
		urgent = true

func checkpoint(name: String, region, state: Dictionary) -> void:
	if not capturing() or active().seen.has(name): return
	active().seen[name] = true
	emit_event("checkpoint_reached", {"checkpoint": name, "region": region}, state)

func stop_capture(message: String) -> void:
	if not active().is_empty(): active().stopped = true
	session_live = false
	warning = message
	urgent = true

func seal_batch() -> Dictionary:
	if data.staging.is_empty() or corrupt: return {}
	var first: Dictionary = data.staging[0]
	var run: Dictionary = data.runs[first.run_id]
	var body: Dictionary = run.context.duplicate(true)
	body.session_id = first.session_id
	body.batch_id = identifier()
	body.events = []
	var count := 0
	var queued: float = first.created_at
	for entry in data.staging:
		if entry.run_id != first.run_id or entry.session_id != first.session_id or count >= 32: break
		body.events.append(entry.event)
		if JSON.stringify(body).to_utf8_buffer().size() > 65536:
			body.events.pop_back()
			break
		queued = minf(queued, entry.created_at)
		count += 1
	if count == 0: return {}
	var job := _job(body, run.endpoint, queued)
	data.jobs.append(job)
	data.staging = data.staging.slice(count)
	urgent = not data.staging.is_empty()
	_recount()
	return job

func queue_comment(text: String) -> bool:
	var run := active()
	text = text.strip_edges()
	if not confirmed or corrupt or run.is_empty() or not run.closed or run.stopped or int(run.comments) >= 10 or text.is_empty() or text.length() > 1000 or _count >= MAX_EVENTS - 2 or _bytes > MAX_BYTES - 16384: return false
	var body: Dictionary = run.context.duplicate(true)
	body.session_id = run.last_session_id
	body.batch_id = identifier()
	body.events = [run.last_event.duplicate(true)]
	body.comment = {"comment_id": identifier(), "text": text}
	run.comments = int(run.comments) + 1
	data.jobs.append(_job(body, run.endpoint, Time.get_unix_time_from_system()))
	urgent = true
	_recount()
	return true

func _job(body: Dictionary, endpoint: String, queued: float) -> Dictionary:
	return {"id": body.batch_id, "run_id": body.run_id, "round_id": body.round_id, "endpoint": endpoint,
		"body": JSON.stringify(body), "queued_at": queued, "attempts": 0, "next_retry": 0, "blocked": false}

func next_job(now := Time.get_unix_time_from_system()) -> Dictionary:
	if not confirmed or not configured() or corrupt: return {}
	for job in data.jobs:
		if not job.blocked and job.endpoint == config.endpoint and data.choices.get(job.round_id, false) and now >= job.next_retry: return job
	return {}

func find_job(id: String) -> Dictionary:
	for job in data.jobs:
		if job.id == id: return job
	return {}

func attempted(job: Dictionary) -> void:
	job.attempts = int(job.attempts) + 1
	job.next_retry = Time.get_unix_time_from_system() + 10

func postpone(job: Dictionary, retry_after := 0.0) -> void:
	var delay := minf(120, pow(2, mini(int(job.attempts) - 1, 7))) * randf_range(0.8, 1.2)
	job.next_retry = Time.get_unix_time_from_system() + maxf(delay, retry_after)

func reject(job: Dictionary, message: String) -> void:
	for entry in data.jobs:
		if entry.run_id == job.run_id: entry.blocked = true
	data.runs[job.run_id].stopped = true
	if data.active == job.run_id: session_live = false
	warning = message

func split_job(job: Dictionary) -> bool:
	var body: Dictionary = JSON.parse_string(job.body)
	if body.events.size() < 2: return false
	var middle := int(body.events.size() / 2)
	var index: int = data.jobs.find(job)
	data.jobs.remove_at(index)
	for half in [body.events.slice(0, middle), body.events.slice(middle)]:
		var part := body.duplicate(true)
		part.batch_id = identifier()
		part.events = half
		if half == body.events.slice(0, middle): part.erase("comment")
		data.jobs.insert(index, _job(part, job.endpoint, job.queued_at))
		index += 1
	_recount()
	return true

func confirm_job(job: Dictionary, receipt: Dictionary) -> Dictionary:
	var body: Dictionary = JSON.parse_string(job.body)
	if receipt.get("batch_id") != job.id or not valid_id(receipt.get("receipt_id")) or typeof(receipt.get("accepted_comment")) != TYPE_BOOL or not _integer(receipt.get("accepted_events"), body.events.size()) or typeof(receipt.get("run_expires_at")) != TYPE_STRING: return {}
	if not receipt.run_expires_at.ends_with("Z") or Time.get_unix_time_from_datetime_string(receipt.run_expires_at) <= 0: return {}
	var undo := {"job": job.duplicate(true), "receipt": data.receipt.duplicate(true)}
	data.jobs.erase(job)
	data.receipt = receipt.duplicate(true)
	_recount()
	return undo

func undo_confirmation(undo: Dictionary) -> void:
	data.jobs.push_front(undo.job)
	data.receipt = undo.receipt
	_recount()

func expire(now := Time.get_unix_time_from_system()) -> bool:
	var old: int = data.jobs.size() + data.staging.size()
	data.jobs = data.jobs.filter(func(j: Dictionary) -> bool: return now - j.queued_at <= TTL)
	data.staging = data.staging.filter(func(e: Dictionary) -> bool: return now - e.created_at <= TTL)
	if old == data.jobs.size() + data.staging.size(): return false
	data.losses = int(data.losses) + 1
	stop_capture("Pending playtest records expired after seven days. Coverage is incomplete; gameplay continues.")
	_recount()
	return true

func pending_events() -> int:
	return _count

func _recount() -> void:
	_count = data.staging.size()
	_bytes = 0
	for entry in data.staging: _bytes += JSON.stringify(entry.event).to_utf8_buffer().size()
	for job in data.jobs:
		_bytes += job.body.to_utf8_buffer().size()
		_count += JSON.parse_string(job.body).events.size()

func _prune_runs() -> void:
	var used := {data.active: true}
	for entry in data.staging: used[entry.run_id] = true
	for job in data.jobs: used[job.run_id] = true
	for id in data.runs.keys():
		if not used.has(id): data.runs.erase(id)

static func _number(value, maximum := 9007199254740991.0) -> bool:
	return typeof(value) in [TYPE_INT, TYPE_FLOAT] and is_finite(value) and value >= 0 and value <= maximum

static func _integer(value, maximum := 9007199254740991.0) -> bool:
	return _number(value, maximum) and value == floor(value)

static func _valid_store(value) -> bool:
	if typeof(value) == TYPE_DICTIONARY and value.is_empty(): return true
	if typeof(value) != TYPE_DICTIONARY or value.get("version") != 1: return false
	for key in ["choices", "testers", "runs", "receipt"]:
		if typeof(value.get(key)) != TYPE_DICTIONARY or value[key].size() > 64: return false
	for key in ["staging", "jobs"]:
		if typeof(value.get(key)) != TYPE_ARRAY or value[key].size() > MAX_EVENTS: return false
	if typeof(value.get("active")) != TYPE_STRING or typeof(value.get("signature")) != TYPE_STRING or not _integer(value.get("losses")): return false
	if value.active != "" and not value.runs.has(value.active): return false
	for id in value.testers:
		if not valid_id(id) or not valid_id(value.testers[id]): return false
	for id in value.choices:
		if not valid_id(id) or typeof(value.choices[id]) != TYPE_BOOL: return false
	for id in value.runs:
		var run = value.runs[id]
		if not valid_id(id) or typeof(run) != TYPE_DICTIONARY: return false
		if typeof(run.get("context")) != TYPE_DICTIONARY or typeof(run.get("clock")) != TYPE_DICTIONARY or not valid_id(run.get("session_id")) or not _integer(run.get("sequence"), 20000) or not _integer(run.get("sessions"), 64) or run.sessions < 1 or not _integer(run.get("comments"), 10): return false
		for key in ["closed", "stopped"]:
			if typeof(run.get(key)) != TYPE_BOOL: return false
		for key in ["seen", "last_event"]:
			if typeof(run.get(key)) != TYPE_DICTIONARY: return false
		if typeof(run.get("last_session_id")) != TYPE_STRING or not valid_endpoint(run.get("endpoint")): return false
		for key in ["collection_id", "build_id", "round_id", "tester_id", "run_id"]:
			if not valid_id(run.context.get(key)): return false
		if run.context.get("run_id") != id or run.context.get("schema_version") != "1.0" or run.context.get("adapter") != "awakening-1" or run.context.get("participation_version") != "playtests-v1" or not run.context.get("coverage") in ["from_start", "partial"]: return false
		if not run.last_event.is_empty() and not _valid_event(run.last_event): return false
		if run.closed and (not valid_id(run.last_session_id) or run.last_event.get("type") not in ["run_finished", "run_abandoned"]): return false
		var sum := 0
		for key in CLOCK_KEYS:
			if not _integer(run.clock.get(key), 604800000): return false
			if key != "elapsed_ms": sum += int(run.clock[key])
		if sum != run.clock.elapsed_ms: return false
	for entry in value.staging:
		if typeof(entry) != TYPE_DICTIONARY or not value.runs.has(entry.get("run_id", "")) or not valid_id(entry.get("session_id")) or not _valid_event(entry.get("event")) or not _number(entry.get("created_at")): return false
	for job in value.jobs:
		if typeof(job) != TYPE_DICTIONARY or not value.runs.has(job.get("run_id", "")) or not valid_id(job.get("id")) or not valid_id(job.get("round_id")) or not valid_endpoint(job.get("endpoint")): return false
		if typeof(job.get("body")) != TYPE_STRING or job.body.length() > 65536 or not _number(job.get("queued_at")) or not _integer(job.get("attempts")) or not _number(job.get("next_retry")) or typeof(job.get("blocked")) != TYPE_BOOL: return false
		var body = JSON.parse_string(job.body)
		if typeof(body) != TYPE_DICTIONARY or typeof(body.get("events")) != TYPE_ARRAY or body.events.is_empty() or body.events.size() > 64 or body.get("batch_id") != job.id: return false
		if not valid_id(body.get("session_id")) or body.get("run_id") != job.run_id: return false
		for event in body.events:
			if not _valid_event(event): return false
	return true

static func _valid_event(event) -> bool:
	if typeof(event) != TYPE_DICTIONARY or not valid_id(event.get("event_id")) or not _integer(event.get("sequence"), 20000) or event.sequence < 1 or not _integer(event.get("game_month"), 224): return false
	if event.get("phase") not in ["placing", "playing", "alignment", "awakening", "ended"] or typeof(event.get("data")) != TYPE_DICTIONARY or typeof(event.get("clock")) != TYPE_DICTIONARY: return false
	if event.get("type") not in ["run_started", "capture_started", "session_started", "starting_region_selected", "action_performed", "doctrine_selected", "awakening_started", "phase_changed", "checkpoint_reached", "time_checkpoint", "capture_gap", "run_finished", "run_abandoned"]: return false
	var total := 0
	for key in CLOCK_KEYS:
		if not _integer(event.clock.get(key), 604800000): return false
		if key != "elapsed_ms": total += int(event.clock[key])
	return total == event.clock.elapsed_ms
