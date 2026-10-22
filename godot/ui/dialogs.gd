extends Control
## Modal dialogs over the strategy screen. The clock is held while one is open.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")

var game
var screen
var kind := ""
var _box: PanelContainer
var _content: VBoxContainer
var _confirm_abandon := false


func _init(owner_game, owner_screen) -> void:
	game = owner_game
	screen = owner_screen


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	var backdrop := ColorRect.new()
	backdrop.color = Color(P.INK, 0.74)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(backdrop)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	_box = Kit.panel(P.PANEL_SOLID, P.EDGE_STRONG, Vector4(32, 28, 32, 28))
	center.add_child(_box)
	_content = Kit.vbox(16)
	_box.add_child(_content)
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
	game.release()


func _rebuild() -> void:
	Kit.clear(_content)
	_box.custom_minimum_size = Vector2(560, 0)
	match kind:
		"guide": _guide()
		"menu": _menu()
		"doctrine": _doctrine()
		"chronicle": _chronicle()
		"alignment": _alignment()
		"end": _end()


func _title(text: String, color := P.TEXT) -> void:
	_content.add_child(Kit.label(text, 34, color, "condensed_bold"))


func _row_buttons(buttons: Array) -> HBoxContainer:
	var row := Kit.hbox(10)
	for button in buttons:
		row.add_child(button)
	_content.add_child(row)
	return row


func _guide() -> void:
	_box.custom_minimum_size = Vector2(620, 0)
	_title("HOW THE AWAKENING WORKS")
	var steps := [
		["GROW", "Cells recruit on their own and earn Influence. Spread into neighbouring regions."],
		["SACRIFICE", "Followers become Dread. Dread pays for the rites."],
		["BUILD THREE SITES", "A site needs a quarter of its region's people and 24 months. Small regions are easier to hold."],
		["STAY HIDDEN", "Threats strike your cells. Obstruct them, conceal evidence, and keep world awareness under 100%."],
		["AWAKEN", "At month 200 the stars align. Spend 100 Dread and hold three sites for 12 months."],
	]
	for i in steps.size():
		var row := Kit.hbox(16)
		var number := Kit.label(str(i + 1), 34, P.CULT, "condensed_bold")
		number.custom_minimum_size = Vector2(28, 0)
		row.add_child(number)
		var text := Kit.vbox(0)
		text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		text.add_child(Kit.label(steps[i][0], 18, P.TEXT, "condensed_bold"))
		text.add_child(Kit.body(steps[i][1], 16, P.MUTED))
		row.add_child(text)
		_content.add_child(row)
	var go := Kit.primary("UNDERSTOOD", func() -> void:
		game.complete_tutorial()
		close())
	_row_buttons([go])


func _menu() -> void:
	_box.custom_minimum_size = Vector2(440, 0)
	_title("PAUSED")
	var column := Kit.vbox(8)
	_content.add_child(column)
	for entry in [["RESUME", close], ["HOW TO PLAY", func() -> void: open("guide")], ["CHRONICLE", func() -> void: open("chronicle")], ["EXPORT PLAYTEST REPORT", func() -> void: game.export_report()]]:
		var button := Kit.button(entry[0], entry[1])
		button.custom_minimum_size = Vector2(0, 42)
		column.add_child(button)
	column.add_child(Kit.rule())
	var sound := Kit.hbox(14)
	var toggle := Kit.button("SOUND OFF" if game.settings.muted else "SOUND ON")
	toggle.custom_minimum_size = Vector2(130, 38)
	var slider := HSlider.new()
	slider.min_value = 0
	slider.max_value = 100
	slider.value = game.settings.volume
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	toggle.pressed.connect(func() -> void:
		game.set_audio(not game.settings.muted, slider.value)
		toggle.text = "SOUND OFF" if game.settings.muted else "SOUND ON")
	slider.value_changed.connect(func(value: float) -> void: game.set_audio(game.settings.muted, value))
	slider.drag_ended.connect(func(_changed: bool) -> void: game.sfx.play("act"))
	sound.add_child(toggle)
	sound.add_child(slider)
	column.add_child(sound)
	column.add_child(Kit.rule())
	var abandon := Kit.outline("ABANDON THIS RUN", Callable(), P.DANGER)
	abandon.pressed.connect(func() -> void:
		if _confirm_abandon:
			close()
			game.abandon()
		else:
			_confirm_abandon = true
			abandon.text = "CLICK AGAIN TO ABANDON")
	column.add_child(abandon)
	if OS.is_debug_build():
		column.add_child(Kit.caption("Playtest controls · no resources granted"))
		var dev := Kit.hbox(6)
		for months in [1, 12, 120]:
			var button := Kit.button("+%d MO" % months, func() -> void:
				game.debug_advance(months)
				screen.refresh())
			button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			button.disabled = not GameEngine.is_active(game.state)
			dev.add_child(button)
		column.add_child(dev)


