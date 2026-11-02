class_name PlaytestPlatform
extends RefCounted
## One writer per save; native atomic file replacement or web IndexedDB transaction.
var ready := false
var writable := false
var failed := false
var saved_generation := 0
var generation := 0
var path := ""
var _token := ""
var _lock_path := ""
var _bridge

func initialize(save_path: String) -> void:
	path = save_path
	if OS.has_feature("web"):
		JavaScriptBridge.eval(FileAccess.get_file_as_string("res://core/playtest_browser.js"), true)
		_bridge = JavaScriptBridge.get_interface("AwakeningLab")
		_bridge.init(path)
		return
	_lock_path = ProjectSettings.globalize_path(path + ".lock")
	if DirAccess.dir_exists_absolute(_lock_path):
		var owner = JSON.parse_string(FileAccess.get_file_as_string(_lock_path + "/owner.json"))
		if typeof(owner) == TYPE_DICTIONARY and typeof(owner.get("pid")) in [TYPE_INT, TYPE_FLOAT] and OS.has_feature("linux") and not DirAccess.dir_exists_absolute("/proc/" + str(int(owner.pid))):
			DirAccess.remove_absolute(_lock_path + "/owner.json")
			DirAccess.remove_absolute(_lock_path)
	if DirAccess.make_dir_absolute(_lock_path) == OK:
		_token = PlaytestCapture.identifier()
		var file := FileAccess.open(_lock_path + "/owner.json", FileAccess.WRITE)
		if file:
			file.store_string(JSON.stringify({"pid": OS.get_process_id(), "token": _token}))
			file.flush()
			writable = file.get_error() == OK
			file.close()
	ready = true

func poll() -> void:
	if _bridge == null: return
	var status: Dictionary = JSON.parse_string(_bridge.status())
	ready = status.ready
	writable = status.writable
	failed = status.failed
	saved_generation = int(status.saved)

func load_save() -> Dictionary:
	if _bridge != null and _bridge.load() != null:
		var raw: String = _bridge.load()
		var decoded = SaveData.decode_save(raw)
		if decoded == null:
			return {"save": SaveData.default_save(), "blocked": true, "notice": "The browser save is unreadable. It has been retained; this session continues in memory."}
		return {"save": decoded, "blocked": false, "notice": SaveData.take_retired_notice(decoded)}
	return SaveData.load_run(path)

func persist(save: Dictionary) -> int:
	if not ready or not writable or failed: return -1
	generation += 1
	if _bridge != null:
		_bridge.save(JSON.stringify(save), generation)
	elif SaveData.save_run(save, path):
		saved_generation = generation
	else:
		failed = true
		writable = false
		return -1
	return generation

func now() -> int:
	return int(_bridge.now()) if _bridge != null else Time.get_ticks_msec()

func visibility_events() -> Array:
	return JSON.parse_string(_bridge.takeVisibility()) if _bridge != null else []

func send(id: String, endpoint: String, body: String) -> void:
	_bridge.send(id, endpoint, body)

func responses() -> Array:
	return JSON.parse_string(_bridge.poll()) if _bridge != null else []

func cancel() -> void:
	if _bridge != null: _bridge.cancel()

func dispose() -> void:
	if _bridge != null:
		_bridge.dispose()
	elif _token and FileAccess.file_exists(_lock_path + "/owner.json"):
		var owner = JSON.parse_string(FileAccess.get_file_as_string(_lock_path + "/owner.json"))
		if typeof(owner) == TYPE_DICTIONARY and owner.get("token") == _token:
			DirAccess.remove_absolute(_lock_path + "/owner.json")
			DirAccess.remove_absolute(_lock_path)
	writable = false
