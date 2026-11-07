extends Control
## Root of the game: owns the run, the monthly clock, saving, sound and screen switching.
## Screens read `state` and call the command methods below; every change emits `changed`.

signal changed
signal events(entries: Array)

const B = preload("res://core/balance.gd")
const StartScreen = preload("res://ui/start_screen.gd")
const GameScreen = preload("res://ui/game_screen.gd")
const P = preload("res://ui/palette.gd")
## Interface sizes the player can pick. The canvas is designed at 1600x900; a larger size shows
## the same layout over fewer, bigger units.
const UI_SCALES: Array[float] = [1.0, 1.3, 1.6, 2.0]
## A layout narrower or shorter than this, in canvas units, switches to the compact layout.
const COMPACT_SIZE := Vector2(1240, 700)
## The smallest canvas the compact layout fits.
const MIN_CANVAS_HEIGHT := 430.0

var state = null
var settings := { "muted": false, "volume": 40.0, "uiScale": 1.0, "reducedMotion": false, "hints": [] }
var tutorial_complete := false
var last_result = null
var blocked := false
var storage_notice := ""
var save_path := SaveData.SAVE_PATH
var lab_options := {}
var lab := PlaytestCapture.new()
var lab_platform := PlaytestPlatform.new()
var lab_client: PlaytestClient
var _lab_at := 0
var _lab_mode := "excluded"
var _lab_focused := true
var _lab_visible := true
var _lab_saved_at := 0
var _lab_checkpoint_at := 0
var sfx: Sfx

var _elapsed := 0.0
var _holds := 0
var _screen: Control
var _shot := {}
var _rotate: Control


func _ready() -> void:
	_shot = _shot_args()
	Kit.touch = DisplayServer.is_touchscreen_available() or _shot.has("touch")
	theme = UiTheme.build()
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	get_window().size_changed.connect(_on_window_resized)
	sfx = Sfx.new()
	add_child(sfx)
	if _shot:
		blocked = true
	else:
		lab_platform.initialize(save_path)
		var deadline := Time.get_ticks_msec() + 5000
		while not lab_platform.ready and Time.get_ticks_msec() < deadline:
			lab_platform.poll()
			await get_tree().process_frame
		var loaded := lab_platform.load_save()
		var save: Dictionary = loaded.save
		state = save.state
		settings = SaveData.with_default_settings(save.settings)
		tutorial_complete = save.tutorialComplete
		last_result = save.lastResult
		blocked = loaded.blocked
		storage_notice = loaded.notice
		if not lab_platform.writable or not lab_platform.ready:
			blocked = true
			storage_notice = "Another window owns this save, or storage is unavailable. This window plays in memory; saving and playtest capture are disabled."
		if lab_options.is_empty():
			var configured = JSON.parse_string(FileAccess.get_file_as_string("res://data/playtest-config.json"))
			lab_options = configured if typeof(configured) == TYPE_DICTIONARY else {}
		lab.configure(lab_options, save.get("playtest", {}))
		if not blocked: lab.restore()
		_lab_at = lab_platform.now()
		_lab_saved_at = _lab_at
		_lab_checkpoint_at = _lab_at
		lab_client = PlaytestClient.new()
		lab_client.setup(lab, lab_platform, _commit)
		add_child(lab_client)
	_apply_audio()
	if _shot.has("scale"):
		settings.uiScale = float(_shot.scale)
	if _shot.has("motion"):
		settings.reducedMotion = _shot.motion == "reduced"
	Kit.reduced_motion = settings.reducedMotion
	_fit_window()
	if _shot:
		_stage_shot.call_deferred()
	else:
		show_start()


func _notification(what: int) -> void:
	if what in [NOTIFICATION_APPLICATION_FOCUS_OUT, NOTIFICATION_APPLICATION_FOCUS_IN]:
		if not OS.has_feature("web"):
			_lab_touch()
			_lab_focused = what == NOTIFICATION_APPLICATION_FOCUS_IN
			_lab_sync()
			_lab_checkpoint("focus_changed")
		if what == NOTIFICATION_APPLICATION_FOCUS_OUT and state != null and GameEngine.is_active(state) and state.speed != 0:
			set_speed(0)


func _exit_tree() -> void:
	if lab_client != null: lab_client.cancel()
	lab_platform.dispose()


