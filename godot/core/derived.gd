class_name Derived
## Values derived from game state. Mirrors src/game/derived.ts. Sums run in REGION_IDS order,
## the same order JavaScript iterates the regions object, so floating-point results match.

const B = preload("res://core/balance.gd")


static func clamp_pct(value: float, minimum := 0.0, maximum := 100.0) -> float:
	return minf(maximum, maxf(minimum, value))


static func total_followers(state: Dictionary) -> float:
	var sum := 0.0
	for id in GameData.REGION_IDS:
		sum += state.regions[id].population.followers
	return sum


static func live_population(r: Dictionary) -> float:
	var p: Dictionary = r.population
	return p.unaware + p.deniers + p.believers + p.followers


static func recruitable(r: Dictionary) -> float:
	return r.population.unaware + r.population.believers


## Population is conserved, so the five buckets still sum to the region's original capacity.
static func capacity(r: Dictionary) -> float:
	var p: Dictionary = r.population
	return p.unaware + p.deniers + p.believers + p.followers + p.losses


static func site_requirement(r: Dictionary) -> float:
	return maxf(B.SITE_FOLLOWERS, Js.round_half_up(capacity(r) * B.SITE_SUPPORT_SHARE))


static func supported(r: Dictionary) -> bool:
	return r.population.followers >= site_requirement(r)


static func cult_share(state: Dictionary) -> float:
	return total_followers(state) / GameData.WORLD_POPULATION


## A watched world gives less and listens less. At the awareness cap the run is already lost.
static func awareness_drag(state: Dictionary) -> float:
	return maxf(0.0, 1.0 - state.awareness / B.AWARENESS_DRAG)


static func ready_sites(state: Dictionary) -> int:
	var count := 0
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		if r.site.invested and r.site.progress >= B.SITE_MONTHS and supported(r):
			count += 1
	return count


static func calendar(month: int) -> String:
	return "Year %d · Month %d" % [floori(month / 12.0) + 1, month % 12 + 1]


static func region_income(r: Dictionary) -> float:
	if r.population.followers <= 0:
		return 0.0
	var celebrity: float = 0.6 if r.figure != null and r.figure.kind == "celebrity" and not r.figure.revealed else 0.0
	return r.population.followers * B.INFLUENCE_PER_FOLLOWER + celebrity


static func influence_per_month(state: Dictionary) -> float:
	var sum := 0.0
	for id in GameData.REGION_IDS:
		sum += region_income(state.regions[id])
	return sum * awareness_drag(state)


static func recruitment_per_month(state: Dictionary, r: Dictionary) -> float:
	if r.population.followers <= 0:
		return 0.0
	return minf(recruitable(r), (B.RECRUITMENT_BASE + r.population.followers * B.RECRUITMENT_RATE)
		* (B.PUBLIC_RECRUITMENT if r.public else 1.0)
		* (1.35 if state.specialization == "recruitment" else 1.0)
		* (0.55 if state.elapsedMonths < r.backlashUntil else 1.0)
		* (1.0 + r.madness / 500.0) * GameData.DIFFICULTIES[state.difficulty].recruitment * awareness_drag(state))


static func site_label(r: Dictionary) -> String:
	if not r.site.invested:
		return "No ritual site"
	if not supported(r):
		return "Interrupted · needs " + Js.num(site_requirement(r)) + " followers"
	return "Ritual prepared" if r.site.progress >= B.SITE_MONTHS else "Preparing · %d/%d months" % [r.site.progress, B.SITE_MONTHS]


static func add_log(state: Dictionary, message: String) -> void:
	state.log.append({ "id": state.nextLogId, "month": state.elapsedMonths, "message": message })
	state.nextLogId += 1
	if state.log.size() > 100:
		state.log = state.log.slice(-100)


## Transfers never create population; beliefs and membership use one source of truth.
static func recruit(r: Dictionary, amount: float) -> float:
	var p: Dictionary = r.population
	var from_believers := minf(p.believers, amount)
	var from_unaware := minf(p.unaware, maxf(0.0, amount - from_believers))
	p.believers -= from_believers
	p.unaware -= from_unaware
	p.followers += from_believers + from_unaware
	return from_believers + from_unaware


static func lose_followers(r: Dictionary, amount: float) -> float:
	var p: Dictionary = r.population
	var removed := minf(p.followers, amount)
	p.followers -= removed
	p.losses += removed
	if p.followers <= 0:
		r.public = false
		r.site = { "invested": false, "progress": 0 }
	return removed
