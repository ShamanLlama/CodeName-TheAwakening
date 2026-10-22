class_name GameEngine
## The monthly cult simulation. Mirrors src/game/engine.ts.
## Commands never mutate their input: they return a result holding a changed deep copy, or the
## untouched input state when refused.

const B = preload("res://core/balance.gd")


static func is_active(s: Dictionary) -> bool:
	return s.status == "playing" or s.status == "alignment" or s.status == "awakening"


static func is_over(s: Dictionary) -> bool:
	return s.status == "won" or s.status == "lost"


static func fail(state: Dictionary, message: String) -> Dictionary:
	return { "ok": false, "message": message, "state": state }


static func success(state: Dictionary) -> Dictionary:
	return { "ok": true, "state": state }


static func create_game_state(difficulty: String, seed: String) -> Dictionary:
	var regions := {}
	for id in GameData.REGION_IDS:
		var region_capacity: int = GameData.REGIONS[id].populationWeight * B.POPULATION_SCALE
		var deniers := Js.round_half_up(region_capacity * GameData.REGIONS[id].skepticism / 200.0)
		regions[id] = {
			"population": { "unaware": region_capacity - deniers, "deniers": deniers, "believers": 0.0, "followers": 0.0, "losses": 0.0 },
			"public": false, "exposure": 0.0, "madness": 0.0, "backlashUntil": 0, "cooldowns": {},
			"site": { "invested": false, "progress": 0 }, "threat": null, "opportunity": null, "figure": null,
		}
	return {
		"version": 2, "ruleset": "alignment-1", "status": "placing", "difficulty": difficulty, "seed": seed,
		"rngState": Rng.seed_to_state(seed), "elapsedMonths": 0, "speed": 1,
		"influence": B.STARTING_INFLUENCE, "dread": B.STARTING_DREAD, "awareness": 0.0,
		"specialization": null, "finalProgress": 0, "regions": regions, "log": [], "nextLogId": 1, "resultReason": null,
		"telemetry": { "actions": {}, "sacrificed": 0.0, "raidLosses": 0.0, "influenceEarned": 0.0, "influenceSpent": 0.0,
			"dreadEarned": 0.0, "dreadSpent": 0.0, "threatsCountered": 0, "revelations": {} },
	}


static func place_starting_foothold(state: Dictionary, id: String) -> Dictionary:
	if state.status != "placing" or not GameData.REGION_IDS.has(id):
		return fail(state, "Choose a region before the run begins.")
	var next := state.duplicate(true)
	Derived.recruit(next.regions[id], B.STARTING_FOLLOWERS)
	next.regions[id].exposure = 5.0
	next.status = "playing"
	Derived.add_log(next, "A hidden congregation gathers in " + GameData.REGIONS[id].name + ". The alignment is %d months away." % B.PREPARATION_MONTHS)
	return success(next)


## Obstruction is priced by the severity you let build up, so ignoring exposure gets expensive.
static func counter_cost(state: Dictionary, r: Dictionary) -> int:
	var base: int = B.COUNTER_SEVERITY_COST * (r.threat.severity if r.threat != null else 1)
	return int(Js.round_half_up(base * 2 / 3.0)) if state.specialization == "infiltration" else base


## Each further ritual site costs more Dread than the last.
static func site_dread_cost(state: Dictionary) -> int:
	var funded := 0
	for id in GameData.REGION_IDS:
		if state.regions[id].site.invested:
			funded += 1
	return B.SITE_DREAD_COSTS[mini(funded, B.SITE_DREAD_COSTS.size() - 1)]


