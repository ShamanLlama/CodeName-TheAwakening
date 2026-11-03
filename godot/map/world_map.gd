class_name WorldMap
extends Control
## The strategy map, drawn as an old survey chart: a dark sea inside a ruled neatline, land in
## foxed parchment with inked borders. Land is static Polygon2D geometry; borders redraw on
## change; follower dots, threat markers, omens and ritual sites redraw every frame.

signal region_pressed(id: String)
signal region_hovered(id: String)

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const MAX_DOTS := 260
## Where Johansen found R'lyeh, in The Call of Cthulhu.
const RLYEH := Vector2(-126.72, -47.15)
const LAT_RANGE := Vector2(-58, 84)

var state: Dictionary = {}
var selected := ""
var hovered := ""
var show_labels := true

var _sea: Control
var _land: Node2D
var _polys: Dictionary = {}
var _dots: Dictionary = {}
var _borders: Control
var _fx: Control
var _world_rect: Rect2
var _xform := Transform2D.IDENTITY
var _zoom := 1.0
var _time := 0.0
var _hits: Array[Dictionary] = []


func _ready() -> void:
	clip_contents = true
	mouse_filter = Control.MOUSE_FILTER_STOP
	_world_rect = Geo.bounds().grow_individual(8, 4, 8, 4)
	_sea = _layer(_draw_sea)
	_land = Node2D.new()
	_land.material = P.paper(0.3)
	add_child(_land)
	for id in GameData.REGION_IDS:
		_polys[id] = []
		for country in Geo.regions()[id]:
			for ring in country.rings:
				var poly := Polygon2D.new()
				poly.polygon = ring
				# A few rings (Sudan, Belgium) pinch at a vertex and fail to triangulate, which
				# leaves a hole in the land. Growing them by a hair repairs the fill.
				if Geometry2D.triangulate_polygon(ring).is_empty():
					var repaired := Geometry2D.offset_polygon(ring, 0.02)
					if repaired.size() == 1:
						poly.polygon = repaired[0]
				poly.color = P.LAND
				poly.use_parent_material = true
				_land.add_child(poly)
				_polys[id].append(poly)
		_dots[id] = Geo.scatter(id, MAX_DOTS)
	_borders = _layer(_draw_borders)
	_fx = _layer(_draw_fx)
	resized.connect(_layout)
	_layout()


func _layer(painter: Callable) -> Control:
	var layer := Control.new()
	layer.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	layer.draw.connect(painter.bind(layer))
	add_child(layer)
	return layer


func set_state(next: Dictionary, selected_id: String) -> void:
	state = next
	selected = selected_id
	_recolor()
	_borders.queue_redraw()


func _process(delta: float) -> void:
	_time += delta
	_fx.queue_redraw()


func _layout() -> void:
	if size.x <= 0 or size.y <= 0:
		return
	_zoom = minf(size.x / _world_rect.size.x, size.y / _world_rect.size.y)
	var offset := (size - _world_rect.size * _zoom) * 0.5 - _world_rect.position * _zoom
	_xform = Transform2D(0.0, Vector2(_zoom, _zoom), 0.0, offset)
	_land.transform = _xform
	_sea.queue_redraw()
	_borders.queue_redraw()


func to_screen(world: Vector2) -> Vector2:
	return _xform * world


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var id := _pick(event.position)
		if id != hovered:
			hovered = id
			mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND if id else Control.CURSOR_ARROW
			_recolor()
			_borders.queue_redraw()
			region_hovered.emit(id)
	elif event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var id := _pick(event.position)
		if id:
			region_pressed.emit(id)
			accept_event()


func _notification(what: int) -> void:
	if what == NOTIFICATION_MOUSE_EXIT and hovered:
		hovered = ""
		_recolor()
		_borders.queue_redraw()
		region_hovered.emit("")


## Markers win over the land beneath them.
func _pick(local: Vector2) -> String:
	for hit in _hits:
		if local.distance_to(hit.at) <= hit.radius:
			return hit.region
	return Geo.region_at(_xform.affine_inverse() * local)


func _share(id: String) -> float:
	if state.is_empty():
		return 0.0
	var r: Dictionary = state.regions[id]
	return r.population.followers / maxf(1.0, Derived.capacity(r))


func _recolor() -> void:
	for id in GameData.REGION_IDS:
		var color := P.LAND
		if not state.is_empty():
			var r: Dictionary = state.regions[id]
			if r.population.followers > 0:
				var tint := clampf(0.25 + _share(id) * 1.6, 0.0, 1.0)
				# The cult soaks into the paper like a verdigris stain.
				color = P.LAND.lerp(Color("6f8a52") if r.public else Color("8b9466"), tint)
		if id == hovered:
			color = color.lightened(0.08)
		if id == selected:
			color = color.lightened(0.14)
		for poly in _polys[id]:
			poly.color = color


