extends SceneTree
## Headless test runner:
##   godot --headless --path godot -s res://tests/run_tests.gd [-- test_name_filter]
## Exits 0 when every test passes, 1 otherwise.


func _init() -> void:
	var filter := ""
	var args := OS.get_cmdline_user_args()
	if args:
		filter = args[0]
	var passed := 0
	var failed := 0
	var files := Array(DirAccess.get_files_at("res://tests")).filter(func(f: String) -> bool: return f.begins_with("test_") and f.ends_with(".gd") and f != "test_case.gd")
	files.sort()
	for file in files:
		var script: GDScript = load("res://tests/" + file)
		if script == null or not script.can_instantiate():
			failed += 1
			print("  FAIL  %s: script failed to load" % file)
			continue
		for method in script.get_script_method_list():
			var test_name: String = method.name
			if not test_name.begins_with("test_") or filter and not (file + ":" + test_name).contains(filter):
				continue
			var test = script.new()
			var started := Time.get_ticks_msec()
			test.call(test_name)
			var elapsed := Time.get_ticks_msec() - started
			if test.failures.is_empty():
				passed += 1
				print("  ok    %s:%s (%d ms)" % [file, test_name, elapsed])
			else:
				failed += 1
				print("  FAIL  %s:%s" % [file, test_name])
				for failure in test.failures.slice(0, 8):
					print("        " + failure)
	print("\n%d passed, %d failed" % [passed, failed])
	quit(1 if failed or passed == 0 else 0)
