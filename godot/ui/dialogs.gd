extends Control
## Modal dialogs over the strategy screen, printed as framed sheets. The clock is held while
## one is open.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const PlaytestPanel = preload("res://ui/playtest_panel.gd")

var game
var screen
var kind := ""
var _box: PanelContainer
var _content: VBoxContainer
var _scroll: ScrollContainer
var _confirm_abandon := false


func _init(owner_game, owner_screen) -> void:
	game = owner_game
	screen = owner_screen


func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	var backdrop := ColorRect.new()
	backdrop.color = Color(P.DESK, 0.8)
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(backdrop)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(center)
	var pad := 22 if Kit.compact else 40
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
		"playtest": _playtest()
	var room := get_viewport_rect().size
	_box.custom_minimum_size.x = minf(_box.custom_minimum_size.x, room.x - 32)
	_fit_height.call_deferred()


## Sizes the scroll area to the sheet's content, up to the screen's height.
func _fit_height() -> void:
	_scroll.custom_minimum_size.y = 0
	await get_tree().process_frame
	if not is_inside_tree():
		return
	var pad: float = _box.get_theme_stylebox("panel").get_minimum_size().y
	var limit := get_viewport_rect().size.y - pad - 24
	_scroll.custom_minimum_size.y = minf(_content.get_combined_minimum_size().y, limit)
	_scroll.scroll_vertical = 0


func _title(text: String, color := P.TEXT) -> void:
	var title := Kit.vbox(4)
	title.add_child(Kit.label(text, 32 if Kit.compact else 40, color, "display"))
	title.add_child(Kit.rule(Color(color, 0.6)))
	_content.add_child(title)


func _row_buttons(buttons: Array) -> HBoxContainer:
	var row := Kit.hbox(10)
	for button in buttons:
		row.add_child(button)
	_content.add_child(row)
	return row


func _guide() -> void:
	_box.custom_minimum_size = Vector2(620, 0)
	_title("How the Awakening Works")
	var steps := [
		["GROW", "Cells recruit on their own and earn Influence. Spread into neighbouring regions."],
		["SACRIFICE", "Followers become Dread. Dread pays for the rites."],
		["BUILD THREE SITES", "A site needs a quarter of its region's people and 24 months, and costs %s Influence every month. Small regions are easier to hold." % Js.num(B.SITE_UPKEEP)],
		["STAY HIDDEN", "Threats strike your cells. Obstruct them, and conceal to lower world awareness; concealment costs more as awareness rises. Keep awareness under 100%."],
		["AWAKEN", "In September 1924, month 200, the stars align and the world hunts your sites. Spend 100 Dread and hold three sites for 12 months before September 1926."],
	]
	for i in steps.size():
		var row := Kit.hbox(16)
		var number := Kit.label(["I", "II", "III", "IV", "V"][i], 30, P.CULT, "display")
		number.custom_minimum_size = Vector2(44, 0)
		number.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		row.add_child(number)
		var text := Kit.vbox(0)
		text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		text.add_child(Kit.label(steps[i][0].capitalize(), 21, P.TEXT, "display_sc"))
		text.add_child(Kit.body(steps[i][1], 16, P.MUTED))
		row.add_child(text)
		_content.add_child(row)
	var go := Kit.primary("Understood", func() -> void:
		game.complete_tutorial()
		close())
	_row_buttons([go])


func _menu() -> void:
	_box.custom_minimum_size = Vector2(440, 0)
	_title("Paused")
	var column := Kit.vbox(8)
	_content.add_child(column)
	for entry in [["RESUME", close], ["HOW TO PLAY", func() -> void: open("guide")], ["CHRONICLE", func() -> void: open("chronicle")], ["GTC PLAYTEST LAB", func() -> void: open("playtest")], ["EXPORT PLAYTEST REPORT", func() -> void: game.export_report()]]:
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
	_title("Choose a Doctrine")
	_content.add_child(Kit.body("One per run, for %d Influence. The choice is permanent." % GameData.SPECIALIZATION_COST, 17, P.MUTED, "italic"))
	var row := Kit.hbox(12)
	_content.add_child(row)
	for id in GameData.SPECIALIZATION_IDS:
		var def: Dictionary = GameData.SPECIALIZATIONS[id]
		var chosen: bool = s.specialization == id
		var card := Kit.panel(P.PAPER_LIGHT, P.CULT if chosen else P.PAPER_EDGE, Vector4(18, 16, 18, 16))
		if chosen:
			card.get_theme_stylebox("panel").set_border_width_all(2)
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		card.custom_minimum_size = Vector2(0, 210)
		var box := Kit.vbox(10)
		card.add_child(box)
		box.add_child(Kit.label(def.name, 26, P.CULT if chosen else P.TEXT, "display_sc"))
		var text := Kit.body(def.description, 16, P.MUTED)
		text.size_flags_vertical = Control.SIZE_EXPAND_FILL
		box.add_child(text)
		var pick := Kit.primary("Chosen" if chosen else "Choose", func() -> void:
			game.choose_doctrine(id)
			close())
		pick.disabled = s.specialization != null or s.influence < GameData.SPECIALIZATION_COST or not GameEngine.is_active(s)
		box.add_child(pick)
		row.add_child(card)
	_row_buttons([Kit.button("CLOSE", close)])


