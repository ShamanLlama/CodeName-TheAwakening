class_name UiTheme
## Builds the game's Theme in code. Controls default to paper and ink, because nearly every
## control lives on a document; the few that sit on the dark desk override their colours.

const P = preload("res://ui/palette.gd")


static func box(fill: Color, edge := Color.TRANSPARENT, width := 0, pad := Vector4(10, 6, 10, 6)) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = fill
	style.border_color = edge
	style.set_border_width_all(width)
	style.content_margin_left = pad.x
	style.content_margin_top = pad.y
	style.content_margin_right = pad.z
	style.content_margin_bottom = pad.w
	style.anti_aliasing = false
	return style


static func build() -> Theme:
	var theme := Theme.new()
	theme.default_font = P.font("body")
	theme.default_font_size = 16

	theme.set_color("font_color", "Label", P.TEXT)

	var pad := Vector4(12, 7, 12, 7)
	theme.set_stylebox("normal", "Button", box(P.PAPER_SHADE, P.PAPER_EDGE, 1, pad))
	theme.set_stylebox("hover", "Button", box(P.PAPER_SHADE.darkened(0.08), P.TEXT, 1, pad))
	theme.set_stylebox("pressed", "Button", box(P.CULT_WASH, P.CULT, 1, pad))
	theme.set_stylebox("hover_pressed", "Button", box(P.CULT_WASH, P.CULT, 1, pad))
	theme.set_stylebox("disabled", "Button", box(Color(P.PAPER_SHADE, 0.45), Color(P.PAPER_EDGE, 0.4), 1, pad))
	theme.set_stylebox("focus", "Button", box(Color.TRANSPARENT, P.CULT, 2))
	theme.set_font("font", "Button", P.font("display_sc"))
	theme.set_font_size("font_size", "Button", 19)
	theme.set_color("font_color", "Button", P.TEXT)
	theme.set_color("font_hover_color", "Button", P.TEXT)
	theme.set_color("font_pressed_color", "Button", P.CULT)
	theme.set_color("font_hover_pressed_color", "Button", P.CULT)
	theme.set_color("font_focus_color", "Button", P.TEXT)
	theme.set_color("font_disabled_color", "Button", P.FAINT)

	theme.set_stylebox("panel", "Panel", box(P.PAPER, P.PAPER_EDGE, 1))
	theme.set_stylebox("panel", "PanelContainer", box(P.PAPER, P.PAPER_EDGE, 1, Vector4(16, 14, 16, 14)))

	theme.set_stylebox("normal", "LineEdit", box(P.PAPER_LIGHT, P.PAPER_EDGE, 1, Vector4(12, 9, 12, 9)))
	theme.set_stylebox("focus", "LineEdit", box(Color.TRANSPARENT, P.TEXT, 1))
	theme.set_font("font", "LineEdit", P.font("type"))
	theme.set_color("font_color", "LineEdit", P.TEXT)
	theme.set_color("font_placeholder_color", "LineEdit", P.FAINT)
	theme.set_color("caret_color", "LineEdit", P.TEXT)
	theme.set_color("selection_color", "LineEdit", Color(P.CULT, 0.3))

	theme.set_stylebox("panel", "TooltipPanel", box(P.PAPER_LIGHT, P.PAPER_EDGE, 1, Vector4(12, 9, 12, 9)))
	theme.set_color("font_color", "TooltipLabel", P.TEXT)
	theme.set_font("font", "TooltipLabel", P.font("body"))
	theme.set_font_size("font_size", "TooltipLabel", 15)

	theme.set_stylebox("background", "ProgressBar", box(Color(P.TEXT, 0.12), Color.TRANSPARENT, 0, Vector4.ZERO))
	theme.set_stylebox("fill", "ProgressBar", box(P.CULT, Color.TRANSPARENT, 0, Vector4.ZERO))
	theme.set_constant("outline_size", "ProgressBar", 0)

	theme.set_stylebox("slider", "HSlider", box(Color(P.TEXT, 0.25), Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))
	theme.set_stylebox("grabber_area", "HSlider", box(P.CULT, Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))
	theme.set_stylebox("grabber_area_highlight", "HSlider", box(P.CULT, Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))

	# Check boxes and the slider grabber are drawn here, in ink on paper, instead of using the
	# engine's SVG icons: the web build ships without the SVG module to download faster.
	var box := 22 if Kit.touch else 18
	theme.set_icon("unchecked", "CheckBox", _check_icon(box, false, P.TEXT))
	theme.set_icon("checked", "CheckBox", _check_icon(box, true, P.CULT))
	theme.set_icon("unchecked_disabled", "CheckBox", _check_icon(box, false, P.FAINT))
	theme.set_icon("checked_disabled", "CheckBox", _check_icon(box, true, P.FAINT))
	theme.set_color("font_color", "CheckBox", P.TEXT)
	theme.set_color("font_hover_color", "CheckBox", P.TEXT)
	theme.set_color("font_pressed_color", "CheckBox", P.TEXT)
	theme.set_color("font_hover_pressed_color", "CheckBox", P.TEXT)
	theme.set_color("font_disabled_color", "CheckBox", P.FAINT)
	var grab := 22 if Kit.touch else 16
	theme.set_icon("grabber", "HSlider", _dot_icon(grab, P.CULT))
	theme.set_icon("grabber_highlight", "HSlider", _dot_icon(grab, P.CULT.lightened(0.15)))
	theme.set_icon("grabber_disabled", "HSlider", _dot_icon(grab, P.FAINT))

	# On touch screens a wider, darker bar shows there is more to read below.
	var thumb := 6 if Kit.touch else 3
	var scroll := box(Color(P.TEXT, 0.06) if Kit.touch else Color.TRANSPARENT, Color.TRANSPARENT, 0, Vector4(thumb, 0, thumb, 0))
	theme.set_stylebox("scroll", "VScrollBar", scroll)
	theme.set_stylebox("grabber", "VScrollBar", box(Color(P.TEXT, 0.4 if Kit.touch else 0.22), Color.TRANSPARENT, 0, Vector4(thumb, thumb, thumb, thumb)))
	theme.set_stylebox("grabber_highlight", "VScrollBar", box(Color(P.TEXT, 0.5), Color.TRANSPARENT, 0, Vector4(thumb, thumb, thumb, thumb)))
	theme.set_stylebox("grabber_pressed", "VScrollBar", box(Color(P.TEXT, 0.6), Color.TRANSPARENT, 0, Vector4(thumb, thumb, thumb, thumb)))
	return theme


