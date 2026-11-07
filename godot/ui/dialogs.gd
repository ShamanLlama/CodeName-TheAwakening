extends Control
## Modal dialogs over the strategy screen, printed as framed sheets. The clock is held while
## one is open. Buttons take keyboard focus (Tab, arrows, Enter); long sheets scroll.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const PlaytestPanel = preload("res://ui/playtest_panel.gd")
const Notes = preload("res://ui/notes.gd")
const KEYS := [["Space", "pause"], ["1  2", "speed"], ["Tab", "next region"], ["↑ ↓", "pick action"], ["Enter", "act"], ["← →", "file tab"],
	["C", "chronicle"], ["D", "doctrine"], ["R", "whole world"], ["N", "close note"], ["Wheel", "zoom"], ["Esc", "menu"]]

var game
var screen
var kind := ""
var _backdrop: ColorRect
var _box: PanelContainer
var _scroll: ScrollContainer
var _content: VBoxContainer
var _confirm_abandon := false


func _init(owner_game, owner_screen) -> void:
	game = owner_game
	screen = owner_screen


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	_backdrop = ColorRect.new()
	_backdrop.color = Color(P.DESK, 0.8)
	_backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(_backdrop)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	var pad := 24 if Kit.compact else 40
	_box = Kit.panel(P.PAPER, P.DESK_EDGE, Vector4(pad, pad * 0.85, pad, pad * 0.85))
	_box.material = P.paper(0.16)
	Kit.frame(_box)
	center.add_child(_box)
	# Long sheets scroll inside the frame instead of running off a short screen.
	_scroll = ScrollContainer.new()
	_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_box.add_child(_scroll)
	_content = Kit.vbox(12 if Kit.compact else 16)
	_content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_scroll.add_child(_content)
	visible = false


func _unhandled_input(event: InputEvent) -> void:
	if visible and event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		if kind != "guide":
			close()
		get_viewport().set_input_as_handled()


func open(next: String) -> void:
	if visible:
		close()
	kind = next
	_confirm_abandon = false
	_rebuild()
	visible = true
	game.hold()


func close() -> void:
	if not visible:
		return
	visible = false
	kind = ""
	Kit.clear(_content)
	_backdrop.color = Color(P.DESK, 0.8)
	game.release()


func _rebuild() -> void:
	Kit.clear(_content)
	_box.custom_minimum_size = Vector2(560, 0)
	_backdrop.color = Color(P.DESK, 0.8)
	match kind:
		"guide": _guide()
		"menu": _menu()
		"doctrine": _doctrine()
		"chronicle": _chronicle()
		"alignment": _alignment()
		"end": _end()
		"playtest": _playtest()
	var room := get_viewport_rect().size
	_box.custom_minimum_size.x = minf(_box.custom_minimum_size.x, room.x - 32)
	_fit_height.call_deferred()


## Sizes the scroll area to the sheet's content, up to the screen's height, then focuses the
## sheet's main button so the keyboard can act at once.
func _fit_height() -> void:
	_scroll.custom_minimum_size.y = 0
	await get_tree().process_frame
	if not is_inside_tree() or not visible:
		return
	var pad: float = _box.get_theme_stylebox("panel").get_minimum_size().y
	var limit := get_viewport_rect().size.y - pad - 24
	_scroll.custom_minimum_size.y = minf(_content.get_combined_minimum_size().y, limit)
	_scroll.scroll_vertical = 0
	_enable_focus(_content)
	var first := _first_focus(_content)
	if first != null:
		first.grab_focus()
	if kind == "end":
		_enter()


func _enable_focus(node: Node) -> void:
	for child in node.get_children():
		if child is BaseButton or child is HSlider:
			child.focus_mode = Control.FOCUS_ALL
		_enable_focus(child)


## The sheet's primary button if it is enabled, else its first enabled button.
func _first_focus(node: Node) -> Control:
	var buttons: Array[Button] = []
	_collect(node, buttons)
	for button in buttons:
		if not button.disabled and button.has_meta("primary"):
			return button
	for button in buttons:
		if not button.disabled:
			return button
	return null


func _collect(node: Node, into: Array[Button]) -> void:
	for child in node.get_children():
		if child is Button:
			into.append(child)
		_collect(child, into)


