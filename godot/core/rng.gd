class_name Rng
## Seeded xorshift32, bit-compatible with src/game/random.ts. GDScript integers are 64-bit
## signed, so every step masks back to 32 unsigned bits.

const MASK := 0xFFFFFFFF


## FNV-1a over UTF-16 code units, as JavaScript's charCodeAt sees the seed.
static func seed_to_state(seed: String) -> int:
	var hash := 2166136261
	var units := seed.to_utf16_buffer()
	for i in range(0, units.size(), 2):
		hash = (hash ^ (units[i] | (units[i + 1] << 8))) & MASK
		hash = (hash * 16777619) & MASK
	return hash if hash != 0 else 1


static func next_random(state: int) -> Dictionary:
	var x := state & MASK
	if x == 0:
		x = 1
	x = (x ^ (x << 13)) & MASK
	x = x ^ (x >> 17)
	x = (x ^ (x << 5)) & MASK
	return { "value": float(x) / 4294967296.0, "state": x if x != 0 else 1 }


static func random_int(state: int, minimum: int, maximum: int) -> Dictionary:
	var random := next_random(state)
	return { "value": int(floor(random.value * (maximum - minimum + 1))) + minimum, "state": random.state }
