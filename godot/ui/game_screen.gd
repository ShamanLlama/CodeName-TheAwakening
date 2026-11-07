extends Control
## The strategy screen: a survey chart on a dark desk, between a slim top bar and the ledger,
## with press clippings for threats on the left, telegrams at the top, the selected region's
## case file on the right, a note for first-time situations, and dialogs.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const RegionPanel = preload("res://ui/region_panel.gd")
const Dialogs = preload("res://ui/dialogs.gd")
const Gauges = preload("res://ui/gauges.gd")
const Notes = preload("res://ui/notes.gd")
const LAMP_SHADER = preload("res://ui/lamp.gdshader")
# Bar heights; slimmer in the compact layout so the chart keeps its room.
var TOP := 48.0 if Kit.compact else 58.0
var BOTTOM := 86.0 if Kit.compact else 104.0

var game
var map: WorldMap
var panel: RegionPanel
var dialogs: Dialogs
var selected := ""

var _date: Label
var _countdown: Label
var _speed: Array[IconButton] = []
var _influence: Label
var _influence_rate: Label
var _dread: Label
var _followers: Label
var _doctrine: Button
var _doctrine_note: Label
var _ticker_box: Control
var _ticker_head: Label
var _ticker: Label
var _awareness_caption: Label
var _awareness: Gauges.Awareness
var _sites_caption: Label
var _sites: Gauges.Sites
var _rite: Button
var _threats: VBoxContainer
var _threat_scroll: ScrollContainer
var _noting := false
var _toasts: VBoxContainer
var _banner: PanelContainer
var _banner_head: Label
var _banner_text: Label
var _prompt: PanelContainer
var _note: PanelContainer
var _note_id := ""
var _note_at := 0
var _rite_bar: ProgressBar
var _reset_view: Button
var _rite_ready_styles := {}
var _last_status := ""


func _init(owner_game) -> void:
	game = owner_game
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = P.DESK
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(backdrop)
	map = WorldMap.new()
	map.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	map.offset_top = TOP + 8
	map.offset_bottom = -BOTTOM - 8
	add_child(map)
	map.region_pressed.connect(select)
	map.view_changed.connect(_on_view_changed)
	var lamp := ColorRect.new()
	lamp.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	lamp.mouse_filter = Control.MOUSE_FILTER_IGNORE
	lamp.material = ShaderMaterial.new()
	lamp.material.shader = LAMP_SHADER
	add_child(lamp)
	_build_top()
	_build_bottom()
	_build_overlays()
	panel = RegionPanel.new(game, self)
	add_child(panel)
	dialogs = Dialogs.new(game, self)
	add_child(dialogs)
	game.changed.connect(refresh)
	game.events.connect(_on_events)
	_last_status = game.state.status
	refresh()
	if not game.tutorial_complete:
		open_dialog("guide")
	elif game.state.status == "alignment":
		open_dialog("alignment")
	elif GameEngine.is_over(game.state):
		open_dialog("end")


func select(id: String) -> void:
	selected = id
	panel.show_region(id)
	# Keep the whole world visible beside the panel.
	map.offset_right = -(panel.WIDTH + 28)
	map.set_state(game.state, selected)
	_layout_overlays()


func deselect() -> void:
	selected = ""
	panel.show_region("")
	map.offset_right = 0
	map.set_state(game.state, selected)
	_layout_overlays()


func open_dialog(kind: String) -> void:
	dialogs.open(kind)


func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey and event.pressed and not event.echo) or dialogs.visible:
		return
	match event.keycode:
		KEY_ESCAPE:
			if selected:
				deselect()
			else:
				open_dialog("menu")
		KEY_TAB:
			_cycle_region(-1 if event.shift_pressed else 1)
		KEY_C:
			open_dialog("chronicle")
		KEY_D:
			if game.state.status != "placing":
				open_dialog("doctrine")
		KEY_H:
			open_dialog("guide")
		KEY_R:
			map.reset_view()
		KEY_N:
			if _note != null and is_instance_valid(_note):
				_dismiss_note()
			else:
				return
		_:
			if selected and panel.handle_key(event):
				pass
			else:
				return
	get_viewport().set_input_as_handled()


