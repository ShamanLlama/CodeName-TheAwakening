extends Control
## The strategy screen: a full-bleed map between a slim top bar and a bottom HUD, with the
## threat stack on the left, toasts at the top, the selected region on the right and dialogs.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const RegionPanel = preload("res://ui/region_panel.gd")
const Dialogs = preload("res://ui/dialogs.gd")
const Gauges = preload("res://ui/gauges.gd")
const TOP := 58.0
const BOTTOM := 104.0

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
var _ticker_head: Label
var _ticker: Label
var _awareness_caption: Label
var _awareness: Gauges.Awareness
var _sites_caption: Label
var _sites: Gauges.Sites
var _rite: Button
var _threats: VBoxContainer
var _toasts: VBoxContainer
var _banner: PanelContainer
var _banner_text: Label
var _prompt: PanelContainer
var _last_status := ""


func _init(owner_game) -> void:
	game = owner_game
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = P.OCEAN
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	backdrop.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(backdrop)
	map = WorldMap.new()
	map.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	map.offset_top = TOP + 8
	map.offset_bottom = -BOTTOM - 8
	add_child(map)
	map.region_pressed.connect(select)
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


func deselect() -> void:
	selected = ""
	panel.show_region("")
	map.offset_right = 0
	map.set_state(game.state, selected)


func open_dialog(kind: String) -> void:
	dialogs.open(kind)


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		if selected:
			deselect()
		else:
			open_dialog("menu")
		get_viewport().set_input_as_handled()


# --- Layout ------------------------------------------------------------------------------

func _bar(top: bool) -> PanelContainer:
	var bar := Kit.panel(P.PANEL, Color.TRANSPARENT, Vector4(20, 0, 14, 0))
	var style := UiTheme.box(P.PANEL, P.EDGE, 0, Vector4(20, 0, 14, 0))
	if top:
		style.border_width_bottom = 1
	else:
		style.border_width_top = 1
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
	var mark := Kit.label("THE AWAKENING", 20, P.TEXT, "condensed_bold")
	left.add_child(mark)
	left.add_child(Kit.label("OF CTHULHU", 20, P.CULT, "condensed_bold"))
	row.add_child(left)
	var center := Kit.vbox(0)
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	_date = Kit.label("", 19, P.TEXT, "condensed_semibold")
	_date.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_countdown = Kit.caption("")
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
	right.add_child(IconButton.make("menu", "Menu  (Esc)", func() -> void: open_dialog("menu")))
	row.add_child(right)


func _readout(parent: Control, caption: String, color: Color, hint: String) -> Array[Label]:
	var box := Kit.vbox(0)
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	box.custom_minimum_size = Vector2(118, 0)
	box.tooltip_text = hint
	box.mouse_filter = Control.MOUSE_FILTER_PASS
	box.add_child(Kit.caption(caption))
	var value := Kit.label("0", 32, color, "condensed_bold")
	var sub := Kit.label("", 14, P.MUTED, "condensed")
	box.add_child(value)
	box.add_child(sub)
	parent.add_child(box)
	return [value, sub]


func _build_bottom() -> void:
	var bar := _bar(false)
	var row := Kit.hbox(26)
	bar.add_child(row)
	var influence := _readout(row, "Influence", P.TEXT, "Earned each month by your followers. Spent on every cult action.")
	_influence = influence[0]
	_influence_rate = influence[1]
	_dread = _readout(row, "Dread", P.OMEN, "Power for the rites. Gained only by sacrifice and omens.\nSites cost 60, 100 and 160. The final rite costs 100.")[0]
	_followers = _readout(row, "Followers", P.CULT, "Living members of the cult, in every region.")[0]
	var doctrine := Kit.vbox(4)
	doctrine.alignment = BoxContainer.ALIGNMENT_CENTER
	doctrine.add_child(Kit.caption("Doctrine"))
	_doctrine = Kit.button("", func() -> void: open_dialog("doctrine"))
	_doctrine.custom_minimum_size = Vector2(150, 0)
	doctrine.add_child(_doctrine)
	row.add_child(doctrine)
	row.add_child(_divider())

	var ticker := Kit.vbox(3)
	ticker.alignment = BoxContainer.ALIGNMENT_CENTER
	ticker.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	ticker.mouse_filter = Control.MOUSE_FILTER_STOP
	ticker.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	ticker.tooltip_text = "Open the chronicle"
	ticker.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			open_dialog("chronicle"))
	_ticker_head = Kit.caption("")
	_ticker = Kit.body("", 16, P.TEXT)
	_ticker.max_lines_visible = 2
	_ticker.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	ticker.add_child(_ticker_head)
	ticker.add_child(_ticker)
	row.add_child(ticker)
	row.add_child(_divider())

	var awareness := Kit.vbox(8)
	awareness.alignment = BoxContainer.ALIGNMENT_CENTER
	awareness.tooltip_text = "How much of the world knows. It climbs with the share of humanity that follows you, with public worship and with ritual sites.\nIt slows income and recruitment. At 100% the run is lost."
	awareness.mouse_filter = Control.MOUSE_FILTER_PASS
	_awareness_caption = Kit.caption("")
	_awareness = Gauges.Awareness.new()
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

	_rite = Kit.primary("BEGIN THE FINAL RITE", func() -> void: open_dialog("alignment"))
	_rite.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	row.add_child(_rite)


