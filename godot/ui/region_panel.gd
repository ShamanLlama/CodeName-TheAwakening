extends PanelContainer
## The selected region's case file: its people, what threatens it, and what the cult can do
## there. Explanations live in the detail box under the actions, not under every button.
## With a mouse, hovering an action explains it and clicking performs it. On touch screens a
## tap only opens the explanation under that action, with a button to perform it, and the
## file scrolls by dragging. Actions come before the statistics so their text stays in view.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const GROUPS := {
	"CULT": ["recruit", "expand", "reveal", "hide", "conceal", "counter"],
	"RITES": ["sacrifice", "manifest", "prepare"],
	"OMENS": ["exploit", "convert", "reveal_figure"],
}
const WIDTH := 392.0

var game
var screen
var region := ""
var group := "CULT"

var _body: VBoxContainer
var _file: Label
var _name: Label
var _stance: Label
var _stats: GridContainer
var _population: PopulationBar
var _legend: HFlowContainer
var _cards: VBoxContainer
var _profile: VBoxContainer
var _tabs: HBoxContainer
var _actions: VBoxContainer
var _rows: Dictionary = {}
var _detail: VBoxContainer
var _detail_id := ""
var _scroll: ScrollContainer
var _detail_shown := ""


func _init(owner_game, owner_screen) -> void:
	game = owner_game
	screen = owner_screen