func _process(delta: float) -> void:
	if lab_platform.failed and not blocked:
		blocked = true
		storage_notice = "Saving is unavailable. This session continues in memory."
		lab.stop_capture("Storage is unavailable. Capture and sending stopped; export the local record.")
		changed.emit()
	_lab_touch()
	if lab.capturing():
		var now := lab_platform.now()
		if now - _lab_checkpoint_at >= 20000:
			_lab_checkpoint("periodic")
		if now - _lab_saved_at >= 5000: _commit()
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
	_lab_touch()
	_holds = 0
	_swap(StartScreen.new(self))
	_lab_sync()


func show_game() -> void:
	_lab_touch()
	if state != null: lab.resume(state)
	_holds = 0
	_elapsed = 0.0
	_swap(GameScreen.new(self))
	_lab_sync()
	_commit()


func _swap(screen: Control) -> void:
	if _screen:
		_screen.queue_free()
	_screen = screen
	add_child(screen)
	if _rotate != null and _rotate.visible:
		move_child(_rotate, -1)


# --- Interface size and orientation ------------------------------------------------------

## Applies the chosen interface size and decides whether the canvas needs the compact layout.
## Returns true when the layout changed, so the caller can rebuild the screen.
func _fit_window() -> bool:
	var window := get_window()
	var scale := clampf(float(settings.get("uiScale", 1.0)), 1.0, UI_SCALES[-1])
	# Never shrink the canvas below what the compact layout fits.
	scale = minf(scale, maxf(1.0, _canvas_at(1.0).y / MIN_CANVAS_HEIGHT))
	window.content_scale_factor = scale
	var canvas := _canvas_at(scale)
	var was := Kit.compact
	Kit.compact = canvas.x < COMPACT_SIZE.x or canvas.y < COMPACT_SIZE.y
	_show_rotate_hint()
	return was != Kit.compact


## The canvas size, in layout units, that [param scale] gives in the current window.
func _canvas_at(scale: float) -> Vector2:
	var window := get_window()
	var base := Vector2(window.content_scale_size)
	if base.x <= 0 or base.y <= 0:
		base = Vector2(1600, 900)
	var pixels := Vector2(window.size)
	if pixels.x <= 0 or pixels.y <= 0:
		return base / scale
	# canvas_items with "expand": the base fits the window, then the longer side grows.
	var fit := minf(pixels.x / base.x, pixels.y / base.y) * scale
	return pixels / fit


## The interface sizes that still leave room for the compact layout in this window.
func available_ui_scales() -> Array[float]:
	var largest := maxf(1.0, _canvas_at(1.0).y / MIN_CANVAS_HEIGHT)
	return UI_SCALES.filter(func(value: float) -> bool: return value <= largest + 0.001)


func set_ui_scale(scale: float) -> void:
	settings.uiScale = scale
	_fit_window()
	_commit()
	_rebuild_screen()


func set_reduced_motion(reduced: bool) -> void:
	settings.reducedMotion = reduced
	Kit.reduced_motion = reduced
	_commit()


func _on_window_resized() -> void:
	if _fit_window():
		_rebuild_screen()


## Sizes are fixed when a screen is built, so a new layout rebuilds the current screen.
func _rebuild_screen() -> void:
	if _screen == null:
		return
	theme = UiTheme.build()
	if _screen is GameScreen:
		var region: String = _screen.selected
		var dialog: String = _screen.dialogs.kind
		_holds = 0
		_swap(GameScreen.new(self))
		if region:
			_screen.select.call_deferred(region)
		if dialog:
			_screen.open_dialog.call_deferred(dialog)
	else:
		show_start()


## A phone held upright cannot show the chart at a readable size; ask for landscape.
## The upright canvas is very tall, so the hint is sized to the real screen width.
func _show_rotate_hint() -> void:
	var size := Vector2(get_window().size)
	var upright := Kit.touch and size.x < size.y
	if not upright:
		if _rotate != null:
			_rotate.visible = false
		return
	if _rotate == null:
		_rotate = ColorRect.new()
		_rotate.color = P.DESK
		_rotate.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
		_rotate.mouse_filter = Control.MOUSE_FILTER_STOP
		add_child(_rotate)
	Kit.clear(_rotate)
	var factor := maxf(1.0, _canvas_at(get_window().content_scale_factor).x / 480.0)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var column := Kit.vbox(roundi(18 * factor))
	var glyph := RotateGlyph.new()
	glyph.custom_minimum_size = Vector2(120, 120) * factor
	glyph.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	column.add_child(glyph)
	var text := Kit.label("Turn your phone sideways", roundi(34 * factor), P.LAMP, "display")
	text.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	column.add_child(text)
	var sub := Kit.label("The chart needs the long side of the screen.", roundi(19 * factor), P.LAMP_DIM, "display_italic")
	sub.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	column.add_child(sub)
	center.add_child(column)
	_rotate.add_child(center)
	_rotate.visible = true
	move_child(_rotate, -1)
	if state != null and GameEngine.is_active(state) and state.speed != 0:
		set_speed(0)