func _divider() -> ColorRect:
	var line := ColorRect.new()
	line.color = P.EDGE
	line.custom_minimum_size = Vector2(1, 56)
	line.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	return line


func _build_overlays() -> void:
	_threats = Kit.vbox(8)
	_threats.position = Vector2(16, TOP + 16)
	_threats.custom_minimum_size = Vector2(310, 0)
	add_child(_threats)

	_toasts = Kit.vbox(6)
	_toasts.set_anchors_preset(Control.PRESET_CENTER_TOP)
	_toasts.offset_top = TOP + 14
	_toasts.offset_left = -300
	_toasts.offset_right = 300
	_toasts.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_toasts)

	_banner = Kit.panel(Color(0.17, 0.11, 0.03, 0.92), Color(P.WARN, 0.6), Vector4(14, 8, 14, 8))
	_banner.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_banner.offset_bottom = -BOTTOM - 14
	_banner.offset_left = -330
	_banner.offset_right = 330
	_banner.grow_vertical = Control.GROW_DIRECTION_BEGIN
	_banner.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_banner_text = Kit.body("", 15, Color("ffd9a0"))
	_banner_text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_banner.add_child(_banner_text)
	add_child(_banner)

	_prompt = Kit.panel(P.PANEL, P.CULT_DIM, Vector4(22, 12, 22, 14))
	_prompt.set_anchors_preset(Control.PRESET_CENTER_TOP)
	_prompt.offset_top = TOP + 22
	_prompt.offset_left = -260
	_prompt.offset_right = 260
	var prompt := Kit.vbox(2)
	var title := Kit.label("CHOOSE WHERE THE DREAM TAKES ROOT", 22, P.CULT, "condensed_bold")
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	var sub := Kit.label("Pick a region on the map", 15, P.MUTED)
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	prompt.add_child(title)
	prompt.add_child(sub)
	_prompt.add_child(prompt)
	add_child(_prompt)


# --- Refresh -----------------------------------------------------------------------------

func refresh() -> void:
	if game.state == null:
		return
	var s: Dictionary = game.state
	map.set_state(s, selected)
	var placing: bool = s.status == "placing"
	var over := GameEngine.is_over(s)
	var month: int = s.elapsedMonths
	_date.text = Derived.calendar(month).to_upper()
	match s.status:
		"placing":
			_countdown.text = "%d MONTHS TO ALIGNMENT" % B.PREPARATION_MONTHS
		"playing":
			_countdown.text = "%d MONTHS TO ALIGNMENT" % (B.PREPARATION_MONTHS - month)
		"alignment", "awakening":
			_countdown.text = "THE STARS ARE RIGHT · %d MONTHS LEFT" % (B.DEADLINE - month)
		_:
			_countdown.text = "CTHULHU AWAKENS" if s.status == "won" else "THE RUN HAS ENDED"
	_countdown.add_theme_color_override("font_color", P.CULT if s.status in ["alignment", "awakening", "won"] else P.MUTED)
	for i in _speed.size():
		_speed[i].active = s.speed == i and not placing and not over
		_speed[i].disabled = placing or over

	_influence.text = Format.count(s.influence)
	_influence_rate.text = Format.rate(Derived.influence_per_month(s)) + " / MONTH"
	_dread.text = Format.count(s.dread)
	_followers.text = Format.count(Derived.total_followers(s))
	if s.specialization != null:
		_doctrine.text = GameData.SPECIALIZATIONS[s.specialization].name.to_upper()
	else:
		_doctrine.text = "CHOOSE · 40"
	_doctrine.disabled = placing or over

	var last: Dictionary = s.log[-1] if s.log else {}
	_ticker_head.text = ("CHRONICLE · " + Derived.calendar(last.month).to_upper()) if last else "CHRONICLE"
	_ticker.text = last.get("message", "The world sleeps. Choose where the dream begins.")

	_awareness.value = s.awareness
	_awareness_caption.text = "WORLD AWARENESS  %d%%" % floori(s.awareness)
	_awareness_caption.add_theme_color_override("font_color", P.DANGER if s.awareness >= 60 else P.MUTED)

	var ready := Derived.ready_sites(s)
	_sites.state = s
	_sites.queue_redraw()
	if s.status == "awakening":
		_sites_caption.text = "FINAL RITE  %d / %d" % [s.finalProgress, B.FINAL_MONTHS]
	else:
		_sites_caption.text = "RITUAL SITES  %d / %d" % [ready, B.REQUIRED_SITES]
	var forecast := Forecast.ritual(s)
	_sites.get_parent().tooltip_text = forecast.message
	_rite.visible = s.status == "alignment"
	var reason := GameEngine.awakening_reason(s)
	_rite.tooltip_text = reason if reason else "Ready. Spend 100 Dread and hold three sites for 12 months."

	_banner.visible = forecast.urgent and not placing and not over
	_banner_text.text = forecast.message
	_prompt.visible = placing
	_rebuild_threats(s)
	panel.refresh()

	if s.status != _last_status:
		if s.status == "alignment":
			open_dialog("alignment")
		elif GameEngine.is_over(s):
			open_dialog("end")
		_last_status = s.status