## Tab walks the regions in map order; the cult's own regions come first.
func _cycle_region(step: int) -> void:
	var s: Dictionary = game.state
	var ids: Array = GameData.REGION_IDS.duplicate()
	if s.status != "placing":
		var held: Array = ids.filter(func(id: String) -> bool: return s.regions[id].population.followers > 0)
		var rest: Array = ids.filter(func(id: String) -> bool: return s.regions[id].population.followers <= 0)
		ids = held + rest
	var at := ids.find(selected)
	select(ids[posmod(at + step, ids.size())] if at >= 0 else ids[0 if step > 0 else -1])


# --- Layout ------------------------------------------------------------------------------

## The top bar is the dark edge of the desk; the bottom one is a strip of ledger paper.
func _bar(top: bool) -> PanelContainer:
	var bar := Kit.panel()
	var side := 14 if Kit.compact else 20
	var style := UiTheme.box(P.DESK_RAISED if top else P.PAPER, P.BRASS if top else P.PAPER_EDGE, 0, Vector4(side, 0, side - 6, 0))
	if top:
		style.border_width_bottom = 2
		bar.material = null
	else:
		style.border_width_top = 3
		style.border_color = P.DESK_EDGE
	bar.add_theme_stylebox_override("panel", style)
	bar.set_anchors_preset(Control.PRESET_TOP_WIDE if top else Control.PRESET_BOTTOM_WIDE)
	if top:
		bar.offset_bottom = TOP
	else:
		bar.offset_top = -BOTTOM
	add_child(bar)
	return bar


func _build_top() -> void:
	var bar := _bar(true)
	var row := Kit.hbox(0)
	bar.add_child(row)
	var left := Kit.hbox(10)
	left.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left.alignment = BoxContainer.ALIGNMENT_BEGIN
	left.add_child(Kit.label("The Awakening", 22 if Kit.compact else 24, P.LAMP, "display_sc"))
	if not Kit.compact:
		left.add_child(Kit.label("of Cthulhu", 22, P.BRASS, "display_italic"))
	row.add_child(left)
	var center := Kit.vbox(-2)
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	_date = Kit.label("", 23 if Kit.compact else 25, P.LAMP, "display")
	_date.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_countdown = Kit.caption("", P.LAMP_DIM)
	_countdown.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	center.add_child(_date)
	center.add_child(_countdown)
	row.add_child(center)
	var right := Kit.hbox(2)
	right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	right.alignment = BoxContainer.ALIGNMENT_END
	for entry in [["pause", "Pause  (Space)", 0], ["play", "Normal speed  (1)", 1], ["fast", "Double speed  (2)", 2]]:
		var speed: int = entry[2]
		var button := IconButton.make(entry[0], entry[1], func() -> void: game.set_speed(speed))
		right.add_child(button)
		_speed.append(button)
	var gap := Control.new()
	gap.custom_minimum_size = Vector2(18, 0)
	right.add_child(gap)
	var menu := IconButton.make("menu", "Menu  (Esc)", func() -> void: open_dialog("menu"))
	right.add_child(menu)
	row.add_child(right)


func _readout(parent: Control, caption: String, color: Color, hint: String) -> Array[Label]:
	var box := Kit.vbox(0)
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	box.custom_minimum_size = Vector2(92 if Kit.compact else 118, 0)
	box.tooltip_text = hint
	box.mouse_filter = Control.MOUSE_FILTER_PASS
	box.add_child(Kit.caption(caption))
	var value := Kit.label("0", 26 if Kit.compact else 31, color, "body_bold")
	var sub := Kit.label("", 12, P.MUTED, "type")
	box.add_child(value)
	box.add_child(sub)
	parent.add_child(box)
	return [value, sub]