## A phone outline turning a quarter.
class RotateGlyph extends Control:
	func _init() -> void:
		custom_minimum_size = Vector2(120, 120)

	func _draw() -> void:
		var k := size.x / 120.0
		var c := size * 0.5
		var ink := Color("bf9a55")
		draw_set_transform(c, 0.0, Vector2(k, k))
		draw_rect(Rect2(Vector2(-22, -38), Vector2(44, 76)), Color(ink, 0.35), false, 2.0)
		draw_set_transform(c, PI / 2, Vector2(k, k))
		draw_rect(Rect2(Vector2(-22, -38), Vector2(44, 76)), ink, false, 2.5)
		draw_set_transform(c, 0.0, Vector2(k, k))
		draw_arc(Vector2.ZERO, 54, -PI * 0.85, -PI * 0.6, 16, ink, 2.0, true)
		var tip := Vector2.from_angle(-PI * 0.6) * 54
		draw_line(tip, tip + Vector2(-9, -2), ink, 2.0, true)
		draw_line(tip, tip + Vector2(-2, 9), ink, 2.0, true)


## Dialogs and the guide hold the clock while open.
func hold() -> void:
	_lab_touch()
	_holds += 1
	_lab_sync()
	_lab_checkpoint("menu_changed")


func release() -> void:
	_lab_touch()
	_holds = maxi(0, _holds - 1)
	_lab_sync()
	_lab_checkpoint("menu_changed")


# --- Commands ----------------------------------------------------------------------------

func begin(difficulty: String, seed: String) -> void:
	_lab_touch()
	if state != null and lab.capturing():
		lab.emit_event("run_abandoned", {"reason": "replaced_by_new_run"}, state, "ended")
	if not _shot and not blocked and not OS.has_feature("web"):
		blocked = not SaveData.retain_recovery(save_path, save_path + ".recovery")
	var chosen := seed.strip_edges()
	if chosen.is_empty():
		chosen = "%08x" % (randi() & 0xFFFFFFFF)
	state = GameEngine.create_game_state(difficulty, chosen)
	if not blocked: lab.start_run(state)
	_lab_at = lab_platform.now()
	_lab_sync()
	_commit()
	show_game()


func abandon() -> void:
	_lab_touch()
	if state != null: lab.emit_event("run_abandoned", {"reason": "explicit_abandon"}, state, "ended")
	state = null
	_commit()
	show_start()


func place(region_id: String) -> void:
	_lab_touch()
	_apply(GameEngine.place_starting_foothold(state, region_id), "starting_region_selected", {"region": region_id})


func act(action_id: String, region_id: String) -> void:
	_lab_touch()
	_apply(GameEngine.perform_action(state, action_id, region_id), "action_performed", {"action": action_id, "region": region_id})


func choose_doctrine(choice: String) -> void:
	_lab_touch()
	_apply(GameEngine.choose_specialization(state, choice), "doctrine_selected", {"doctrine": choice})


func awaken() -> void:
	_lab_touch()
	_apply(GameEngine.start_awakening(state), "awakening_started")


func set_speed(speed: int) -> void:
	_lab_touch()
	var next := GameEngine.set_game_speed(state, speed)
	if next != state:
		sfx.play("click")
		state = next
		_elapsed = 0.0
		_lab_sync()
		_lab_checkpoint("menu_changed")
		_commit()


## Playtest control: advances time without granting anything.
func debug_advance(months: int) -> void:
	_lab_touch()
	if state != null: lab.emit_event("capture_gap", {"reason": "unknown_time", "missing_events": null}, state)
	_advance(months)


func _advance(months: int) -> void:
	_lab_touch()
	var before: Dictionary = state
	state = GameEngine.advance_simulation(state, months)
	if state == before:
		return
	_lab_transition(before)
	_lab_sync()
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