func _doctrine() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(860, 0)
	_title("CHOOSE A DOCTRINE")
	_content.add_child(Kit.body("One per run, for %d Influence. The choice is permanent." % GameData.SPECIALIZATION_COST, 16, P.MUTED))
	var row := Kit.hbox(12)
	_content.add_child(row)
	for id in GameData.SPECIALIZATION_IDS:
		var def: Dictionary = GameData.SPECIALIZATIONS[id]
		var chosen: bool = s.specialization == id
		var card := Kit.panel(P.CULT_DEEP if chosen else P.RAISED, P.CULT if chosen else P.EDGE, Vector4(18, 16, 18, 16))
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		card.custom_minimum_size = Vector2(0, 210)
		var box := Kit.vbox(10)
		card.add_child(box)
		box.add_child(Kit.label(def.name.to_upper(), 24, P.CULT if chosen else P.TEXT, "condensed_bold"))
		var text := Kit.body(def.description, 16, P.MUTED)
		text.size_flags_vertical = Control.SIZE_EXPAND_FILL
		box.add_child(text)
		var pick := Kit.primary("CHOSEN" if chosen else "CHOOSE", func() -> void:
			game.choose_doctrine(id)
			close())
		pick.disabled = s.specialization != null or s.influence < GameData.SPECIALIZATION_COST or not GameEngine.is_active(s)
		box.add_child(pick)
		row.add_child(card)
	_row_buttons([Kit.button("CLOSE", close)])


func _chronicle() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(760, 0)
	_title("CHRONICLE")
	_content.add_child(Kit.caption("Seed %s · %s · latest first" % [s.seed, GameData.DIFFICULTIES[s.difficulty].name]))
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(0, 470)
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var list := Kit.vbox(0)
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(list)
	_content.add_child(scroll)
	var entries: Array = s.log.duplicate()
	entries.reverse()
	for entry in entries:
		var kind_of: String = game.classify(entry.message)
		var accent: Color = { "threat": P.DANGER, "loss": P.DANGER, "omen": P.OMEN, "site": P.CULT, "phase": P.CULT }.get(kind_of, P.FAINT)
		var row := Kit.hbox(14)
		var stripe := ColorRect.new()
		stripe.color = accent
		stripe.custom_minimum_size = Vector2(2, 0)
		row.add_child(stripe)
		var when := Kit.label(Derived.calendar(entry.month).replace("Year ", "Y").replace(" · Month ", " · M"), 14, P.MUTED, "condensed_semibold")
		when.custom_minimum_size = Vector2(76, 0)
		row.add_child(when)
		var text := Kit.body(entry.message, 15, P.TEXT)
		text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		row.add_child(text)
		var pad := MarginContainer.new()
		pad.add_theme_constant_override("margin_top", 7)
		pad.add_theme_constant_override("margin_bottom", 7)
		pad.add_child(row)
		list.add_child(pad)
	if entries.is_empty():
		list.add_child(Kit.body("The world has not yet heard your name.", 16, P.MUTED))
	_row_buttons([Kit.button("CLOSE", close)])