static func preview_action(state: Dictionary, id: String, region_id: String) -> Dictionary:
	if not GameData.ACTIONS.has(id) or not state.regions.has(region_id):
		return { "ok": false, "reason": "Unknown action or region.", "influence": 0, "dread": 0, "followers": 0, "effect": "", "risk": "" }
	var a: Dictionary = GameData.ACTIONS[id]
	var r: Dictionary = state.regions[region_id]
	var p: Dictionary = r.population
	var cost_influence: int = counter_cost(state, r) if id == "counter" else a.influence
	var cost_dread: int = 15 if id == "manifest" and state.specialization == "terror" else site_dread_cost(state) if id == "prepare" else a.dread
	var cost_followers: int = a.followers
	var reason := "" if is_active(state) else "Start a run before acting; completed runs cannot change."
	var cooldown: int = r.cooldowns.get(id, 0) - state.elapsedMonths
	if not reason and cooldown > 0: reason = "Available in %d months." % cooldown
	if not reason and state.influence < cost_influence: reason = "Need %d Influence." % cost_influence
	if not reason and state.dread < cost_dread: reason = "Need %d Dread." % cost_dread
	if not reason and p.followers < cost_followers: reason = "Need %d local followers." % cost_followers
	if not reason and id != "expand" and p.followers <= 0: reason = "Establish a cult here first."
	if not reason and (id == "recruit" or id == "expand") and Derived.recruitable(r) < 1: reason = "No recruits available. Use a manifestation to change disbelief."
	if not reason and id == "expand" and p.followers > 0: reason = "This region already has a cell."
	if not reason and id == "expand" and Derived.total_followers(state) > 0 and not _has_adjacent_cell(state, region_id): reason = "Requires a cult in an adjacent region."
	if not reason and id == "reveal" and r.public: reason = "Worship is already public."
	if not reason and id == "hide" and not r.public: reason = "This cult is already underground."
	if not reason and id == "conceal" and r.exposure <= _evidence_floor(r): reason = "No concealable evidence. Public rites remain visible."
	if not reason and id == "manifest" and p.deniers + p.unaware < 1: reason = "Everyone here already believes."
	if not reason and id == "prepare" and r.site.invested: reason = "This site's preparation is already funded."
	if not reason and id == "prepare" and not Derived.supported(r): reason = "Maintain at least " + Js.num(Derived.site_requirement(r)) + " local followers."
	# An unfunded site can still be accelerated once funded: an eclipse exploit, or revealing a converted figure.
	var acceleration: int = (8 if r.opportunity != null and r.opportunity.kind == "eclipse" else 0) \
		+ (B.FIGURE_SITE_PROGRESS if r.figure != null and not r.figure.revealed else 0)
	var preparation_left: int = B.DEADLINE - state.elapsedMonths - B.FINAL_MONTHS
	if not reason and id == "prepare" and preparation_left < B.SITE_MONTHS - acceleration:
		reason = "Too late: preparation needs %d more months and the final rite needs %d, but only %d remain." % [B.SITE_MONTHS - acceleration, B.FINAL_MONTHS, maxi(0, B.DEADLINE - state.elapsedMonths)]
	if not reason and id == "counter" and r.threat == null: reason = "No approaching threat in this region."
	if not reason and id == "exploit" and (r.opportunity == null or not ["nightmare", "eclipse"].has(r.opportunity.kind)): reason = "Requires an active nightmare or eclipse."
	if not reason and id == "exploit" and r.opportunity != null and r.opportunity.kind == "eclipse" and (not r.site.invested or r.site.progress >= B.SITE_MONTHS): reason = "An eclipse needs an unfinished ritual site."
	if not reason and id == "convert" and (r.figure != null or r.opportunity == null or not ["celebrity", "politician"].has(r.opportunity.kind)): reason = "Requires an unconverted prominent-person opportunity."
	if not reason and id == "reveal_figure" and (r.figure == null or r.figure.revealed): reason = "Requires a secretly converted figure."
	if not reason and id == "reveal_figure" and not r.site.invested: reason = "Prepare a local ritual site to receive their awakening contribution."
	var effect: String = a.description
	var risk := ""
	if id == "sacrifice":
		var gain: int = 35 if state.specialization == "terror" else B.SACRIFICE_DREAD
		effect = "+" + Js.fixed(minf(gain, B.DREAD_CAP - state.dread), 0) + " Dread. " + Js.fixed(maxf(0.0, p.followers - cost_followers), 0) \
			+ " followers remain; income falls by " + Js.fixed(cost_followers * B.INFLUENCE_PER_FOLLOWER, 2) + "/month."
		risk = "+12 Exposure. Fewer than " + Js.num(Derived.site_requirement(r)) + " followers interrupts this site's ritual."
	elif id == "reveal":
		risk = "+18 Exposure, +5 world awareness; recruitment backlash for 6 months, then full public growth."
	elif id == "manifest":
		risk = "+15 Exposure and +8 awareness. New believers can strengthen resistance."
	elif id == "prepare":
		risk = "Site remains exposed, even underground. An occult response is scheduled."
		if preparation_left < B.SITE_MONTHS:
			risk += " Needs acceleration to finish in time: %d preparation months, then a %d-month final rite." % [B.SITE_MONTHS - acceleration, B.FINAL_MONTHS]
	elif id == "reveal_figure":
		risk = "+25 Exposure, +8 awareness; 12 months of recruitment backlash. Secret benefits end."
	elif id == "recruit":
		effect = "+" + Js.fixed(minf(B.RECRUIT_AMOUNT, Derived.recruitable(r)), 0) + " followers; +3 Exposure."
	elif id == "exploit":
		effect = "+30 Dread once, up to the resource cap." if r.opportunity != null and r.opportunity.kind == "nightmare" else "+8 months of site preparation once."
	return { "ok": not reason, "reason": reason, "influence": cost_influence, "dread": cost_dread, "followers": cost_followers, "effect": effect, "risk": risk }


