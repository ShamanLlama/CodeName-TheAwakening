class_name Kit
## Small builders for code-constructed interface nodes.

const P = preload("res://ui/palette.gd")

## Touch devices pick an action first and confirm it, because a tap is also a hover. Set by
## main before the theme and the first screen are built.
static var touch := false


static func label(text: String, size := 16, color := P.TEXT, weight := "body") -> Label:
	var node := Label.new()
	node.text = text
	node.add_theme_font_override("font", P.font(weight))
	node.add_theme_font_size_override("font_size", size)
	node.add_theme_color_override("font_color", color)
	return node


## Typewritten capitals used for field names and readout captions, as on a filed form.
static func caption(text: String, color := P.MUTED) -> Label:
	return label(text.to_upper(), 12, color, "type_bold")


static func body(text: String, size := 15, color := P.TEXT, weight := "body") -> Label:
	var node := label(text, size, color, weight)
	node.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	return node


static func button(text: String, on_press: Callable = Callable()) -> Button:
	var node := Button.new()
	node.text = text
	node.focus_mode = Control.FOCUS_NONE
	node.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	if on_press.is_valid():
		node.pressed.connect(on_press)
	return node


## A printed label in solid ink: the main call to action. Red for defensive actions.
static func primary(text: String, on_press: Callable = Callable(), accent := P.CULT) -> Button:
	var node := button(text, on_press)
	var pad := Vector4(18, 9, 18, 10)
	node.add_theme_stylebox_override("normal", UiTheme.box(accent, accent.darkened(0.35), 1, pad))
	node.add_theme_stylebox_override("hover", UiTheme.box(accent.lightened(0.12), P.TEXT, 1, pad))
	node.add_theme_stylebox_override("pressed", UiTheme.box(accent.darkened(0.2), P.TEXT, 1, pad))
	node.add_theme_stylebox_override("disabled", UiTheme.box(Color(P.PAPER_SHADE, 0.5), Color(P.PAPER_EDGE, 0.5), 1, pad))
	for state in ["font_color", "font_hover_color", "font_pressed_color", "font_focus_color"]:
		node.add_theme_color_override(state, P.PAPER_LIGHT)
	node.add_theme_font_override("font", P.font("display_sc"))
	node.add_theme_font_size_override("font_size", 21)
	return node


## A rubber stamp: a ruled box in one ink, typed capitals.
static func stamp(text: String, on_press: Callable, accent: Color) -> Button:
	var node := button(text, on_press)
	var pad := Vector4(10, 4, 10, 4)
	node.add_theme_stylebox_override("normal", UiTheme.box(Color(accent, 0.04), accent, 2, pad))
	node.add_theme_stylebox_override("hover", UiTheme.box(Color(accent, 0.16), accent, 2, pad))
	node.add_theme_stylebox_override("pressed", UiTheme.box(Color(accent, 0.28), accent, 2, pad))
	node.add_theme_stylebox_override("disabled", UiTheme.box(Color.TRANSPARENT, Color(accent, 0.3), 2, pad))
	node.add_theme_font_override("font", P.font("type_bold"))
	node.add_theme_font_size_override("font_size", 14)
	node.add_theme_color_override("font_color", accent)
	node.add_theme_color_override("font_hover_color", accent.darkened(0.2))
	node.add_theme_color_override("font_pressed_color", accent.darkened(0.3))
	node.add_theme_color_override("font_disabled_color", Color(accent, 0.4))
	return node


static func hbox(gap := 8) -> HBoxContainer:
	var node := HBoxContainer.new()
	node.add_theme_constant_override("separation", gap)
	return node


static func vbox(gap := 8) -> VBoxContainer:
	var node := VBoxContainer.new()
	node.add_theme_constant_override("separation", gap)
	return node


## A sheet of aged paper. Its grain does not reach the text on it.
static func panel(fill := P.PAPER, edge := P.PAPER_EDGE, pad := Vector4(16, 14, 16, 14)) -> PanelContainer:
	var node := PanelContainer.new()
	node.add_theme_stylebox_override("panel", UiTheme.box(fill, edge, 1, pad))
	node.material = P.paper()
	return node


static func spacer(horizontal := true) -> Control:
	var node := Control.new()
	if horizontal:
		node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	else:
		node.size_flags_vertical = Control.SIZE_EXPAND_FILL
	return node


## A printer's rule: thick over thin, or a single hairline.
static func rule(color := Color(P.TEXT, 0.45), double := true) -> Control:
	var node := Rule.new()
	node.color = color
	node.double = double
	return node


## Ruled inner border with corner diamonds, as on a printed frontispiece. Add it as the
## first child of a PanelContainer; it draws around the panel's padding.
static func frame(panel_node: PanelContainer, color := Color(P.TEXT, 0.55), inset := 7.0) -> void:
	var node := Frame.new()
	node.color = color
	node.inset = inset
	panel_node.add_child(node)
	panel_node.move_child(node, 0)


static func clear(node: Node) -> void:
	for child in node.get_children():
		node.remove_child(child)
		child.queue_free()


class Rule extends Control:
	var color := Color.BLACK
	var double := true

	func _init() -> void:
		custom_minimum_size = Vector2(0, 5)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var y := size.y * 0.5
		if double:
			draw_rect(Rect2(0, y - 2, size.x, 1.5), color)
			draw_rect(Rect2(0, y + 1.5, size.x, 0.75), color)
		else:
			draw_rect(Rect2(0, y, size.x, 1), color)


class Frame extends Control:
	var color := Color.BLACK
	var inset := 7.0

	func _init() -> void:
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var style: StyleBox = get_parent().get_theme_stylebox("panel")
		var outer := Rect2(-Vector2(style.content_margin_left, style.content_margin_top),
			size + Vector2(style.content_margin_left + style.content_margin_right, style.content_margin_top + style.content_margin_bottom))
		var a := outer.grow(-inset)
		var b := outer.grow(-inset - 3)
		draw_rect(a, color, false, 1.5)
		draw_rect(b, Color(color, color.a * 0.6), false, 0.75)
		for corner in [a.position, Vector2(a.end.x, a.position.y), a.end, Vector2(a.position.x, a.end.y)]:
			var diamond := PackedVector2Array([corner + Vector2(0, -5), corner + Vector2(5, 0), corner + Vector2(0, 5), corner + Vector2(-5, 0)])
			draw_colored_polygon(diamond, color)
