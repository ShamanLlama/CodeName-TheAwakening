class_name WorldMap
extends Control
## The strategy map, drawn as an old survey chart: a dark sea inside a ruled neatline, land in
## foxed parchment with inked borders. Land is static Polygon2D geometry; borders redraw on
## change; follower dots, threat markers, omens and ritual sites redraw every frame.
## The wheel or a pinch zooms, a drag pans, and a click or tap without a drag picks a region.

signal region_pressed(id: String)
signal region_hovered(id: String)
## True while the view is zoomed or panned away from the whole world.
signal view_changed(free_view: bool)

const P = preload("res://ui/palette.gd")
const B = preload("res://core/balance.gd")
const Notes = preload("res://ui/notes.gd")
const MAX_DOTS := 260
## Where Johansen found R'lyeh, in The Call of Cthulhu.
const RLYEH := Vector2(-126.72, -47.15)
const LAT_RANGE := Vector2(-58, 84)
const MAX_VIEW_SCALE := 4.0
## A press that moves further than this, in pixels, pans instead of picking.
const DRAG_THRESHOLD := 8.0

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
## Each region's coastline and outer border, without the borders between its own countries.
var _outlines: Dictionary = {}
var _view_scale := 1.0
var _view_center := Vector2.ZERO
var _press_at := Vector2.ZERO
var _pressing := false
var _dragging := false
var _moved := false
var _touches: Dictionary = {}
var _pinch_distance := 0.0
var _gesture := false
var _view_tween: Tween


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
		_outlines[id] = _outline(id)
	_view_center = _world_rect.get_center()
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
	# With reduced motion the clock stands still: markers keep their resting pose.
	if not Kit.reduced_motion:
		_time += delta
	_fx.queue_redraw()


## Segments that belong to exactly one country ring of the region: its outer edge.
func _outline(id: String) -> PackedVector2Array:
	var count := {}
	var segments := {}
	for country in Geo.regions()[id]:
		for ring in country.rings:
			for i in ring.size():
				var a: Vector2 = ring[i]
				var b: Vector2 = ring[(i + 1) % ring.size()]
				var ka := Vector2i(roundi(a.x * 50), roundi(a.y * 50))
				var kb := Vector2i(roundi(b.x * 50), roundi(b.y * 50))
				if ka == kb:
					continue
				var key := [ka, kb] if ka < kb else [kb, ka]
				var k := str(key)
				count[k] = count.get(k, 0) + 1
				segments[k] = [a, b]
	var lines := PackedVector2Array()
	for k in count:
		if count[k] == 1:
			lines.append(segments[k][0])
			lines.append(segments[k][1])
	return lines


func _fit_zoom() -> float:
	return minf(size.x / _world_rect.size.x, size.y / _world_rect.size.y)


func _layout() -> void:
	if size.x <= 0 or size.y <= 0:
		return
	_zoom = _fit_zoom() * _view_scale
	_view_center = _clamp_center(_view_center, _zoom)
	_xform = Transform2D(0.0, Vector2(_zoom, _zoom), 0.0, size * 0.5 - _view_center * _zoom)
	_land.transform = _xform
	_sea.queue_redraw()
	_borders.queue_redraw()


## Keeps the world covering the view where it is big enough to, and centred where not.
func _clamp_center(center: Vector2, zoom: float) -> Vector2:
	var half := size * 0.5 / zoom
	for axis in 2:
		var low: float = _world_rect.position[axis] + half[axis]
		var high: float = _world_rect.end[axis] - half[axis]
		center[axis] = clampf(center[axis], low, high) if low <= high else _world_rect.get_center()[axis]
	return center


func is_free_view() -> bool:
	return _view_scale > 1.01


## Zooms by [param factor] keeping the world point under [param at] still.
func zoom_at(at: Vector2, factor: float) -> void:
	var before := _xform.affine_inverse() * at
	_view_scale = clampf(_view_scale * factor, 1.0, MAX_VIEW_SCALE)
	var zoom := _fit_zoom() * _view_scale
	_view_center = before + (size * 0.5 - at) / zoom
	_layout()
	view_changed.emit(is_free_view())


func pan_by(pixels: Vector2) -> void:
	_view_center -= pixels / _zoom
	_layout()


func reset_view() -> void:
	if _view_tween:
		_view_tween.kill()
	var from_scale := _view_scale
	var from_center := _view_center
	var target := _world_rect.get_center()
	if Kit.reduced_motion or not is_inside_tree():
		_view_scale = 1.0
		_view_center = target
		_layout()
	else:
		_view_tween = create_tween()
		_view_tween.tween_method(func(t: float) -> void:
			_view_scale = lerpf(from_scale, 1.0, t)
			_view_center = from_center.lerp(target, t)
			_layout(), 0.0, 1.0, 0.3).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
	view_changed.emit(false)