## The ending sheet rises into place; a victory takes its time.
func _enter() -> void:
	if Kit.reduced_motion:
		return
	var won: bool = game.state.status == "won"
	_box.pivot_offset = _box.size * 0.5
	_box.modulate.a = 0.0
	_box.scale = Vector2(0.96, 0.96)
	var tween := _box.create_tween().set_parallel()
	tween.tween_property(_box, "modulate:a", 1.0, 0.9 if won else 0.35)
	tween.tween_property(_box, "scale", Vector2.ONE, 0.9 if won else 0.35).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)


func _title(text: String, color := P.TEXT, size := 40) -> void:
	var title := Kit.vbox(4)
	title.add_child(Kit.label(text, roundi(size * 0.8) if Kit.compact else size, color, "display"))
	title.add_child(Kit.rule(Color(color, 0.6)))
	_content.add_child(title)


func _row_buttons(buttons: Array) -> HBoxContainer:
	var row := Kit.hbox(10)
	for button in buttons:
		row.add_child(button)
	_content.add_child(row)
	return row


## The main call to action on a sheet, focused when the sheet opens.
func _primary(text: String, on_press: Callable) -> Button:
	var button := Kit.primary(text, on_press)
	button.set_meta("primary", true)
	button.custom_minimum_size = Vector2(0, 46)
	return button


func _secondary(text: String, on_press: Callable) -> Button:
	var button := Kit.button(text, on_press)
	button.custom_minimum_size = Vector2(0, 46)
	return button


func _guide() -> void:
	_box.custom_minimum_size = Vector2(620, 0)
	_title("How the Awakening Works")
	var steps := [
		["Grow", "Cells recruit and earn Influence. Spread to neighbouring regions."],
		["Sacrifice", "Followers become Dread, which pays for the rites."],
		["Build three sites", "Each takes 24 months and needs a quarter of its region as followers."],
		["Stay hidden", "Obstruct threats and conceal evidence. At 100% world awareness the run is lost."],
		["Awaken", "From September 1924, spend 100 Dread and hold three sites for 12 months."],
	]
	for i in steps.size():
		var row := Kit.hbox(16)
		var number := Kit.label(["I", "II", "III", "IV", "V"][i], 28, P.CULT, "display")
		number.custom_minimum_size = Vector2(44, 0)
		number.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		row.add_child(number)
		var text := Kit.vbox(0)
		text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		text.add_child(Kit.label(steps[i][0], 21, P.TEXT, "display_sc"))
		text.add_child(Kit.body(steps[i][1], 16, P.MUTED))
		row.add_child(text)
		_content.add_child(row)
	_content.add_child(Kit.body("Time runs by itself; pause whenever you like, every action works while paused. Notes appear the first time something new happens.", 15, P.TEXT, "italic"))
	if not Kit.touch:
		_content.add_child(_keys())
	_row_buttons([_primary("Understood", func() -> void:
		game.complete_tutorial()
		close())])