func _apply(result: Dictionary, kind := "", payload := {}) -> void:
	if result.ok:
		var before: Dictionary = state
		state = result.state
		if kind:
			if kind != "starting_region_selected": payload.spent = _lab_spent(before, state)
			lab.emit_event(kind, payload, state)
		_lab_transition(before)
		_lab_sync()
		sfx.play("act")
		_commit()
		events.emit([state.log[-1]])
	else:
		sfx.play("refuse")
		events.emit([{ "id": -1, "month": state.elapsedMonths, "message": result.message, "refused": true }])


func set_audio(muted: bool, volume: float) -> void:
	settings.muted = muted
	settings.volume = volume
	_apply_audio()
	_commit()


## Contextual tips are shown once per device.
func hint_seen(id: String) -> bool:
	return settings.hints.has(id)


func mark_hint(id: String) -> void:
	if not settings.hints.has(id):
		settings.hints.append(id)
		_commit()


func complete_tutorial() -> void:
	_lab_touch()
	if state != null and not tutorial_complete: lab.checkpoint("tutorial_completed", null, state)
	tutorial_complete = true
	_commit()


func _apply_audio() -> void:
	sfx.muted = settings.muted
	sfx.volume = settings.volume / 100.0


func _commit() -> int:
	var generation := -1
	if not blocked:
		generation = lab_platform.persist(_save_envelope())
		_lab_saved_at = lab_platform.now()
		if generation < 0:
			blocked = true
			storage_notice = "Saving is unavailable. This session continues in memory."
			lab.stop_capture("Saving is unavailable. Capture stopped; export the local record.")
	changed.emit()
	return generation


func _save_envelope() -> Dictionary:
	return {"version": 2, "state": state, "settings": settings, "tutorialComplete": tutorial_complete,
		"lastResult": last_result, "playtest": lab.snapshot(state)}


func set_participation(accepted: bool) -> void:
	_lab_touch()
	if accepted:
		if blocked or not lab.accept(): return
		if state != null and _screen is GameScreen: lab.resume(state)
	else:
		if lab_client != null: lab_client.cancel()
		lab.withdraw()
	_lab_at = lab_platform.now()
	_lab_sync()
	_commit()


## The start screen asks once per round before the first run on this device.
func needs_participation_choice() -> bool:
	return lab.configured() and not blocked and not lab.corrupt and not lab.confirmed and not lab.answered()


func decline_participation() -> void:
	lab.decline()
	_commit()


func playtest_status() -> String:
	if lab.warning: return lab.warning
	if not lab.configured(): return "Playtest collection is not configured in this build."
	if blocked: return storage_notice
	if not lab.confirmed: return "Not sharing. Participation is optional; any pending records stay on this device until you join."
	if lab.pending_events() > 0: return "%d records pending. %s" % [lab.pending_events(), lab_client.notice if lab_client != null else ""]
	return "Receipt confirmed and saved locally." if not lab.data.receipt.is_empty() else "Participation enabled. No receipt yet."


func submit_playtest_comment(text: String) -> bool:
	if not lab.queue_comment(text): return false
	return _commit() >= 0


func export_playtest_record() -> void:
	_lab_touch()
	var report := JSON.stringify(_save_envelope(), "  ")
	_download_record(report, "awakening-local-playtest.json")


func _download_record(report: String, filename: String) -> void:
	if OS.has_feature("web"):
		JavaScriptBridge.download_buffer(report.to_utf8_buffer(), filename, "application/json")
	else:
		DirAccess.make_dir_recursive_absolute("user://reports")
		var path := "user://reports/" + filename
		var file := FileAccess.open(path, FileAccess.WRITE)
		if file:
			file.store_string(report)
			file.close()
			OS.shell_show_in_file_manager(ProjectSettings.globalize_path(path))


func _lab_current_mode() -> String:
	if not _lab_visible or not _lab_focused: return "background"
	if state == null or _screen is not GameScreen or GameEngine.is_over(state): return "excluded"
	return "simulation" if _holds == 0 and GameEngine.is_active(state) and state.speed != 0 else "planning"


func _lab_sync() -> void:
	_lab_mode = _lab_current_mode()


func _lab_account(at: int) -> void:
	if at < _lab_at: return
	var milliseconds := at - _lab_at
	if milliseconds > 30000 and _lab_mode != "background" and lab.capturing():
		lab.tick(milliseconds, "excluded")
		lab.emit_event("capture_gap", {"reason": "unknown_time", "missing_events": null}, state)
	else: lab.tick(milliseconds, _lab_mode)
	_lab_at = at