func _ready() -> void:
	add_theme_stylebox_override("panel", UiTheme.box(P.PAPER, P.DESK_EDGE, 1, Vector4(0, 0, 0, 0)))
	material = P.paper(0.14)
	anchor_left = 1.0
	anchor_right = 1.0
	anchor_bottom = 1.0
	offset_left = -WIDTH - 14
	offset_right = -14
	offset_top = screen.TOP + 14
	offset_bottom = -screen.BOTTOM - 14
	_scroll = ScrollContainer.new()
	_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(_scroll)
	var margin := MarginContainer.new()
	margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for side in ["left", "right", "top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 18)
	_scroll.add_child(margin)
	_body = Kit.vbox(14)
	margin.add_child(_body)

	var head := Kit.hbox(8)
	var titles := Kit.vbox(0)
	titles.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_file = Kit.caption("", P.FAINT)
	_name = Kit.label("", 36, P.TEXT, "display")
	_stance = Kit.caption("")
	titles.add_child(_file)
	titles.add_child(_name)
	titles.add_child(_stance)
	head.add_child(titles)
	var close := IconButton.make("close", "Close  (Esc)", func() -> void: screen.deselect(), true)
	close.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	head.add_child(close)
	_body.add_child(head)
	_body.add_child(Kit.rule())

	_stats = GridContainer.new()
	_stats.columns = 3
	_stats.add_theme_constant_override("h_separation", 18)
	_stats.add_theme_constant_override("v_separation", 10)
	_body.add_child(_stats)

	var people := Kit.vbox(6)
	people.add_child(Kit.caption("People"))
	_population = PopulationBar.new()
	people.add_child(_population)
	_legend = HFlowContainer.new()
	_legend.add_theme_constant_override("h_separation", 12)
	people.add_child(_legend)
	_body.add_child(people)

	_cards = Kit.vbox(10)
	_body.add_child(_cards)
	_profile = Kit.vbox(10)
	_body.add_child(_profile)

	# Index tabs: the open one is the same paper as the file, the others sit behind it.
	_tabs = Kit.hbox(3)
	var pad := Vector4(8, 5, 8, 5)
	var open := UiTheme.box(P.PAPER, P.PAPER_EDGE, 0, pad)
	open.border_width_top = 2
	open.border_width_left = 1
	open.border_width_right = 1
	open.border_color = P.TEXT
	var behind := UiTheme.box(P.PAPER_SHADE, P.PAPER_EDGE, 0, pad)
	behind.border_width_bottom = 1
	behind.border_width_top = 1
	behind.border_width_left = 1
	behind.border_width_right = 1
	for name in GROUPS:
		var tab := Kit.button(name.capitalize(), func() -> void: _set_group(name))
		tab.toggle_mode = true
		tab.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		tab.add_theme_font_override("font", P.font("display_sc"))
		tab.add_theme_font_size_override("font_size", 19)
		tab.add_theme_stylebox_override("normal", behind)
		tab.add_theme_stylebox_override("hover", behind)
		tab.add_theme_stylebox_override("pressed", open)
		tab.add_theme_stylebox_override("hover_pressed", open)
		tab.add_theme_color_override("font_color", P.MUTED)
		tab.add_theme_color_override("font_hover_color", P.TEXT)
		tab.add_theme_color_override("font_pressed_color", P.TEXT)
		tab.add_theme_color_override("font_hover_pressed_color", P.TEXT)
		_tabs.add_child(tab)
	_body.add_child(_tabs)
	_actions = Kit.vbox(0)
	_body.add_child(_actions)
	_detail = Kit.vbox(6)
	_body.add_child(_detail)
	# Actions and their explanation come first; statistics follow.
	for node in [_stats, people]:
		_body.move_child(node, -1)
	visible = false


func show_region(id: String) -> void:
	region = id
	visible = id != ""
	_detail_id = ""
	if visible:
		_build_rows()
		refresh()


func _set_group(name: String) -> void:
	group = name
	_detail_id = ""
	_build_rows()
	refresh()


## Connects a button so that, on touch screens, a drag that scrolls the file does not also
## press whatever was under the finger.
func _tap(button: BaseButton, action: Callable) -> void:
	if not Kit.touch:
		button.pressed.connect(action)
		return
	button.mouse_filter = Control.MOUSE_FILTER_PASS
	button.button_down.connect(func() -> void: button.set_meta("scroll", _scroll.scroll_vertical))
	button.pressed.connect(func() -> void:
		if absi(_scroll.scroll_vertical - int(button.get_meta("scroll", _scroll.scroll_vertical))) < 8:
			action.call())


func _build_rows() -> void:
	if _detail.get_parent() == _actions:
		_detail.reparent(_body)
		_body.move_child(_detail, _actions.get_index() + 1)
	Kit.clear(_actions)
	_rows.clear()
	_detail_shown = ""
	for tab in _tabs.get_children():
		tab.button_pressed = tab.text == group.capitalize()
	# Actions read like entries in a price list, ruled between rows.
	var pad := Vector4(10, 8, 10, 8)
	var row_style := UiTheme.box(Color.TRANSPARENT, Color(P.TEXT, 0.18), 0, pad)
	row_style.border_width_bottom = 1
	var row_hover := UiTheme.box(Color(P.TEXT, 0.07), Color(P.TEXT, 0.5), 0, pad)
	row_hover.border_width_bottom = 1
	for id in GROUPS[group]:
		var button := Kit.button("")
		_tap(button, (func() -> void: _choose(id)) if Kit.touch else (func() -> void: game.act(id, region)))
		button.toggle_mode = Kit.touch
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size = Vector2(0, 46 if Kit.touch else 40)
		button.add_theme_font_override("font", P.font("body"))
		button.add_theme_font_size_override("font_size", 18)
		for state in ["normal", "disabled"]:
			button.add_theme_stylebox_override(state, row_style)
		for state in ["hover", "pressed", "hover_pressed"]:
			button.add_theme_stylebox_override(state, row_hover)
		button.add_theme_color_override("font_pressed_color", P.TEXT)
		button.add_theme_color_override("font_hover_pressed_color", P.TEXT)
		var cost := Kit.label("", 14, P.MUTED, "type_bold")
		cost.set_anchors_preset(Control.PRESET_RIGHT_WIDE)
		cost.offset_left = -170
		cost.offset_right = -12
		cost.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		cost.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		cost.mouse_filter = Control.MOUSE_FILTER_IGNORE
		button.add_child(cost)
		if not Kit.touch:
			button.mouse_entered.connect(func() -> void:
				_detail_id = id
				_refresh_detail())
		_actions.add_child(button)
		_rows[id] = { "button": button, "cost": cost }


func refresh() -> void:
	if not visible or game.state == null:
		return
	var s: Dictionary = game.state
	var r: Dictionary = s.regions[region]
	var placing: bool = s.status == "placing"
	_file.text = "CASE FILE No. %d" % (GameData.REGION_IDS.find(region) + 1)
	_name.text = GameData.REGIONS[region].name
	var stance := "NO CULT PRESENCE"
	var stance_color := P.MUTED
	if r.population.followers > 0:
		stance = "PUBLIC CONGREGATION" if r.public else "HIDDEN CELL"
		stance_color = P.CULT
	if r.backlashUntil > s.elapsedMonths:
		stance += "  ·  BACKLASH %d MO" % (r.backlashUntil - s.elapsedMonths)
	_stance.text = stance
	_stance.add_theme_color_override("font_color", stance_color)
	_fill_stats(s, r)
	_population.population = r.population
	_population.queue_redraw()
	_fill_legend(r)
	_fill_cards(s, r)
	_profile.visible = placing
	_stats.visible = not placing
	_tabs.visible = not placing
	_actions.visible = not placing
	_detail.visible = not placing
	if placing:
		_fill_profile(s, r)
		return
	for id in _rows:
		var row: Dictionary = _rows[id]
		var preview := GameEngine.preview_action(s, id, region)
		var button: Button = row.button
		button.text = GameData.ACTIONS[id].name
		button.disabled = not preview.ok
		var hidden: bool = id == "reveal" and r.public or id == "hide" and not r.public
		button.visible = not hidden
		row.cost.text = _cost_text(preview)
		row.cost.add_theme_color_override("font_color", P.FAINT if not preview.ok else P.OMEN if preview.dread > 0 else P.TEXT)
	_refresh_detail()


func _fill_stats(s: Dictionary, r: Dictionary) -> void:
	Kit.clear(_stats)
	var items := [
		["Followers", Format.count(r.population.followers), P.CULT],
		["Recruits", Format.rate(Derived.recruitment_per_month(s, r)) + "/mo", P.TEXT],
		["Influence", Format.rate(Derived.region_income(r)) + "/mo", P.TEXT],
		["Exposure", "%d%%" % roundi(r.exposure), P.DANGER if r.exposure >= 60 else P.WARN if r.exposure >= 30 else P.TEXT],
		["Madness", "%d%%" % roundi(r.madness), P.OMEN if r.madness > 0 else P.TEXT],
		["Site needs", Format.count(Derived.site_requirement(r)), P.TEXT if Derived.supported(r) else P.MUTED],
	]
	var hints := {
		"Exposure": "Evidence of the cult here. Raises threat severity. Public worship keeps it above 20%, a ritual site above 40%.",
		"Madness": "Left by manifestations. Speeds recruitment slightly and draws opposition. Fades over time.",
		"Site needs": "Followers a ritual site here must keep: a quarter of the region's people.",
		"Recruits": "Automatic recruitment each month.",
	}
	for item in items:
		var cell := Kit.vbox(0)
		cell.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		cell.tooltip_text = hints.get(item[0], "")
		cell.mouse_filter = Control.MOUSE_FILTER_PASS
		cell.add_child(Kit.caption(item[0]))
		cell.add_child(Kit.label(item[1], 19, item[2], "body_bold"))
		_stats.add_child(cell)


func _fill_legend(r: Dictionary) -> void:
	Kit.clear(_legend)
	for key in PopulationBar.ORDER:
		var item := Kit.hbox(5)
		var swatch := ColorRect.new()
		swatch.color = PopulationBar.COLORS[key]
		swatch.custom_minimum_size = Vector2(8, 8)
		swatch.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		item.add_child(swatch)
		item.add_child(Kit.label("%s %s" % [key.capitalize(), Format.count(r.population[key])], 13, P.MUTED, "type"))
		_legend.add_child(item)


func _card(accent: Color) -> VBoxContainer:
	var card := Kit.panel(P.PAPER_LIGHT, accent, Vector4(14, 11, 14, 12))
	card.get_theme_stylebox("panel").border_width_left = 4
	var box := Kit.vbox(5)
	card.add_child(box)
	_cards.add_child(card)
	return box


func _fill_cards(s: Dictionary, r: Dictionary) -> void:
	Kit.clear(_cards)
	if GameEngine.is_over(s):
		return
	if r.threat != null:
		var threat: Dictionary = r.threat
		var box := _card(P.DANGER)
		var head := Kit.hbox(8)
		var title := Kit.body(GameData.THREAT_NAMES[threat.kind], 21, P.DANGER, "display_sc")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label(Format.months(threat.dueMonth - s.elapsedMonths), 15, P.DANGER, "type_bold"))
		box.add_child(head)
		box.add_child(Kit.body("If ignored: " + Threats.describe(r), 15, P.TEXT, "italic"))
		var preview := GameEngine.preview_action(s, "counter", region)
		var counter := Kit.stamp("OBSTRUCT · %d INFLUENCE" % preview.influence, Callable(), P.DANGER)
		_tap(counter, func() -> void: game.act("counter", region))
		counter.disabled = not preview.ok
		counter.tooltip_text = preview.reason
		box.add_child(counter)
	if r.opportunity != null:
		var omen: Dictionary = r.opportunity
		var box := _card(P.OMEN)
		var head := Kit.hbox(8)
		var title := Kit.body(GameData.OPPORTUNITY_NAMES[omen.kind], 21, P.OMEN, "display")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label(Format.months(omen.expiresMonth - s.elapsedMonths), 15, P.OMEN, "type_bold"))
		box.add_child(head)
		var action := "exploit" if omen.kind in ["nightmare", "eclipse"] else "convert"
		var preview := GameEngine.preview_action(s, action, region)
		var button := Kit.stamp(GameData.ACTIONS[action].name.to_upper() + " · %d" % preview.influence, Callable(), P.OMEN)
		_tap(button, func() -> void: game.act(action, region))
		button.disabled = not preview.ok
		button.tooltip_text = preview.effect + ("\n" + preview.reason if not preview.ok else "")
		box.add_child(Kit.body(preview.effect, 15, P.TEXT, "italic"))
		box.add_child(button)
	if r.figure != null:
		var box := _card(P.OMEN)
		var who := "celebrity" if r.figure.kind == "celebrity" else "politician"
		if r.figure.revealed:
			box.add_child(Kit.label("Public " + who, 20, P.OMEN, "display_sc"))
		else:
			box.add_child(Kit.label("Secret " + who, 20, P.OMEN, "display_sc"))
			box.add_child(Kit.body("+0.6 Influence each month." if who == "celebrity" else "New threats here arrive 2 months later.", 15, P.TEXT, "italic"))
	if r.site.invested:
		var ready: bool = r.site.progress >= B.SITE_MONTHS
		var supported := Derived.supported(r)
		var accent := P.DANGER if not supported else P.CULT if ready else P.OMEN
		var box := _card(accent)
		var head := Kit.hbox(8)
		var title := Kit.label("Ritual Site", 21, accent, "display_sc")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label("%d / %d MO" % [r.site.progress, B.SITE_MONTHS], 15, accent, "type_bold"))
		box.add_child(head)
		var bar := ProgressBar.new()
		bar.max_value = B.SITE_MONTHS
		bar.value = r.site.progress
		bar.show_percentage = false
		bar.custom_minimum_size = Vector2(0, 6)
		bar.add_theme_stylebox_override("fill", UiTheme.box(accent, Color.TRANSPARENT, 0, Vector4.ZERO))
		box.add_child(bar)
		if not supported:
			box.add_child(Kit.body("Stalled. Needs %s followers here." % Format.count(Derived.site_requirement(r)), 15, P.DANGER, "italic"))