func _rebuild_threats(s: Dictionary) -> void:
	Kit.clear(_threats)
	if GameEngine.is_over(s):
		return
	var ids: Array = GameData.REGION_IDS.filter(func(id: String) -> bool: return s.regions[id].threat != null)
	ids.sort_custom(func(a: String, b: String) -> bool: return s.regions[a].threat.dueMonth < s.regions[b].threat.dueMonth)
	for id in ids:
		_threats.add_child(_threat_card(s, id))


func _threat_card(s: Dictionary, id: String) -> Control:
	var r: Dictionary = s.regions[id]
	var threat: Dictionary = r.threat
	var months: int = threat.dueMonth - s.elapsedMonths
	var urgent := months <= 3
	var accent := P.WARN if urgent else P.DANGER
	var card := Kit.panel(Color(0.09, 0.04, 0.04, 0.93), Color(accent, 0.45), Vector4(12, 9, 12, 10))
	card.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	card.tooltip_text = GameData.THREAT_NAMES[threat.kind] + ".\nIf ignored: " + Threats.describe(r)
	card.gui_input.connect(func(event: InputEvent) -> void:
		if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
			select(id))
	var row := Kit.hbox(10)
	card.add_child(row)
	var text := Kit.vbox(2)
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	text.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var kind_name := { "investigation": "INVESTIGATION", "raid": "RAID", "occult": "OCCULT HUNTERS" }
	text.add_child(Kit.label(kind_name[threat.kind] + " · " + GameData.REGIONS[id].name.to_upper(), 15, accent, "condensed_bold"))
	var c := Threats.consequences(r)
	var effects: Array[String] = ["−%d followers" % ceili(c.followers)]
	if c.preparationLost:
		effects.append("site −%d mo" % c.preparationLost)
	if c.destroysCell:
		effects.append("cell lost")
	elif c.interruptsSite:
		effects.append("rite broken")
	text.add_child(Kit.label(" · ".join(effects), 14, Color("e8c4bd")))
	row.add_child(text)
	var right := Kit.vbox(4)
	right.alignment = BoxContainer.ALIGNMENT_CENTER
	var due := Kit.label("%dM" % months, 22, accent, "condensed_bold")
	due.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	right.add_child(due)
	row.add_child(right)
	var preview := GameEngine.preview_action(s, "counter", id)
	var counter := Kit.outline("OBSTRUCT · %d" % preview.influence, func() -> void: game.act("counter", id), accent)
	counter.disabled = not preview.ok
	counter.tooltip_text = preview.reason if not preview.ok else "Burn local connections to stop it. They take 12 months to rebuild."
	counter.add_theme_font_size_override("font_size", 14)
	text.add_child(counter)
	counter.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	return card


func _on_events(entries: Array) -> void:
	for entry in entries:
		var kind: String = "refused" if entry.get("refused", false) else game.classify(entry.message)
		if kind in ["info", "faded"]:
			continue
		_toast(entry.message, kind)


func _toast(message: String, kind: String) -> void:
	var accent: Color = { "threat": P.DANGER, "loss": P.DANGER, "omen": P.OMEN, "site": P.CULT, "phase": P.CULT, "end": P.TEXT, "refused": P.WARN }.get(kind, P.MUTED)
	var toast := Kit.panel(Color(0.04, 0.05, 0.07, 0.95), Color(accent, 0.5), Vector4(14, 9, 14, 10))
	toast.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var row := Kit.hbox(12)
	var stripe := ColorRect.new()
	stripe.color = accent
	stripe.custom_minimum_size = Vector2(3, 0)
	row.add_child(stripe)
	var text := Kit.body(message, 15, P.TEXT)
	text.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(text)
	toast.add_child(row)
	_toasts.add_child(toast)
	while _toasts.get_child_count() > 3:
		_toasts.get_child(0).free()
	toast.modulate.a = 0.0
	var tween := toast.create_tween()
	tween.tween_property(toast, "modulate:a", 1.0, 0.18)
	tween.tween_interval(4.5 if kind != "refused" else 2.5)
	tween.tween_property(toast, "modulate:a", 0.0, 0.5)
	tween.tween_callback(toast.queue_free)
