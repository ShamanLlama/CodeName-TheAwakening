extends PanelContainer
## The selected region: its people, what threatens it, and what the cult can do there.
## Explanations live in the detail box under the actions, not under every button.

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


func _init(owner_game, owner_screen) -> void:
	game = owner_game
	screen = owner_screen


func _ready() -> void:
	add_theme_stylebox_override("panel", UiTheme.box(P.PANEL_SOLID, P.EDGE, 1, Vector4(0, 0, 0, 0)))
	anchor_left = 1.0
	anchor_right = 1.0
	anchor_bottom = 1.0
	offset_left = -WIDTH - 14
	offset_right = -14
	offset_top = screen.TOP + 14
	offset_bottom = -screen.BOTTOM - 14
	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	add_child(scroll)
	var margin := MarginContainer.new()
	margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for side in ["left", "right", "top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 18)
	scroll.add_child(margin)
	_body = Kit.vbox(14)
	margin.add_child(_body)

	var head := Kit.hbox(8)
	var titles := Kit.vbox(0)
	titles.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_name = Kit.label("", 30, P.TEXT, "condensed_bold")
	_stance = Kit.caption("")
	titles.add_child(_name)
	titles.add_child(_stance)
	head.add_child(titles)
	var close := IconButton.make("close", "Close  (Esc)", func() -> void: screen.deselect())
	close.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	head.add_child(close)
	_body.add_child(head)

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

	_tabs = Kit.hbox(0)
	for name in GROUPS:
		var tab := Kit.button(name, func() -> void: _set_group(name))
		tab.toggle_mode = true
		tab.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		tab.add_theme_font_size_override("font_size", 15)
		_tabs.add_child(tab)
	_body.add_child(_tabs)
	_actions = Kit.vbox(4)
	_body.add_child(_actions)
	_detail = Kit.vbox(6)
	_body.add_child(_detail)
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


func _build_rows() -> void:
	Kit.clear(_actions)
	_rows.clear()
	for tab in _tabs.get_children():
		tab.button_pressed = tab.text == group
	for id in GROUPS[group]:
		var button := Kit.button("", func() -> void: game.act(id, region))
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		button.custom_minimum_size = Vector2(0, 40)
		button.add_theme_font_size_override("font_size", 17)
		var cost := Kit.label("", 15, P.MUTED, "condensed_semibold")
		cost.set_anchors_preset(Control.PRESET_RIGHT_WIDE)
		cost.offset_left = -170
		cost.offset_right = -12
		cost.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		cost.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		cost.mouse_filter = Control.MOUSE_FILTER_IGNORE
		button.add_child(cost)
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
	_name.text = GameData.REGIONS[region].name.to_upper()
	var stance := "NO CULT PRESENCE"
	var stance_color := P.MUTED
	if r.population.followers > 0:
		stance = "PUBLIC CONGREGATION" if r.public else "HIDDEN CELL"
		stance_color = P.CULT if r.public else P.CULT_DIM.lightened(0.3)
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
		cell.add_child(Kit.label(item[1], 21, item[2], "condensed_semibold"))
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
		item.add_child(Kit.label("%s %s" % [key.capitalize(), Format.count(r.population[key])], 13, P.MUTED, "condensed"))
		_legend.add_child(item)


func _card(accent: Color, deep: Color) -> VBoxContainer:
	var card := Kit.panel(deep, Color(accent, 0.45), Vector4(14, 11, 14, 12))
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
		var box := _card(P.DANGER, Color(0.1, 0.04, 0.04, 0.9))
		var head := Kit.hbox(8)
		var title := Kit.label(GameData.THREAT_NAMES[threat.kind], 17, P.DANGER, "condensed_bold")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label("%dM" % (threat.dueMonth - s.elapsedMonths), 17, P.DANGER, "condensed_bold"))
		box.add_child(head)
		box.add_child(Kit.body("If ignored: " + Threats.describe(r), 14, Color("e8c4bd")))
		var preview := GameEngine.preview_action(s, "counter", region)
		var counter := Kit.outline("OBSTRUCT · %d INFLUENCE" % preview.influence, func() -> void: game.act("counter", region), P.DANGER)
		counter.disabled = not preview.ok
		counter.tooltip_text = preview.reason
		box.add_child(counter)
	if r.opportunity != null:
		var omen: Dictionary = r.opportunity
		var box := _card(P.OMEN, Color(0.07, 0.05, 0.12, 0.9))
		var head := Kit.hbox(8)
		var title := Kit.label(GameData.OPPORTUNITY_NAMES[omen.kind], 17, P.OMEN, "condensed_bold")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label("%dM" % (omen.expiresMonth - s.elapsedMonths), 17, P.OMEN, "condensed_bold"))
		box.add_child(head)
		var action := "exploit" if omen.kind in ["nightmare", "eclipse"] else "convert"
		var preview := GameEngine.preview_action(s, action, region)
		var button := Kit.outline(GameData.ACTIONS[action].name.to_upper() + " · %d" % preview.influence, func() -> void: game.act(action, region), P.OMEN)
		button.disabled = not preview.ok
		button.tooltip_text = preview.effect + ("\n" + preview.reason if not preview.ok else "")
		box.add_child(Kit.body(preview.effect, 14, Color("cfc4f2")))
		box.add_child(button)
	if r.figure != null:
		var box := _card(P.OMEN, Color(0.07, 0.05, 0.12, 0.9))
		var who := "celebrity" if r.figure.kind == "celebrity" else "politician"
		if r.figure.revealed:
			box.add_child(Kit.label("PUBLIC " + who.to_upper(), 15, P.OMEN, "condensed_bold"))
		else:
			box.add_child(Kit.label("SECRET " + who.to_upper(), 15, P.OMEN, "condensed_bold"))
			box.add_child(Kit.body("+0.6 Influence each month." if who == "celebrity" else "New threats here arrive 2 months later.", 14, Color("cfc4f2")))
	if r.site.invested:
		var ready: bool = r.site.progress >= B.SITE_MONTHS
		var supported := Derived.supported(r)
		var accent := P.DANGER if not supported else P.CULT if ready else P.OMEN
		var box := _card(accent, Color(0.05, 0.06, 0.08, 0.9))
		var head := Kit.hbox(8)
		var title := Kit.label("RITUAL SITE", 17, accent, "condensed_bold")
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		head.add_child(title)
		head.add_child(Kit.label("%d / %d MO" % [r.site.progress, B.SITE_MONTHS], 17, accent, "condensed_bold"))
		box.add_child(head)
		var bar := ProgressBar.new()
		bar.max_value = B.SITE_MONTHS
		bar.value = r.site.progress
		bar.show_percentage = false
		bar.custom_minimum_size = Vector2(0, 6)
		bar.add_theme_stylebox_override("fill", UiTheme.box(accent, Color.TRANSPARENT, 0, Vector4.ZERO))
		box.add_child(bar)
		if not supported:
			box.add_child(Kit.body("Stalled. Needs %s followers here." % Format.count(Derived.site_requirement(r)), 14, Color("e8c4bd")))


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
	_profile.add_child(Kit.body("Spreads to " + ", ".join(borders) + ".", 15, P.MUTED))
	var start := Kit.primary("BEGIN HERE", func() -> void: game.place(region))
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


func _refresh_detail() -> void:
	Kit.clear(_detail)
	if not _detail_id or game.state == null or not _rows.has(_detail_id):
		return
	var preview := GameEngine.preview_action(game.state, _detail_id, region)
	_detail.add_child(Kit.rule())
	_detail.add_child(Kit.label(GameData.ACTIONS[_detail_id].name.to_upper(), 16, P.TEXT, "condensed_bold"))
	_detail.add_child(Kit.body(preview.effect, 15, P.TEXT))
	if preview.risk:
		_detail.add_child(Kit.body(preview.risk, 14, Color("f0a69c")))
	if not preview.ok:
		_detail.add_child(Kit.body(preview.reason, 14, P.WARN))


## Unaware, deniers, believers and followers as one stacked bar.
class PopulationBar extends Control:
	const ORDER := ["unaware", "deniers", "believers", "followers"]
	const COLORS := { "unaware": Color("3a4352"), "deniers": Color("6b7486"), "believers": Color("a68bff"), "followers": Color("c4f135") }
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