func _fill_profile(s: Dictionary, r: Dictionary) -> void:
	Kit.clear(_profile)
	var def: Dictionary = GameData.REGIONS[region]
	for fact in [["Skepticism", def.skepticism, "Share of people who deny the dream outright."], ["Institutions", def.institutionalResistance, "How hard the authorities push back."], ["Mysticism", def.mysticism, "Openness to the impossible."]]:
		var row := Kit.hbox(10)
		row.tooltip_text = fact[2]
		row.mouse_filter = Control.MOUSE_FILTER_PASS
		var name := Kit.caption(fact[0])
		name.custom_minimum_size = Vector2(100, 0)
		row.add_child(name)
		var bar := ProgressBar.new()
		bar.max_value = 100
		bar.value = fact[1]
		bar.show_percentage = false
		bar.custom_minimum_size = Vector2(0, 6)
		bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		bar.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		bar.add_theme_stylebox_override("fill", UiTheme.box(P.MUTED, Color.TRANSPARENT, 0, Vector4.ZERO))
		row.add_child(bar)
		_profile.add_child(row)
	var borders: Array[String] = []
	for other in def.adjacent:
		borders.append(GameData.REGIONS[other].name)
	_profile.add_child(Kit.body("Spreads to " + ", ".join(borders) + ".", 16, P.MUTED, "italic"))
	var start := Kit.primary("Begin Here")
	_tap(start, func() -> void: game.place(region))
	start.disabled = s.status != "placing"
	_profile.add_child(start)


