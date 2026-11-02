extends Control
## Title and run setup, set like a book's frontispiece beside a chart where the cult is
## already spreading.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const PlaytestPanel = preload("res://ui/playtest_panel.gd")

var game
var _difficulty := "normal"
var _difficulty_buttons: Dictionary = {}
var _seed: LineEdit
var _ask: Control
var _form_scroll: ScrollContainer


func _init(owner_game) -> void:
	game = owner_game
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = P.DESK
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(backdrop)
	var map := WorldMap.new()
	map.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	map.offset_left = 380
	map.offset_top = 60
	map.offset_bottom = -60
	map.mouse_filter = Control.MOUSE_FILTER_IGNORE
	map.show_labels = false
	add_child(map)
	map.set_state(_omen_state(), "")
	var shade := TextureRect.new()
	var gradient := Gradient.new()
	gradient.set_color(0, Color(P.DESK, 0.98))
	gradient.set_color(1, Color(P.DESK, 0.35))
	gradient.add_point(0.45, Color(P.DESK, 0.85))
	var texture := GradientTexture2D.new()
	texture.gradient = gradient
	texture.fill_to = Vector2(1, 0)
	shade.texture = texture
	shade.stretch_mode = TextureRect.STRETCH_SCALE
	shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	shade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(shade)

	var margin := MarginContainer.new()
	margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	for side in ["left", "right"]:
		margin.add_theme_constant_override("margin_" + side, 32 if Kit.compact else 96)
	for side in ["top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 16 if Kit.compact else 72)
	add_child(margin)
	var row := Kit.hbox(28 if Kit.compact else 64)
	margin.add_child(row)

	var title := Kit.vbox(0)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title.alignment = BoxContainer.ALIGNMENT_CENTER
	title.add_child(Kit.label("BEING A CHRONICLE OF THE YEARS 1908 – 1926", 14, P.BRASS, "type_bold"))
	var name_lines := Kit.vbox(-16 if Kit.compact else -30)
	name_lines.add_child(Kit.label("The Awakening", 62 if Kit.compact else 112, P.LAMP, "display"))
	var of := Kit.label("of Cthulhu", 52 if Kit.compact else 92, P.BRASS, "display_italic")
	name_lines.add_child(of)
	title.add_child(name_lines)
	var gap := Control.new()
	gap.custom_minimum_size = Vector2(0, 14)
	title.add_child(gap)
	var rule := Kit.rule(Color(P.BRASS, 0.7))
	rule.custom_minimum_size = Vector2(300 if Kit.compact else 420, 5)
	rule.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	title.add_child(rule)
	var gap2 := Control.new()
	gap2.custom_minimum_size = Vector2(0, 14)
	title.add_child(gap2)
	title.add_child(Kit.label("Build the cult. Feed the rite. Wake the god.", 20 if Kit.compact else 26, P.LAMP_DIM, "display_italic"))
	var gap3 := Control.new()
	gap3.custom_minimum_size = Vector2(0, 22)
	title.add_child(gap3)
	title.add_child(Kit.caption("%d months to prepare  ·  3 ritual sites  ·  1 sleeping god" % B.PREPARATION_MONTHS, P.LAMP_DIM))
	row.add_child(title)

	var pad := 20 if Kit.compact else 32
	var setup := Kit.panel(P.PAPER, P.DESK_EDGE, Vector4(pad, pad, pad, pad))
	setup.material = P.paper(0.16)
	Kit.frame(setup)
	setup.custom_minimum_size = Vector2(410 if Kit.compact else 430, 0)
	setup.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	# The form scrolls inside its sheet when the screen is short.
	_form_scroll = ScrollContainer.new()
	_form_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	setup.add_child(_form_scroll)
	var form := Kit.vbox(10 if Kit.compact else 14)
	form.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_form_scroll.add_child(form)
	row.add_child(setup)
	_fit_form.call_deferred(form, setup)

	if game.storage_notice:
		form.add_child(Kit.body(game.storage_notice, 15, P.WARN, "body_bold"))
	var saved = game.state
	if saved != null and not GameEngine.is_over(saved):
		form.add_child(Kit.caption("Run in progress"))
		form.add_child(Kit.label("%s  ·  %s" % [Format.date(saved.elapsedMonths), GameData.DIFFICULTIES[saved.difficulty].name], 22, P.TEXT, "display"))
		var resume := Kit.primary("Continue", func() -> void: game.show_game())
		resume.custom_minimum_size = Vector2(0, 50)
		form.add_child(resume)
		form.add_child(Kit.rule())

	form.add_child(Kit.caption("New awakening"))
	var levels := Kit.hbox(0)
	for id in GameData.DIFFICULTY_IDS:
		var button := Kit.button(GameData.DIFFICULTIES[id].name, func() -> void: _pick(id))
		button.add_theme_font_override("font", P.font("display_sc"))
		button.add_theme_font_size_override("font_size", Kit.fs(20))
		button.toggle_mode = true
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.custom_minimum_size = Vector2(0, 42)
		_difficulty_buttons[id] = button
		levels.add_child(button)
	form.add_child(levels)
	_seed = LineEdit.new()
	_seed.placeholder_text = "World seed (optional)"
	_seed.max_length = 40
	_seed.add_theme_font_size_override("font_size", Kit.fs(17))
	form.add_child(_seed)
	if game.lab.configured() or game.lab.corrupt or game.lab.pending_events() > 0:
		form.add_child(PlaytestPanel.new(game, true))
	var begin := Kit.primary("Begin", _begin)
	begin.custom_minimum_size = Vector2(0, 50)
	form.add_child(begin)
	_seed.text_submitted.connect(func(_text: String) -> void: _begin())
	if saved != null and not GameEngine.is_over(saved):
		form.add_child(Kit.body("Beginning anew replaces the run in progress.", 15, P.MUTED, "italic"))
	if game.last_result != null:
		var result: Dictionary = game.last_result
		form.add_child(Kit.body("Last run: %s in %s · seed %s" % ["awakened" if result.won else "failed", Format.date(result.month), result.seed], 15, P.MUTED, "italic"))
	_pick(_difficulty)

	var credit := Kit.label("Map: Natural Earth, public domain  ·  v" + str(ProjectSettings.get_setting("application/config/version")), 12, Color(P.LAMP_DIM, 0.6), "type")
	credit.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	credit.position = Vector2(32 if Kit.compact else 96, -40)
	credit.offset_top = -40
	credit.offset_bottom = -20
	add_child(credit)


func _fit_form(form: Control, setup: PanelContainer) -> void:
	await get_tree().process_frame
	if not is_inside_tree():
		return
	var pad: float = setup.get_theme_stylebox("panel").get_minimum_size().y
	var limit := get_viewport_rect().size.y - pad - (32 if Kit.compact else 144)
	_form_scroll.custom_minimum_size.y = minf(form.get_combined_minimum_size().y, limit)


func _begin() -> void:
	if game.needs_participation_choice():
		_ask_participation()
	else:
		game.begin(_difficulty, _seed.text)


## Asked once per round on this device: an explicit choice, remembered until changed in the menu.
func _ask_participation() -> void:
	if _ask != null:
		return
	_ask = Control.new()
	_ask.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_ask.mouse_filter = Control.MOUSE_FILTER_STOP
	var shade := ColorRect.new()
	shade.color = Color(P.DESK, 0.82)
	shade.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_ask.add_child(shade)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_ask.add_child(center)
	var sheet := Kit.panel(P.PAPER, P.DESK_EDGE, Vector4(40, 34, 40, 34))
	sheet.material = P.paper(0.16)
	Kit.frame(sheet)
	sheet.custom_minimum_size = Vector2(600, 0)
	center.add_child(sheet)
	var content := Kit.vbox(16)
	sheet.add_child(content)
	content.add_child(Kit.label("Join the playtest?", 40, P.TEXT, "display"))
	content.add_child(Kit.rule(Color(P.TEXT, 0.6)))
	content.add_child(Kit.body("This build is part of a GTC Playtest Lab round. " + PlaytestPanel.NOTICE, 17, P.TEXT))
	content.add_child(Kit.body("Your answer is remembered on this device. Change it any time in Menu → GTC Playtest Lab.", 15, P.MUTED, "italic"))
	var row := Kit.hbox(12)
	var join := Kit.primary("Join the playtest & play", func() -> void: _answer(true))
	var skip := Kit.button("Play without sharing", func() -> void: _answer(false))
	skip.add_theme_font_override("font", P.font("display_sc"))
	skip.add_theme_font_size_override("font_size", Kit.fs(21))
	for button in [join, skip]:
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.custom_minimum_size = Vector2(0, 52)
		row.add_child(button)
	content.add_child(row)
	add_child(_ask)


func _answer(join: bool) -> void:
	if join:
		game.set_participation(true)
	else:
		game.decline_participation()
	_ask.queue_free()
	_ask = null
	game.begin(_difficulty, _seed.text)


func _unhandled_input(event: InputEvent) -> void:
	if _ask != null and event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		_ask.queue_free()
		_ask = null
		get_viewport().set_input_as_handled()


func _pick(id: String) -> void:
	_difficulty = id
	for key in _difficulty_buttons:
		_difficulty_buttons[key].button_pressed = key == id


## A decorative world for the title screen: a few cells, one already public.
static func _omen_state() -> Dictionary:
	var s := GameEngine.create_game_state("normal", "title")
	for entry in [["latin_america", 0.22, false], ["africa", 0.35, true], ["oceania", 0.5, false], ["south_asia", 0.08, false], ["europe", 0.05, false]]:
		var r: Dictionary = s.regions[entry[0]]
		var people := Derived.capacity(r)
		r.population.unaware -= people * entry[1]
		r.population.followers = people * entry[1]
		r.public = entry[2]
	return s
