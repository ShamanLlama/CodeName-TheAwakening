class_name Js
## JavaScript-compatible number helpers. The rules were authored in TypeScript; matching its
## rounding and number-to-text conversion keeps seeded runs and log text identical.

## Math.round: halves round towards positive infinity.
static func round_half_up(x: float) -> float:
	return floor(x + 0.5)


## Number.prototype.toString for the plain decimals the game produces.
static func num(x: float) -> String:
	if x == floor(x) and absf(x) < 1e15:
		return "%d" % int(x)
	for digits in range(1, 18):
		var text := String.num(x, digits)
		if text.to_float() == x:
			return text
	return String.num(x, 17)


## Number.prototype.toFixed: ties round away from zero, unlike printf's "%.2f".
static func fixed(x: float, digits: int) -> String:
	var negative := x < 0.0
	var scaled := int(floor(absf(x) * pow(10.0, digits) + 0.5))
	var text := str(scaled)
	if digits > 0:
		while text.length() <= digits:
			text = "0" + text
		text = text.left(text.length() - digits) + "." + text.right(digits)
	return ("-" if negative else "") + text
