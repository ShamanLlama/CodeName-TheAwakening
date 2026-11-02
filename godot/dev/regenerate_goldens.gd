extends SceneTree
## Rewrites tests/golden/ for the current ruleset. Each trace replays its recorded commands
## through the current engine and records the new previews, results, states and logs; policy
## endings record Playthrough's final states. Run only after an intentional rules change:
##   godot --headless --path godot -s res://dev/regenerate_goldens.gd

const DIR := "res://tests/golden/"
const POLICY_RUNS := [["early", "rlyeh"], ["late", "dagon"], ["hub", "the-deep"], ["early", "resume"]]


func _init() -> void:
	var golden = load("res://tests/test_golden.gd")
	for file in DirAccess.get_files_at(DIR):
		if not file.ends_with(".json.gz") or file == "policy-endings.json.gz":
			continue
		var old: Dictionary = golden.load_trace(DIR + file)
		_write(DIR + file, _replay(golden, old))
		print("rewrote ", file)
	var endings := {"ruleset": Balance.RULESET, "runs": []}
	for pair in POLICY_RUNS:
		var final := Playthrough.play(pair[0], pair[1])
		var plain := final.duplicate()
		plain.erase("log")
		endings.runs.append({"strategy": pair[0], "seed": pair[1], "state": plain})
	_write(DIR + "policy-endings.json.gz", endings)
	print("rewrote policy-endings.json.gz")
	quit()


static func _snapshot(golden, state: Dictionary, log_from: int, with_previews: bool) -> Dictionary:
	var plain := state.duplicate()
	plain.erase("log")
	var snapshot := {"state": plain, "newLog": state.log.filter(func(e: Dictionary) -> bool: return e.id >= log_from), "derived": golden.derived(state)}
	if with_previews:
		snapshot.previews = golden.previews(state)
	return snapshot


static func _replay(golden, old: Dictionary) -> Dictionary:
	var state := GameEngine.create_game_state(old.difficulty, old.seed)
	var trace := {"name": old.name, "seed": old.seed, "difficulty": old.difficulty, "ruleset": Balance.RULESET,
		"initial": _snapshot(golden, state, 1, old.initial.has("previews")), "steps": []}
	var log_from: int = state.nextLogId
	for step in old.steps:
		var commands := []
		for command in step.commands:
			var entry := {"op": command.op}
			var result: Dictionary
			match command.op:
				"place":
					entry.region = command.region
					result = GameEngine.place_starting_foothold(state, command.region)
				"action":
					entry.id = command.id
					entry.region = command.region
					entry.preview = GameEngine.preview_action(state, command.id, command.region)
					result = GameEngine.perform_action(state, command.id, command.region)
				"doctrine":
					entry.choice = command.choice
					result = GameEngine.choose_specialization(state, command.choice)
				"awaken":
					result = GameEngine.start_awakening(state)
			entry.ok = result.ok
			if not result.ok:
				entry.message = result.message
			commands.append(entry)
			state = result.state
		state = GameEngine.advance_simulation(state, step.months)
		trace.steps.append({"commands": commands, "months": step.months, "after": _snapshot(golden, state, log_from, step.after.has("previews"))})
		log_from = state.nextLogId
	trace.final = state.status
	return trace


static func _write(path: String, value: Dictionary) -> void:
	var bytes := JSON.stringify(value).to_utf8_buffer().compress(FileAccess.COMPRESSION_GZIP)
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_buffer(bytes)
	file.close()
