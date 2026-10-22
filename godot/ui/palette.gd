class_name Palette
## Colours and fonts for the whole game. One accent per meaning:
## cult growth is acid green, danger is red, omens and Dread are violet.

const INK := Color("05070b")
const OCEAN := Color("080c13")
const OCEAN_LINE := Color("101824")
const LAND := Color("161c25")
const LAND_HOVER := Color("1f2733")
const LAND_EDGE := Color("2a3442")
const PANEL := Color(0.043, 0.055, 0.075, 0.94)
const PANEL_SOLID := Color("0b0e13")
const RAISED := Color("131922")
const EDGE := Color("222a35")
const EDGE_STRONG := Color("344051")
const TEXT := Color("e7e9ec")
const MUTED := Color("8b94a3")
const FAINT := Color("566070")

const CULT := Color("c4f135")
const CULT_DIM := Color("6f8a1f")
const CULT_DEEP := Color("1e2a10")
const DANGER := Color("ff4b3e")
const DANGER_DEEP := Color("2c1210")
const WARN := Color("ffb02e")
const OMEN := Color("a68bff")
const OMEN_DEEP := Color("1d1730")

static var _fonts: Dictionary = {}


static func font(weight: String) -> Font:
	if not _fonts.has(weight):
		var files := {
			"regular": "res://fonts/Barlow-Regular.ttf", "medium": "res://fonts/Barlow-Medium.ttf", "semibold": "res://fonts/Barlow-SemiBold.ttf",
			"condensed": "res://fonts/BarlowCondensed-Medium.ttf", "condensed_semibold": "res://fonts/BarlowCondensed-SemiBold.ttf",
			"condensed_bold": "res://fonts/BarlowCondensed-Bold.ttf",
		}
		var file: FontFile = load(files[weight])
		var variation := FontVariation.new()
		variation.base_font = file
		variation.fallbacks = [ThemeDB.fallback_font]
		if weight.begins_with("condensed"):
			variation.spacing_glyph = 1
		_fonts[weight] = variation
	return _fonts[weight]