static func _has_adjacent_cell(state: Dictionary, region_id: String) -> bool:
	for other in GameData.REGIONS[region_id].adjacent:
		if state.regions[other].population.followers > 0:
			return true
	return false


static func _evidence_floor(r: Dictionary) -> float:
	return 40.0 if r.site.invested else 20.0 if r.public else 0.0


static func perform_action(state: Dictionary, id: String, region_id: String) -> Dictionary:
	var preview := preview_action(state, id, region_id)
	if not preview.ok:
		return fail(state, preview.reason)
	var next := state.duplicate(true)
	var r: Dictionary = next.regions[region_id]
	var t: Dictionary = next.telemetry
	next.influence -= preview.influence
	next.dread -= preview.dread
	t.influenceSpent += preview.influence
	t.dreadSpent += preview.dread
	t.actions[id] = t.actions.get(id, 0) + 1
	r.cooldowns[id] = next.elapsedMonths + GameData.ACTIONS[id].cooldown
	match id:
		"recruit", "expand":
			Derived.recruit(r, B.RECRUIT_AMOUNT if id == "recruit" else B.EXPAND_AMOUNT)
			r.exposure = Derived.clamp_pct(r.exposure + 3)
		"sacrifice":
			t.sacrificed += Derived.lose_followers(r, preview.followers)
			_grant_dread(next, 35 if next.specialization == "terror" else B.SACRIFICE_DREAD)
			r.exposure = Derived.clamp_pct(r.exposure + 12)
		"reveal":
			r.public = true
			r.exposure = Derived.clamp_pct(r.exposure + 18)
			next.awareness = Derived.clamp_pct(next.awareness + 5)
			r.backlashUntil = maxi(r.backlashUntil, next.elapsedMonths + 6)
			if not t.revelations.has(region_id):
				t.revelations[region_id] = next.elapsedMonths
		"hide":
			r.public = false
		"conceal":
			r.exposure = maxf(_evidence_floor(r), r.exposure - 25)
			next.awareness = Derived.clamp_pct(next.awareness - B.CONCEAL_AWARENESS)
		"manifest":
			var p: Dictionary = r.population
			var denied := minf(p.deniers, B.MANIFESTATION_DENIERS)
			var unaware := minf(p.unaware, B.MANIFESTATION_UNAWARE)
			p.deniers -= denied
			p.unaware -= unaware
			p.believers += denied + unaware
			r.madness = Derived.clamp_pct(r.madness + 20)
			r.exposure = Derived.clamp_pct(r.exposure + 15)
			next.awareness = Derived.clamp_pct(next.awareness + 8)
		"prepare":
			r.site.invested = true
			r.exposure = maxf(40.0, r.exposure)
			next.awareness = Derived.clamp_pct(next.awareness + 8)
			Director.schedule_threat(next, region_id, true)
		"counter":
			r.threat = null
			t.threatsCountered += 1
		"exploit":
			if r.opportunity.kind == "nightmare":
				_grant_dread(next, 30)
			else:
				r.site.progress = mini(B.SITE_MONTHS, r.site.progress + 8)
			r.opportunity = null
		"convert":
			r.figure = { "kind": r.opportunity.kind, "revealed": false }
			r.opportunity = null
			r.exposure = Derived.clamp_pct(r.exposure + 5)
		"reveal_figure":
			r.figure.revealed = true
			Derived.recruit(r, 60)
			r.site.progress = mini(B.SITE_MONTHS, r.site.progress + B.FIGURE_SITE_PROGRESS)
			r.exposure = Derived.clamp_pct(r.exposure + 25)
			next.awareness = Derived.clamp_pct(next.awareness + 8)
			r.backlashUntil = next.elapsedMonths + 12
	Derived.add_log(next, GameData.ACTIONS[id].name + " · " + GameData.REGIONS[region_id].name + ". " + preview.effect)
	return success(next)


static func _grant_dread(s: Dictionary, amount: float) -> void:
	var gain := minf(amount, B.DREAD_CAP - s.dread)
	s.dread += gain
	s.telemetry.dreadEarned += gain


