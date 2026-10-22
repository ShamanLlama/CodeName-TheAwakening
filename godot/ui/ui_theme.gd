class_name UiTheme
## Builds the game's Theme in code: flat dark surfaces, square corners, one-pixel edges.

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
	theme.default_font = P.font("regular")
	theme.default_font_size = 16

	theme.set_color("font_color", "Label", P.TEXT)

	theme.set_stylebox("normal", "Button", box(P.RAISED, P.EDGE, 1, Vector4(12, 7, 12, 7)))
	theme.set_stylebox("hover", "Button", box(Color("1a2230"), P.EDGE_STRONG, 1, Vector4(12, 7, 12, 7)))
	theme.set_stylebox("pressed", "Button", box(P.CULT_DEEP, P.CULT_DIM, 1, Vector4(12, 7, 12, 7)))
	theme.set_stylebox("hover_pressed", "Button", box(P.CULT_DEEP, P.CULT, 1, Vector4(12, 7, 12, 7)))
	theme.set_stylebox("disabled", "Button", box(Color("0e1218"), Color("1a2029"), 1, Vector4(12, 7, 12, 7)))
	theme.set_stylebox("focus", "Button", box(Color.TRANSPARENT, P.CULT, 1))
	theme.set_font("font", "Button", P.font("condensed_semibold"))
	theme.set_font_size("font_size", "Button", 17)
	theme.set_color("font_color", "Button", P.TEXT)
	theme.set_color("font_hover_color", "Button", Color.WHITE)
	theme.set_color("font_pressed_color", "Button", P.CULT)
	theme.set_color("font_hover_pressed_color", "Button", P.CULT)
	theme.set_color("font_focus_color", "Button", P.TEXT)
	theme.set_color("font_disabled_color", "Button", P.FAINT)

	theme.set_stylebox("panel", "Panel", box(P.PANEL, P.EDGE, 1))
	theme.set_stylebox("panel", "PanelContainer", box(P.PANEL, P.EDGE, 1, Vector4(16, 14, 16, 14)))

	theme.set_stylebox("normal", "LineEdit", box(P.PANEL_SOLID, P.EDGE_STRONG, 1, Vector4(12, 9, 12, 9)))
	theme.set_stylebox("focus", "LineEdit", box(Color.TRANSPARENT, P.CULT, 1))
	theme.set_color("font_color", "LineEdit", P.TEXT)
	theme.set_color("font_placeholder_color", "LineEdit", P.FAINT)
	theme.set_color("caret_color", "LineEdit", P.CULT)

	theme.set_stylebox("panel", "TooltipPanel", box(Color("0d1117"), P.EDGE_STRONG, 1, Vector4(12, 9, 12, 9)))
	theme.set_color("font_color", "TooltipLabel", P.TEXT)
	theme.set_font_size("font_size", "TooltipLabel", 15)

	theme.set_stylebox("background", "ProgressBar", box(Color("10151c"), Color.TRANSPARENT, 0, Vector4.ZERO))
	theme.set_stylebox("fill", "ProgressBar", box(P.CULT, Color.TRANSPARENT, 0, Vector4.ZERO))
	theme.set_constant("outline_size", "ProgressBar", 0)

	theme.set_stylebox("slider", "HSlider", box(P.EDGE_STRONG, Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))
	theme.set_stylebox("grabber_area", "HSlider", box(P.CULT_DIM, Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))
	theme.set_stylebox("grabber_area_highlight", "HSlider", box(P.CULT, Color.TRANSPARENT, 0, Vector4(0, 2, 0, 2)))

	var scroll := box(Color.TRANSPARENT, Color.TRANSPARENT, 0, Vector4.ZERO)
	var grab := box(P.EDGE_STRONG, Color.TRANSPARENT, 0, Vector4(3, 3, 3, 3))
	theme.set_stylebox("scroll", "VScrollBar", scroll)
	theme.set_stylebox("grabber", "VScrollBar", grab)
	theme.set_stylebox("grabber_highlight", "VScrollBar", box(P.MUTED, Color.TRANSPARENT, 0, Vector4(3, 3, 3, 3)))
	theme.set_stylebox("grabber_pressed", "VScrollBar", box(P.TEXT, Color.TRANSPARENT, 0, Vector4(3, 3, 3, 3)))
	return theme
