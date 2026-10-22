extends RefCounted
## Small drawn gauges for the bottom HUD.

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")


## Segmented awareness meter. The run is lost at 100.
class Awareness extends Control:
	var value := 0.0:
		set(next):
			value = next
			queue_redraw()

	func _init() -> void:
		custom_minimum_size = Vector2(230, 14)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var segments := 25
		var gap := 2.0
		var width := (size.x - gap * (segments - 1)) / segments
		for i in segments:
			var lit := value >= (i + 1) * 100.0 / segments
			var partial := not lit and value > i * 100.0 / segments
			var color := P.DANGER if lit else Color(P.DANGER, 0.45) if partial else Color("1b2029")
			if lit and i >= segments - 5:
				color = P.DANGER.lightened(0.25)
			draw_rect(Rect2(i * (width + gap), 0, width, size.y), color)


## One diamond per required site: hollow when unfunded, filling as preparation advances,
## solid green when ready, red when its region no longer supports it.
class Sites extends Control:
	var state: Dictionary = {}

	func _init() -> void:
		custom_minimum_size = Vector2(120, 22)
		mouse_filter = Control.MOUSE_FILTER_IGNORE

	func _draw() -> void:
		var sites: Array[Dictionary] = []
		if not state.is_empty():
			for id in GameData.REGION_IDS:
				if state.regions[id].site.invested:
					sites.append(state.regions[id])
		sites.sort_custom(func(a: Dictionary, b: Dictionary) -> bool: return a.site.progress > b.site.progress)
		for i in B.REQUIRED_SITES:
			var c := Vector2(14 + i * 36, size.y * 0.5)
			var diamond := PackedVector2Array([c + Vector2(0, -10), c + Vector2(10, 0), c + Vector2(0, 10), c + Vector2(-10, 0), c + Vector2(0, -10)])
			if i >= sites.size():
				draw_polyline(diamond, P.FAINT, 1.4, true)
				continue
			var r: Dictionary = sites[i]
			var ready: bool = r.site.progress >= B.SITE_MONTHS
			var color := P.DANGER if not Derived.supported(r) else P.CULT if ready else P.OMEN
			if ready:
				draw_colored_polygon(diamond.slice(0, 4), color)
			else:
				var fill := float(r.site.progress) / B.SITE_MONTHS
				var level := c.y + 10 - 20 * fill
				var clipped := Geometry2D.intersect_polygons(diamond.slice(0, 4), PackedVector2Array([Vector2(c.x - 11, level), Vector2(c.x + 11, level), Vector2(c.x + 11, c.y + 11), Vector2(c.x - 11, c.y + 11)]))
				for part in clipped:
					draw_colored_polygon(part, Color(color, 0.55))
				draw_polyline(diamond, color, 1.4, true)
