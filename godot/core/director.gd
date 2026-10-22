class_name Director
## World response: threats and opportunities. Mirrors src/game/director.ts. Mutates only an
## engine-owned copy. A threat is separate from the opportunity slot.

const B = preload("res://core/balance.gd")


static func random(state: Dictionary) -> float:
	var value := Rng.next_random(state.rngState)
	state.rngState = value.state
	return value.value


static func schedule_threat(state: Dictionary, id: String, ritual := false) -> void:
	var r: Dictionary = state.regions[id]
	if r.threat != null or r.population.followers <= 0:
		return
	var warning: int = B.THREAT_WARNING_MONTHS + GameData.DIFFICULTIES[state.difficulty].warning \
		+ (2 if state.specialization == "infiltration" else 0) \
		+ (2 if r.figure != null and r.figure.kind == "politician" and not r.figure.revealed else 0)
	var kind := "occult" if ritual or r.site.invested else "raid" if r.public else "investigation"
	var severity := int(Derived.clamp_pct(1 + floor((r.exposure + state.awareness) / 60.0), 1, 3))
	r.threat = { "kind": kind, "dueMonth": state.elapsedMonths + warning, "severity": severity }
	Derived.add_log(state, GameData.THREAT_NAMES[kind] + " in " + GameData.REGIONS[id].name + ". Respond within %d months. If ignored: " % warning + Threats.describe(r))


static func resolve_world(state: Dictionary) -> void:
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		var region_name: String = GameData.REGIONS[id].name
		if r.opportunity != null and r.opportunity.expiresMonth <= state.elapsedMonths:
			Derived.add_log(state, GameData.OPPORTUNITY_NAMES[r.opportunity.kind] + " passes in " + region_name + ".")
			r.opportunity = null
		if r.threat != null and r.threat.dueMonth <= state.elapsedMonths:
			var threat: Dictionary = r.threat
			var c := Threats.consequences(r)
			var loss := Derived.lose_followers(r, c.followers)
			state.telemetry.raidLosses += loss
			if threat.kind == "occult" and r.site.invested:
				r.site.progress -= c.preparationLost
			r.exposure = Derived.clamp_pct(r.exposure + 5)
			r.threat = null
			var detail := ""
			if c.destroysCell:
				detail = " The cell and any ritual site are destroyed."
			elif c.preparationLost:
				detail = " Ritual loses %d preparation months." % c.preparationLost
			if not c.destroysCell and c.interruptsSite:
				detail += " Ritual support is interrupted."
			Derived.add_log(state, region_name + " loses " + Js.fixed(loss, 0) + " followers to " + threat.kind + "; income −" + Js.fixed(c.incomeLost, 2) + "/month." + detail)


static func tick(state: Dictionary) -> void:
	if state.elapsedMonths % 4 == 0:
		for id in GameData.REGION_IDS:
			var r: Dictionary = state.regions[id]
			if r.population.followers <= 0 or r.threat != null:
				continue
			var believers: float = r.population.believers / maxf(1.0, Derived.live_population(r))
			var pressure: float = (0.02 + r.exposure / 500.0 + (0.1 if r.public else 0.0) + (0.12 if r.site.invested else 0.0)
				+ state.awareness / 1500.0 + believers * 0.15 + r.madness / 2000.0) \
				* GameData.DIFFICULTIES[state.difficulty].pressure * (0.6 + GameData.REGIONS[id].institutionalResistance / 150.0)
			if random(state) < pressure:
				schedule_threat(state, id)
	if state.elapsedMonths % 12 == 0:
		var available: Array[String] = []
		for id in GameData.REGION_IDS:
			if state.regions[id].opportunity == null and state.regions[id].population.followers > 0:
				available.append(id)
		# The draw happens even when no region qualifies, as in the original.
		var index := int(floor(random(state) * available.size()))
		if available.is_empty():
			return
		var id: String = available[index]
		var r: Dictionary = state.regions[id]
		var kinds: Array = ["nightmare", "eclipse"] if r.figure != null else ["nightmare", "eclipse", "celebrity", "politician"]
		var kind: String = kinds[int(floor(random(state) * kinds.size()))]
		r.opportunity = { "kind": kind, "expiresMonth": state.elapsedMonths + 8 }
		Derived.add_log(state, GameData.OPPORTUNITY_NAMES[kind] + " in " + GameData.REGIONS[id].name + ". Opportunity lasts 8 months.")
