class_name WorldMap
extends Control
## The strategy map. Land is static Polygon2D geometry; borders redraw on change; follower
## dots, threat markers, omens and ritual sites redraw every frame.

signal region_pressed(id: String)
signal region_hovered(id: String)

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const MAX_DOTS := 260

var state: Dictionary = {}
var selected := ""
var hovered := ""
var show_labels := true

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
	_land = Node2D.new()
	add_child(_land)
	for id in GameData.REGION_IDS:
		_polys[id] = []
		for country in Geo.regions()[id]:
			for ring in country.rings:
				var poly := Polygon2D.new()
				poly.polygon = ring
				poly.color = P.LAND
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
				color = P.LAND.lerp(Color("2b3a16") if r.public else Color("1f2a17"), tint)
		if id == hovered:
			color = color.lightened(0.07)
		if id == selected:
			color = color.lightened(0.1)
		for poly in _polys[id]:
			poly.color = color


func _draw_borders(canvas: Control) -> void:
	# Graticule: curved meridians and parallels in the projection.
	for lon in range(-180, 181, 30):
		var line := PackedVector2Array()
		for lat in range(-60, 85, 4):
			line.append(to_screen(Geo.project(Vector2(lon, lat))))
		canvas.draw_polyline(line, P.OCEAN_LINE, 1.0, true)
	for lat in [-60, -40, -20, 0, 20, 40, 60, 80]:
		var line := PackedVector2Array()
		for lon in range(-180, 181, 6):
			line.append(to_screen(Geo.project(Vector2(lon, lat))))
		canvas.draw_polyline(line, Color("16202e") if lat == 0 else P.OCEAN_LINE, 1.0, true)
	for id in GameData.REGION_IDS:
		var edge := P.LAND_EDGE
		for country in Geo.regions()[id]:
			for ring in country.rings:
				canvas.draw_polyline(_closed_screen(ring), edge, 1.0, true)
	for id in [hovered, selected]:
		if id:
			var color := Color(P.TEXT, 0.6) if id == selected else Color(P.TEXT, 0.3)
			for country in Geo.regions()[id]:
				for ring in country.rings:
					canvas.draw_polyline(_closed_screen(ring), color, 1.2, true)
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
		var color := (P.CULT_DIM if occupied else P.MUTED)
		color.a = 0.55
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
	for i in range(0, points.size() - 1, 2):
		canvas.draw_line(points[i], points[i + 1], color, 1.2, true)


func _draw_fx(canvas: Control) -> void:
	_hits.clear()
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
			var flicker := 0.7 + 0.3 * sin(_time * 1.7 + i * 2.39)
			if r.public:
				canvas.draw_circle(at, dot_size * 2.6, Color(P.CULT, 0.08 * flicker))
				canvas.draw_circle(at, dot_size, Color(P.CULT, 0.95 * flicker))
			else:
				canvas.draw_circle(at, dot_size * 0.85, Color(P.CULT, 0.55 * flicker))
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
	var font := P.font("condensed_semibold")
	var title: String = GameData.REGIONS[id].name.to_upper()
	var focus := id == selected or id == hovered
	var width := font.get_string_size(title, HORIZONTAL_ALIGNMENT_LEFT, -1, 14).x
	var origin := at + Vector2(-width * 0.5, -4)
	canvas.draw_string_outline(font, origin, title, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, 4, P.INK)
	canvas.draw_string(font, origin, title, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, P.TEXT if focus else Color(P.MUTED, 0.9))
	var followers: float = state.regions[id].population.followers
	if followers > 0:
		var count := Format.count(followers)
		var count_width := font.get_string_size(count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15).x
		var count_origin := at + Vector2(-count_width * 0.5, 12)
		canvas.draw_string_outline(font, count_origin, count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15, 4, P.INK)
		canvas.draw_string(font, count_origin, count, HORIZONTAL_ALIGNMENT_LEFT, -1, 15, P.CULT)


func _draw_site(canvas: Control, id: String, r: Dictionary) -> void:
	var at := to_screen(Geo.anchor(id)) + Vector2(0, 32)
	var progress: float = r.site.progress / float(B.SITE_MONTHS)
	var ready: bool = r.site.progress >= B.SITE_MONTHS
	var supported := Derived.supported(r)
	var color := P.DANGER if not supported else P.CULT if ready else P.OMEN
	var pulse := 0.5 + 0.5 * sin(_time * (5.0 if not supported else 2.0))
	canvas.draw_circle(at, 13.0, Color(P.INK, 0.75))
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
	canvas.draw_circle(at, 15.0, Color(P.OMEN, 0.10 + 0.08 * glow))
	var diamond := PackedVector2Array([at + Vector2(0, -10), at + Vector2(10, 0), at + Vector2(0, 10), at + Vector2(-10, 0)])
	canvas.draw_colored_polygon(diamond, P.OMEN_DEEP)
	diamond.append(diamond[0])
	canvas.draw_polyline(diamond, P.OMEN, 1.6, true)
	# An eye: the omen is watching back.
	var lid := PackedVector2Array()
	for k in 9:
		var t := k / 8.0
		lid.append(at + Vector2(lerpf(-5.5, 5.5, t), -sin(t * PI) * 3.2))
	for k in 9:
		var t := 1.0 - k / 8.0
		lid.append(at + Vector2(lerpf(-5.5, 5.5, t), sin(t * PI) * 3.2))
	canvas.draw_polyline(lid, P.OMEN, 1.2, true)
	canvas.draw_circle(at, 1.6, P.OMEN)
	_hits.append({ "at": at, "radius": 14.0, "region": id })


func _draw_threat(canvas: Control, id: String, threat: Dictionary) -> void:
	var at := to_screen(Geo.threat_position(state.seed, id, threat))
	var months: int = threat.dueMonth - state.elapsedMonths
	var urgent := months <= 3
	var color := P.WARN if urgent else P.DANGER
	var phase := fmod(_time * (1.6 if urgent else 0.8), 1.0)
	canvas.draw_arc(at, 6.0 + phase * 14.0, 0.0, TAU, 32, Color(color, 0.7 * (1.0 - phase)), 1.5, true)
	canvas.draw_circle(at, 7.0, Color(P.INK, 0.85))
	canvas.draw_arc(at, 7.0, 0.0, TAU, 24, color, 1.6, true)
	canvas.draw_circle(at, 3.2, color)
	var font := P.font("condensed_bold")
	var text := "%dM" % months
	var width := font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14).x
	var origin := at + Vector2(-width * 0.5, -12)
	canvas.draw_string_outline(font, origin, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, 4, P.INK)
	canvas.draw_string(font, origin, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 14, color)
	_hits.append({ "at": at, "radius": 14.0, "region": id })
