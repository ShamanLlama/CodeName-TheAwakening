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


## Monthly rates read to one decimal: +4.1. Two decimals suggested a precision nobody plans with.
static func rate(value: float) -> String:
	var rounded := snappedf(value, 0.1)
	return ("+" if rounded >= 0 else "−") + "%.1f" % absf(rounded)


static func months(value: int) -> String:
	return "%d month%s" % [value, "" if value == 1 else "s"]


## The game opens in January 1908. With a 200-month preparation the alignment opens in
## September 1924 and closes in September 1926, around the night R'lyeh rose: 23 March 1925.
const FIRST_YEAR := 1908
const MONTH_NAMES := ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]


## "March 1925"
static func date(month: int) -> String:
	return "%s %d" % [MONTH_NAMES[month % 12], FIRST_YEAR + floori(month / 12.0)]


## "Mar 1925"
static func date_short(month: int) -> String:
	return "%s %d" % [MONTH_NAMES[month % 12].left(3), FIRST_YEAR + floori(month / 12.0)]
