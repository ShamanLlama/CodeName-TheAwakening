extends SceneTree
## Layout regression check (debug builds, needs a window). Stages a scenario through the
## screenshot harness, then walks every region, tab, action explanation and dialog and reports
## anything that leaves the canvas or hides under the region file.
##   godot --path godot --resolution 1688x780 --script res://dev/layout_check.gd -- \
##     --shot=/tmp/unused.png --wait=120 --scenario=late --touch=1 --scale=2
## Exits 1 when something overflows.

var game
var bounds: Rect2
var failures: Array[String] = []


func _initialize() -> void:
	_run.call_deferred()


func _frames(count := 4) -> void:
	for i in count:
		await process_frame


func _fail(what: String) -> void:
	if not failures.has(what):
		failures.append(what)


func _within(node: Control, what: String) -> void:
	if not node.is_visible_in_tree():
		return
	var rect := node.get_global_rect()
	if rect.position.x < bounds.position.x - 1 or rect.end.x > bounds.end.x + 1 or rect.end.y > bounds.end.y + 1:
		_fail("%s outside the canvas: %s, canvas %s" % [what, rect, bounds.size])


func _run() -> void:
	game = load("res://main.tscn").instantiate()
	root.add_child(game)
	await _frames(40)
	var screen = game._screen
	bounds = game.get_global_rect()
	var crowded: bool = game._shot.has("crowd")
	var label := "%s %s scale %s%s%s" % [root.size, game._shot.get("scenario", "?"), game.settings.uiScale, " touch" if Kit.touch else "", " crowded" if crowded else ""]
	# --crowd=1: every held region under threat, an open note and a zoomed chart, the busiest
	# the overlays can get.
	if crowded and game.state != null and not GameEngine.is_over(game.state):
		for id in GameData.REGION_IDS:
			var r: Dictionary = game.state.regions[id]
			if r.population.followers > 0 and r.threat == null:
				r.threat = { "kind": ["investigation", "raid", "occult"][id.length() % 3], "dueMonth": game.state.elapsedMonths + 3, "severity": 1.0 }
		game.settings.hints = []
		screen.map.zoom_at(screen.map.size * 0.5, 1.6)
		screen._banner_text.visible = true
		screen.refresh()
		await _frames(8)
	if screen.has_method("select") and game.state != null:
		game.state.speed = 0
		screen.dialogs.close()
		await _frames()
		_check_bar(screen)
		_check_overlays(screen, "")
		for id in GameData.REGION_IDS:
			screen.select(id)
			await _frames(2)
			_check_overlays(screen, id)
			if game.state.status == "placing":
				_within(screen.panel, "file %s" % id)
				continue
			for group in screen.panel.GROUPS:
				screen.panel._set_group(group)
				await _frames(2)
				for action in screen.panel.GROUPS[group]:
					var row: Button = screen.panel._rows[action].button
					if not row.visible:
						continue
					if Kit.touch:
						screen.panel._detail_id = ""
						screen.panel._choose(action)
					else:
						screen.panel._detail_id = action
						screen.panel._refresh_detail()
					await _frames(2)
					_within(screen.panel, "file %s %s/%s" % [id, group, action])
					if screen.panel.get_combined_minimum_size().x > screen.panel.WIDTH + 1:
						_fail("file %s %s/%s wider than %d: %d" % [id, group, action, screen.panel.WIDTH, screen.panel.get_combined_minimum_size().x])
		screen.deselect()
		for kind in ["menu", "doctrine", "chronicle", "guide"] + (["alignment"] if game.state.status == "alignment" else []) + (["end"] if GameEngine.is_over(game.state) else []):
			screen.open_dialog(kind)
			await _frames(6)
			_within(screen.dialogs._box, "dialog %s" % kind)
			screen.dialogs.close()
	print(("ok    " if failures.is_empty() else "FAIL  ") + label)
	for line in failures:
		print("        " + line)
	quit(0 if failures.is_empty() else 1)


func _check_bar(screen) -> void:
	for node in screen._rite.get_parent().get_children():
		_within(node, "ledger item %s" % node.name)


func _check_overlays(screen, id: String) -> void:
	var shown: Array = []
	for pair in [[screen._banner, "timetable"], [screen._note, "note"], [screen._reset_view, "whole-world button"], [screen._threat_scroll, "threat clippings"]]:
		var node = pair[0]
		if node != null and is_instance_valid(node) and node.is_visible_in_tree() and node.get_global_rect().size.y > 0:
			shown.append(pair)
			_within(node, pair[1])
			if id and screen.panel.visible and node.get_global_rect().intersects(screen.panel.get_global_rect()):
				_fail("%s under the file of %s" % [pair[1], id])
	# No two overlays may cover each other.
	for i in shown.size():
		for j in range(i + 1, shown.size()):
			if shown[i][0].get_global_rect().intersects(shown[j][0].get_global_rect()):
				_fail("%s covers %s%s: %s / %s" % [shown[i][1], shown[j][1], " (file of %s open)" % id if id else "", shown[i][0].get_global_rect(), shown[j][0].get_global_rect()])
	var bar_top: float = bounds.end.y - screen.BOTTOM
	if screen._threat_scroll.get_global_rect().end.y > bar_top + 1:
		_fail("threat clippings reach the ledger")
