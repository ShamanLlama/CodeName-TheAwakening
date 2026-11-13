extends SceneTree
## Balance measurement with the scripted policy (a bot, not people):
##   godot --headless --path godot -s res://dev/measure.gd [-- --seeds=24]
## Prints, per strategy: wins, how runs were lost, median final Influence, awareness and Dread,
## and how often each threat response was used.

const STRATEGIES := ["early", "late", "hub"]
const RESPONSES := ["counter", "scatter", "silence"]


func _init() -> void:
	var seeds := 24
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--seeds="):
			seeds = int(arg.trim_prefix("--seeds="))
	print("ruleset %s · normal · %d seeds per strategy" % [Balance.RULESET, seeds])
	var total_wins := 0
	for strategy in STRATEGIES:
		var wins := 0
		var lost_awareness := 0
		var lost_deadline := 0
		var influence: Array[float] = []
		var awareness: Array[float] = []
		var dread: Array[float] = []
		var landed: Array[float] = []
		var used := {}
		for response in RESPONSES:
			used[response] = [] as Array[float]
		for i in seeds:
			var final := Playthrough.play(strategy, "measure-%d" % i)
			if final.status == "won":
				wins += 1
			elif final.awareness >= 100:
				lost_awareness += 1
			else:
				lost_deadline += 1
			influence.append(final.influence)
			awareness.append(final.awareness)
			dread.append(final.dread)
			landed.append(final.telemetry.raidLosses)
			for response in RESPONSES:
				used[response].append(float(final.telemetry.actions.get(response, 0)))
		total_wins += wins
		print("%-6s wins %d/%d · lost: awareness %d, deadline %d · final Influence %.0f, awareness %.0f, Dread %.0f · followers lost to threats %.0f · %s" % [
			strategy, wins, seeds, lost_awareness, lost_deadline, _median(influence), _median(awareness), _median(dread), _median(landed),
			", ".join(RESPONSES.map(func(r: String) -> String: return "%s %.1f" % [r, _median(used[r])]))])
	print("total wins %d/%d" % [total_wins, seeds * STRATEGIES.size()])
	quit()


static func _median(values: Array[float]) -> float:
	var sorted := values.duplicate()
	sorted.sort()
	var half := sorted.size() / 2
	return sorted[half] if sorted.size() % 2 == 1 else (sorted[half - 1] + sorted[half]) / 2.0
