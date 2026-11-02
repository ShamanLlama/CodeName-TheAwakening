extends Node
## Test-only web harness. Excluded from production exports.
var game
var bridge
var callbacks := []
var _probe_ready := false

func _ready() -> void:
	game = load("res://main.gd").new()
	game.save_path = "user://e4-browser-probe.json"
	add_child(game)
	while game._screen == null: await get_tree().process_frame
	JavaScriptBridge.eval("globalThis.E4Probe={commands:[],result:'',ready:true,command(value){this.commands.push(value);},take(){return JSON.stringify(this.commands.splice(0));}}", true)
	bridge = JavaScriptBridge.get_interface("E4Probe")
	_probe_ready = true

func _process(_delta: float) -> void:
	if not _probe_ready: return
	var commands: Array = JSON.parse_string(bridge.take())
	for command in commands:
		match command.op:
			"legacy":
				var legacy := SaveData.default_save()
				legacy.state = GameEngine.place_starting_foothold(GameEngine.create_game_state("easy", "e4-legacy"), "oceania").state
				SaveData.save_run(legacy, game.save_path)
				JavaScriptBridge.force_fs_sync()
			"join": game.set_participation(true)
			"withdraw": game.set_participation(false)
			"begin":
				game.tutorial_complete = true
				game.begin("normal", "e4-browser-fixture")
			"place": game.place("africa")
			"act": game.act("recruit", "africa")
			"pause": game.set_speed(0)
			"play": game.set_speed(1)
			"menu": game.hold()
			"resume": game.show_game()
			"advance": game.debug_advance(int(command.get("months", 1)))
			"abandon": game.abandon()
			"flush": game.lab.urgent = true
	bridge.result = JSON.stringify({"blocked": game.blocked, "writable": game.lab_platform.writable,
		"confirmed": game.lab.confirmed, "pending": game.lab.pending_events(), "status": game.playtest_status(),
		"saved_generation": game.lab_platform.saved_generation, "generation": game.lab_platform.generation,
		"clock": game.lab.active().get("clock", {}), "save": game._save_envelope()})