func _menu() -> void:
	_box.custom_minimum_size = Vector2(480, 0)
	_title("Paused")
	var column := Kit.vbox(8)
	_content.add_child(column)
	var resume := _primary("Resume", close)
	column.add_child(resume)
	for entry in [["How to Play", func() -> void: open("guide")], ["Chronicle", func() -> void: open("chronicle")]]:
		column.add_child(_secondary(entry[0], entry[1]))
	column.add_child(Kit.rule())

	column.add_child(Kit.caption("Sound"))
	var sound := Kit.hbox(14)
	var toggle := Kit.button("Sound Off" if game.settings.muted else "Sound On")
	toggle.custom_minimum_size = Vector2(130, 40)
	var slider := HSlider.new()
	slider.min_value = 0
	slider.max_value = 100
	slider.value = game.settings.volume
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	toggle.pressed.connect(func() -> void:
		game.set_audio(not game.settings.muted, slider.value)
		toggle.text = "Sound Off" if game.settings.muted else "Sound On")
	slider.value_changed.connect(func(value: float) -> void: game.set_audio(game.settings.muted, value))
	slider.drag_ended.connect(func(_changed: bool) -> void: game.sfx.play("act"))
	sound.add_child(toggle)
	sound.add_child(slider)
	column.add_child(sound)

	column.add_child(Kit.caption("Interface size"))
	var sizes := Kit.hbox(0)
	var available: Array[float] = game.available_ui_scales()
	for scale in game.UI_SCALES:
		var option := Kit.button("%d%%" % roundi(scale * 100), func() -> void: game.set_ui_scale(scale))
		option.toggle_mode = true
		option.button_pressed = is_equal_approx(float(game.settings.uiScale), scale)
		option.disabled = not available.has(scale)
		option.tooltip_text = "" if available.has(scale) else "Too large for this window."
		option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		option.custom_minimum_size = Vector2(0, 40)
		sizes.add_child(option)
	column.add_child(sizes)

	column.add_child(Kit.caption("Motion"))
	var motion := Kit.hbox(0)
	for entry in [["Full", false], ["Reduced", true]]:
		var reduced: bool = entry[1]
		var option := Kit.button(entry[0])
		option.toggle_mode = true
		option.button_pressed = Kit.reduced_motion == reduced
		option.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		option.custom_minimum_size = Vector2(0, 40)
		option.pressed.connect(func() -> void:
			game.set_reduced_motion(reduced)
			for other in motion.get_children():
				other.set_pressed_no_signal(other == option))
		motion.add_child(option)
	column.add_child(motion)
	column.add_child(Kit.rule())

	var links := Kit.hbox(18)
	links.add_child(Kit.link("GTC PLAYTEST LAB", func() -> void: open("playtest")))
	links.add_child(Kit.link("EXPORT PLAYTEST REPORT", func() -> void: game.export_report()))
	column.add_child(links)
	var abandon := Kit.stamp("ABANDON THIS RUN", Callable(), P.DANGER)
	abandon.custom_minimum_size = Vector2(0, 38)
	abandon.pressed.connect(func() -> void:
		if _confirm_abandon:
			close()
			game.abandon()
		else:
			_confirm_abandon = true
			abandon.text = "CLICK AGAIN TO ABANDON")
	column.add_child(abandon)
	if not Kit.touch:
		column.add_child(_keys())
	if OS.is_debug_build():
		column.add_child(Kit.caption("Playtest controls · no resources granted"))
		var dev := Kit.hbox(6)
		for months in [1, 12, 120]:
			var button := Kit.button("+%d Mo" % months, func() -> void:
				game.debug_advance(months)
				screen.refresh())
			button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			button.disabled = not GameEngine.is_active(game.state)
			dev.add_child(button)
		column.add_child(dev)


## The keyboard and mouse controls, as a small ruled table.
func _keys() -> Control:
	var grid := GridContainer.new()
	grid.columns = 6
	grid.add_theme_constant_override("h_separation", 10)
	grid.add_theme_constant_override("v_separation", 3)
	for entry in KEYS:
		grid.add_child(Kit.label(entry[0], 13, P.TEXT, "type_bold"))
		grid.add_child(Kit.label(entry[1], 13, P.MUTED, "type"))
	return grid


func _doctrine() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(860, 0)
	_title("Choose a Doctrine")
	var cost: int = GameData.SPECIALIZATION_COST
	var short := cost - floori(s.influence)
	_content.add_child(Kit.body("One per run, for %d Influence. The choice is permanent." % cost, 17, P.MUTED, "italic"))
	if s.specialization == null and short > 0:
		_content.add_child(Kit.body("You have %d Influence; %d more to choose." % [floori(s.influence), short], 16, P.WARN, "body_bold"))
	var row := Kit.hbox(12)
	_content.add_child(row)
	for id in GameData.SPECIALIZATION_IDS:
		var def: Dictionary = GameData.SPECIALIZATIONS[id]
		var chosen: bool = s.specialization == id
		var card := Kit.panel(P.PAPER_LIGHT, P.CULT if chosen else P.PAPER_EDGE, Vector4(18, 16, 18, 16))
		if chosen:
			card.get_theme_stylebox("panel").set_border_width_all(2)
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		card.custom_minimum_size = Vector2(0, 180 if Kit.compact else 210)
		var box := Kit.vbox(10)
		card.add_child(box)
		box.add_child(Kit.label(def.name, 24 if Kit.compact else 26, P.CULT if chosen else P.TEXT, "display_sc"))
		var text := Kit.body(def.description, 16, P.MUTED)
		text.size_flags_vertical = Control.SIZE_EXPAND_FILL
		box.add_child(text)
		var label := "Chosen" if chosen else "Choose · %d" % cost
		var pick := Kit.primary(label, func() -> void:
			game.choose_doctrine(id)
			close())
		pick.disabled = s.specialization != null or s.influence < cost or not GameEngine.is_active(s)
		box.add_child(pick)
		row.add_child(card)
	if Kit.compact:
		row.add_theme_constant_override("separation", 8)
	_row_buttons([_secondary("Close", close)])


