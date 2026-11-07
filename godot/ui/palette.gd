class_name Palette
## Colours and fonts for the whole game: a cult's archive on a lamp-lit desk, 1908–1926.
## Documents are paper printed in ink; the desk, the sea and the bars around them are dark.
## One accent per meaning: verdigris for the cult, oxblood for danger, bruised violet for
## omens and Dread. Each accent has an ink for paper and a brighter tone for the dark.

# The desk, the sea, and text that sits on them.
const DESK := Color("0d0b08")
const DESK_RAISED := Color("1c1711")
const DESK_EDGE := Color("3b3022")
const LAMP := Color("ebdfc2")
const LAMP_DIM := Color("a39278")
const BRASS := Color("bf9a55")
const OCEAN := Color("0f1b1d")
const OCEAN_LINE := Color("1e3234")
const LAND := Color("ab9873")
const LAND_EDGE := Color("5e4c33")

# Paper and the ink printed on it.
const PAPER := Color("d7c7a1")
const PAPER_LIGHT := Color("e4d7b5")
const PAPER_SHADE := Color("c6b38c")
const PAPER_EDGE := Color("7d6a4b")
const TEXT := Color("21180e")
const MUTED := Color("5a4833")
# Decorative ink and disabled labels: 4.7:1 on paper, 3.8:1 on shaded paper.
const FAINT := Color("624f33")

const CULT := Color("34552c")
const CULT_BRIGHT := Color("9cbc63")
const CULT_WASH := Color(0.2, 0.33, 0.17, 0.14)
const DANGER := Color("8e1d14")
const DANGER_BRIGHT := Color("d9483b")
const WARN := Color("6e4608")
const WARN_BRIGHT := Color("dcab4c")
const OMEN := Color("553878")
const OMEN_BRIGHT := Color("ab8fd6")
const OMEN_DEEP := Color("231832")

const PAPER_SHADER := preload("res://ui/paper.gdshader")

static var _fonts: Dictionary = {}
static var _paper: Dictionary = {}


## body: Old Standard, a 1900s textbook face. display: IM Fell, printed antiquarian titles.
## type: Courier Prime, the typewriter in the case files.
static func font(role: String) -> Font:
	if not _fonts.has(role):
		var files := {
			"body": "res://fonts/OldStandard-Regular.ttf", "body_bold": "res://fonts/OldStandard-Bold.ttf",
			"italic": "res://fonts/OldStandard-Italic.ttf", "display": "res://fonts/IMFellEnglish-Regular.ttf",
			"display_italic": "res://fonts/IMFellEnglish-Italic.ttf", "display_sc": "res://fonts/IMFellEnglishSC-Regular.ttf",
			"type": "res://fonts/CourierPrime-Regular.ttf", "type_bold": "res://fonts/CourierPrime-Bold.ttf",
		}
		var file: FontFile = load(files[role])
		var variation := FontVariation.new()
		variation.base_font = file
		variation.fallbacks = [ThemeDB.fallback_font]
		if role == "display_sc":
			variation.spacing_glyph = 1
		_fonts[role] = variation
	return _fonts[role]


## Aged-paper grain for anything drawn with this material. Stronger foxing for old maps.
static func paper(foxing := 0.1) -> ShaderMaterial:
	if not _paper.has(foxing):
		var material := ShaderMaterial.new()
		material.shader = PAPER_SHADER
		material.set_shader_parameter("foxing", foxing)
		_paper[foxing] = material
	return _paper[foxing]