func _build_bottom() -> void:
	var bar := _bar(false)
	var row := Kit.hbox(14 if Kit.compact else 24)
	bar.add_child(row)
	var influence := _readout(row, "Influence", P.TEXT, "Earned each month by your followers, minus %s per funded site.\nSpent on every cult action. Keep a reserve for the alignment." % Js.num(B.SITE_UPKEEP))
	_influence = influence[0]
	_influence_rate = influence[1]
	_dread = _readout(row, "Dread", P.OMEN, "Power for the rites. Gained only by sacrifice and omens.\nSites cost 60, 100 and 160. The final rite costs 100.")[0]
	_followers = _readout(row, "Followers", P.CULT, "Living members of the cult, in every region.")[0]
	var doctrine := Kit.vbox(3)
	doctrine.alignment = BoxContainer.ALIGNMENT_CENTER
	doctrine.add_child(Kit.caption("Doctrine"))
	_doctrine = Kit.button("", func() -> void: open_dialog("doctrine"))
	_doctrine.custom_minimum_size = Vector2(124 if Kit.compact else 150, 0)
	_doctrine.add_theme_font_override("font", P.font("display_sc"))
	_doctrine.add_theme_font_size_override("font_size", 18)
	_doctrine.tooltip_text = "One permanent bonus per run.  (D)"
	doctrine.add_child(_doctrine)
	_doctrine_note = Kit.label("", 12, P.WARN, "type_bold")
	doctrine.add_child(_doctrine_note)
	row.add_child(doctrine)
	row.add_child(_divider())

	var ticker := Kit.vbox(3)
	_ticker_box = ticker
	ticker.alignment = BoxContainer.ALIGNMENT_CENTER
	ticker.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	ticker.mouse_filter = Control.MOUSE_FILTER_STOP
	ticker.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	ticker.tooltip_text = "Open the chronicle  (C)"
	ticker.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			open_dialog("chronicle"))
	_ticker_head = Kit.caption("")
	_ticker = Kit.body("", 16 if Kit.compact else 17, P.TEXT, "italic")
	_ticker.max_lines_visible = 2
	_ticker.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	ticker.add_child(_ticker_head)
	ticker.add_child(_ticker)
	row.add_child(ticker)
	row.add_child(_divider())
	# Short screens drop the ticker; the chronicle stays a key, a menu entry and the telegrams.
	if Kit.compact:
		ticker.visible = false
		row.get_child(-1).visible = false

	var awareness := Kit.vbox(2)
	awareness.alignment = BoxContainer.ALIGNMENT_CENTER
	awareness.tooltip_text = "How much of the world knows. It climbs with the share of humanity that follows you, with public worship and with ritual sites.\nIt slows income and recruitment. At 100% the run is lost."
	awareness.mouse_filter = Control.MOUSE_FILTER_PASS
	_awareness_caption = Kit.caption("")
	_awareness = Gauges.Awareness.new()
	_awareness.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	awareness.add_child(_awareness_caption)
	awareness.add_child(_awareness)
	row.add_child(awareness)

	var sites := Kit.vbox(6)
	sites.alignment = BoxContainer.ALIGNMENT_CENTER
	sites.mouse_filter = Control.MOUSE_FILTER_PASS
	_sites_caption = Kit.caption("")
	_sites = Gauges.Sites.new()
	sites.add_child(_sites_caption)
	sites.add_child(_sites)
	row.add_child(sites)

	_rite = Kit.primary("Final Rite", func() -> void: open_dialog("alignment"))
	_rite.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	for state in ["normal", "hover", "pressed"]:
		_rite_ready_styles[state] = _rite.get_theme_stylebox(state)
	row.add_child(_rite)


func _divider() -> ColorRect:
	var line := ColorRect.new()
	line.color = Color(P.TEXT, 0.35)
	line.custom_minimum_size = Vector2(1, 56 if Kit.compact else 64)
	line.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	return line