func _meridian(lon: float, from_lat: float, to_lat: float) -> PackedVector2Array:
	var line := PackedVector2Array()
	for k in 37:
		line.append(to_screen(Geo.project(Vector2(lon, lerpf(from_lat, to_lat, k / 36.0)))))
	return line


func _parallel(lat: float, from_lon := -180.0, to_lon := 180.0) -> PackedVector2Array:
	var line := PackedVector2Array()
	for k in 61:
		line.append(to_screen(Geo.project(Vector2(lerpf(from_lon, to_lon, k / 60.0), lat))))
	return line


## The sea inside the chart's outline, its graticule, a ruled neatline and a compass rose.
func _draw_sea(canvas: Control) -> void:
	var outline := _meridian(-180, LAT_RANGE.x, LAT_RANGE.y)
	outline.append_array(_parallel(LAT_RANGE.y))
	outline.append_array(_meridian(180, LAT_RANGE.y, LAT_RANGE.x))
	outline.append_array(_parallel(LAT_RANGE.x, 180, -180))
	canvas.draw_colored_polygon(outline, P.OCEAN)
	for lon in range(-150, 151, 30):
		canvas.draw_polyline(_meridian(lon, LAT_RANGE.x, LAT_RANGE.y), P.OCEAN_LINE, 1.0, true)
	for lat in [-40, -20, 0, 20, 40, 60, 80]:
		canvas.draw_polyline(_parallel(lat), P.OCEAN_LINE.lightened(0.12) if lat == 0 else P.OCEAN_LINE, 1.0, true)
	var closed := outline.duplicate()
	closed.append(outline[0])
	canvas.draw_polyline(closed, Color(P.BRASS, 0.75), 2.0, true)
	var center := to_screen(Vector2.ZERO)
	var outer := PackedVector2Array()
	for point in closed:
		outer.append(point + (point - center).normalized() * 5.0)
	canvas.draw_polyline(outer, Color(P.BRASS, 0.4), 1.0, true)
	_draw_compass(canvas, to_screen(Geo.project(Vector2(-24, -44))), clampf(_zoom * 26.0, 22.0, 40.0))


func _draw_compass(canvas: Control, at: Vector2, radius: float) -> void:
	var ink := Color(P.BRASS, 0.55)
	canvas.draw_arc(at, radius * 0.72, 0.0, TAU, 48, ink, 1.0, true)
	canvas.draw_arc(at, radius * 0.66, 0.0, TAU, 48, Color(ink, 0.3), 1.0, true)
	for k in 8:
		var angle := -PI / 2 + k * TAU / 8.0
		var length := radius if k % 2 == 0 else radius * 0.55
		var tip := at + Vector2.from_angle(angle) * length
		var left := at + Vector2.from_angle(angle - PI / 2) * radius * 0.09
		var right := at + Vector2.from_angle(angle + PI / 2) * radius * 0.09
		canvas.draw_colored_polygon(PackedVector2Array([tip, left, at]), ink)
		canvas.draw_colored_polygon(PackedVector2Array([tip, at, right]), Color(ink, 0.25))
	var font := P.font("display")
	var width := font.get_string_size("N", HORIZONTAL_ALIGNMENT_LEFT, -1, 16).x
	canvas.draw_string(font, at + Vector2(-width * 0.5, -radius - 5), "N", HORIZONTAL_ALIGNMENT_LEFT, -1, 16, ink)


## One small mark in the South Pacific, which stirs once the stars are right.
func _draw_rlyeh(canvas: Control) -> void:
	var at := to_screen(Geo.project(RLYEH))
	var stirring: bool = not state.is_empty() and state.status in ["alignment", "awakening", "won"]
	var pulse := 0.5 + 0.5 * sin(_time * (2.4 if stirring else 0.6))
	var color := Color(P.OMEN_BRIGHT, (0.6 if stirring else 0.42) + 0.25 * pulse)
	canvas.draw_line(at + Vector2(-4, -4), at + Vector2(4, 4), color, 1.2, true)
	canvas.draw_line(at + Vector2(4, -4), at + Vector2(-4, 4), color, 1.2, true)
	canvas.draw_arc(at, 7.0 + pulse * (5.0 if stirring else 2.0), 0.0, TAU, 24, Color(color, color.a * 0.5), 1.0, true)
	canvas.draw_string(P.font("display_italic"), at + Vector2(11, 4), "R'lyeh", HORIZONTAL_ALIGNMENT_LEFT, -1, 15, color)
	canvas.draw_string(P.font("type"), at + Vector2(11, 17), "47°9′S 126°43′W", HORIZONTAL_ALIGNMENT_LEFT, -1, 11, Color(color, color.a * 0.8))