func to_screen(world: Vector2) -> Vector2:
	return _xform * world


func _gui_input(event: InputEvent) -> void:
	if event is InputEventScreenTouch or event is InputEventScreenDrag:
		_touch(event)
		return
	if event is InputEventMagnifyGesture:
		zoom_at(event.position, event.factor)
		accept_event()
	elif event is InputEventPanGesture:
		pan_by(-event.delta * 12.0)
		accept_event()
	elif event is InputEventMouseMotion:
		if _pressing and not _gesture:
			if not _moved and event.position.distance_to(_press_at) > DRAG_THRESHOLD:
				_moved = true
				_dragging = is_free_view()
			if _dragging:
				pan_by(event.relative)
				mouse_default_cursor_shape = Control.CURSOR_DRAG
				return
		var id := _pick(event.position)
		if id != hovered:
			hovered = id
			mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND if id else Control.CURSOR_ARROW
			_recolor()
			_borders.queue_redraw()
			region_hovered.emit(id)
	elif event is InputEventMouseButton and event.button_index in [MOUSE_BUTTON_WHEEL_UP, MOUSE_BUTTON_WHEEL_DOWN] and event.pressed:
		zoom_at(event.position, 1.15 if event.button_index == MOUSE_BUTTON_WHEEL_UP else 1.0 / 1.15)
		accept_event()
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_pressing = true
			_dragging = false
			_moved = false
			_gesture = _touches.size() >= 2
			_press_at = event.position
		else:
			var picked := _pressing and not _moved and not _gesture
			_pressing = false
			_dragging = false
			mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND if hovered else Control.CURSOR_ARROW
			var id := _pick(event.position) if picked else ""
			if id:
				region_pressed.emit(id)
		accept_event()