func _build_overlays() -> void:
	# Threat clippings scroll when they would reach the timetable or the ledger.
	_threat_scroll = ScrollContainer.new()
	_threat_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	_threat_scroll.mouse_filter = Control.MOUSE_FILTER_PASS
	add_child(_threat_scroll)
	_threats = Kit.vbox(8)
	_threats.custom_minimum_size = Vector2(250 if Kit.compact else 310, 0)
	_threat_scroll.add_child(_threats)

	_toasts = Kit.vbox(6)
	_toasts.set_anchors_preset(Control.PRESET_CENTER_TOP)
	_toasts.offset_top = TOP + 14
	_toasts.offset_left = -300
	_toasts.offset_right = 300
	_toasts.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_toasts)

	# The ritual timetable: a headline that names the problem, then the detail.
	_banner = Kit.panel(P.PAPER_LIGHT, P.WARN, Vector4(16, 9, 16, 10))
	_banner.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	_banner.offset_bottom = -BOTTOM - (10 if Kit.compact else 14)
	_banner.grow_vertical = Control.GROW_DIRECTION_BEGIN
	# On short screens only the headline shows; a click or tap unfolds the detail.
	_banner.mouse_filter = Control.MOUSE_FILTER_STOP if Kit.compact else Control.MOUSE_FILTER_IGNORE
	_banner.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			_banner_text.visible = not _banner_text.visible
			refresh()
			_layout_overlays.call_deferred())
	var banner := Kit.vbox(2)
	_banner_head = Kit.label("", 20, P.WARN, "display_sc")
	_banner_head.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_banner_text = Kit.body("", 14, P.TEXT)
	_banner_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_banner_text.visible = not Kit.compact
	banner.add_child(_banner_head)
	# During the final rite the timetable becomes a progress bar that stays on screen.
	_rite_bar = ProgressBar.new()
	_rite_bar.max_value = B.FINAL_MONTHS
	_rite_bar.show_percentage = false
	_rite_bar.custom_minimum_size = Vector2(0, 8)
	_rite_bar.visible = false
	banner.add_child(_rite_bar)
	banner.add_child(_banner_text)
	_banner.add_child(banner)
	add_child(_banner)
	_banner.resized.connect(func() -> void: _layout_overlays.call_deferred())

	_prompt = Kit.panel(P.PAPER, P.PAPER_EDGE, Vector4(26, 16, 26, 16))
	Kit.frame(_prompt, Color(P.CULT, 0.6), 5.0)
	_prompt.set_anchors_preset(Control.PRESET_CENTER_TOP)
	_prompt.offset_top = TOP + 22
	_prompt.offset_left = -290
	_prompt.offset_right = 290
	var prompt := Kit.vbox(0)
	var title := Kit.label("Choose where the dream takes root", 27, P.CULT, "display")
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	var sub := Kit.label("Pick a region on the map · small regions are easier to hold", 15, P.MUTED, "italic")
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt.add_child(title)
	prompt.add_child(sub)
	_prompt.add_child(prompt)
	add_child(_prompt)

	_reset_view = Kit.button("Whole world", func() -> void: map.reset_view())
	_reset_view.tooltip_text = "Show the whole chart  (R)"
	_reset_view.add_theme_font_size_override("font_size", 17)
	_reset_view.visible = false
	add_child(_reset_view)