func _draw_borders(canvas: Control) -> void:
	for id in GameData.REGION_IDS:
		var edge := P.LAND_EDGE
		for country in Geo.regions()[id]:
			for ring in country.rings:
				canvas.draw_polyline(_closed_screen(ring), edge, 1.0, true)
	for id in [hovered, selected]:
		if id:
			var color := Color(P.TEXT, 0.85) if id == selected else Color(P.TEXT, 0.45)
			for country in Geo.regions()[id]:
				for ring in country.rings:
					canvas.draw_polyline(_closed_screen(ring), color, 1.8 if id == selected else 1.3, true)
	_draw_routes(canvas)


func _closed_screen(ring: PackedVector2Array) -> PackedVector2Array:
	var line := _xform * ring
	line.append(line[0])
	return line


## Dashed routes from the selected cell to the regions it can spread to.
func _draw_routes(canvas: Control) -> void:
	if state.is_empty() or not selected or state.regions[selected].population.followers <= 0:
		return
	for other in GameData.REGIONS[selected].adjacent:
		var a := Geo.ANCHORS[selected] as Vector2
		var b := Geo.ANCHORS[other] as Vector2
		var occupied: bool = state.regions[other].population.followers > 0
		var color := P.CULT if occupied else P.TEXT
		if absf(a.x - b.x) > 180.0:
			# Across the Pacific: leave one edge of the map and re-enter at the other.
			var edge_a := Vector2(-180.0 if a.x < 0 else 180.0, lerpf(a.y, b.y, 0.5))
			var edge_b := Vector2(-edge_a.x, edge_a.y)
			_dashed_arc(canvas, to_screen(Geo.project(a)), to_screen(Geo.project(edge_a)), color)
			_dashed_arc(canvas, to_screen(Geo.project(edge_b)), to_screen(Geo.project(b)), color)
		else:
			_dashed_arc(canvas, to_screen(Geo.project(a)), to_screen(Geo.project(b)), color)


func _dashed_arc(canvas: Control, from: Vector2, to: Vector2, color: Color) -> void:
	var control := (from + to) * 0.5 + (to - from).orthogonal().normalized() * from.distance_to(to) * 0.18
	var points := PackedVector2Array()
	for i in 33:
		var t := i / 32.0
		points.append(from.lerp(control, t).lerp(control.lerp(to, t), t))
	# A pale underline keeps the inked route legible over the dark sea.
	for i in range(0, points.size() - 1, 2):
		canvas.draw_line(points[i], points[i + 1], Color(P.PAPER_LIGHT, 0.55), 3.4, true)
		canvas.draw_line(points[i], points[i + 1], color, 1.4, true)


func _draw_fx(canvas: Control) -> void:
	_hits.clear()
	_draw_rlyeh(canvas)
	if state.is_empty():
		return
	var over := GameEngine.is_over(state)
	var dot_size := clampf(_zoom * 1.1, 1.2, 2.6)
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		if r.population.followers <= 0:
			continue
		var count := clampi(roundi(MAX_DOTS * clampf(_share(id) * 1.6, 0.0, 1.0)), 4, MAX_DOTS)
		var points: PackedVector2Array = _dots[id]
		for i in mini(count, points.size()):
			var at := to_screen(points[i])
			var flicker := 0.75 + 0.25 * sin(_time * 1.7 + i * 2.39)
			if r.public:
				canvas.draw_circle(at, dot_size * 2.4, Color(P.CULT, 0.12 * flicker))
				canvas.draw_circle(at, dot_size, Color("1f3519", 0.95 * flicker))
			else:
				canvas.draw_circle(at, dot_size * 0.85, Color("1f3519", 0.6 * flicker))
	for id in GameData.REGION_IDS:
		if show_labels:
			_draw_label(canvas, id)
		var r: Dictionary = state.regions[id]
		if r.site.invested:
			_draw_site(canvas, id, r)
		if not over and r.opportunity != null:
			_draw_omen(canvas, id)
		if not over and r.threat != null:
			_draw_threat(canvas, id, r.threat)