func _alignment() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(600, 0)
	_title("THE STARS ARE RIGHT", P.CULT)
	_content.add_child(Kit.body("%d months remain to complete the awakening. Time is paused." % (B.DEADLINE - s.elapsedMonths), 17, P.TEXT))
	var checks := Kit.vbox(8)
	_content.add_child(checks)
	var sites := 0
	for id in GameData.REGION_IDS:
		var r: Dictionary = s.regions[id]
		if not r.site.invested:
			continue
		sites += 1
		var ready: bool = r.site.progress >= B.SITE_MONTHS and Derived.supported(r)
		checks.add_child(_check(ready, GameData.REGIONS[id].name, Derived.site_label(r)))
	for i in maxi(0, B.REQUIRED_SITES - sites):
		checks.add_child(_check(false, "Unfunded site", "Fund a site in a small region"))
	checks.add_child(_check(s.dread >= B.FINAL_DREAD, "Dread", "%s / %d" % [Format.count(s.dread), B.FINAL_DREAD]))
	var reason := GameEngine.awakening_reason(s)
	var begin := Kit.primary("BEGIN THE FINAL RITE · 100 DREAD", func() -> void:
		close()
		game.awaken()
		# The alignment paused the clock; the rite needs time to pass.
		if game.state.status == "awakening":
			game.set_speed(1))
	begin.disabled = reason != ""
	var later := Kit.button("NOT YET", close)
	later.custom_minimum_size = Vector2(0, 44)
	_row_buttons([begin, later])
	if reason:
		_content.add_child(Kit.body(reason, 15, P.WARN))


func _check(ok: bool, name: String, detail: String) -> HBoxContainer:
	var row := Kit.hbox(12)
	var mark := Kit.label("◆" if ok else "◇", 18, P.CULT if ok else P.FAINT)
	row.add_child(mark)
	var title := Kit.label(name.to_upper(), 17, P.TEXT if ok else P.MUTED, "condensed_semibold")
	title.custom_minimum_size = Vector2(170, 0)
	row.add_child(title)
	row.add_child(Kit.label(detail, 16, P.MUTED))
	return row


func _end() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(620, 0)
	var won: bool = s.status == "won"
	var aware: bool = s.resultReason != null and s.resultReason.begins_with("The world knows")
	_title("CTHULHU AWAKENS" if won else "THE WORLD KNOWS" if aware else "THE ALIGNMENT HAS PASSED", P.CULT if won else P.DANGER)
	_content.add_child(Kit.body(s.resultReason if s.resultReason else "", 17, P.TEXT))
	var grid := GridContainer.new()
	grid.columns = 3
	grid.add_theme_constant_override("h_separation", 28)
	grid.add_theme_constant_override("v_separation", 12)
	var t: Dictionary = s.telemetry
	for item in [["Month", str(s.elapsedMonths)], ["Followers", Format.count(Derived.total_followers(s))], ["Sites held", "%d / 3" % Derived.ready_sites(s)],
		["Sacrificed", Format.count(t.sacrificed)], ["Lost to opposition", Format.count(t.raidLosses)], ["Threats obstructed", str(t.threatsCountered)]]:
		var cell := Kit.vbox(0)
		cell.add_child(Kit.caption(item[0]))
		cell.add_child(Kit.label(item[1], 24, P.TEXT, "condensed_semibold"))
		grid.add_child(cell)
	_content.add_child(grid)
	_content.add_child(Kit.caption("Seed %s · %s" % [s.seed, GameData.DIFFICULTIES[s.difficulty].name]))
	var again := Kit.primary("NEW AWAKENING", func() -> void:
		close()
		game.abandon())
	_row_buttons([again, Kit.button("EXPORT REPORT", func() -> void: game.export_report()), Kit.button("VIEW THE MAP", close)])