## Overlays share the visible chart without covering one another or the region's file: the
## timetable at the bottom centre, the threat clippings down the left (scrolling above the
## timetable), the whole-world button at the top right, and a note at the lower left or, when
## that would cover the timetable or the clippings, at the top right below the button.
func _layout_overlays() -> void:
	if size.x <= 0 or _banner == null:
		return
	var gap := 10.0 if Kit.compact else 14.0
	var area := Rect2(gap, TOP + gap, size.x + map.offset_right - gap * 2.0, size.y - TOP - BOTTOM - gap * 2.0)
	# The timetable is centred over the chart, but moves right of the threat clippings when
	# both are shown; only if that leaves it too narrow do the clippings stop above it.
	var column_width := maxf(_threats.custom_minimum_size.x, _threats.get_combined_minimum_size().x) + 10.0
	var has_threats := _threats.get_child_count() > 0
	var left := area.position.x
	if has_threats:
		left = area.position.x + column_width + 8.0
	var width := minf(620.0, area.end.x - left)
	var centred := area.position.x + (area.size.x - minf(620.0, area.size.x)) * 0.5
	if not has_threats or width < 320.0:
		left = centred
		width = minf(620.0, area.size.x)
	else:
		left = maxf(left, centred)
		width = minf(620.0, area.end.x - left)
	_banner.offset_left = left
	_banner.offset_right = left + width
	# The banner's height follows its wrapped text; it re-runs this layout when it resizes.
	var banner_height := maxf(_banner.size.y, _banner.get_combined_minimum_size().y)
	var banner_rect := Rect2(left, area.end.y - banner_height, width, banner_height)
	_reset_view.size = Vector2.ZERO
	var button := _reset_view.get_combined_minimum_size()
	_reset_view.position = Vector2(area.end.x - button.x, area.position.y)
	var below_button := area.position.y + (button.y + 8.0 if _reset_view.visible else 0.0)
	var column := Rect2(area.position, Vector2(column_width, area.size.y))
	if _banner.visible and column.intersects(banner_rect):
		column.size.y = banner_rect.position.y - 8.0 - area.position.y
	_threat_scroll.position = column.position
	_threat_scroll.size = Vector2(column.size.x, minf(_threats.get_combined_minimum_size().y, maxf(0.0, column.size.y)))
	if _note == null or not is_instance_valid(_note):
		return
	var note_size := Vector2(_note.custom_minimum_size.x, _note.get_combined_minimum_size().y)
	var spot := Rect2(Vector2(area.position.x, area.end.y - note_size.y), note_size)
	var clippings := Rect2(_threat_scroll.position, _threat_scroll.size)
	var blocked := func(rect: Rect2) -> bool:
		return (_banner.visible and rect.intersects(banner_rect)) or (_threats.get_child_count() > 0 and rect.intersects(clippings)) \
			or rect.end.y > area.end.y + 1 or rect.position.x < area.position.x - 1
	if Kit.compact or blocked.call(spot):
		spot.position = Vector2(area.end.x - note_size.x, below_button)
	# With no free corner the note waits, still unread, until the chart has room for it.
	_note.visible = not blocked.call(spot)
	if _note.visible and not game.hint_seen(_note_id):
		_noting = true
		game.mark_hint(_note_id)
		_noting = false
	_note.set_anchors_preset(Control.PRESET_TOP_LEFT)
	_note.position = spot.position
	_note.size = Vector2(note_size.x, 0)


func _on_view_changed(free_view: bool) -> void:
	_reset_view.visible = free_view
	_layout_overlays()


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_layout_overlays.call_deferred()


# --- Refresh -----------------------------------------------------------------------------