## A ruled square, filled with a smaller inked square when checked.
static func _check_icon(side: int, checked: bool, ink: Color) -> ImageTexture:
	var image := Image.create_empty(side, side, false, Image.FORMAT_RGBA8)
	image.fill(Color(P.PAPER_LIGHT, 1.0))
	for i in side:
		for edge in [0, 1, side - 2, side - 1]:
			image.set_pixel(i, edge, ink)
			image.set_pixel(edge, i, ink)
	if checked:
		var inset := int(side * 0.28)
		image.fill_rect(Rect2i(inset, inset, side - inset * 2, side - inset * 2), ink)
	return ImageTexture.create_from_image(image)


## A filled disc with a paper rim, for slider grabbers.
static func _dot_icon(side: int, ink: Color) -> ImageTexture:
	var image := Image.create_empty(side, side, false, Image.FORMAT_RGBA8)
	var center := Vector2(side, side) * 0.5
	for y in side:
		for x in side:
			var d := Vector2(x + 0.5, y + 0.5).distance_to(center)
			var r := side * 0.5
			if d <= r - 1.0:
				image.set_pixel(x, y, ink if d <= r - 3.0 else P.PAPER_LIGHT)
			elif d <= r:
				image.set_pixel(x, y, Color(ink, r - d))
	return ImageTexture.create_from_image(image)
