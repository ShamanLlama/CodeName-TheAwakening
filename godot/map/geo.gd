class_name Geo
## World geometry: loads Natural Earth country rings (built by scripts/build-earth-map.mjs)
## and projects longitude/latitude with the Natural Earth projection.

## World units per projected radian. The projected world is about 1000 units wide.
const SCALE := 182.8
const DATA_PATH := "res://data/earth_regions.json"

## Where each region's name, count and ritual site sit on the map.
const ANCHORS := {
	"north_america": Vector2(-101, 44), "latin_america": Vector2(-60, -10), "europe": Vector2(16, 53),
	"africa": Vector2(20, 6), "middle_east": Vector2(46, 28), "south_asia": Vector2(78, 21),
	"east_asia": Vector2(106, 40), "oceania": Vector2(134, -25),
}

## Land locations for threat markers. Mirrors src/components/threat-markers.ts.
const THREAT_LOCATIONS := {
	"north_america": [Vector2(-123, 53), Vector2(-102, 39), Vector2(-101, 24), Vector2(-80, 43), Vector2(-150, 64), Vector2(-98, 60)],
	"latin_america": [Vector2(-74, -10), Vector2(-52, -10), Vector2(-65, -32), Vector2(-66, 7), Vector2(-86, 14), Vector2(-69, -22)],
	"europe": [Vector2(2, 47), Vector2(14, 51), Vector2(26, 62), Vector2(40, 56), Vector2(-4, 40), Vector2(20, 45)],
	"africa": [Vector2(-10, 12), Vector2(18, 5), Vector2(30, -6), Vector2(23, -23), Vector2(25, 26), Vector2(10, -1)],
	"middle_east": [Vector2(44, 33), Vector2(54, 32), Vector2(45, 23), Vector2(34, 39), Vector2(44, 16), Vector2(46, 25)],
	"south_asia": [Vector2(76, 23), Vector2(69, 30), Vector2(89, 24), Vector2(81, 8), Vector2(67, 34), Vector2(82, 27)],
	"east_asia": [Vector2(105, 36), Vector2(99, 47), Vector2(104, 17), Vector2(136, 36), Vector2(117, -2), Vector2(95, 60)],
	"oceania": [Vector2(135, -25), Vector2(146, -22), Vector2(122, -22), Vector2(145, -6), Vector2(174, -39), Vector2(130, -18)],
}

static var _regions: Dictionary = {}


## Natural Earth projection (Šavrič, Patterson & Jenny, 2011), as in d3-geo. North is up.
static func project(lon_lat: Vector2) -> Vector2:
	var lam := deg_to_rad(lon_lat.x)
	var phi := deg_to_rad(lon_lat.y)
	var phi2 := phi * phi
	var phi4 := phi2 * phi2
	var x := lam * (0.8707 - 0.131979 * phi2 + phi4 * (-0.013791 + phi4 * (0.003971 * phi2 - 0.001529 * phi4)))
	var y := phi * (1.007226 + phi2 * (0.015085 + phi4 * (-0.044475 + 0.028874 * phi2 - 0.005916 * phi4)))
	return Vector2(x, -y) * SCALE


## { region_id: [{ name, rings: [PackedVector2Array in world units] }] }
static func regions() -> Dictionary:
	if _regions.is_empty():
		var raw: Dictionary = JSON.parse_string(FileAccess.get_file_as_string(DATA_PATH))
		for id in GameData.REGION_IDS:
			var countries: Array = []
			for country in raw[id]:
				var rings: Array[PackedVector2Array] = []
				for flat in country.rings:
					var ring := PackedVector2Array()
					for i in range(0, flat.size(), 2):
						ring.append(project(Vector2(flat[i], flat[i + 1])))
					rings.append(ring)
				countries.append({ "name": country.name, "rings": rings })
			_regions[id] = countries
	return _regions


static func bounds() -> Rect2:
	var rect := Rect2(project(Vector2(-170, 84)), Vector2.ZERO)
	for id in GameData.REGION_IDS:
		for country in regions()[id]:
			for ring in country.rings:
				for point in ring:
					rect = rect.expand(point)
	return rect


static func anchor(region_id: String) -> Vector2:
	return project(ANCHORS[region_id])


## Mirrors threatMapPosition: visual variation that does not consume game RNG.
static func threat_position(seed: String, region_id: String, threat: Dictionary) -> Vector2:
	var points: Array = THREAT_LOCATIONS[region_id]
	var index := (Rng.seed_to_state(seed + region_id + threat.kind) + floori(threat.dueMonth / 12.0)) % points.size()
	return project(points[index])


static func region_at(world_point: Vector2) -> String:
	for id in GameData.REGION_IDS:
		for country in regions()[id]:
			for ring in country.rings:
				if Geometry2D.is_point_in_polygon(world_point, ring):
					return id
	return ""


static func ring_area(ring: PackedVector2Array) -> float:
	var area := 0.0
	for i in ring.size():
		var a := ring[i]
		var b := ring[(i + 1) % ring.size()]
		area += a.x * b.y - b.x * a.y
	return absf(area) * 0.5


## Deterministic points scattered over a region's land, for the follower dots.
static func scatter(region_id: String, count: int) -> PackedVector2Array:
	var rings: Array[PackedVector2Array] = []
	var areas: Array[float] = []
	var total := 0.0
	for country in regions()[region_id]:
		for ring in country.rings:
			var area := ring_area(ring)
			if area > 0.5:
				rings.append(ring)
				areas.append(area)
				total += area
	var points := PackedVector2Array()
	var state := Rng.seed_to_state("dots:" + region_id)
	var guard := 0
	while points.size() < count and guard < count * 200:
		guard += 1
		var roll := Rng.next_random(state)
		state = roll.state
		var pick: float = roll.value * total
		var index := 0
		while index < areas.size() - 1 and pick > areas[index]:
			pick -= areas[index]
			index += 1
		var ring := rings[index]
		var box := Rect2(ring[0], Vector2.ZERO)
		for point in ring:
			box = box.expand(point)
		var rx := Rng.next_random(state)
		var ry := Rng.next_random(rx.state)
		state = ry.state
		var candidate := box.position + Vector2(rx.value * box.size.x, ry.value * box.size.y)
		if Geometry2D.is_point_in_polygon(candidate, ring):
			points.append(candidate)
	return points
