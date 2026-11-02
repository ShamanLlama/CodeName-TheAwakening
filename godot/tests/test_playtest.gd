extends "res://tests/test_case.gd"

const Capture = preload("res://core/playtest_capture.gd")

static func configuration() -> Dictionary:
	return {"enabled": true, "endpoint": "https://playtest.example.invalid/api/playtests/v1/batches/",
		"collection_id": "10000000-0000-4000-8000-000000000001", "build_id": "10000000-0000-4000-8000-000000000002",
		"round_id": "10000000-0000-4000-8000-000000000003"}

func test_consent_precedes_ids_and_start_events() -> void:
	var capture := Capture.new()
	capture.configure(configuration())
	var state := GameEngine.create_game_state("hard", "consent-fixture")
	capture.start_run(state)
	check_eq(capture.pending_events(), 0, "no capture before explicit participation")
	capture.accept()
	capture.start_run(state)
	check_eq(capture.pending_events(), 2, "accepted run stores start and session before any network request")

func joined() -> PlaytestCapture:
	var capture := Capture.new()
	capture.configure(configuration())
	capture.accept()
	return capture

func test_saved_retry_is_immutable_and_resume_starts_a_fresh_clock() -> void:
	var state := GameEngine.create_game_state("normal", "saved-retry")
	var capture := joined()
	capture.start_run(state)
	capture.tick(1200, "planning")
	capture.emit_event("time_checkpoint", {"reason": "before_save"}, state)
	var job := capture.seal_batch()
	var body: String = job.body
	capture.attempted(job)
	capture.postpone(job, 600)
	var saved: Dictionary = JSON.parse_string(JSON.stringify(capture.snapshot(state)))
	var restored := Capture.new()
	restored.configure(configuration(), saved)
	check(not restored.corrupt, "valid stored queue reloads")
	check(restored.next_job(9999999999).is_empty(), "opening does not silently renew consent")
	restored.accept()
	check_eq(restored.next_job(9999999999).body, body, "retry keeps exact body and IDs")
	var id: String = restored.data.active
	var previous_session: String = restored.active().session_id
	restored.resume(state)
	check_eq(restored.data.active, id, "resume keeps run ID")
	check(restored.active().session_id != previous_session, "resume opens another session")
	check_eq(restored.active().clock, Capture.empty_clock(), "closed time never enters session clock")
	check_eq(restored.data.staging[0].event.data.mode, "resume")

func test_clocks_partition_planning_simulation_background_and_menu() -> void:
	var state := GameEngine.create_game_state("normal", "clock")
	var capture := joined()
	capture.start_run(state)
	for pair in [[1000, "simulation"], [2000, "planning"], [3000, "background"], [4000, "excluded"]]: capture.tick(pair[0], pair[1])
	check_eq(capture.active().clock, {"elapsed_ms": 10000, "simulation_ms": 1000, "planning_ms": 2000, "background_ms": 3000, "excluded_ms": 4000})
	capture.withdraw()
	capture.tick(1000, "simulation")
	capture.start_run(state)
	check_eq(capture.pending_events(), 0, "withdraw removes unsent records and halts recording")
	check(capture.data.testers.is_empty(), "withdraw removes local round identity")

func test_legacy_or_unobserved_progress_has_partial_coverage() -> void:
	var state: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("easy", "legacy"), "africa").state
	var capture := joined()
	capture.resume(state)
	check_eq(capture.active().context.coverage, "partial")
	check_eq(capture.data.staging[0].event.type, "capture_started")
	check_eq(capture.data.staging[0].event.phase, "playing")
	check_eq(capture.data.staging.size(), 2, "does not invent an initial-region event")
	var saved: Dictionary = capture.snapshot(state)
	state = GameEngine.perform_action(state, "recruit", "africa").state
	var restored := Capture.new()
	restored.configure(configuration(), saved)
	restored.accept()
	restored.resume(state)
	check(restored.data.active != saved.active, "unobserved progress starts a partial run")
	check_eq(restored.active().context.coverage, "partial")