func _chronicle() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(760, 0)
	_title("The Chronicle")
	_content.add_child(Kit.caption("Seed %s · %s · latest first" % [s.seed, GameData.DIFFICULTIES[s.difficulty].name]))
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 240 if Kit.compact else 470)
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var list := Kit.vbox(0)
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(list)
	_content.add_child(scroll)
	var entries: Array = s.log.duplicate()
	entries.reverse()
	for entry in entries:
		var kind_of: String = game.classify(entry.message)
		var accent: Color = { "threat": P.DANGER, "loss": P.DANGER, "omen": P.OMEN, "site": P.CULT, "phase": P.CULT }.get(kind_of, Color(P.TEXT, 0.25))
		var row := Kit.hbox(14)
		var stripe := ColorRect.new()
		stripe.color = accent
		stripe.custom_minimum_size = Vector2(3, 0)
		row.add_child(stripe)
		var when := Kit.label(Format.date_short(entry.month), 13, P.MUTED, "type_bold")
		when.custom_minimum_size = Vector2(80, 0)
		row.add_child(when)
		var text := Kit.body(entry.message, 16, P.TEXT)
		text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(text)
		var pad := MarginContainer.new()
		pad.add_theme_constant_override("margin_top", 7)
		pad.add_theme_constant_override("margin_bottom", 7)
		pad.add_child(row)
		list.add_child(pad)
	if entries.is_empty():
		list.add_child(Kit.body("The world has not yet heard your name.", 17, P.MUTED, "italic"))
	_row_buttons([_secondary("Close", close)])


func _alignment() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(620, 0)
	_title("The Stars Are Right", P.CULT)
	_content.add_child(Kit.body("%d months remain to complete the awakening; the alignment closes in %s. Time is paused." % [B.DEADLINE - s.elapsedMonths, Format.date(B.DEADLINE)], 17, P.TEXT, "italic"))
	var checks := Kit.vbox(8)
	_content.add_child(checks)
	var sites := 0
	for id in GameData.REGION_IDS:
		var r: Dictionary = s.regions[id]
		if not r.site.invested:
			continue
		sites += 1
		var ready: bool = r.site.progress >= B.SITE_MONTHS and Derived.supported(r) and not r.site.dormant
		checks.add_child(_check(ready, GameData.REGIONS[id].name, Derived.site_label(r)))
	for i in maxi(0, B.REQUIRED_SITES - sites):
		checks.add_child(_check(false, "Unfunded site", "Fund a site in a small region"))
	checks.add_child(_check(s.dread >= B.FINAL_DREAD, "Dread", "%s / %d" % [Format.count(s.dread), B.FINAL_DREAD]))
	var reason := GameEngine.awakening_reason(s)
	if reason:
		_content.add_child(Kit.body(reason, 16, P.WARN, "body_bold"))
	var begin := _primary("Begin the Final Rite · 100 Dread", func() -> void:
		close()
		game.awaken()
		# The alignment paused the clock; the rite needs time to pass.
		if game.state.status == "awakening":
			game.set_speed(1))
	begin.disabled = reason != ""
	_row_buttons([begin, _secondary("Not Yet", close)])


## A checklist line. The mark differs in shape as well as colour.
func _check(ok: bool, name: String, detail: String) -> HBoxContainer:
	var row := Kit.hbox(12)
	var mark := Kit.label("◆" if ok else "×", 20 if not ok else 18, P.CULT if ok else P.DANGER, "body_bold")
	mark.custom_minimum_size = Vector2(18, 0)
	row.add_child(mark)
	var title := Kit.label(name, 20, P.TEXT, "display_sc")
	title.custom_minimum_size = Vector2(170, 0)
	row.add_child(title)
	var text := Kit.body(detail, 16, P.TEXT if ok else P.DANGER, "italic")
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(text)
	return row


