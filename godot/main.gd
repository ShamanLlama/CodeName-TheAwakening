extends Control
## Root of the game: owns the run, the monthly clock, saving, sound and screen switching.
## Screens read `state` and call the command methods below; every change emits `changed`.

signal changed
signal events(entries: Array)

const B = preload("res://core/balance.gd")
const StartScreen = preload("res://ui/start_screen.gd")
const GameScreen = preload("res://ui/game_screen.gd")

var state = null
var settings := { "muted": false, "volume": 40.0 }
var tutorial_complete := false
var last_result = null
var blocked := false
var storage_notice := ""
var sfx: Sfx

var _elapsed := 0.0
var _holds := 0
var _screen: Control
var _shot := {}


func _ready() -> void:
	theme = UiTheme.build()
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	sfx = Sfx.new()
	add_child(sfx)
	_shot = _shot_args()
	if _shot:
		blocked = true
	else:
		var loaded := SaveData.load_run()
		var save: Dictionary = loaded.save
		state = save.state
		settings = save.settings
		tutorial_complete = save.tutorialComplete
		last_result = save.lastResult
		blocked = loaded.blocked
		storage_notice = loaded.notice
	_apply_audio()
	if _shot:
		_stage_shot.call_deferred()
	else:
		show_start()


func _notification(what: int) -> void:
	# Leaving the window pauses play, like switching browser tabs did.
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT and state != null and GameEngine.is_active(state) and state.speed != 0:
		set_speed(0)


func _process(delta: float) -> void:
	if state == null or _holds > 0 or _screen is not GameScreen or not GameEngine.is_active(state) or state.speed == 0:
		return
	_elapsed += minf(delta, 0.5) * state.speed / B.SECONDS_PER_MONTH
	var months := floori(_elapsed)
	if months > 0:
		_elapsed -= months
		_advance(months)


func _unhandled_key_input(event: InputEvent) -> void:
	if not event.pressed or event.echo or state == null or _screen is not GameScreen or _holds > 0:
		return
	match event.keycode:
		KEY_SPACE:
			set_speed(0 if state.speed != 0 else 1)
		KEY_1:
			set_speed(1)
		KEY_2:
			set_speed(2)


# --- Screens -----------------------------------------------------------------------------

func show_start() -> void:
	_holds = 0
	_swap(StartScreen.new(self))


func show_game() -> void:
	_holds = 0
	_elapsed = 0.0
	_swap(GameScreen.new(self))


func _swap(screen: Control) -> void:
	if _screen:
		_screen.queue_free()
	_screen = screen
	add_child(screen)


## Dialogs and the guide hold the clock while open.
func hold() -> void:
	_holds += 1


func release() -> void:
	_holds = maxi(0, _holds - 1)


# --- Commands ----------------------------------------------------------------------------

func begin(difficulty: String, seed: String) -> void:
	blocked = not SaveData.retain_recovery() if not _shot else true
	storage_notice = "This session is not saving." if blocked and not _shot else ""
	var chosen := seed.strip_edges()
	if chosen.is_empty():
		chosen = "%08x" % (randi() & 0xFFFFFFFF)
	state = GameEngine.create_game_state(difficulty, chosen)
	_commit()
	show_game()


func abandon() -> void:
	state = null
	_commit()
	show_start()


func place(region_id: String) -> void:
	_apply(GameEngine.place_starting_foothold(state, region_id))


func act(action_id: String, region_id: String) -> void:
	_apply(GameEngine.perform_action(state, action_id, region_id))


func choose_doctrine(choice: String) -> void:
	_apply(GameEngine.choose_specialization(state, choice))


func awaken() -> void:
	_apply(GameEngine.start_awakening(state))


func set_speed(speed: int) -> void:
	var next := GameEngine.set_game_speed(state, speed)
	if next != state:
		sfx.play("click")
		state = next
		_elapsed = 0.0
		_commit()


## Playtest control: advances time without granting anything.
func debug_advance(months: int) -> void:
	_advance(months)


func _advance(months: int) -> void:
	var before: Dictionary = state
	state = GameEngine.advance_simulation(state, months)
	if state == before:
		return
	var fresh: Array = state.log.filter(func(e: Dictionary) -> bool: return e.id >= before.nextLogId)
	if before.status != state.status and GameEngine.is_over(state):
		sfx.play("end")
		last_result = SaveData.result_for(state)
	elif fresh.any(func(e: Dictionary) -> bool: return classify(e.message) == "threat"):
		sfx.play("threat")
	elif fresh.any(func(e: Dictionary) -> bool: return classify(e.message) == "site"):
		sfx.play("site")
	elif fresh.any(func(e: Dictionary) -> bool: return classify(e.message) == "omen"):
		sfx.play("omen")
	_commit()
	events.emit(fresh)


