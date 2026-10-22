class_name Kit
## Small builders for code-constructed interface nodes.

const P = preload("res://ui/palette.gd")


static func label(text: String, size := 16, color := P.TEXT, weight := "regular") -> Label:
	var node := Label.new()
	node.text = text
	node.add_theme_font_override("font", P.font(weight))
	node.add_theme_font_size_override("font_size", size)
	node.add_theme_color_override("font_color", color)
	return node


## Small capitals used for section names and readout captions.
static func caption(text: String, color := P.MUTED) -> Label:
	return label(text.to_upper(), 13, color, "condensed_semibold")


static func body(text: String, size := 15, color := P.TEXT) -> Label:
	var node := label(text, size, color)
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


## A filled call-to-action in the cult colour, or red for defensive actions.
static func primary(text: String, on_press: Callable = Callable(), accent := P.CULT) -> Button:
	var node := button(text, on_press)
	var pad := Vector4(18, 10, 18, 10)
	node.add_theme_stylebox_override("normal", UiTheme.box(accent, accent, 1, pad))
	node.add_theme_stylebox_override("hover", UiTheme.box(accent.lightened(0.15), accent.lightened(0.15), 1, pad))
	node.add_theme_stylebox_override("pressed", UiTheme.box(accent.darkened(0.15), accent, 1, pad))
	node.add_theme_stylebox_override("disabled", UiTheme.box(Color("12171e"), Color("1e252f"), 1, pad))
	for state in ["font_color", "font_hover_color", "font_pressed_color", "font_focus_color"]:
		node.add_theme_color_override(state, P.INK)
	node.add_theme_font_override("font", P.font("condensed_bold"))
	node.add_theme_font_size_override("font_size", 18)
	return node


static func outline(text: String, on_press: Callable, accent: Color) -> Button:
	var node := button(text, on_press)
	var pad := Vector4(12, 7, 12, 7)
	node.add_theme_stylebox_override("normal", UiTheme.box(Color(accent, 0.08), Color(accent, 0.7), 1, pad))
	node.add_theme_stylebox_override("hover", UiTheme.box(Color(accent, 0.18), accent, 1, pad))
	node.add_theme_stylebox_override("pressed", UiTheme.box(Color(accent, 0.28), accent, 1, pad))
	node.add_theme_color_override("font_color", accent.lightened(0.2))
	node.add_theme_color_override("font_hover_color", Color.WHITE)
	return node


static func hbox(gap := 8) -> HBoxContainer:
	var node := HBoxContainer.new()
	node.add_theme_constant_override("separation", gap)
	return node


static func vbox(gap := 8) -> VBoxContainer:
	var node := VBoxContainer.new()
	node.add_theme_constant_override("separation", gap)
	return node


static func panel(fill := P.PANEL, edge := P.EDGE, pad := Vector4(16, 14, 16, 14)) -> PanelContainer:
	var node := PanelContainer.new()
	node.add_theme_stylebox_override("panel", UiTheme.box(fill, edge, 1, pad))
	return node


static func spacer(horizontal := true) -> Control:
	var node := Control.new()
	if horizontal:
		node.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	else:
		node.size_flags_vertical = Control.SIZE_EXPAND_FILL
	return node


static func rule() -> ColorRect:
	var node := ColorRect.new()
	node.color = P.EDGE
	node.custom_minimum_size = Vector2(0, 1)
	return node


static func clear(node: Node) -> void:
	for child in node.get_children():
		node.remove_child(child)
		child.queue_free()
