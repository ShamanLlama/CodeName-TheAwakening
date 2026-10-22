extends SceneTree
## Loads every script so parse errors surface: godot --headless --path godot -s res://dev/check_scripts.gd


func _init() -> void:
	var bad := 0
	for dir in ["res://core", "res://map", "res://ui", "res://dev", "res://tests", "res://"]:
		for file in DirAccess.get_files_at(dir):
			if file.ends_with(".gd"):
				var path: String = dir.path_join(file)
				var script: GDScript = load(path)
				if script == null or not script.can_instantiate():
					bad += 1
					print("BROKEN ", path)
	print("%d broken scripts" % bad)
	quit(1 if bad else 0)