func _cost_text(preview: Dictionary) -> String:
	if preview.reason.begins_with("Available in"):
		return preview.reason.trim_prefix("Available in ").trim_suffix(" months.") + " MO"
	var parts: Array[String] = []
	if preview.influence > 0:
		parts.append("%d INF" % preview.influence)
	if preview.dread > 0:
		parts.append("%d DREAD" % preview.dread)
	if preview.followers > 0:
		parts.append("%d FOLLOWERS" % preview.followers)
	return "  ".join(parts) if parts else "FREE"


## Touch: a tap opens the explanation right under the chosen action; tapping it again closes it.
func _choose(id: String) -> void:
	_detail_id = "" if _detail_id == id else id
	_refresh_detail()
	if _detail_id:
		_scroll_to(_rows[_detail_id].button)


## Brings the chosen action to the top of the file, so its explanation reads downwards.
func _scroll_to(row: Control) -> void:
	if not is_inside_tree():
		return
	await get_tree().process_frame
	if not is_instance_valid(row):
		return
	var content: Control = _scroll.get_child(0)
	_scroll.scroll_vertical = maxi(0, int(row.global_position.y - content.global_position.y) - 6)


func _refresh_detail() -> void:
	for id in _rows:
		_rows[id].button.set_pressed_no_signal(Kit.touch and id == _detail_id)
	var preview := GameEngine.preview_action(game.state, _detail_id, region) if _detail_id and game.state != null and _rows.has(_detail_id) else {}
	# The clock refreshes the file every month; rebuilding an unchanged explanation would
	# swallow a tap that lands between frames.
	var shown := JSON.stringify([_detail_id, region, preview])
	if shown == _detail_shown and _detail.get_child_count() > 0:
		return
	_detail_shown = shown
	Kit.clear(_detail)
	if preview.is_empty():
		return
	if Kit.touch:
		if _detail.get_parent() != _actions:
			_detail.reparent(_actions)
		_actions.move_child(_detail, _rows[_detail_id].button.get_index() + 1)
	else:
		_detail.add_child(Kit.rule())
		_detail.add_child(Kit.label(GameData.ACTIONS[_detail_id].name, 21, P.TEXT, "display_sc"))
	_detail.add_child(Kit.body(preview.effect, 15, P.TEXT))
	if preview.risk:
		_detail.add_child(Kit.body(preview.risk, 15, P.DANGER, "italic"))
	if not preview.ok:
		_detail.add_child(Kit.body(preview.reason, 15, P.WARN, "body_bold"))
	if Kit.touch:
		var id := _detail_id
		var cost := _cost_text(preview) if preview.ok else ""
		var perform := Kit.primary(GameData.ACTIONS[id].name + (" · " + cost if cost else ""))
		_tap(perform, func() -> void: game.act(id, region))
		perform.disabled = not preview.ok
		perform.custom_minimum_size = Vector2(0, 46)
		_detail.add_child(perform)
		var spacer := Control.new()
		spacer.custom_minimum_size = Vector2(0, 6)
		_detail.add_child(spacer)


## Unaware, deniers, believers and followers as one stacked bar.
class PopulationBar extends Control:
	const ORDER := ["unaware", "deniers", "believers", "followers"]
	const COLORS := { "unaware": Color("b4a07a"), "deniers": Color("7a6a52"), "believers": P.OMEN, "followers": P.CULT }
	var population: Dictionary = {}

	func _init() -> void:
		custom_minimum_size = Vector2(0, 10)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		if population.is_empty():
			return
		var total := 0.0
		for key in ORDER:
			total += population[key]
		var x := 0.0
		for key in ORDER:
			var width: float = size.x * population[key] / maxf(1.0, total)
			draw_rect(Rect2(x, 0, width, size.y), COLORS[key])
			x += width