func _lab_touch() -> void:
	if _shot or not lab_platform.ready: return
	for entry in lab_platform.visibility_events():
		_lab_account(int(entry.at))
		var visibility_changed: bool = _lab_visible != entry.visible
		_lab_focused = entry.focused
		_lab_visible = entry.visible
		_lab_sync()
		if state != null: lab.emit_event("time_checkpoint", {"reason": "visibility_changed" if visibility_changed else "focus_changed"}, state)
	_lab_account(lab_platform.now())
	_lab_sync()


func _lab_checkpoint(reason: String) -> void:
	if state == null or not lab.capturing(): return
	lab.emit_event("time_checkpoint", {"reason": reason}, state)
	_lab_checkpoint_at = lab_platform.now()
	_commit()


static func _lab_spent(before: Dictionary, after: Dictionary) -> Dictionary:
	return {"influence": maxf(0, after.telemetry.influenceSpent - before.telemetry.influenceSpent),
		"dread": maxf(0, after.telemetry.dreadSpent - before.telemetry.dreadSpent),
		"followers": maxf(0, after.telemetry.sacrificed - before.telemetry.sacrificed)}


func _lab_transition(before: Dictionary) -> void:
	if before.status == "playing" and state.status == "alignment":
		lab.emit_event("phase_changed", {"from_phase": "playing"}, state)
	if GameEngine.is_over(state):
		lab.emit_event("run_finished", {"outcome": state.status,
			"reason": "awakened" if state.status == "won" else "world_awareness" if state.awareness >= 100 else "deadline"}, state)
		return
	for id in GameData.REGION_IDS:
		var site: Dictionary = state.regions[id]
		var previous: Dictionary = before.regions[id]
		if site.site.progress >= B.SITE_MONTHS and Derived.supported(site) and (previous.site.progress < B.SITE_MONTHS or not Derived.supported(previous)):
			lab.checkpoint("first_site_ready", id, state)


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
	if message.contains("lies dormant") or message.begins_with("The world senses the coming alignment"):
		return "threat"
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
	settings.hints = [] if _shot.has("hints") else ["first_cell", "spread", "threat", "omen", "sites", "doctrine", "awareness", "unsupported", "sacrifice"]
	match scenario:
		"start":
			show_start()
		"consent":
			# A configured round with no answer yet, as a new player sees it after Begin.
			lab.configure({"enabled": true, "endpoint": "https://playtest.example.invalid/api/playtests/v1/batches/",
				"collection_id": "10000000-0000-4000-8000-000000000001", "build_id": "10000000-0000-4000-8000-000000000002",
				"round_id": "10000000-0000-4000-8000-000000000003"})
			blocked = false
			show_start()
			_screen._begin()
		"placing", "guide":
			state = GameEngine.create_game_state("normal", "omen")
			show_game()
		"early":
			state = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "omen"), "africa").state
			state = GameEngine.perform_action(state, "recruit", "africa").state
			state = GameEngine.advance_simulation(state, 14)
			show_game()
		"awakening":
			# This seed's run reaches the final rite; month 206 is six months into it.
			state = Playthrough.play("early", "resume", 206)
			show_game()
		_:
			var months := { "mid": 96, "late": 170, "alignment": 200, "won": B.DEADLINE, "lost": B.DEADLINE }
			# With this strategy the "resume" seed awakens Cthulhu and "rlyeh" runs out of time.
			state = Playthrough.play("early", "resume" if scenario == "won" else "rlyeh", months.get(scenario, 96))
			show_game()
	if _shot.has("select") and _screen is GameScreen:
		_screen.select(_shot.select)
		if _shot.has("choose"):
			for group in _screen.panel.GROUPS:
				if _screen.panel.GROUPS[group].has(_shot.choose):
					_screen.panel._set_group(group)
			_screen.panel._choose(_shot.choose)
	if _shot.has("dialog") and _screen is GameScreen:
		# --dialog=none shows the screen without the dialog a phase opens by itself.
		if _shot.dialog == "none":
			_screen.dialogs.close()
		else:
			_screen.open_dialog(_shot.dialog)
	for _frame in 20:
		await get_tree().process_frame
	await get_tree().create_timer(float(_shot.get("wait", "0.6"))).timeout
	get_viewport().get_texture().get_image().save_png(_shot.shot)
	get_tree().quit()
