extends "res://tests/test_case.gd"
## Replays the TypeScript engine's recorded runs (src/tests/golden-traces.test.ts) and requires
## identical state, derived values, action previews, command results and log text.

const GOLDEN_DIR := "res://tests/golden/"


func test_every_trace_matches_the_typescript_engine() -> void:
	var files := Array(DirAccess.get_files_at(GOLDEN_DIR)).filter(func(f: String) -> bool: return f.ends_with(".json.gz"))
	check(files.size() >= 9, "expected at least nine golden traces, found %d" % files.size())
	for file in files:
		replay(load_trace(GOLDEN_DIR + file))


static func load_trace(path: String) -> Dictionary:
	var bytes := FileAccess.get_file_as_bytes(path).decompress_dynamic(-1, FileAccess.COMPRESSION_GZIP)
	return JSON.parse_string(bytes.get_string_from_utf8())


func replay(trace: Dictionary) -> void:
	var name: String = trace.name
	var state := GameEngine.create_game_state(trace.difficulty, trace.seed)
	if not snapshot_matches(trace.initial, state, 1, name + " initial"):
		return
	var log_from: int = state.nextLogId
	for index in trace.steps.size():
		var step: Dictionary = trace.steps[index]
		var where := "%s step %d (month %d)" % [name, index, state.elapsedMonths]
		for command in step.commands:
			var result: Dictionary
			match command.op:
				"place":
					result = GameEngine.place_starting_foothold(state, command.region)
				"action":
					if not check_eq(GameEngine.preview_action(state, command.id, command.region), command.preview, where + " preview " + command.id + "@" + command.region):
						return
					result = GameEngine.perform_action(state, command.id, command.region)
				"doctrine":
					result = GameEngine.choose_specialization(state, command.choice)
				"awaken":
					result = GameEngine.start_awakening(state)
			var outcome := { "ok": result.ok }
			if not result.ok:
				outcome.message = result.message
			var expected := { "ok": command.ok }
			if command.has("message"):
				expected.message = command.message
			if not check_eq(outcome, expected, where + " " + command.op + " " + str(command.get("id", ""))):
				return
			state = result.state
		state = GameEngine.advance_simulation(state, step.months)
		if not snapshot_matches(step.after, state, log_from, where):
			return
		log_from = state.nextLogId
	check(state.status == trace.final, name + " should end " + trace.final)


func snapshot_matches(expected: Dictionary, state: Dictionary, log_from: int, where: String) -> bool:
	var plain := state.duplicate()
	plain.erase("log")
	if not check_eq(plain, expected.state, where + " state"):
		return false
	var new_log: Array = state.log.filter(func(entry: Dictionary) -> bool: return entry.id >= log_from)
	if not check_eq(new_log, expected.newLog, where + " log"):
		return false
	if not check_eq(derived(state), expected.derived, where + " derived"):
		return false
	if expected.has("previews") and not check_eq(previews(state), expected.previews, where + " previews"):
		return false
	# Every snapshot must also survive a save and reload unchanged.
	var save := SaveData.default_save()
	save.state = state
	var decoded = SaveData.decode_save(JSON.stringify(save))
	if not check(decoded != null, where + " save was rejected on reload"):
		return false
	return check_eq(decoded.state, state, where + " reloaded save")


static func derived(state: Dictionary) -> Dictionary:
	var regions := {}
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		regions[id] = { "recruitment": Derived.recruitment_per_month(state, r), "income": Derived.region_income(r),
			"siteRequirement": Derived.site_requirement(r), "siteLabel": Derived.site_label(r), "threat": Threats.describe(r),
			"consequences": Threats.consequences(r) if r.threat != null else null }
	return {
		"totalFollowers": Derived.total_followers(state), "influencePerMonth": Derived.influence_per_month(state),
		"cultShare": Derived.cult_share(state), "awarenessDrag": Derived.awareness_drag(state), "readySites": Derived.ready_sites(state),
		"forecast": Forecast.ritual(state), "awakeningReason": GameEngine.awakening_reason(state),
		"calendar": Derived.calendar(state.elapsedMonths), "regions": regions,
	}


static func previews(state: Dictionary) -> Dictionary:
	var all := {}
	for id in GameData.REGION_IDS:
		all[id] = {}
		for action in GameData.ACTION_IDS:
			all[id][action] = GameEngine.preview_action(state, action, id)
	return all


func test_playthrough_policy_port_reaches_the_recorded_endings() -> void:
	for pair in [["early", "rlyeh"], ["late", "dagon"], ["hub", "the-deep"], ["early", "resume"]]:
		var trace := load_trace(GOLDEN_DIR + "strategy-%s-%s.json.gz" % pair)
		var final := Playthrough.play(pair[0], pair[1])
		var plain := final.duplicate()
		plain.erase("log")
		check_eq(plain, trace.steps[-1].after.state, "%s/%s final state" % pair)