func _end() -> void:
	var s: Dictionary = game.state
	var won: bool = s.status == "won"
	var aware: bool = s.resultReason != null and s.resultReason.begins_with("The world knows")
	var story := Notes.ending(s)
	_box.custom_minimum_size = Vector2(700 if won else 660, 0)
	if won:
		_backdrop.color = Color(P.OMEN_DEEP, 0.9)
		_content.add_child(Kit.caption(Format.date(s.elapsedMonths) + " · R'lyeh rises", P.CULT))
		_title("Cthulhu Awakens", P.CULT, 60)
		var verse := Kit.body("That is not dead which can eternal lie,\nAnd with strange aeons even death may die.", 20, P.MUTED, "display_italic")
		_content.add_child(verse)
		_content.add_child(Kit.body(story.cause + " The dreamer wakes.", 17, P.TEXT, "italic"))
	else:
		_title("The World Knows" if aware else "The Alignment Has Passed", P.DANGER)
		_content.add_child(Kit.caption(Format.date(s.elapsedMonths)))
		_content.add_child(Kit.body(story.cause, 18, P.TEXT, "body_bold"))
		if not story.checks.is_empty():
			var checks := Kit.vbox(6)
			for check in story.checks:
				checks.add_child(_check(check[0], check[1], check[2]))
			_content.add_child(checks)
		if not story.advice.is_empty():
			_content.add_child(Kit.caption("Next time", P.CULT))
			for line in story.advice:
				var row := Kit.hbox(10)
				var dash := Kit.label("—", 16, P.CULT)
				dash.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
				row.add_child(dash)
				var text := Kit.body(line, 16, P.TEXT)
				text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
				row.add_child(text)
				_content.add_child(row)
	_content.add_child(Kit.rule(Color(P.TEXT, 0.3), false))
	var grid := GridContainer.new()
	grid.columns = 6 if not Kit.compact else 3
	grid.add_theme_constant_override("h_separation", 22)
	grid.add_theme_constant_override("v_separation", 10)
	var t: Dictionary = s.telemetry
	for item in [["Months", str(s.elapsedMonths)], ["Followers", Format.count(Derived.total_followers(s))], ["Sites held", "%d / 3" % Derived.ready_sites(s)],
		["Sacrificed", Format.count(t.sacrificed)], ["Lost to threats", Format.count(t.raidLosses)], ["Obstructed", str(t.threatsCountered)]]:
		var cell := Kit.vbox(0)
		cell.add_child(Kit.caption(item[0]))
		cell.add_child(Kit.label(item[1], 22, P.TEXT, "body_bold"))
		grid.add_child(cell)
	_content.add_child(grid)
	_content.add_child(Kit.caption("Seed %s · %s" % [s.seed, GameData.DIFFICULTIES[s.difficulty].name]))
	if game.lab.confirmed and game.lab.active().get("closed", false):
		_content.add_child(Kit.body("Optional comment to GTC (up to 1,000 characters). Please omit personal details.", 15, P.MUTED))
		var comment := LineEdit.new()
		comment.max_length = 1000
		comment.placeholder_text = "What stood out during this run?"
		_content.add_child(comment)
		var status := Kit.caption("")
		_content.add_child(_secondary("Queue Comment", func() -> void:
			if game.submit_playtest_comment(comment.text):
				comment.text = ""
				status.text = "Comment saved locally for delivery."
			else: status.text = "Comment could not be queued. Export the local record."))
		_content.add_child(status)
	var again := _primary("New Awakening", func() -> void:
		close()
		game.abandon())
	_row_buttons([again, _secondary("View the Map", close)])
	var links := Kit.hbox(18)
	links.add_child(Kit.link("EXPORT REPORT", func() -> void: game.export_report()))
	links.add_child(Kit.link("GTC PLAYTEST STATUS", func() -> void: open("playtest")))
	_content.add_child(links)


func _playtest() -> void:
	_box.custom_minimum_size = Vector2(650, 0)
	_title("GTC Playtest Lab")
	_content.add_child(PlaytestPanel.new(game))
	_row_buttons([_secondary("Close", close)])
