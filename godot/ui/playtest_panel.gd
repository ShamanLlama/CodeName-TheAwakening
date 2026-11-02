extends VBoxContainer
## Optional participation, with the same archive typography as the game.
const P = preload("res://ui/palette.gd")
var game
var compact := false
var _status: Label
var _refresh_in := 0.0

func _init(owner_game, small := false) -> void:
	game = owner_game
	compact = small
	add_theme_constant_override("separation", 8)

func _ready() -> void:
	add_child(Kit.caption("GTC Playtest Lab · optional"))
	add_child(Kit.body("Share choices (including difficulty and world seed), resource spending, progress and session times with GTC. Random ID per round; no name or email. Detailed records are kept for six months. Keep personal details out of your seed. You can play without sharing.", 14 if compact else 16, P.MUTED))
	if not compact:
		add_child(Kit.body("Join for this opening. Closing the game pauses uploads until you join again. Stop clears unsent records on this device; it does not erase records already received. Contact the GTC operator to manage those records. Local exports stay under your control.", 15, P.MUTED))
	var choice := CheckBox.new()
	choice.name = "PlaytestParticipation"
	choice.text = "I agree to share this playtest with GTC"
	choice.add_theme_font_size_override("font_size", 15)
	choice.button_pressed = game.lab.confirmed
	choice.disabled = game.blocked or not game.lab.configured() or game.lab.corrupt
	choice.toggled.connect(func(value: bool) -> void:
		game.set_participation(value)
		choice.set_pressed_no_signal(game.lab.confirmed)
		_refresh())
	add_child(choice)
	_status = Kit.body(game.playtest_status(), 13 if compact else 15, P.MUTED)
	add_child(_status)
	if not compact or game.lab.corrupt:
		add_child(Kit.button("EXPORT LOCAL SAVE & PLAYTEST RECORD", game.export_playtest_record))
		add_child(Kit.button("STOP & CLEAR UNSENT RECORDS", func() -> void:
			game.set_participation(false)
			choice.set_pressed_no_signal(false)
			_refresh()))

func _process(delta: float) -> void:
	_refresh_in -= delta
	if _refresh_in <= 0:
		_refresh_in = 1
		_refresh()

func _refresh() -> void:
	if _status != null: _status.text = game.playtest_status()
