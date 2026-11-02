extends "res://tests/test_case.gd"
## Save validation and storage, ported from src/game/persistence.test.ts.

const TEST_SAVE := "user://test-save.json"
const TEST_RECOVERY := "user://test-save-recovery.json"


static func envelope(state) -> String:
	var save := SaveData.default_save()
	save.state = state
	return JSON.stringify(save)


func test_awareness_loss_survives_reload() -> void:
	# The web build rejected this save, discarding the whole envelope on reload.
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "x"), "africa").state
	s.awareness = 99.99
	var lost := GameEngine.advance_simulation(s, 1)
	check_eq(lost.status, "lost")
	check(lost.elapsedMonths < Balance.DEADLINE, "lost before the deadline")
	var decoded = SaveData.decode_save(envelope(lost))
	check(decoded != null, "save should reload")
	if decoded != null:
		check_eq(decoded.state, lost)


func test_rejects_corrupt_and_impossible_saves() -> void:
	check(SaveData.decode_save("{not json") == null, "malformed JSON")
	check(SaveData.decode_save("[]") == null, "wrong shape")
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "x"), "africa").state
	var created := s.duplicate(true)
	created.regions.africa.population.followers += 1
	check(SaveData.decode_save(envelope(created)) == null, "population that does not sum to capacity")
	var early_loss := s.duplicate(true)
	early_loss.status = "lost"
	early_loss.speed = 0
	check(SaveData.decode_save(envelope(early_loss)) == null, "lost early with low awareness")


func test_decoded_integers_support_month_arithmetic() -> void:
	var s: Dictionary = GameEngine.advance_simulation(GameEngine.place_starting_foothold(GameEngine.create_game_state("hard", "ints"), "europe").state, 30)
	var decoded = SaveData.decode_save(envelope(s))
	check_eq(typeof(decoded.state.elapsedMonths), TYPE_INT)
	check_eq(GameEngine.advance_simulation(decoded.state, 40), GameEngine.advance_simulation(s, 40), "continuation after reload")


func test_files_round_trip_and_keep_a_recovery_copy() -> void:
	var save := SaveData.default_save()
	save.state = GameEngine.create_game_state("easy", "file")
	check(SaveData.save_run(save, TEST_SAVE), "save written")
	var loaded := SaveData.load_run(TEST_SAVE)
	check(not loaded.blocked, "readable save")
	check_eq(loaded.save.state, save.state)
	var file := FileAccess.open(TEST_SAVE, FileAccess.WRITE)
	file.store_string("{broken")
	file.close()
	check(SaveData.load_run(TEST_SAVE).blocked, "corrupt save blocks autosave")
	check(SaveData.retain_recovery(TEST_SAVE, TEST_RECOVERY), "recovery written")
	check_eq(FileAccess.get_file_as_string(TEST_RECOVERY), "{broken")
	DirAccess.remove_absolute(TEST_SAVE)
	DirAccess.remove_absolute(TEST_RECOVERY)
	check(not SaveData.load_run(TEST_SAVE).blocked, "missing save starts fresh")


func test_retired_ruleset_run_is_dropped_but_the_save_is_kept() -> void:
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "old"), "africa").state
	s.ruleset = "alignment-1"
	var save := SaveData.default_save()
	save.state = s
	save.settings.volume = 70
	save.tutorialComplete = true
	var decoded = SaveData.decode_save(JSON.stringify(save))
	check(decoded != null, "envelope still loads")
	if decoded == null:
		return
	check(decoded.state == null, "retired run cannot continue")
	check_eq(decoded.settings.volume, 70.0)
	check(decoded.tutorialComplete, "tutorial flag kept")
	var notice := SaveData.take_retired_notice(decoded)
	check(notice.contains("alignment-1") and notice.contains(Balance.RULESET), notice)
	check(not decoded.has("retiredRuleset"), "marker removed before saving")
	s.ruleset = "unknown-9"
	save.state = s
	check(SaveData.decode_save(JSON.stringify(save)) == null, "unknown rulesets stay unreadable")
