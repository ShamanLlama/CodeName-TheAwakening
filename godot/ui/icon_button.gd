class_name IconButton
extends Button
## A square button whose glyph is drawn in code: pause, play, fast, menu or close.
## Brass on the dark desk by default; ink when it sits on paper.

const P = preload("res://ui/palette.gd")

var glyph := "play"
var on_paper := false
var active := false:
	set(value):
		active = value
		queue_redraw()


static func make(kind: String, hint: String, on_press: Callable, paper := false) -> IconButton:
	var node := IconButton.new()
	node.glyph = kind
	node.on_paper = paper
	node.tooltip_text = hint
	node.custom_minimum_size = Vector2(40, 36)
	node.flat = true
	node.focus_mode = Control.FOCUS_NONE
	node.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
	node.pressed.connect(on_press)
	return node


func _draw() -> void:
	var color: Color
	if on_paper:
		color = P.FAINT if disabled else P.TEXT if is_hovered() else P.MUTED
	else:
		color = P.BRASS if active else Color(P.LAMP_DIM, 0.35) if disabled else P.LAMP if is_hovered() else P.LAMP_DIM
	if active:
		draw_rect(Rect2(Vector2.ZERO, size), Color(P.BRASS, 0.1))
		draw_rect(Rect2(Vector2(4, size.y - 3), Vector2(size.x - 8, 1.5)), P.BRASS)
		draw_rect(Rect2(Vector2(4, size.y - 0.75), Vector2(size.x - 8, 0.75)), P.BRASS)
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
