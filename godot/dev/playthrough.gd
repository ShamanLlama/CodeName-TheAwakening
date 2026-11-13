class_name Playthrough
## The test-only player policy, first ported from src/tests/playthrough.ts. Every gain comes
## through normal commands. Used for balance checks and for building realistic states for screenshots.
## alignment-2: concealment is priced per use, so the policy conceals only real evidence, or
## awareness when it can still afford obstruction. Optional spending keeps a reserve for pending
## obstructions and a few months of site upkeep.

const B = preload("res://core/balance.gd")
# Ritual support scales with regional population, so sites go in small regions.
const TARGETS: Array[String] = ["latin_america", "middle_east", "oceania"]
const FARM := "africa"


static func dread_owed(state: Dictionary) -> int:
	var funded := 0
	for id in TARGETS:
		if state.regions[id].site.invested:
			funded += 1
	var owed: int = B.FINAL_DREAD
	for i in range(funded, TARGETS.size()):
		owed += B.SITE_DREAD_COSTS[mini(i, B.SITE_DREAD_COSTS.size() - 1)]
	return owed


static func reserve(state: Dictionary) -> float:
	var owed := 0.0
	for id in GameData.REGION_IDS:
		var r: Dictionary = state.regions[id]
		if r.site.invested:
			owed += B.SITE_UPKEEP * 4
		if r.threat != null and r.cooldowns.get("counter", 0) <= state.elapsedMonths:
			owed += GameEngine.counter_cost(state, r)
	return owed


## Evidence above the floor is concealed as before; awareness only from spare Influence.
static func conceal_need(state: Dictionary, r: Dictionary) -> String:
	var floor := 40.0 if r.site.invested else 20.0 if r.public else 0.0
	if r.exposure > maxf(30.0, floor + 10.0):
		return "evidence"
	if state.awareness >= 50 and state.influence >= GameEngine.conceal_cost(state) + 40:
		return "awareness"
	return ""


## alignment-3: one answer per threat. Site regions are obstructed as before. When that is on
## cooldown or unaffordable, a site or cell in danger is saved with Dread the rite does not need;
## a cell with no site goes to ground when the strike would cost more than the months in hiding.
static func respond(state: Dictionary, id: String) -> Dictionary:
	var r: Dictionary = state.regions[id]
	if TARGETS.has(id) and GameEngine.preview_action(state, "counter", id).ok:
		return GameEngine.perform_action(state, "counter", id).state
	var c := Threats.consequences(r)
	var silence := GameEngine.preview_action(state, "silence", id)
	var owed := 0 if state.status == "awakening" else dread_owed(state)
	if silence.ok and state.dread - silence.dread >= owed and (c.destroysCell or r.site.invested and (c.interruptsSite or c.preparationLost > 0)):
		return GameEngine.perform_action(state, "silence", id).state
	var hiding_cost: float = B.SCATTER_MONTHS * Derived.recruitment_per_month(state, r)
	if not r.site.invested and not r.public and c.followers > hiding_cost and GameEngine.preview_action(state, "scatter", id).ok:
		return GameEngine.perform_action(state, "scatter", id).state
	return state


## strategy: "early", "late" or "hub". Regions captured before an action keep their pre-action
## values, exactly as the TypeScript policy's object references do.
static func play(strategy: String, seed: String, stop_month := B.DEADLINE) -> Dictionary:
	var run := { "s": GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", seed), FARM).state }
	var act := func(id: String, region: String) -> void:
		if GameEngine.preview_action(run.s, id, region).ok:
			run.s = GameEngine.perform_action(run.s, id, region).state
	var spare := func(id: String, region: String) -> void:
		var preview := GameEngine.preview_action(run.s, id, region)
		if preview.ok and run.s.influence - preview.influence >= reserve(run.s):
			run.s = GameEngine.perform_action(run.s, id, region).state
	for _step in stop_month:
		if GameEngine.is_over(run.s):
			break
		for id in GameData.REGION_IDS:
			if run.s.regions[id].threat != null:
				run.s = respond(run.s, id)
		for id in TARGETS:
			var r: Dictionary = run.s.regions[id]
			if r.opportunity != null and (r.opportunity.kind == "nightmare" or r.opportunity.kind == "eclipse" and r.site.invested):
				act.call("exploit", id)
		for id in TARGETS:
			if run.s.regions[id].population.followers <= 0:
				act.call("expand", id)
		if run.s.specialization == null and run.s.elapsedMonths >= 20 and run.s.influence >= 70:
			var doctrine := "recruitment" if strategy == "early" else "infiltration" if strategy == "late" else "terror"
			run.s = GameEngine.choose_specialization(run.s, doctrine).state
		# Funding a site outranks every other use of Influence: it is the only thing that wins.
		for id in TARGETS:
			if not run.s.regions[id].site.invested:
				act.call("prepare", id)
		var owed := dread_owed(run.s)
		var farm: Dictionary = run.s.regions[FARM]
		if farm.population.followers < 700:
			spare.call("recruit", FARM)
		if run.s.dread < owed and farm.population.followers - B.SACRIFICE_FOLLOWERS >= 260:
			act.call("sacrifice", FARM)
		var farm_need := conceal_need(run.s, farm)
		if farm_need == "evidence":
			act.call("conceal", FARM)
		elif farm_need == "awareness":
			spare.call("conceal", FARM)
		for id in TARGETS:
			var r: Dictionary = run.s.regions[id]
			if r.population.followers <= 0:
				continue
			var need := Derived.site_requirement(r)
			var buffer := need + 160
			if r.population.followers < buffer:
				spare.call("recruit", id)
			var reveal_month := 160 if strategy == "late" else 8
			var allows_public: bool = run.s.elapsedMonths >= reveal_month and (strategy != "hub" or id == FARM)
			if allows_public and not r.public and r.population.followers < need:
				spare.call("reveal", id)
			if r.public and (not allows_public or r.population.followers >= buffer):
				act.call("hide", id)
			var need_conceal := conceal_need(run.s, r)
			if need_conceal == "evidence":
				act.call("conceal", id)
			elif need_conceal == "awareness":
				spare.call("conceal", id)
			if run.s.dread < owed and r.population.followers - B.SACRIFICE_FOLLOWERS >= need + 120:
				act.call("sacrifice", id)
		if run.s.status == "alignment" and Derived.ready_sites(run.s) >= 3:
			run.s = GameEngine.start_awakening(run.s).state
		run.s = GameEngine.advance_simulation(run.s, 1)
	return run.s
