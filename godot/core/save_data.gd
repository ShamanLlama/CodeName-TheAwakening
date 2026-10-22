class_name SaveData
## Save validation and storage. Mirrors src/game/persistence.ts, with LocalStorage replaced by
## a file in user:// (IndexedDB on the web build).
## Fixes the web build's rejection of runs lost to world awareness before the deadline.

const B = preload("res://core/balance.gd")
const SAVE_PATH := "user://save-v2.json"
const RECOVERY_PATH := "user://save-v2-recovery.json"
const SAVE_VERSION := 2


static func default_save() -> Dictionary:
	return { "version": 2, "state": null, "settings": { "muted": true, "volume": 40 }, "tutorialComplete": false, "lastResult": null }


static func _record(v) -> bool:
	return typeof(v) == TYPE_DICTIONARY


static func _number(v, maximum := 9007199254740991.0) -> bool:
	return (typeof(v) == TYPE_FLOAT or typeof(v) == TYPE_INT) and is_finite(v) and v >= 0 and v <= maximum


static func _integer(v, maximum := 9007199254740991.0) -> bool:
	return _number(v, maximum) and v == floor(v)


static func _one_of(v, choices: Array) -> bool:
	return typeof(v) == TYPE_STRING and choices.has(v)


static func _settings(v) -> bool:
	return _record(v) and typeof(v.get("muted")) == TYPE_BOOL and _number(v.get("volume"), 100)


static func is_game_state(v) -> bool:
	if not _record(v) or v.get("version") != 2 or v.get("ruleset") != "alignment-1" or typeof(v.get("seed")) != TYPE_STRING \
		or not _integer(v.get("rngState"), 4294967295) or not _integer(v.get("elapsedMonths"), B.DEADLINE) \
		or not _one_of(v.get("status"), GameData.STATUSES) or not _one_of(v.get("difficulty"), GameData.DIFFICULTY_IDS) \
		or not _integer(v.get("speed"), 2) or not _number(v.get("influence"), B.INFLUENCE_CAP) \
		or not _number(v.get("dread"), B.DREAD_CAP) or not _number(v.get("awareness"), 100) \
		or (v.get("specialization") != null and not _one_of(v.get("specialization"), GameData.SPECIALIZATION_IDS)) \
		or not _integer(v.get("finalProgress"), B.FINAL_MONTHS) or not _integer(v.get("nextLogId")) \
		or (v.get("resultReason") != null and typeof(v.get("resultReason")) != TYPE_STRING) or not _record(v.get("regions")):
		return false
	var status: String = v.status
	var month: float = v.elapsedMonths
	# A run is lost either at the deadline or earlier, when world awareness reaches 100.
	if status == "placing" and month != 0 or status == "playing" and month >= B.PREPARATION_MONTHS \
		or ["alignment", "awakening", "won"].has(status) and month < B.PREPARATION_MONTHS \
		or status == "won" and v.finalProgress != B.FINAL_MONTHS \
		or status == "lost" and month != B.DEADLINE and v.awareness < 100 \
		or ["won", "lost"].has(status) and v.speed != 0:
		return false
	for id in GameData.REGION_IDS:
		var r = v.regions.get(id)
		if not _record(r) or not _record(r.get("population")) or typeof(r.get("public")) != TYPE_BOOL or not _number(r.get("exposure"), 100) \
			or not _number(r.get("madness"), 100) or not _integer(r.get("backlashUntil")) or not _record(r.get("cooldowns")) or not _record(r.get("site")) \
			or typeof(r.site.get("invested")) != TYPE_BOOL or not _integer(r.site.get("progress"), B.SITE_MONTHS) \
			or not r.site.invested and r.site.progress != 0:
			return false
		var population: Dictionary = r.population
		var sum := 0.0
		for key in ["unaware", "deniers", "believers", "followers", "losses"]:
			if not _number(population.get(key)):
				return false
			sum += population[key]
		if absf(sum - GameData.REGIONS[id].populationWeight * B.POPULATION_SCALE) > 0.00001:
			return false
		for key in r.cooldowns:
			if not GameData.ACTION_IDS.has(key) or not _integer(r.cooldowns[key]):
				return false
		var threat = r.get("threat")
		if threat != null and (not _record(threat) or not _one_of(threat.get("kind"), GameData.THREAT_KINDS)
			or not _integer(threat.get("dueMonth")) or threat.dueMonth <= month or not _integer(threat.get("severity"), 3) or threat.severity < 1):
			return false
		var opportunity = r.get("opportunity")
		if opportunity != null and (not _record(opportunity) or not _one_of(opportunity.get("kind"), GameData.OPPORTUNITY_KINDS)
			or not _integer(opportunity.get("expiresMonth")) or opportunity.expiresMonth <= month):
			return false
		var figure = r.get("figure")
		if figure != null and (not _record(figure) or not _one_of(figure.get("kind"), ["celebrity", "politician"]) or typeof(figure.get("revealed")) != TYPE_BOOL):
			return false
	var log = v.get("log")
	if typeof(log) != TYPE_ARRAY or log.size() > 100:
		return false
	var last_id := 0.0
	for entry in log:
		if not _record(entry) or not _integer(entry.get("id")) or entry.id <= last_id or entry.id >= v.nextLogId \
			or not _integer(entry.get("month"), month) or typeof(entry.get("message")) != TYPE_STRING:
			return false
		last_id = entry.id
	var t = v.get("telemetry")
	if not _record(t) or not _record(t.get("actions")) or not _record(t.get("revelations")):
		return false
	for key in ["sacrificed", "raidLosses", "influenceEarned", "influenceSpent", "dreadEarned", "dreadSpent", "threatsCountered"]:
		if not _number(t.get(key)):
			return false
	for key in t.actions:
		if not GameData.ACTION_IDS.has(key) or not _integer(t.actions[key]):
			return false
	for key in t.revelations:
		if not GameData.REGION_IDS.has(key) or not _integer(t.revelations[key], month):
			return false
	return true