func _apply(result: Dictionary) -> void:
	if result.ok:
		state = result.state
		sfx.play("act")
		_commit()
		events.emit([state.log[-1]])
	else:
		sfx.play("refuse")
		events.emit([{ "id": -1, "month": state.elapsedMonths, "message": result.message, "refused": true }])


func set_audio(muted: bool, volume: float) -> void:
	settings = { "muted": muted, "volume": volume }
	_apply_audio()
	_commit()


func complete_tutorial() -> void:
	tutorial_complete = true
	_commit()


func _apply_audio() -> void:
	sfx.muted = settings.muted
	sfx.volume = settings.volume / 100.0


func _commit() -> void:
	if not blocked:
		var save := { "version": 2, "state": state, "settings": settings, "tutorialComplete": tutorial_complete, "lastResult": last_result }
		if not SaveData.save_run(save):
			blocked = true
			storage_notice = "Saving is unavailable. This session continues in memory."
	changed.emit()


## Sorts world-event log lines for toasts and sound. Matches the engine's fixed message text.
static func classify(message: String) -> String:
	for kind in GameData.THREAT_NAMES:
		if message.begins_with(GameData.THREAT_NAMES[kind]):
			return "threat"
	if message.contains(" loses ") and message.contains(" followers to "):
		return "loss"
	for kind in GameData.OPPORTUNITY_NAMES:
		if message.begins_with(GameData.OPPORTUNITY_NAMES[kind]):
			return "faded" if message.contains(" passes in ") else "omen"
	if message.contains("is prepared"):
		return "site"
	if message.begins_with("The stars align") or message.begins_with("The final rite begins"):
		return "phase"
	if message.begins_with("Three surviving") or message.begins_with("The world knows") or message.begins_with("The alignment has passed"):
		return "end"
	return "info"


func export_report() -> void:
	var report := SaveData.export_telemetry(state)
	var filename := "cthulhu-playtest-%s-m%d.json" % [state.seed, state.elapsedMonths]
	if OS.has_feature("web"):
		JavaScriptBridge.download_buffer(report.to_utf8_buffer(), filename, "application/json")
		return
	DirAccess.make_dir_recursive_absolute("user://reports")
	var path := "user://reports/" + filename
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file:
		file.store_string(report)
		file.close()
		OS.shell_show_in_file_manager(ProjectSettings.globalize_path(path))


# --- Screenshot harness (debug builds) ----------------------------------------------------
# godot --path godot -- --shot=/tmp/a.png --scenario=mid [--select=europe] [--dialog=menu]

func _shot_args() -> Dictionary:
	var parsed := {}
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--") and arg.contains("="):
			parsed[arg.substr(2, arg.find("=") - 2)] = arg.substr(arg.find("=") + 1)
	return parsed if parsed.has("shot") and OS.is_debug_build() else {}


func _stage_shot() -> void:
	var scenario: String = _shot.get("scenario", "start")
	tutorial_complete = scenario != "guide"
	match scenario:
		"start":
			show_start()
		"placing", "guide":
			state = GameEngine.create_game_state("normal", "omen")
			show_game()
		"early":
			state = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "omen"), "africa").state
			state = GameEngine.perform_action(state, "recruit", "africa").state
			state = GameEngine.advance_simulation(state, 14)
			show_game()
		"awakening":
			state = Playthrough.play("early", "rlyeh", 206)
			show_game()
		_:
			var months := { "mid": 96, "late": 170, "alignment": 200, "won": B.DEADLINE, "lost": B.DEADLINE }
			state = Playthrough.play("early", "resume" if scenario == "lost" else "rlyeh", months.get(scenario, 96))
			show_game()
	if _shot.has("select") and _screen is GameScreen:
		_screen.select(_shot.select)
	if _shot.has("dialog") and _screen is GameScreen:
		_screen.open_dialog(_shot.dialog)
	for _frame in 20:
		await get_tree().process_frame
	await get_tree().create_timer(float(_shot.get("wait", "0.6"))).timeout
	get_viewport().get_texture().get_image().save_png(_shot.shot)
	get_tree().quit()