func refresh() -> void:
	if game.state == null:
		return
	var s: Dictionary = game.state
	map.set_state(s, selected)
	var placing: bool = s.status == "placing"
	var over := GameEngine.is_over(s)
	var month: int = s.elapsedMonths
	_date.text = Format.date(month)
	match s.status:
		"placing":
			_countdown.text = "%d MONTHS TO THE ALIGNMENT" % B.PREPARATION_MONTHS
		"playing":
			_countdown.text = "%d MONTHS TO THE ALIGNMENT" % (B.PREPARATION_MONTHS - month)
		"alignment", "awakening":
			_countdown.text = "THE STARS ARE RIGHT · %d MONTHS LEFT" % (B.DEADLINE - month)
		_:
			_countdown.text = "CTHULHU AWAKENS" if s.status == "won" else "THE RUN HAS ENDED"
	# The deadline brightens as it nears: amber in the last three years, verdigris once aligned.
	var near: bool = s.status == "playing" and B.PREPARATION_MONTHS - month <= 36
	_countdown.add_theme_color_override("font_color", P.CULT_BRIGHT if s.status in ["alignment", "awakening", "won"] else P.WARN_BRIGHT if near else P.LAMP_DIM)
	if s.status == "awakening":
		_countdown.text = "FINAL RITE %d / %d · %d MONTHS LEFT" % [s.finalProgress, B.FINAL_MONTHS, B.DEADLINE - month]
	for i in _speed.size():
		_speed[i].active = s.speed == i and not placing and not over
		_speed[i].disabled = placing or over

	_influence.text = Format.count(s.influence)
	var upkeep := Derived.site_upkeep(s)
	var net := Derived.net_influence_per_month(s)
	if Kit.compact:
		_influence_rate.text = Format.rate(net) + " NET / MO"
	else:
		_influence_rate.text = Format.rate(net) + " / MONTH" + (" · SITES −" + Js.num(upkeep) if upkeep > 0 else "")
	_influence_rate.add_theme_color_override("font_color", P.DANGER if net < 0 else P.MUTED)
	_dread.text = Format.count(s.dread)
	_followers.text = Format.count(Derived.total_followers(s))
	_refresh_doctrine(s, placing, over)

	var last: Dictionary = s.log[-1] if s.log else {}
	_ticker_head.text = ("Chronicle · " + Format.date(last.month)).to_upper() if last else "CHRONICLE"
	_ticker.text = last.get("message", "The world sleeps. Choose where the dream begins.")

	_awareness.value = s.awareness
	_awareness_caption.text = ("AWARENESS  %d%%" if Kit.compact else "WORLD AWARENESS  %d%%") % floori(s.awareness)
	_awareness_caption.add_theme_color_override("font_color", P.DANGER if s.awareness >= 60 else P.MUTED)
	_awareness_caption.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

	var ready := Derived.ready_sites(s)
	_sites.state = s
	_sites.queue_redraw()
	if s.status == "awakening":
		_sites_caption.text = "FINAL RITE  %d / %d" % [s.finalProgress, B.FINAL_MONTHS]
	else:
		_sites_caption.text = "RITUAL SITES  %d / %d" % [ready, B.REQUIRED_SITES]
	var forecast := Forecast.ritual(s)
	_sites.get_parent().tooltip_text = forecast.message
	_refresh_rite(s)

	var rite: bool = s.status == "awakening"
	_banner.visible = (forecast.urgent or rite) and not placing and not over
	_banner_head.text = Notes.forecast_headline(s) + ("  · more" if Kit.compact and not _banner_text.visible else "")
	_banner_head.add_theme_color_override("font_color", P.CULT if rite and Derived.ready_sites(s) >= B.REQUIRED_SITES else P.WARN)
	_banner_text.text = forecast.message
	_rite_bar.visible = rite
	_rite_bar.value = s.finalProgress
	_prompt.visible = placing
	_rebuild_threats(s)
	panel.refresh()
	_refresh_note(s)
	_layout_overlays.call_deferred()

	if s.status != _last_status:
		_last_status = s.status
		if s.status == "alignment":
			open_dialog("alignment")
		elif GameEngine.is_over(s):
			open_dialog("end")


## The doctrine button always opens the choices, but says plainly when they cannot be bought.
func _refresh_doctrine(s: Dictionary, placing: bool, over: bool) -> void:
	_doctrine.disabled = placing or over
	_doctrine_note.text = ""
	if s.specialization != null:
		_doctrine.text = GameData.SPECIALIZATIONS[s.specialization].name
		_doctrine.add_theme_color_override("font_color", P.CULT)
		return
	_doctrine.text = "Choose · %d" % GameData.SPECIALIZATION_COST
	var short := GameData.SPECIALIZATION_COST - floori(s.influence)
	var affordable := short <= 0
	_doctrine.add_theme_color_override("font_color", P.TEXT if affordable else P.MUTED)
	if not affordable and not placing and not over:
		_doctrine_note.text = "%d MORE INFLUENCE" % short