func test_receipts_splitting_and_permanent_rejection() -> void:
	var state := GameEngine.create_game_state("normal", "receipt")
	var capture := joined()
	capture.start_run(state)
	var job := capture.seal_batch()
	check(capture.confirm_job(job, {"batch_id": job.id}).is_empty(), "invalid receipt keeps pending batch")
	check(capture.split_job(job), "413 splits a multiple-event batch")
	var first: Dictionary = capture.data.jobs[0]
	var second: Dictionary = capture.data.jobs[1]
	check(first.id != job.id and second.id != job.id and first.id != second.id, "split changes batch IDs")
	check_eq(JSON.parse_string(first.body).events[0], JSON.parse_string(job.body).events[0], "event remains byte-equivalent in new envelope")
	check_eq(first.queued_at, job.queued_at, "splitting does not extend TTL")
	var receipt := {"batch_id": first.id, "receipt_id": Capture.identifier(), "accepted_events": 1, "accepted_comment": false, "run_expires_at": "2027-04-30T12:00:00Z"}
	var undo := capture.confirm_job(first, receipt)
	check(not undo.is_empty(), "valid receipt permits removal")
	check_eq(capture.pending_events(), 1)
	capture.undo_confirmation(undo)
	check_eq(capture.pending_events(), 2, "storage failure can restore unconfirmed local job")
	capture.reject(first, "HTTP 409")
	check(capture.next_job(9999999999).is_empty(), "permanent rejection does not retry forever")
	check(not capture.capturing(), "rejected run stops generating unusable records")

func test_queue_expiry_and_overflow_are_visible_and_bounded() -> void:
	var state := GameEngine.create_game_state("normal", "limit")
	var capture := joined()
	capture.start_run(state)
	for index in Capture.MAX_EVENTS:
		capture.emit_event("time_checkpoint", {"reason": "periodic"}, state)
	check(not capture.capturing(), "full queue stops capture")
	check_eq(capture.data.staging[-1].event.type, "capture_gap")
	check_eq(capture.data.staging[-1].event.data.reason, "queue_full")
	check(capture.pending_events() <= Capture.MAX_EVENTS, "reserve leaves room for gap")
	capture.seal_batch()
	var queued: float = capture.data.jobs[0].queued_at
	check(capture.expire(queued + Capture.TTL + 1), "old pending records expire")
	check_eq(capture.pending_events(), 0)
	check(not capture.warning.is_empty(), "expiry is not silent")

func test_corrupt_optional_metadata_does_not_corrupt_game_save() -> void:
	var save := SaveData.default_save()
	save.state = GameEngine.create_game_state("normal", "safe-game")
	save.playtest = {"version": 1, "jobs": "broken"}
	var decoded = SaveData.decode_save(JSON.stringify(save))
	check(decoded != null, "old game validation ignores optional telemetry")
	var capture := Capture.new()
	capture.configure(configuration(), decoded.playtest)
	check(capture.corrupt, "invalid optional record cannot send")
	check_eq(capture.snapshot(decoded.state), save.playtest, "retain unreadable optional record for export")
	check(not capture.accept(), "corrupt telemetry is never resumed")
	for malformed in [null, true, [], "broken"]:
		capture.configure(configuration(), malformed)
		check(capture.corrupt, "malformed optional shapes do not crash the game")

func test_terminal_comment_reuses_event_and_sequence() -> void:
	var state := GameEngine.create_game_state("normal", "comment")
	var capture := joined()
	capture.start_run(state)
	capture.emit_event("run_abandoned", {"reason": "explicit_abandon"}, state, "ended")
	var terminal: Dictionary = capture.active().last_event.duplicate(true)
	check(capture.queue_comment("A synthetic observation"), "terminal run allows optional comment")
	var comment: Dictionary = JSON.parse_string(capture.data.jobs[0].body)
	check_eq(comment.events, [terminal], "comment never appends after terminal")
	check_eq(capture.active().sequence, terminal.sequence)
	check_eq(comment.session_id, capture.active().last_session_id)
	var receipt := {"batch_id": comment.batch_id, "receipt_id": Capture.identifier(), "accepted_events": 0,
		"accepted_comment": false, "run_expires_at": "2027-04-30T12:00:00Z"}
	check(not capture.confirm_job(capture.data.jobs[0], receipt).is_empty(), "valid duplicate receipt confirms already-stored comment, even when zero comments are new")