## Two fingers pinch and pan; one finger arrives as an emulated mouse and pans or picks.
func _touch(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		if event.pressed:
			_touches[event.index] = event.position
		else:
			_touches.erase(event.index)
		# A pinch never ends in a pick; the flag clears on the next press.
		if _touches.size() >= 2:
			_gesture = true
			_pinch_distance = _pair_distance()
	elif event is InputEventScreenDrag and _touches.has(event.index):
		var before := _pair_middle()
		_touches[event.index] = event.position
		if _touches.size() >= 2:
			var distance := _pair_distance()
			if _pinch_distance > 0.0 and distance > 0.0:
				zoom_at(_pair_middle(), distance / _pinch_distance)
			_pinch_distance = distance
			pan_by(_pair_middle() - before)
	accept_event()


func _pair_distance() -> float:
	var points: Array = _touches.values()
	return (points[0] as Vector2).distance_to(points[1]) if points.size() >= 2 else 0.0


func _pair_middle() -> Vector2:
	var points: Array = _touches.values()
	return ((points[0] as Vector2) + (points[1] as Vector2)) * 0.5 if points.size() >= 2 else Vector2.ZERO


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
	_draw_compass(canvas, to_screen(Geo.project(Vector2(-24, -44))), clampf(_fit_zoom() * 26.0, 22.0, 40.0))


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
		# Country borders inside a region are faint; the region's own edge is inked.
		var inner := Color(P.LAND_EDGE, 0.45 if id == selected else 0.7)
		for country in Geo.regions()[id]:
			for ring in country.rings:
				canvas.draw_polyline(_closed_screen(ring), inner, 1.0, true)
	for id in GameData.REGION_IDS:
		canvas.draw_multiline(_xform * _outlines[id], P.LAND_EDGE, 1.2, true)
	for id in [hovered, selected]:
		if id:
			var color := Color(P.TEXT, 0.9) if id == selected else Color(P.TEXT, 0.5)
			canvas.draw_multiline(_xform * _outlines[id], color, 2.6 if id == selected else 1.6, true)
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
	var dot_size := clampf(_zoom * 1.1, 1.2, 3.4)
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
	if show_labels:
		_draw_labels(canvas)
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		if r.site.invested:
			_draw_site(canvas, id, r)
		if not over and r.opportunity != null:
			_draw_omen(canvas, id)
		if not over and r.threat != null:
			_draw_threat(canvas, id, r.threat)


## Region names and follower counts on a pale halo, so borders never cross the letters.
## Type grows a little as the chart is zoomed. Labels are placed in order of importance; one
## that would cover another moves just above or below its anchor.
func _draw_labels(canvas: Control) -> void:
	var font := P.font("display_sc")
	var digits := P.font("type_bold")
	var grow := clampf(sqrt(_view_scale), 1.0, 1.5)
	var size_title := roundi((18 if _fit_zoom() < 1.0 else 20) * grow)
	var size_count := roundi((15 if _fit_zoom() < 1.0 else 17) * grow)
	var order: Array = GameData.REGION_IDS.duplicate()
	order.sort_custom(func(a: String, b: String) -> bool: return _label_rank(a) > _label_rank(b))
	# Markers are placed first, so a name moves off a site, an omen or a threat's countdown.
	var placed: Array[Rect2] = _marker_rects()
	var markers := placed.size()
	for id in order:
		var title: String = GameData.REGIONS[id].name
		var followers: float = state.regions[id].population.followers
		var count := Format.count(followers) if followers > 0 else ""
		var title_width := font.get_string_size(title, HORIZONTAL_ALIGNMENT_LEFT, -1, size_title).x
		var count_width := digits.get_string_size(count, HORIZONTAL_ALIGNMENT_LEFT, -1, size_count).x if count else 0.0
		var height := size_title * 0.85 + (size_count * 1.05 if count else 0.0)
		var width := maxf(title_width, count_width)
		var at := to_screen(Geo.anchor(id))
		var best := Vector2.ZERO
		var best_overlap := INF
		for shift in [0.0, height + 2.0, -(height + 2.0), (height + 2.0) * 2.0, -(height + 2.0) * 2.0]:
			var rect := Rect2(at.x - width * 0.5, at.y - size_title * 0.85 + shift, width, height)
			var overlap := 0.0
			for k in placed.size():
				# Covering a marker costs more than touching another name.
				overlap += rect.intersection(placed[k]).get_area() * (3.0 if k < markers else 1.0)
			if overlap < best_overlap:
				best_overlap = overlap
				best = Vector2(0, shift)
			if overlap <= 0.0:
				break
		var origin := at + best
		placed.append(Rect2(origin.x - width * 0.5, origin.y - size_title * 0.85, width, height).grow(3))
		var focus: bool = id == selected or id == hovered
		var halo := Color(P.PAPER_LIGHT, 0.93)
		var title_at := origin + Vector2(-title_width * 0.5, -4)
		canvas.draw_string_outline(font, title_at, title, HORIZONTAL_ALIGNMENT_LEFT, -1, size_title, 7, halo)
		canvas.draw_string(font, title_at, title, HORIZONTAL_ALIGNMENT_LEFT, -1, size_title, P.TEXT if focus else Color(P.TEXT, 0.86))
		if count:
			var count_at := origin + Vector2(-count_width * 0.5, size_count * 0.9)
			canvas.draw_string_outline(digits, count_at, count, HORIZONTAL_ALIGNMENT_LEFT, -1, size_count, 7, halo)
			canvas.draw_string(digits, count_at, count, HORIZONTAL_ALIGNMENT_LEFT, -1, size_count, P.CULT)


## Screen rectangles of the markers drawn over the land this frame.
func _marker_rects() -> Array[Rect2]:
	var rects: Array[Rect2] = []
	var over := GameEngine.is_over(state)
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		var anchor := to_screen(Geo.anchor(id))
		if r.site.invested:
			rects.append(Rect2(anchor + Vector2(-14, 18), Vector2(28, 28)))
		if not over and r.opportunity != null:
			rects.append(Rect2(anchor + Vector2(23, -41), Vector2(30, 30)))
		if not over and r.threat != null:
			var at := to_screen(Geo.threat_position(state.seed, id, r.threat))
			rects.append(Rect2(at + Vector2(-24, -28), Vector2(48, 38)))
	return rects


## The open region first, then the hovered one, then the cult's regions by size.
func _label_rank(id: String) -> float:
	if id == selected:
		return 1e9
	if id == hovered:
		return 1e8
	return state.regions[id].population.followers


func _draw_site(canvas: Control, id: String, r: Dictionary) -> void:
	var at := to_screen(Geo.anchor(id)) + Vector2(0, 32)
	var progress: float = r.site.progress / float(B.SITE_MONTHS)
	var state := Notes.site_state(r)
	var ready := state == "ready"
	var supported := state in ["ready", "preparing"]
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
	if not supported:
		# A struck-through seal: stalled, readable without the colour.
		canvas.draw_line(at + Vector2(-11, 11), at + Vector2(11, -11), P.PAPER_LIGHT, 4.0, true)
		canvas.draw_line(at + Vector2(-11, 11), at + Vector2(11, -11), color, 2.2, true)
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
