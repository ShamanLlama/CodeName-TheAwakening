extends RefCounted
## Base class for headless tests. Methods named test_* run in file order; see run_tests.gd.

var failures: Array[String] = []


func check(condition: bool, message: String) -> bool:
	if not condition:
		failures.append(message)
	return condition


func check_eq(actual, expected, message := "") -> bool:
	var diffs: Array[String] = []
	compare(expected, actual, "", diffs, 5)
	if diffs:
		failures.append((message + ": " if message else "") + "; ".join(diffs))
	return diffs.is_empty()


## Deep comparison. Numbers compare by value with a 1e-9 relative tolerance, because JSON
## turns every TypeScript number into a float.
static func compare(expected, actual, path: String, diffs: Array[String], limit: int) -> void:
	if diffs.size() >= limit:
		return
	var te := typeof(expected)
	var ta := typeof(actual)
	var numeric := [TYPE_INT, TYPE_FLOAT]
	if numeric.has(te) and numeric.has(ta):
		if absf(float(expected) - float(actual)) > 1e-9 * maxf(1.0, absf(float(expected))):
			diffs.append("%s: expected %s, got %s" % [path, var_to_str(expected), var_to_str(actual)])
	elif te != ta:
		diffs.append("%s: expected %s, got %s" % [path, var_to_str(expected), var_to_str(actual)])
	elif te == TYPE_DICTIONARY:
		for key in expected:
			if not actual.has(key):
				diffs.append("%s.%s: missing" % [path, key])
			else:
				compare(expected[key], actual[key], path + "." + str(key), diffs, limit)
		for key in actual:
			if not expected.has(key):
				diffs.append("%s.%s: unexpected %s" % [path, key, var_to_str(actual[key])])
	elif te == TYPE_ARRAY:
		if expected.size() != actual.size():
			diffs.append("%s: expected %d items, got %d" % [path, expected.size(), actual.size()])
		for i in mini(expected.size(), actual.size()):
			compare(expected[i], actual[i], "%s[%d]" % [path, i], diffs, limit)
	elif expected != actual:
		diffs.append("%s: expected %s, got %s" % [path, var_to_str(expected), var_to_str(actual)])
