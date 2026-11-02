extends SceneTree
## Real HTTP smoke test against the disposable receiver described in docs/playtest-lab.md.
var game
var client: PlaytestClient
var deadline := 0
var output := ""

func _init() -> void:
	_start.call_deferred()

func _start() -> void:
	var options: Dictionary = load("res://tests/test_playtest.gd").configuration()
	options.endpoint = "http://127.0.0.1:58483/api/playtests/v1/batches/"
	output = OS.get_environment("GTC_E4_NATIVE_OUTPUT")
	game = load("res://main.gd").new()
	game.save_path = "user://e4-native-" + PlaytestCapture.identifier() + ".json"
	game.lab_platform.initialize(game.save_path)
	game.sfx = Sfx.new()
	game.sfx.muted = true
	game.add_child(game.sfx)
	game.lab.configure(options)
	game.lab.accept()
	game.begin("hard", "e4-native-http")
	game.place("africa")
	game.act("recruit", "africa")
	game.lab.tick(1000, "planning")
	game.abandon()
	client = PlaytestClient.new()
	client.setup(game.lab, game.lab_platform, game._commit)
	root.add_child(client)
	deadline = Time.get_ticks_msec() + 25000

func _process(_delta: float) -> bool:
	if client == null: return false
	if game.lab.pending_events() == 0 and not game.lab.data.receipt.is_empty() and client.notice == "Receipt confirmed and saved locally.":
		var evidence := {"transport": "Godot HTTPRequest to local Gunicorn + PostgreSQL", "receipt": game.lab.data.receipt,
			"pending": game.lab.pending_events(), "saved_generation": game.lab_platform.saved_generation, "gameplay_clock": "synthetic 1000ms planning"}
		if output:
			var file := FileAccess.open(output, FileAccess.WRITE)
			file.store_string(JSON.stringify(evidence, "  "))
			file.close()
		print("PASS native real HTTP receipt")
		_finish(0)
	elif Time.get_ticks_msec() > deadline:
		printerr("Native HTTP failed: ", game.playtest_status(), " ", client.notice)
		_finish(1)
	return false

func _finish(code: int) -> void:
	client.cancel()
	client.queue_free()
	game.lab_platform.dispose()
	DirAccess.remove_absolute(game.save_path)
	game.free()
	quit(code)
