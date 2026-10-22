extends Control
## Title and run setup, over a dimmed map where the cult is already spreading.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")

var game
var _difficulty := "normal"
var _difficulty_buttons: Dictionary = {}
var _seed: LineEdit


func _init(owner_game) -> void:
	game = owner_game
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)


func _ready() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = P.OCEAN
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
	gradient.set_color(0, Color(P.INK, 0.97))
	gradient.set_color(1, Color(P.INK, 0.25))
	gradient.add_point(0.45, Color(P.INK, 0.82))
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
		margin.add_theme_constant_override("margin_" + side, 96)
	for side in ["top", "bottom"]:
		margin.add_theme_constant_override("margin_" + side, 72)
	add_child(margin)
	var row := Kit.hbox(64)
	margin.add_child(row)

	var title := Kit.vbox(0)
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title.alignment = BoxContainer.ALIGNMENT_CENTER
	title.add_child(Kit.label("A WORLD-MAP STRATEGY GAME", 16, P.CULT_DIM.lightened(0.2), "condensed_semibold"))
	var name_lines := Kit.vbox(-34)
	name_lines.add_child(Kit.label("THE AWAKENING", 104, P.TEXT, "condensed_bold"))
	name_lines.add_child(Kit.label("OF CTHULHU", 104, P.CULT, "condensed_bold"))
	title.add_child(name_lines)
	var gap := Control.new()
	gap.custom_minimum_size = Vector2(0, 18)
	title.add_child(gap)
	title.add_child(Kit.label("Build the cult. Feed the rite. Wake the god.", 24, P.MUTED, "medium"))
	var gap2 := Control.new()
	gap2.custom_minimum_size = Vector2(0, 26)
	title.add_child(gap2)
	title.add_child(Kit.caption("%d months to prepare  ·  3 ritual sites  ·  1 sleeping god" % B.PREPARATION_MONTHS, P.FAINT.lightened(0.25)))
	row.add_child(title)

	var setup := Kit.panel(Color(0.04, 0.05, 0.07, 0.92), P.EDGE_STRONG, Vector4(28, 26, 28, 26))
	setup.custom_minimum_size = Vector2(430, 0)
	setup.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	var form := Kit.vbox(14)
	setup.add_child(form)
	row.add_child(setup)

	if game.storage_notice:
		form.add_child(Kit.body(game.storage_notice, 15, P.WARN))
	var saved = game.state
	if saved != null and not GameEngine.is_over(saved):
		form.add_child(Kit.caption("Run in progress"))
		form.add_child(Kit.label("%s  ·  %s" % [Derived.calendar(saved.elapsedMonths), GameData.DIFFICULTIES[saved.difficulty].name], 19, P.TEXT, "condensed_semibold"))
		var resume := Kit.primary("CONTINUE", func() -> void: game.show_game())
		resume.custom_minimum_size = Vector2(0, 50)
		form.add_child(resume)
		form.add_child(Kit.rule())

	form.add_child(Kit.caption("New awakening"))
	var levels := Kit.hbox(0)
	for id in GameData.DIFFICULTY_IDS:
		var button := Kit.button(GameData.DIFFICULTIES[id].name.to_upper(), func() -> void: _pick(id))
		button.toggle_mode = true
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		button.custom_minimum_size = Vector2(0, 42)
		_difficulty_buttons[id] = button
		levels.add_child(button)
	form.add_child(levels)
	_seed = LineEdit.new()
	_seed.placeholder_text = "World seed (optional)"
	_seed.max_length = 40
	_seed.add_theme_font_size_override("font_size", 17)
	form.add_child(_seed)
	var begin := Kit.primary("BEGIN", func() -> void: game.begin(_difficulty, _seed.text))
	begin.custom_minimum_size = Vector2(0, 50)
	form.add_child(begin)
	_seed.text_submitted.connect(func(_text: String) -> void: game.begin(_difficulty, _seed.text))
	if saved != null and not GameEngine.is_over(saved):
		form.add_child(Kit.body("Beginning anew replaces the run in progress.", 14, P.FAINT.lightened(0.2)))
	if game.last_result != null:
		var result: Dictionary = game.last_result
		form.add_child(Kit.body("Last run: %s at month %d · seed %s" % ["awakened" if result.won else "failed", result.month, result.seed], 14, P.MUTED))
	_pick(_difficulty)

	var credit := Kit.label("Map: Natural Earth, public domain  ·  v" + str(ProjectSettings.get_setting("application/config/version")), 13, P.FAINT)
	credit.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	credit.position = Vector2(96, -40)
	credit.offset_top = -40
	credit.offset_bottom = -20
	add_child(credit)


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