## During the alignment the rite button stays visible; it reads as ready only when it is.
func _refresh_rite(s: Dictionary) -> void:
	_rite.visible = s.status == "alignment"
	var reason := GameEngine.awakening_reason(s)
	if reason:
		_rite.text = "Final Rite" if Kit.compact else "Final Rite · not ready"
		_rite.tooltip_text = reason
		for state in ["normal", "hover", "pressed"]:
			_rite.add_theme_stylebox_override(state, UiTheme.box(P.PAPER_SHADE if state == "normal" else P.PAPER_SHADE.darkened(0.08), P.PAPER_EDGE, 1, Vector4(18, 9, 18, 10)))
		for state in ["font_color", "font_hover_color", "font_pressed_color"]:
			_rite.add_theme_color_override(state, P.MUTED)
	else:
		_rite.text = "Begin Rite" if Kit.compact else "Begin the Final Rite"
		_rite.tooltip_text = "Ready. Spend 100 Dread and hold three sites for 12 months."
		for state in ["normal", "hover", "pressed"]:
			_rite.add_theme_stylebox_override(state, _rite_ready_styles[state])
		for state in ["font_color", "font_hover_color", "font_pressed_color"]:
			_rite.add_theme_color_override(state, P.PAPER_LIGHT)


func _rebuild_threats(s: Dictionary) -> void:
	Kit.clear(_threats)
	if GameEngine.is_over(s):
		return
	var ids: Array = GameData.REGION_IDS.filter(func(id: String) -> bool: return s.regions[id].threat != null)
	ids.sort_custom(func(a: String, b: String) -> bool: return s.regions[a].threat.dueMonth < s.regions[b].threat.dueMonth)
	for id in ids:
		_threats.add_child(_threat_card(s, id))


## A press clipping: the headline names the threat, the stamp obstructs it.
func _threat_card(s: Dictionary, id: String) -> Control:
	var r: Dictionary = s.regions[id]
	var threat: Dictionary = r.threat
	var months: int = threat.dueMonth - s.elapsedMonths
	var urgent := months <= 3
	var card := Kit.panel(P.PAPER_LIGHT, P.DANGER if urgent else P.PAPER_EDGE, Vector4(12, 9, 12, 10))
	if urgent:
		card.get_theme_stylebox("panel").set_border_width_all(2)
	card.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	card.tooltip_text = GameData.THREAT_NAMES[threat.kind] + ".\nIf ignored: " + Threats.describe(r)
	card.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			select(id))
	var row := Kit.hbox(10)
	card.add_child(row)
	var text := Kit.vbox(3)
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	text.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var kind_name := { "investigation": "Investigation", "raid": "Police Raid", "occult": "Occult Hunters" }
	text.add_child(Kit.label(kind_name[threat.kind], 19 if Kit.compact else 21, P.DANGER, "display_sc"))
	# Short screens keep the clipping to a headline; its consequences are in the region's file.
	if Kit.compact:
		text.add_child(Kit.caption(GameData.REGIONS[id].name, P.MUTED))
	else:
		text.add_child(Kit.caption(GameData.REGIONS[id].name + " · if ignored", P.MUTED))
		for effect in Notes.threat_effects(r):
			text.add_child(Kit.label(effect, 15, P.TEXT, "italic"))
	row.add_child(text)
	var right := Kit.vbox(0)
	right.alignment = BoxContainer.ALIGNMENT_BEGIN
	var due := Kit.label(str(months), 26 if Kit.compact else 30, P.DANGER, "body_bold")
	due.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	right.add_child(due)
	var unit := Kit.caption("months" if months != 1 else "month", P.DANGER)
	unit.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	right.add_child(unit)
	row.add_child(right)
	var preview := GameEngine.preview_action(s, "counter", id)
	var stamp_row := Kit.hbox(8)
	var counter := Kit.stamp("OBSTRUCT · %d" % preview.influence, func() -> void: game.act("counter", id), P.DANGER)
	counter.disabled = not preview.ok
	counter.tooltip_text = preview.reason if not preview.ok else "Burn local connections to stop it. They take 12 months to rebuild."
	if Kit.touch:
		counter.custom_minimum_size = Vector2(0, 40)
	stamp_row.add_child(counter)
	if not preview.ok:
		var why := Kit.label(Notes.short_reason(preview), 12, P.WARN, "type_bold")
		why.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		stamp_row.add_child(why)
	text.add_child(stamp_row)
	return card


