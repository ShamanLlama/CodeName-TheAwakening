extends "res://tests/test_case.gd"
## Readability on short screens and the touch flow for actions.


func test_compact_type_grows_small_sizes_only() -> void:
	Kit.compact = false
	check_eq(Kit.fs(12), 12, "desktop sizes are unchanged")
	Kit.compact = true
	check_eq(Kit.fs(12), 15, "captions reach the reading minimum")
	check_eq(Kit.fs(15), 17, "body text grows")
	check_eq(Kit.fs(36), 36, "display sizes stay")
	Kit.compact = false


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
	panel._rows["recruit"].button.pressed.emit()
	panel._set_group("RITES")
	check(panel._detail.get_parent() == panel._body, "changing tab returns the explanation to the file")
	panel.free()
	screen.free()
	game.free()
	Kit.touch = false


class FakeScreen extends Node:
	var TOP := 50.0
	var BOTTOM := 82.0
	func deselect() -> void:
		pass


func test_short_screen_map_magnifies_the_open_region_beside_its_file() -> void:
	var map := WorldMap.new()
	map.size = Vector2(924, 388)
	map._ready()
	var fit := map._zoom
	for id in GameData.REGION_IDS:
		map.focus(id, 414.0, 1.7)
		check(is_equal_approx(map._zoom, fit * 1.7), "%s is magnified" % id)
		var at := map.to_screen(Geo.anchor(id))
		check(at.x > 0.0 and at.x < 924.0 - 414.0 and at.y > 0.0 and at.y < 388.0, "%s stays clear of the file" % id)
	map.focus("")
	check(is_equal_approx(map._zoom, fit), "closing the file shows the whole world again")
	map.trim_poles = true
	map._layout()
	check(map._zoom > fit, "the trimmed overview reads larger")
	for id in GameData.REGION_IDS:
		var at := map.to_screen(Geo.anchor(id))
		check(at.y > 0.0 and at.y < 388.0, "%s stays on the trimmed overview" % id)
	map.free()