func _chronicle() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(760, 0)
	_title("The Chronicle")
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
		var accent: Color = { "threat": P.DANGER, "loss": P.DANGER, "omen": P.OMEN, "site": P.CULT, "phase": P.CULT }.get(kind_of, Color(P.TEXT, 0.25))
		var row := Kit.hbox(14)
		var stripe := ColorRect.new()
		stripe.color = accent
		stripe.custom_minimum_size = Vector2(2, 0)
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
	_row_buttons([Kit.button("CLOSE", close)])


func _alignment() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(600, 0)
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
	var begin := Kit.primary("Begin the Final Rite · 100 Dread", func() -> void:
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
		_content.add_child(Kit.body(reason, 15, P.WARN, "body_bold"))


func _check(ok: bool, name: String, detail: String) -> HBoxContainer:
	var row := Kit.hbox(12)
	var mark := Kit.label("◆" if ok else "◇", 18, P.CULT if ok else P.FAINT)
	row.add_child(mark)
	var title := Kit.label(name, 20, P.TEXT if ok else P.MUTED, "display_sc")
	title.custom_minimum_size = Vector2(170, 0)
	row.add_child(title)
	row.add_child(Kit.label(detail, 16, P.MUTED, "italic"))
	return row


func _end() -> void:
	var s: Dictionary = game.state
	_box.custom_minimum_size = Vector2(620, 0)
	var won: bool = s.status == "won"
	var aware: bool = s.resultReason != null and s.resultReason.begins_with("The world knows")
	_title("Cthulhu Awakens" if won else "The World Knows" if aware else "The Alignment Has Passed", P.CULT if won else P.DANGER)
	_content.add_child(Kit.caption(Format.date(s.elapsedMonths)))
	_content.add_child(Kit.body(s.resultReason if s.resultReason else "", 17, P.TEXT, "italic"))
	var grid := GridContainer.new()
	grid.columns = 3
	grid.add_theme_constant_override("h_separation", 28)
	grid.add_theme_constant_override("v_separation", 12)
	var t: Dictionary = s.telemetry
	for item in [["Months", str(s.elapsedMonths)], ["Followers", Format.count(Derived.total_followers(s))], ["Sites held", "%d / 3" % Derived.ready_sites(s)],
		["Sacrificed", Format.count(t.sacrificed)], ["Lost to opposition", Format.count(t.raidLosses)], ["Threats obstructed", str(t.threatsCountered)]]:
		var cell := Kit.vbox(0)
		cell.add_child(Kit.caption(item[0]))
		cell.add_child(Kit.label(item[1], 25, P.TEXT, "body_bold"))
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
		_content.add_child(Kit.button("QUEUE COMMENT", func() -> void:
			if game.submit_playtest_comment(comment.text):
				comment.text = ""
				status.text = "Comment saved locally for delivery."
			else: status.text = "Comment could not be queued. Export the local record."))
		_content.add_child(status)
	_content.add_child(Kit.button("GTC PLAYTEST STATUS", func() -> void: open("playtest")))
	var again := Kit.primary("New Awakening", func() -> void:
		close()
		game.abandon())
	_row_buttons([again, Kit.button("EXPORT REPORT", func() -> void: game.export_report()), Kit.button("VIEW THE MAP", close)])


func _playtest() -> void:
	_box.custom_minimum_size = Vector2(650, 0)
	_title("GTC Playtest Lab")
	_content.add_child(PlaytestPanel.new(game))
	_row_buttons([Kit.button("CLOSE", close)])