func test_game_commands_emit_actual_costs_and_preserve_golden_outcomes() -> void:
	var root_script = load("res://main.gd")
	var golden = load("res://tests/test_golden.gd")
	var summaries := []
	for filename in ["strategy-early-rlyeh.json.gz", "strategy-early-resume.json.gz", "fuzz-hard-east-asia.json.gz"]:
		var trace: Dictionary = golden.load_trace("res://tests/golden/" + filename)
		var game = root_script.new()
		game.save_path = "user://e4-test-" + Capture.identifier() + ".json"
		game.lab_platform.initialize(game.save_path)
		game.sfx = Sfx.new()
		game.sfx.muted = true
		game.add_child(game.sfx)
		game.lab.configure(configuration())
		game.lab.accept()
		game.begin(trace.difficulty, trace.seed)
		var costs := {"influence": 0.0, "dread": 0.0, "followers": 0.0}
		var previous_events := 0
		for step in trace.steps:
			for command in step.commands:
				var before_count: int = game.lab.pending_events()
				match command.op:
					"place": game.place(command.region)
					"action": game.act(command.id, command.region)
					"doctrine": game.choose_doctrine(command.choice)
					"awaken": game.awaken()
				if not command.ok: check_eq(game.lab.pending_events(), before_count, "refused commands are not decisions")
			game._advance(int(step.months))
			for index in range(previous_events, game.lab.data.staging.size()):
				var event: Dictionary = game.lab.data.staging[index].event
				if event.data.has("spent"):
					for key in costs: costs[key] += event.data.spent[key]
			previous_events = game.lab.data.staging.size()
		check_eq(game.state.status, trace.final, filename + " outcome")
		var expected: Dictionary = trace.steps[-1].after.state
		var plain: Dictionary = game.state.duplicate(true)
		plain.erase("log")
		check_eq(plain, expected, filename + " engine unchanged")
		check_eq(costs, {"influence": game.state.telemetry.influenceSpent, "dread": game.state.telemetry.dreadSpent, "followers": game.state.telemetry.sacrificed}, filename + " actual costs")
		check_eq(game.lab.active().last_event.type, "run_finished", "known terminal recorded once")
		check(game.lab.queue_comment("Synthetic E4 golden run"), "comment can accompany terminal")
		while not game.lab.data.staging.is_empty(): game.lab.seal_batch()
		var bodies := []
		for job in game.lab.data.jobs: bodies.append(JSON.parse_string(job.body))
		summaries.append({"trace": filename, "batches": bodies, "expected_costs": costs, "outcome": game.state.status})
		game.lab_platform.dispose()
		DirAccess.remove_absolute(game.save_path)
		game.free()
	# Optional explicit path for offline shared-schema / receiver validation.
	var output := OS.get_environment("GTC_E4_TRACE_OUTPUT")
	if output:
		var file := FileAccess.open(output, FileAccess.WRITE)
		file.store_string(JSON.stringify(summaries))
		file.close()

func test_second_native_window_cannot_write_the_same_save() -> void:
	var path := "user://e4-lock-" + Capture.identifier() + ".json"
	var first := PlaytestPlatform.new()
	var second := PlaytestPlatform.new()
	first.initialize(path)
	second.initialize(path)
	check(first.writable and not second.writable, "one writer per save")
	check_eq(second.persist(SaveData.default_save()), -1, "second process cannot save")
	check(first.persist(SaveData.default_save()) > 0, "owner can save")
	first.dispose()
	second.dispose()
	var recovered := PlaytestPlatform.new()
	recovered.initialize(path)
	check(recovered.writable, "closing the owner releases the save")
	recovered.dispose()
	DirAccess.remove_absolute(path)

func test_new_round_separates_testers_and_new_build_starts_partial() -> void:
	var state := GameEngine.create_game_state("normal", "round")
	var capture := joined()
	capture.start_run(state)
	var first: Dictionary = capture.active().context.duplicate(true)
	var saved: Dictionary = capture.snapshot(state)
	var options := configuration()
	options.build_id = Capture.identifier()
	options.round_id = Capture.identifier()
	var next := Capture.new()
	next.configure(options, saved)
	next.accept()
	next.resume(state)
	check(next.active().context.tester_id != first.tester_id, "rounds never reuse tester identity")
	check(next.active().context.run_id != first.run_id, "new build cannot change an existing run context")
	check_eq(next.active().context.coverage, "partial")
	check_eq(next.data.staging[0].event.event_id, saved.staging[0].event.event_id, "old pending records remain independent of new game")

func test_comments_are_bounded_and_withdrawal_can_be_renewed() -> void:
	var state := GameEngine.create_game_state("normal", "comments-limit")
	var capture := joined()
	capture.start_run(state)
	capture.emit_event("run_abandoned", {"reason": "explicit_abandon"}, state, "ended")
	for index in 10: check(capture.queue_comment("Synthetic comment %d" % index), "allow up to ten comments")
	check(not capture.queue_comment("Eleventh comment"), "do not repeatedly send comments beyond receiver quota")
	capture.withdraw()
	check(capture.accept(), "participation can be renewed")
	check(capture.warning.is_empty(), "renewed participation does not display a stale stopped message")

