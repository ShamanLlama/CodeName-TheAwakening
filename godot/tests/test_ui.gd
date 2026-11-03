extends "res://tests/test_case.gd"
## The touch flow for actions.


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