## JSON numbers load as floats. Restore the integer fields so month arithmetic and % work.
static func normalize_state(v: Dictionary) -> Dictionary:
	for key in ["version", "rngState", "elapsedMonths", "speed", "finalProgress", "nextLogId"]:
		v[key] = int(v[key])
	for key in ["influence", "dread", "awareness"]:
		v[key] = float(v[key])
	for id in GameData.REGION_IDS:
		var r: Dictionary = v.regions[id]
		r.backlashUntil = int(r.backlashUntil)
		r.site.progress = int(r.site.progress)
		r.exposure = float(r.exposure)
		r.madness = float(r.madness)
		for key in r.population:
			r.population[key] = float(r.population[key])
		for key in r.cooldowns:
			r.cooldowns[key] = int(r.cooldowns[key])
		if r.threat != null:
			r.threat.dueMonth = int(r.threat.dueMonth)
			r.threat.severity = int(r.threat.severity)
		if r.opportunity != null:
			r.opportunity.expiresMonth = int(r.opportunity.expiresMonth)
	for entry in v.log:
		entry.id = int(entry.id)
		entry.month = int(entry.month)
	var t: Dictionary = v.telemetry
	t.threatsCountered = int(t.threatsCountered)
	for key in t.actions:
		t.actions[key] = int(t.actions[key])
	for key in t.revelations:
		t.revelations[key] = int(t.revelations[key])
	return v


## Returns the decoded envelope, or null when the text is not a valid version-2 save.
static func decode_save(raw: String):
	var json := JSON.new()
	if json.parse(raw) != OK:
		return null
	var v = json.data
	if not _record(v) or v.get("version") != 2 or not _settings(v.get("settings")) or typeof(v.get("tutorialComplete")) != TYPE_BOOL \
		or (v.get("state") != null and not is_game_state(v.get("state"))):
		return null
	var r = v.get("lastResult")
	if r != null and (not _record(r) or typeof(r.get("won")) != TYPE_BOOL or not _integer(r.get("month"), B.DEADLINE)
		or not _number(r.get("followers"), 10000) or not _integer(r.get("sites"), 8) or typeof(r.get("seed")) != TYPE_STRING):
		return null
	v.version = 2
	v.settings.volume = float(v.settings.volume)
	if v.state != null:
		normalize_state(v.state)
	return v


static func load_run(path := SAVE_PATH) -> Dictionary:
	if not FileAccess.file_exists(path):
		return { "save": default_save(), "notice": "", "blocked": false }
	var raw := FileAccess.get_file_as_string(path)
	if raw.is_empty() and FileAccess.get_open_error() != OK:
		return { "save": default_save(), "notice": "Storage is unavailable. You can play, but this session will not save.", "blocked": true }
	var decoded = decode_save(raw)
	if decoded == null:
		return { "save": default_save(), "notice": "This save could not be read. Starting fresh will retain a recovery copy.", "blocked": true }
	return { "save": decoded, "notice": "", "blocked": false }


## Copies an unreadable save aside before a new run overwrites it.
static func retain_recovery(path := SAVE_PATH, recovery_path := RECOVERY_PATH) -> bool:
	if FileAccess.file_exists(path):
		var raw := FileAccess.get_file_as_string(path)
		if decode_save(raw) == null:
			var file := FileAccess.open(recovery_path, FileAccess.WRITE)
			if file == null:
				return false
			file.store_string(raw)
	return true


static func save_run(save: Dictionary, path := SAVE_PATH) -> bool:
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(save))
	return true


static func result_for(state: Dictionary) -> Dictionary:
	return { "won": state.status == "won", "month": state.elapsedMonths, "followers": Derived.total_followers(state), "sites": Derived.ready_sites(state), "seed": state.seed }


static func export_telemetry(state: Dictionary) -> String:
	var regions := {}
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		regions[id] = { "population": r.population, "public": r.public, "exposure": r.exposure, "site": r.site, "threat": r.threat }
	var report := {
		"ruleset": state.ruleset, "seed": state.seed, "difficulty": state.difficulty, "month": state.elapsedMonths,
		"status": state.status, "specialization": state.specialization, "resultReason": state.resultReason,
		"followers": Derived.total_followers(state), "readySites": Derived.ready_sites(state), "finalProgress": state.finalProgress,
		"resources": { "influence": state.influence, "dread": state.dread }, "awareness": state.awareness,
	}
	report.merge(state.telemetry)
	var log: Array = state.log
	report.logCoverage = { "retained": log.size(), "total": state.nextLogId - 1,
		"firstMonth": log[0].month if log else null, "lastMonth": log[-1].month if log else null, "truncated": state.nextLogId - 1 > log.size() }
	report.regions = regions
	report.log = log
	return JSON.stringify(report, "  ")
