extends RefCounted
## Small drawn instruments for the ledger at the bottom of the screen.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")


## A brass dial with a needle. Past 60% it reads in the red; at 100 the run is lost.
class Awareness extends Control:
	var value := 0.0:
		set(next):
			value = next
			queue_redraw()

	func _init() -> void:
		custom_minimum_size = Vector2(96, 52)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _point(center: Vector2, radius: float, percent: float) -> Vector2:
		return center + Vector2.from_angle(PI + PI * percent / 100.0) * radius

	func _draw() -> void:
		var center := Vector2(size.x * 0.5, size.y - 6)
		var radius := minf(size.x * 0.5 - 4, size.y - 10)
		draw_arc(center, radius, PI, TAU, 48, Color(P.TEXT, 0.7), 1.5, true)
		draw_arc(center, radius - 4, PI + PI * 0.6, TAU, 24, Color(P.DANGER, 0.85), 5.0, true)
		for i in 11:
			var outer := _point(center, radius, i * 10.0)
			var inner := _point(center, radius - (7.0 if i % 5 == 0 else 4.0), i * 10.0)
			draw_line(inner, outer, Color(P.TEXT, 0.75), 1.2 if i % 5 == 0 else 0.8, true)
		var tip := _point(center, radius - 3, clampf(value, 0.0, 100.0))
		draw_line(center, tip, P.TEXT, 2.0, true)
		draw_circle(center, 4.5, P.BRASS)
		draw_arc(center, 4.5, 0.0, TAU, 16, P.TEXT, 1.0, true)
		draw_line(Vector2(center.x - radius - 2, center.y + 0.5), Vector2(center.x + radius + 2, center.y + 0.5), Color(P.TEXT, 0.5), 1.0)


## One wax seal per required site: an outline when unfunded, filling as preparation advances,
## verdigris when ready, oxblood when its region no longer supports it.
class Sites extends Control:
	var state: Dictionary = {}

	func _init() -> void:
		custom_minimum_size = Vector2(120, 30)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _seal(c: Vector2, radius: float) -> PackedVector2Array:
		var points := PackedVector2Array()
		for k in 36:
			var angle := k * TAU / 36.0
			points.append(c + Vector2.from_angle(angle) * (radius + (1.2 if k % 2 == 0 else -0.6)))
		return points

	func _star(c: Vector2, radius: float, color: Color) -> void:
		var star := PackedVector2Array()
		for k in 6:
			star.append(c + Vector2.from_angle(-PI / 2 + (k * 2 % 5) * TAU / 5.0) * radius)
		draw_polyline(star, color, 1.2, true)

	func _draw() -> void:
		var sites: Array[Dictionary] = []
		if not state.is_empty():
			for id in GameData.REGION_IDS:
				if state.regions[id].site.invested:
					sites.append(state.regions[id])
		sites.sort_custom(func(a: Dictionary, b: Dictionary) -> bool: return a.site.progress > b.site.progress)
		for i in B.REQUIRED_SITES:
			var c := Vector2(16 + i * 38, size.y * 0.5)
			var seal := _seal(c, 12.0)
			var outline := seal.duplicate()
			outline.append(seal[0])
			if i >= sites.size():
				for k in range(0, outline.size() - 1, 3):
					draw_line(outline[k], outline[k + 1], P.FAINT, 1.2, true)
				continue
			var r: Dictionary = sites[i]
			var ready: bool = r.site.progress >= B.SITE_MONTHS
			var color := P.DANGER if not Derived.supported(r) else P.CULT if ready else P.OMEN
			if ready:
				draw_colored_polygon(seal, color)
				_star(c, 7.0, P.PAPER_LIGHT)
			else:
				var fill := float(r.site.progress) / B.SITE_MONTHS
				var level := c.y + 13 - 26 * fill
				var below := PackedVector2Array([Vector2(c.x - 14, level), Vector2(c.x + 14, level), Vector2(c.x + 14, c.y + 14), Vector2(c.x - 14, c.y + 14)])
				for part in Geometry2D.intersect_polygons(seal, below):
					draw_colored_polygon(part, Color(color, 0.6))
				draw_polyline(outline, color, 1.4, true)
				_star(c, 7.0, Color(color, 0.8))
