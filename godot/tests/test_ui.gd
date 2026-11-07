extends "res://tests/test_case.gd"
## Interface behaviour that does not need a window: the touch flow for actions, why actions
## are unavailable, how a run ended, first-time notes, saved interface settings and the map.

const B = preload("res://core/balance.gd")
const Notes = preload("res://ui/notes.gd")


func test_touch_tap_explains_an_action_before_performing_it() -> void:
	Kit.touch = true
	var game = load("res://main.gd").new()
	game.state = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "touch"), "africa").state
	var screen := FakeScreen.new()
	var panel = load("res://ui/region_panel.gd").new(game, screen)
	panel._ready()
	panel.show_region("africa")
	var before: Dictionary = game.state
	var row: Button = panel._rows["recruit"].button
	row.pressed.emit()
	check(game.state == before, "a tap on the action does not perform it")
	check(panel._detail.get_parent() == panel._actions, "the explanation opens under the action")
	check_eq(panel._detail.get_index(), row.get_index() + 1, "directly under the tapped row")
	var buttons: Array = panel._detail.get_children().filter(func(node: Node) -> bool: return node is Button)
	check_eq(buttons.size(), 1, "the explanation offers one perform button")
	row.pressed.emit()
	check_eq(panel._detail.get_child_count(), 0, "tapping again closes it")
	var blocked: Button = panel._rows["counter"].button
	check(not blocked.disabled, "a blocked row still answers a tap on touch screens")
	blocked.pressed.emit()
	var perform: Array = panel._detail.get_children().filter(func(node: Node) -> bool: return node is Button)
	check(perform.size() == 1 and perform[0].disabled, "it explains itself and offers a disabled perform button")
	blocked.pressed.emit()
	panel._rows["recruit"].button.pressed.emit()
	panel._set_group("RITES")
	check(panel._detail.get_parent() == panel._body, "changing tab returns the explanation to the file")
	panel.free()
	screen.free()
	game.free()
	Kit.touch = false


func test_blocker_names_the_real_obstacle_before_the_price() -> void:
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "blocker"), "africa").state
	s.influence = 0.0
	var conceal := GameEngine.preview_action(s, "conceal", "europe")
	check(conceal.reason.begins_with("Need"), "the engine reports the price first")
	check_eq(Notes.blocker(s, "conceal", "europe", conceal).short, "NO CELL HERE", "no cell beats the price")
	var counter := GameEngine.preview_action(s, "counter", "africa")
	check_eq(Notes.blocker(s, "counter", "africa", counter).short, "NO THREAT", "nothing to obstruct")
	var expand := GameEngine.preview_action(s, "expand", "europe")
	check_eq(Notes.blocker(s, "expand", "europe", expand).short, "NEED 30 INF", "a neighbour waits only on Influence")
	var far := GameEngine.preview_action(s, "expand", "north_america")
	check_eq(Notes.blocker(s, "expand", "north_america", far).short, "NOT NEXT TO A CELL", "a distant region is out of reach")
	s.influence = 200.0
	check_eq(Notes.blocker(s, "recruit", "africa", GameEngine.preview_action(s, "recruit", "africa")).short, "", "an available action has no blocker")


func test_short_reasons_cover_cooldowns_and_costs() -> void:
	check_eq(Notes.short_reason({ "reason": "Available in 5 months." }), "IN 5 MO")
	check_eq(Notes.short_reason({ "reason": "Need 90 Influence." }), "NEED 90 INF")
	check_eq(Notes.short_reason({ "reason": "Need 100 Dread." }), "NEED 100 DREAD")
	check_eq(Notes.short_reason({ "reason": "Something new." }), "UNAVAILABLE")


func test_ending_explains_a_lost_run_and_a_victory() -> void:
	var lost := Playthrough.play("early", "rlyeh", B.DEADLINE)
	check_eq(lost.status, "lost", "the rlyeh seed runs out of time")
	var story := Notes.ending(lost)
	check(story.cause != "", "a loss names its cause")
	check(story.advice.size() >= 1 and story.advice.size() <= 2, "one or two pieces of advice")
	check(story.checks.any(func(c: Array) -> bool: return c[1] == "Final rite"), "the checklist includes the final rite")
	check_eq(story.checks.size(), B.REQUIRED_SITES + 1, "one line per required site and one for the rite")
	var won := Playthrough.play("early", "resume", B.DEADLINE)
	check_eq(won.status, "won", "the resume seed awakens Cthulhu")
	check(Notes.ending(won).advice.is_empty(), "a victory needs no advice")
	var aware: Dictionary = lost.duplicate(true)
	aware.awareness = 100.0
	check(Notes.ending(aware).cause.contains("awareness"), "an awareness loss says so")