func _on_events(entries: Array) -> void:
	for entry in entries:
		var kind: String = "refused" if entry.get("refused", false) else game.classify(entry.message)
		if kind in ["info", "faded"]:
			continue
		_toast(entry.message, kind)


## A telegram strip: typed capitals on yellowed paper, marked in the colour of its news.
func _toast(message: String, kind: String) -> void:
	var accent: Color = { "threat": P.DANGER, "loss": P.DANGER, "omen": P.OMEN, "site": P.CULT, "phase": P.CULT, "end": P.TEXT, "refused": P.WARN }.get(kind, P.MUTED)
	var toast := Kit.panel(Color("e3d5a6"), Color(P.PAPER_EDGE, 0.8), Vector4(12, 8, 14, 9))
	toast.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var row := Kit.hbox(12)
	var stripe := ColorRect.new()
	stripe.color = accent
	stripe.custom_minimum_size = Vector2(4, 0)
	row.add_child(stripe)
	var text := Kit.body(message.to_upper(), 14, P.TEXT, "type")
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(text)
	toast.add_child(row)
	_toasts.add_child(toast)
	while _toasts.get_child_count() > 3:
		_toasts.get_child(0).free()
	toast.modulate.a = 1.0 if Kit.reduced_motion else 0.0
	var tween := toast.create_tween()
	if not Kit.reduced_motion:
		tween.tween_property(toast, "modulate:a", 1.0, 0.18)
	tween.tween_interval(4.5 if kind != "refused" else 2.5)
	tween.tween_property(toast, "modulate:a", 0.0, 0.01 if Kit.reduced_motion else 0.5)
	tween.tween_callback(toast.queue_free)


# --- First-time notes --------------------------------------------------------------------

## One short note the first time a situation comes up, in the lower-left corner of the chart.
## It stays until dismissed and never pauses the clock.
func _refresh_note(s: Dictionary) -> void:
	# Marking a note as seen saves the game, which refreshes this screen; ignore that echo.
	if _noting:
		return
	var showing := _note != null and is_instance_valid(_note)
	var id := Notes.next_hint(s, func(h: String) -> bool: return game.hint_seen(h) or h == _note_id)
	if id == "":
		return
	# A note left open for a while gives way to the next one instead of holding it back.
	if showing:
		if Time.get_ticks_msec() - _note_at < 12000:
			return
		_note.queue_free()
		_note = null
	_noting = true
	_note_at = Time.get_ticks_msec()
	_note_id = id
	var hint: Array = Notes.HINTS[id]
	_note = Kit.panel(P.PAPER_LIGHT, P.CULT, Vector4(16, 12, 14, 12))
	_note.get_theme_stylebox("panel").border_width_left = 4
	_note.custom_minimum_size = Vector2(340 if Kit.compact else 380, 0)
	var box := Kit.vbox(4)
	_note.add_child(box)
	var head := Kit.hbox(8)
	var titles := Kit.vbox(0)
	titles.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	titles.add_child(Kit.caption("Note", P.CULT))
	titles.add_child(Kit.label(hint[0], 20, P.TEXT, "display_sc"))
	head.add_child(titles)
	var close := IconButton.make("close", "Dismiss", _dismiss_note, true)
	close.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	head.add_child(close)
	box.add_child(head)
	box.add_child(Kit.body(hint[1], 15, P.TEXT))
	var ok := Kit.link("GOT IT" if Kit.touch else "GOT IT  (N)", _dismiss_note)
	ok.add_theme_color_override("font_color", P.CULT)
	box.add_child(ok)
	add_child(_note)
	move_child(_note, panel.get_index())
	_noting = false
	_layout_overlays.call_deferred()
	if not Kit.reduced_motion:
		_note.modulate.a = 0.0
		_note.create_tween().tween_property(_note, "modulate:a", 1.0, 0.25)


func _dismiss_note() -> void:
	var id := _note_id
	if _note != null and is_instance_valid(_note):
		_note.queue_free()
	_note = null
	_note_id = ""
	if id:
		game.mark_hint(id)
	refresh.call_deferred()
	refresh.call_deferred()
