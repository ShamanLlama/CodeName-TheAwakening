class_name Format
## Number and time formatting for the interface.


## Whole people, with thousands separators: 12,345.
static func count(value: float) -> String:
	var digits := str(int(floor(value)))
	var negative := digits.begins_with("-")
	if negative:
		digits = digits.substr(1)
	var grouped := ""
	while digits.length() > 3:
		grouped = "," + digits.right(3) + grouped
		digits = digits.left(digits.length() - 3)
	return ("-" if negative else "") + digits + grouped


static func rate(value: float) -> String:
	return ("+" if value >= 0 else "") + "%.2f" % value


static func months(value: int) -> String:
	return "%d month%s" % [value, "" if value == 1 else "s"]