func test_forecast_headline_names_the_problem() -> void:
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "headline"), "africa").state
	check_eq(Notes.forecast_headline(s), "Fund 3 more sites soon")
	s.elapsedMonths = B.LATEST_SITE_START + 1
	check_eq(Notes.forecast_headline(s), "Too late for new sites without help")


func test_first_time_notes_appear_once_each() -> void:
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "notes"), "africa").state
	var seen: Array[String] = []
	var was_seen := func(id: String) -> bool: return seen.has(id)
	check_eq(Notes.next_hint(GameEngine.create_game_state("normal", "notes"), was_seen), "", "nothing while placing")
	check_eq(Notes.next_hint(s, was_seen), "first_cell", "the first cell is explained first")
	seen.append("first_cell")
	s.awareness = 35.0
	check_eq(Notes.next_hint(s, was_seen), "awareness", "then whatever applies next")
	seen.append("awareness")
	check_eq(Notes.next_hint(s, was_seen), "", "no repeats")


func test_interface_settings_are_optional_and_validated() -> void:
	var old := { "version": 2, "state": null, "settings": { "muted": false, "volume": 40 }, "tutorialComplete": true, "lastResult": null }
	var decoded = SaveData.decode_save(JSON.stringify(old))
	check(decoded != null, "a 0.3.4 save still loads")
	var settings := SaveData.with_default_settings(decoded.settings)
	check_eq(settings.uiScale, 1.0, "default interface size")
	check_eq(settings.reducedMotion, false, "full motion by default")
	check_eq(settings.hints, [], "no notes seen")
	var bad := old.duplicate(true)
	bad.settings.uiScale = "huge"
	check(SaveData.decode_save(JSON.stringify(bad)) == null, "a malformed interface size is rejected")
	var good := old.duplicate(true)
	good.settings.merge({ "uiScale": 1.6, "reducedMotion": true, "hints": ["threat"] })
	check(SaveData.decode_save(JSON.stringify(good)) != null, "new settings round-trip")


func test_a_dormant_site_never_reads_as_ready() -> void:
	var r: Dictionary = GameEngine.create_game_state("normal", "dormant").regions["oceania"]
	Derived.recruit(r, Derived.site_requirement(r))
	r.site = { "invested": true, "progress": B.SITE_MONTHS, "dormant": false }
	check_eq(Notes.site_state(r), "ready")
	r.site.dormant = true
	check_eq(Notes.site_state(r), "dormant", "unpaid upkeep wins over a full preparation")
	r.population.followers = 0.0
	r.site.dormant = false
	check_eq(Notes.site_state(r), "unsupported")


func test_awakening_scenario_reaches_the_final_rite() -> void:
	check_eq(Playthrough.play("early", "resume", 206).status, "awakening", "the harness scenario shows the rite in progress")


func test_rates_read_to_one_decimal() -> void:
	check_eq(Format.rate(4.123), "+4.1")
	check_eq(Format.rate(12.46), "+12.5")
	check_eq(Format.rate(-1.26), "−1.3")
	check_eq(Format.rate(-0.01), "+0.0")


func test_region_outline_leaves_out_inner_borders() -> void:
	var map := WorldMap.new()
	var edges := 0
	for country in Geo.regions()["africa"]:
		for ring in country.rings:
			edges += ring.size()
	var outline: PackedVector2Array = map._outline("africa")
	check(outline.size() > 0, "Africa has an outline")
	check(outline.size() / 2 < edges * 0.75, "shared country borders are dropped (%d of %d edges kept)" % [outline.size() / 2, edges])
	map.free()


class FakeScreen extends Node:
	var TOP := 50.0
	var BOTTOM := 82.0
	func deselect() -> void:
		pass