func test_retry_after_honors_server_delays() -> void:
	check_eq(PlaytestClient.retry_after_seconds("300"), 300)
	check_eq(PlaytestClient.retry_after_seconds("invalid"), 0)
	var future := Time.get_unix_time_from_system() + 600
	var capture := joined()
	var state := GameEngine.create_game_state("normal", "retry-after")
	capture.start_run(state)
	var job := capture.seal_batch()
	capture.attempted(job)
	capture.postpone(job, 600)
	check(job.next_retry >= future, "server Retry-After may exceed local backoff cap")
	check(capture.next_job(future - 1).is_empty(), "do not retry before server delay")

func test_opening_without_renewed_consent_still_purges_expired_queue() -> void:
	var state := GameEngine.create_game_state("normal", "expired-opening")
	var capture := joined()
	capture.start_run(state)
	var job := capture.seal_batch()
	job.queued_at = Time.get_unix_time_from_system() - Capture.TTL - 1
	var restored := Capture.new()
	restored.configure(configuration(), capture.snapshot(state))
	var platform := PlaytestPlatform.new()
	platform.ready = true
	platform.writable = true
	var persisted := []
	var client := PlaytestClient.new()
	client.setup(restored, platform, func() -> int:
		persisted.append(restored.snapshot(state))
		return 1)
	client._process(0)
	check(not restored.confirmed, "purging never renews consent")
	check_eq(restored.pending_events(), 0, "expired queue does not wait for rejoining")
	check_eq(persisted.size(), 1, "expiry is saved without sending anything")
	check(not restored.warning.is_empty(), "lost coverage remains visible")
	client.free()

func test_open_game_menu_counts_planning_not_simulation_or_excluded() -> void:
	var game = load("res://main.gd").new()
	game.state = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "clock-menu"), "africa").state
	var screen = load("res://ui/game_screen.gd").new(game)
	game.add_child(screen)
	game._screen = screen
	game._holds = 1
	check_eq(game._lab_current_mode(), "planning", "guide, doctrine and menu keep observed planning time")
	game._lab_focused = false
	check_eq(game._lab_current_mode(), "background", "background takes priority over an open menu")
	game.free()

func test_remembered_choice_is_explicit_per_round_and_device() -> void:
	var state := GameEngine.create_game_state("normal", "remembered")
	var fresh := Capture.new()
	fresh.configure(configuration())
	check(not fresh.answered(), "a new device has no answer")
	check(not fresh.restore(), "nothing to restore without an answer")
	# "Yes" survives reopening, with the same tester, but only through restore().
	var yes := joined()
	var tester: String = yes.data.testers[configuration().round_id]
	var reopened := Capture.new()
	reopened.configure(configuration(), JSON.parse_string(JSON.stringify(yes.snapshot(state))))
	check(not reopened.confirmed, "configure alone never confirms")
	check(reopened.answered(), "the answer is remembered")
	check(reopened.restore() and reopened.confirmed, "a remembered yes confirms again")
	check_eq(reopened.data.testers[configuration().round_id], tester, "same random tester for the round")
	# "No" is remembered without creating identifiers, and never confirms.
	var no := Capture.new()
	no.configure(configuration())
	no.decline()
	check(no.answered() and not no.confirmed, "declining is an answer")
	check(no.data.testers.is_empty(), "declining creates no IDs")
	var no_again := Capture.new()
	no_again.configure(configuration(), JSON.parse_string(JSON.stringify(no.snapshot(state))))
	check(not no_again.restore() and not no_again.confirmed, "a remembered no stays no")
	no_again.accept()
	check(no_again.confirmed, "the player can still join later")
	# Stopping clears records and is remembered as no.
	reopened.withdraw()
	check(reopened.answered() and not reopened.restore(), "stop is remembered as no")
	# Another round asks again.
	var other := configuration()
	other.round_id = "10000000-0000-4000-8000-000000000009"
	var next_round := Capture.new()
	next_round.configure(other, JSON.parse_string(JSON.stringify(yes.snapshot(state))))
	check(not next_round.answered() and not next_round.restore(), "a new round needs a new answer")

func test_start_screen_asks_only_when_a_writable_round_has_no_answer() -> void:
	var game = load("res://main.gd").new()
	game.lab.configure(configuration())
	check(game.needs_participation_choice(), "unanswered round asks")
	game.blocked = true
	check(not game.needs_participation_choice(), "a read-only window never asks")
	game.blocked = false
	game.lab.decline()
	check(not game.needs_participation_choice(), "answered rounds do not ask again")
	var unconfigured = load("res://main.gd").new()
	unconfigured.lab.configure({})
	check(not unconfigured.needs_participation_choice(), "builds without collection never ask")
	game.free()
	unconfigured.free()