func _draw_label(canvas: Control, id: String) -> void:
	var at := to_screen(Geo.anchor(id))
	var font := P.font("display_sc")
	var title: String = GameData.REGIONS[id].name
	var focus := id == selected or id == hovered
	var halo := Color(P.LAND.lightened(0.15), 0.9)
	var width := font.get_string_size(title, HORIZONTAL_ALIGNMENT_LEFT, -1, 18).x
	var origin := at + Vector2(-width * 0.5, -4)
	canvas.draw_string_outline(font, origin, title, HORIZONTAL_ALIGNMENT_LEFT, -1, 18, 5, halo)
	canvas.draw_string(font, origin, title, HORIZONTAL_ALIGNMENT_LEFT, -1, 18, P.TEXT if focus else Color(P.TEXT, 0.72))
	var followers: float = state.regions[id].population.followers
	if followers > 0:
		var digits := P.font("type_bold")
		var count := Format.count(followers)
		var count_width := digits.get_string_size(count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15).x
		var count_origin := at + Vector2(-count_width * 0.5, 13)
		canvas.draw_string_outline(digits, count_origin, count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15, 5, halo)
		canvas.draw_string(digits, count_origin, count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15, P.CULT)


func _draw_site(canvas: Control, id: String, r: Dictionary) -> void:
	var at := to_screen(Geo.anchor(id)) + Vector2(0, 32)
	var progress: float = r.site.progress / float(B.SITE_MONTHS)
	var ready: bool = r.site.progress >= B.SITE_MONTHS
	var supported := Derived.supported(r)
	var color := P.DANGER_BRIGHT if not supported else P.CULT_BRIGHT if ready else P.OMEN_BRIGHT
	var pulse := 0.5 + 0.5 * sin(_time * (5.0 if not supported else 2.0))
	canvas.draw_circle(at, 13.0, Color(P.TEXT, 0.88))
	canvas.draw_arc(at, 11.0, 0.0, TAU, 40, Color(color, 0.25), 1.5, true)
	canvas.draw_arc(at, 11.0, -PI / 2, -PI / 2 + TAU * progress, 40, color, 2.5, true)
	# A slowly turning pentagram: five points joined by every second vertex.
	var spin := _time * 0.35 - PI / 2
	var star := PackedVector2Array()
	for k in 6:
		star.append(at + Vector2.from_angle(spin + (k * 2 % 5) * TAU / 5.0) * 7.0)
	canvas.draw_polyline(star, Color(color, 0.6 + 0.4 * pulse if ready or not supported else 0.85), 1.3, true)
	_hits.append({ "at": at, "radius": 14.0, "region": id })


func _draw_omen(canvas: Control, id: String) -> void:
	var at := to_screen(Geo.anchor(id)) + Vector2(38, -26 + sin(_time * 2.2) * 2.5)
	var glow := 0.5 + 0.5 * sin(_time * 3.0)
	canvas.draw_circle(at, 15.0, Color(P.OMEN, 0.14 + 0.1 * glow))
	var diamond := PackedVector2Array([at + Vector2(0, -10), at + Vector2(10, 0), at + Vector2(0, 10), at + Vector2(-10, 0)])
	canvas.draw_colored_polygon(diamond, P.OMEN_DEEP)
	diamond.append(diamond[0])
	canvas.draw_polyline(diamond, P.OMEN_BRIGHT, 1.6, true)
	# An eye: the omen is watching back.
	var lid := PackedVector2Array()
	for k in 9:
		var t := k / 8.0
		lid.append(at + Vector2(lerpf(-5.5, 5.5, t), -sin(t * PI) * 3.2))
	for k in 9:
		var t := 1.0 - k / 8.0
		lid.append(at + Vector2(lerpf(-5.5, 5.5, t), sin(t * PI) * 3.2))
	canvas.draw_polyline(lid, P.OMEN_BRIGHT, 1.2, true)
	canvas.draw_circle(at, 1.6, P.OMEN_BRIGHT)
	_hits.append({ "at": at, "radius": 14.0, "region": id })


func _draw_threat(canvas: Control, id: String, threat: Dictionary) -> void:
	var at := to_screen(Geo.threat_position(state.seed, id, threat))
	var months: int = threat.dueMonth - state.elapsedMonths
	var urgent := months <= 3
	var color := P.DANGER
	var phase := fmod(_time * (1.8 if urgent else 0.8), 1.0)
	canvas.draw_arc(at, 6.0 + phase * 14.0, 0.0, TAU, 32, Color(color, 0.8 * (1.0 - phase)), 2.0 if urgent else 1.5, true)
	canvas.draw_circle(at, 7.5, P.PAPER_LIGHT)
	canvas.draw_arc(at, 7.5, 0.0, TAU, 24, color, 2.0, true)
	canvas.draw_circle(at, 3.4, color)
	var font := P.font("type_bold")
	var text := "%d MO" % months
	var width := font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14).x
	var origin := at + Vector2(-width * 0.5, -13)
	canvas.draw_string_outline(font, origin, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, 5, Color(P.PAPER_LIGHT, 0.95))
	canvas.draw_string(font, origin, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, color)
	_hits.append({ "at": at, "radius": 14.0, "region": id })
