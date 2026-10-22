class_name IconButton
extends Button
## A square button whose glyph is drawn in code: pause, play, fast, menu or close.

const P = preload("res://ui/palette.gd")

var glyph := "play"
var active := false:
	set(value):
		active = value
		queue_redraw()


static func make(kind: String, hint: String, on_press: Callable) -> IconButton:
	var node := IconButton.new()
	node.glyph = kind
	node.tooltip_text = hint
	node.custom_minimum_size = Vector2(40, 36)
	node.flat = true
	node.focus_mode = Control.FOCUS_NONE
	node.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	node.pressed.connect(on_press)
	return node


func _draw() -> void:
	var color := P.CULT if active else P.FAINT if disabled else P.TEXT if is_hovered() else P.MUTED
	if active:
		draw_rect(Rect2(Vector2.ZERO, size), Color(P.CULT, 0.1))
		draw_rect(Rect2(Vector2(0, size.y - 2), Vector2(size.x, 2)), P.CULT)
	var c := size * 0.5
	match glyph:
		"pause":
			draw_rect(Rect2(c + Vector2(-6, -7), Vector2(4, 14)), color)
			draw_rect(Rect2(c + Vector2(2, -7), Vector2(4, 14)), color)
		"play":
			draw_colored_polygon(PackedVector2Array([c + Vector2(-5, -8), c + Vector2(7, 0), c + Vector2(-5, 8)]), color)
		"fast":
			draw_colored_polygon(PackedVector2Array([c + Vector2(-10, -7), c + Vector2(0, 0), c + Vector2(-10, 7)]), color)
			draw_colored_polygon(PackedVector2Array([c + Vector2(0, -7), c + Vector2(10, 0), c + Vector2(0, 7)]), color)
		"menu":
			for y in [-6, 0, 6]:
				draw_rect(Rect2(c + Vector2(-9, y - 1), Vector2(18, 2)), color)
		"close":
			draw_line(c + Vector2(-6, -6), c + Vector2(6, 6), color, 2.0, true)
			draw_line(c + Vector2(6, -6), c + Vector2(-6, 6), color, 2.0, true)