static func choose_specialization(state: Dictionary, choice: String) -> Dictionary:
	if not is_active(state) or state.specialization != null or not GameData.SPECIALIZATION_IDS.has(choice):
		return fail(state, "Choose one doctrine per run.")
	if state.influence < GameData.SPECIALIZATION_COST:
		return fail(state, "A doctrine requires 40 Influence.")
	var next := state.duplicate(true)
	next.influence -= GameData.SPECIALIZATION_COST
	next.telemetry.influenceSpent += GameData.SPECIALIZATION_COST
	next.specialization = choice
	Derived.add_log(next, "The cult commits to " + choice + ". Other doctrines are closed for this run.")
	return success(next)


static func set_game_speed(state: Dictionary, speed: int) -> Dictionary:
	if is_over(state) or speed < 0 or speed > 2:
		return state
	var next := state.duplicate()
	next.speed = speed
	return next


static func awakening_reason(state: Dictionary) -> String:
	if state.status != "alignment":
		return "The stars align in %d months." % (B.PREPARATION_MONTHS - state.elapsedMonths) if state.status == "playing" else "The final rite is not available now."
	if Derived.ready_sites(state) < B.REQUIRED_SITES:
		return "Need %d prepared sites, each supported by a quarter of its region." % B.REQUIRED_SITES
	if state.dread < B.FINAL_DREAD:
		return "Reserve 100 Dread for the final rite."
	if B.DEADLINE - state.elapsedMonths < B.FINAL_MONTHS:
		return "Fewer than 12 months remain. The final rite can no longer finish in time."
	return ""


static func start_awakening(state: Dictionary) -> Dictionary:
	var reason := awakening_reason(state)
	if reason:
		return fail(state, reason)
	var next := state.duplicate(true)
	next.dread -= B.FINAL_DREAD
	next.telemetry.dreadSpent += B.FINAL_DREAD
	next.status = "awakening"
	Derived.add_log(next, "The final rite begins. Maintain three supported sites for 12 months.")
	return success(next)


static func advance_simulation(state: Dictionary, months: float) -> Dictionary:
	if not is_active(state) or not is_finite(months) or months < 1:
		return state
	var next := state.duplicate(true)
	for _month in mini(int(floor(months)), B.DEADLINE):
		if not is_active(next):
			break
		next.elapsedMonths += 1
		Director.resolve_world(next)
		var income := minf(Derived.influence_per_month(next), maxf(0.0, B.INFLUENCE_CAP - next.influence))
		next.influence += income
		next.telemetry.influenceEarned += income
		for id in GameData.REGION_IDS:
			var r: Dictionary = next.regions[id]
			Derived.recruit(r, Derived.recruitment_per_month(next, r))
			if r.population.followers > 0:
				r.exposure = Derived.clamp_pct(maxf(_evidence_floor(r), r.exposure + (0.6 if r.public else -0.36)))
				if r.public or r.site.invested:
					next.awareness = Derived.clamp_pct(next.awareness + 0.075)
			r.madness = maxf(0.0, r.madness - 0.3)
			if r.site.invested and Derived.supported(r) and r.site.progress < B.SITE_MONTHS:
				r.site.progress += 1
				if r.site.progress == B.SITE_MONTHS:
					Derived.add_log(next, "The ritual site in " + GameData.REGIONS[id].name + " is prepared.")
		next.awareness = Derived.clamp_pct(next.awareness + Derived.cult_share(next) * B.AWARENESS_PER_CAPITA)
		if next.status == "awakening" and Derived.ready_sites(next) >= B.REQUIRED_SITES:
			next.finalProgress += 1
		if next.finalProgress >= B.FINAL_MONTHS:
			next.status = "won"
			next.speed = 0
			next.resultReason = "Three surviving ritual sites opened the way. Cthulhu awakens."
			Derived.add_log(next, next.resultReason)
			break
		if next.awareness >= 100:
			next.status = "lost"
			next.speed = 0
			next.resultReason = "The world knows. Every cell is hunted and the rite dies with them."
			Derived.add_log(next, next.resultReason)
			break
		if next.elapsedMonths >= B.DEADLINE:
			next.status = "lost"
			next.speed = 0
			next.resultReason = "The alignment has passed. %d/3 sites supported; final rite %d/12 months." % [Derived.ready_sites(next), next.finalProgress]
			Derived.add_log(next, next.resultReason)
			break
		Director.tick(next)
		if next.elapsedMonths == B.PREPARATION_MONTHS:
			next.status = "alignment"
			next.speed = 0
			Derived.add_log(next, "The stars align. Time is paused. You have %d months to complete the awakening." % B.ALIGNMENT_MONTHS)
			break
	return next
