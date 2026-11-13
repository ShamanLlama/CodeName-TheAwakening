extends "res://tests/test_case.gd"
## Rule checks ported from src/game/engine.test.ts and foundation.test.ts.

const B = preload("res://core/balance.gd")


static func start() -> Dictionary:
	return GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "engine"), "africa").state


static func ready(month := B.PREPARATION_MONTHS) -> Dictionary:
	var s := start()
	s.elapsedMonths = month
	s.status = "alignment"
	s.dread = 200.0
	for id in ["africa", "europe", "latin_america"]:
		Derived.recruit(s.regions[id], Derived.site_requirement(s.regions[id]) + 60)
		s.regions[id].site = { "invested": true, "progress": 24, "dormant": false }
	return s


func test_starts_with_one_cell_and_three_resources() -> void:
	var s := start()
	check_eq(Derived.total_followers(s), 100)
	check_eq(s.influence, 35)
	check_eq(s.dread, 20)


func test_sacrifice_consumes_followers_without_mutating_input() -> void:
	var s := start()
	var snapshot := s.duplicate(true)
	var before := Derived.influence_per_month(s)
	var result := GameEngine.perform_action(s, "sacrifice", "africa")
	check(result.ok, "sacrifice should succeed")
	check_eq(s, snapshot, "input state")
	check_eq(result.state.regions.africa.population.followers, 60)
	check_eq(result.state.regions.africa.population.losses, 40)
	check_eq(result.state.dread, 45)
	check_eq(Derived.influence_per_month(result.state), before - B.SACRIFICE_FOLLOWERS * B.INFLUENCE_PER_FOLLOWER)


func test_refusals_return_the_same_state() -> void:
	var s := start()
	check(is_same(GameEngine.perform_action(s, "prepare", "africa").state, s), "unaffordable site returns input")
	check(not GameEngine.perform_action(s, "expand", "oceania").ok, "non-adjacent expansion refused")
	var sacrificed: Dictionary = GameEngine.perform_action(s, "sacrifice", "africa").state
	check(is_same(GameEngine.perform_action(sacrificed, "sacrifice", "africa").state, sacrificed), "cooldown refusal returns input")


func test_waiting_grants_no_dread_and_ignores_bad_month_counts() -> void:
	var s := start()
	check_eq(GameEngine.advance_simulation(s, 1).dread, 20)
	check_eq(GameEngine.advance_simulation(GameEngine.set_game_speed(s, 0), 12).elapsedMonths, 12)
	check(is_same(GameEngine.advance_simulation(s, INF), s), "infinite months refused")
	check(is_same(GameEngine.advance_simulation(s, 0.5), s), "fractional month refused")


func test_alignment_pauses_long_advances_and_the_deadline_ends_the_run() -> void:
	var s := GameEngine.advance_simulation(start(), B.DEADLINE)
	check_eq(s.elapsedMonths, B.PREPARATION_MONTHS)
	check_eq(s.speed, 0)
	check_eq(s.status, "alignment")
	var lost := GameEngine.advance_simulation(s, B.ALIGNMENT_MONTHS)
	check_eq(lost.status, "lost")
	check_eq(lost.elapsedMonths, B.DEADLINE)


func test_final_rite_wins_after_twelve_supported_months() -> void:
	var started := GameEngine.start_awakening(ready(B.PREPARATION_MONTHS + B.FINAL_MONTHS))
	check(started.ok, "rite should start")
	var run: Dictionary = started.state
	for _month in B.FINAL_MONTHS:
		# Silence the director so this measures only the alignment arithmetic.
		for id in GameData.REGION_IDS:
			run.regions[id].threat = null
		run = GameEngine.advance_simulation(run, 1)
	check_eq(run.status, "won")


func test_last_follower_lost_destroys_the_site_and_conserves_population() -> void:
	var s := ready()
	var r: Dictionary = s.regions.europe
	r.population.unaware += r.population.followers - 40
	r.population.followers = 40.0
	var sacrificed: Dictionary = GameEngine.perform_action(s, "sacrifice", "europe").state
	check(not sacrificed.regions.europe.site.invested, "site destroyed")
	check_eq(Derived.ready_sites(sacrificed), 2)
	for id in GameData.REGION_IDS:
		check_eq(Derived.capacity(sacrificed.regions[id]), Derived.capacity(s.regions[id]), id + " capacity")


func test_population_adjacency_and_calendar() -> void:
	var s: Dictionary = GameEngine.place_starting_foothold(GameEngine.create_game_state("normal", "population"), "oceania").state
	var total := 0.0
	for id in GameData.REGION_IDS:
		total += Derived.live_population(s.regions[id])
		for other in GameData.REGIONS[id].adjacent:
			check(GameData.REGIONS[other].adjacent.has(id), "%s <-> %s adjacency is symmetric" % [id, other])
	check_eq(total, 10000)
	check_eq(Derived.calendar(0), "Year 1 · Month 1")
	check_eq(Derived.calendar(12), "Year 2 · Month 1")


func test_javascript_number_formatting() -> void:
	check_eq(Js.fixed(0.125, 2), "0.13")
	check_eq(Js.fixed(2.5, 0), "3")
	check_eq(Js.fixed(0.56, 2), "0.56")
	check_eq(Js.fixed(7.0, 2), "7.00")
	check_eq(Js.num(75.0), "75")
	check_eq(Js.num(100.0 - 97.3), "2.700000000000003")
	check_eq(Rng.next_random(Rng.seed_to_state("same")), Rng.next_random(Rng.seed_to_state("same")))


# --- alignment-2 ------------------------------------------------------------------------

func test_site_upkeep_is_paid_monthly_and_unpaid_sites_lie_dormant() -> void:
	var s := ready(B.PREPARATION_MONTHS - 30)
	s.status = "playing"
	for id in ["africa", "europe", "latin_america"]:
		s.regions[id].site.progress = 10
	s.influence = 100.0
	var paid := GameEngine.advance_simulation(s, 1)
	check_eq(paid.telemetry.upkeepPaid, 3 * B.SITE_UPKEEP, "three sites paid")
	check_eq(paid.regions.africa.site.progress, 11, "paid site advances")
	check_eq(Derived.net_influence_per_month(paid), Derived.influence_per_month(paid) - 3 * B.SITE_UPKEEP)
	# An empty treasury and a tiny cult cannot pay: the sites lie dormant and say so.
	var broke := paid.duplicate(true)
	broke.influence = 0.0
	for id in GameData.REGION_IDS:
		var p: Dictionary = broke.regions[id].population
		if p.followers > 10:
			p.unaware += p.followers - 10
			p.followers = 10.0
	var stalled := GameEngine.advance_simulation(broke, 1)
	check(stalled.regions.africa.site.dormant, "unpaid site is dormant")
	check_eq(Derived.site_label(stalled.regions.africa), "Dormant · upkeep unpaid")
	check(stalled.log.any(func(e: Dictionary) -> bool: return e.message.contains("lies dormant")), "dormancy is logged")
	# Paying again wakes the site.
	stalled.influence = 50.0
	check(not GameEngine.advance_simulation(stalled, 1).regions.africa.site.dormant, "paid site wakes")


func test_dormant_sites_cannot_start_or_carry_the_final_rite() -> void:
	var s := ready()
	s.regions.europe.site.dormant = true
	check_eq(Derived.ready_sites(s), 2)
	check(not GameEngine.start_awakening(s).ok, "a dormant site cannot carry the rite")
	s.regions.europe.site.dormant = false
	check(GameEngine.start_awakening(s).ok, "paid sites can")


func test_conceal_price_rises_with_awareness_and_lowers_it() -> void:
	var s := start()
	s.awareness = 40.0
	s.influence = 100.0
	s.regions.africa.exposure = 0.0
	var preview := GameEngine.preview_action(s, "conceal", "africa")
	check(preview.ok, "awareness alone can be concealed: " + preview.reason)
	check_eq(preview.influence, 20 + 20, "20 plus half of awareness")
	var result := GameEngine.perform_action(s, "conceal", "africa")
	check_eq(result.state.awareness, 40.0 - B.CONCEAL_AWARENESS)
	check_eq(result.state.influence, s.influence - 40)
	var calm := start()
	calm.regions.africa.exposure = 0.0
	check(not GameEngine.preview_action(calm, "conceal", "africa").ok, "nothing to conceal without evidence or awareness")
	check_eq(GameEngine.conceal_cost(calm), 20)


func test_alignment_hunt_is_announced_in_advance() -> void:
	var s := start()
	s = GameEngine.advance_simulation(s, B.PREPARATION_MONTHS - B.FINALE_WARNING_MONTHS)
	check(s.log.any(func(e: Dictionary) -> bool: return e.message.begins_with("The world senses the coming alignment")), "warning logged")
	check(not Director.site_hunt(s), "hunt not yet active")
	check(Forecast.ritual(s).message.contains("Threats against ritual sites intensify in %d months" % B.FINALE_WARNING_MONTHS), "forecast warns")
	var aligned := ready()
	check(Director.site_hunt(aligned), "hunt active once the stars are right")
	check(Forecast.ritual(aligned).message.contains("The world hunts ritual sites"), "forecast names the hunt")


func test_new_state_records_the_current_ruleset() -> void:
	check_eq(start().ruleset, B.RULESET)


# --- alignment-3 ------------------------------------------------------------------------

## A 400-strong hidden cell in Africa with a threat five months out.
static func threatened(severity := 1) -> Dictionary:
	var s := start()
	s.elapsedMonths = 20
	Derived.recruit(s.regions.africa, 300)
	s.influence = 100.0
	s.dread = 100.0
	s.regions.africa.threat = { "kind": "investigation", "dueMonth": 25, "severity": severity }
	return s


func test_scatter_ends_the_threat_and_costs_the_cell_its_months() -> void:
	var s := threatened()
	s.regions.africa.public = true
	s.regions.africa.exposure = 30.0
	var preview := GameEngine.preview_action(s, "scatter", "africa")
	check(preview.ok, "a threatened cell can scatter")
	check_eq([preview.influence, preview.dread, preview.followers, preview.awareness], [0, 0, 0, 0.0], "scattering is paid in time")
	var hidden: Dictionary = GameEngine.perform_action(s, "scatter", "africa").state
	var cell: Dictionary = hidden.regions.africa
	check(cell.threat == null, "the threat finds nobody")
	check(cell.scattered and not cell.public, "the cell is in hiding and worship is no longer public")
	check_eq(cell.exposure, 20.0, "exposure falls")
	check_eq(Derived.influence_per_month(hidden), 0.0, "a scattered cell earns nothing")
	check_eq(Derived.recruitment_per_month(hidden, cell), 0.0, "and recruits nobody")
	check_eq(GameEngine.preview_action(hidden, "recruit", "africa").reason, "The flock is scattered for %d more months." % B.SCATTER_MONTHS)
	check(SaveData.is_game_state(hidden), "a scattered cell saves and reloads")
	var later := GameEngine.advance_simulation(hidden, B.SCATTER_MONTHS - 1)
	check(later.regions.africa.scattered, "still hidden a month before the end")
	check_eq(later.regions.africa.population.followers, 400.0, "nobody joined and nobody was taken")
	var back := GameEngine.advance_simulation(later, 1)
	check(not back.regions.africa.scattered, "the flock gathers again")
	check(Derived.influence_per_month(back) > 0, "and earns again")
	back.regions.africa.threat = { "kind": "investigation", "dueMonth": back.elapsedMonths + 5, "severity": 1 }
	check(GameEngine.preview_action(back, "scatter", "africa").reason.begins_with("Available in"), "it cannot scatter again so soon")
	check(not GameEngine.preview_action(start(), "scatter", "africa").ok, "nothing to hide from without a threat")


func test_nobody_hunts_a_scattered_cell() -> void:
	var s := threatened()
	s.regions.africa.threat = null
	s.regions.africa.exposure = 100.0
	s.awareness = 90.0
	s.regions.africa.scattered = true
	s.regions.africa.scatteredUntil = s.elapsedMonths + B.SCATTER_MONTHS
	for attempt in 40:
		s.rngState = Rng.seed_to_state("hunt-%d" % attempt)
		Director.tick(s)
		check(s.regions.africa.threat == null, "no threat is scheduled while scattered")
	s.regions.africa.scattered = false
	var found := false
	for attempt in 40:
		s.rngState = Rng.seed_to_state("hunt-%d" % attempt)
		Director.tick(s)
		found = found or s.regions.africa.threat != null
	check(found, "the same cell is hunted once it gathers")


func test_a_scattered_site_stalls_and_cannot_carry_the_rite() -> void:
	var s := ready()
	s.regions.europe.threat = { "kind": "occult", "dueMonth": s.elapsedMonths + 3, "severity": 1 }
	check_eq(Derived.ready_sites(s), 3)
	var hidden: Dictionary = GameEngine.perform_action(s, "scatter", "europe").state
	check_eq(Derived.ready_sites(hidden), 2, "a scattered flock carries no site")
	check(GameEngine.awakening_reason(hidden) != "", "so the rite cannot begin")
	check_eq(Derived.site_label(hidden.regions.europe), "Stalled · the flock is scattered")
	check_eq(hidden.regions.europe.exposure, 40.0, "a ritual site stays visible")


func test_silence_spends_dread_by_severity_and_the_world_notices() -> void:
	var s := threatened(2)
	var preview := GameEngine.preview_action(s, "silence", "africa")
	check_eq(preview.dread, 2 * B.SILENCE_SEVERITY_DREAD)
	check_eq(preview.awareness, 2 * B.SILENCE_SEVERITY_AWARENESS)
	var done: Dictionary = GameEngine.perform_action(s, "silence", "africa").state
	check(done.regions.africa.threat == null, "the threat ends")
	check_eq(done.dread, 100.0 - 2 * B.SILENCE_SEVERITY_DREAD)
	check_eq(done.awareness, 2 * B.SILENCE_SEVERITY_AWARENESS, "awareness is part of the price")
	check_eq(done.telemetry.dreadSpent, float(2 * B.SILENCE_SEVERITY_DREAD))
	check_eq(done.regions.africa.population.followers, 400.0, "nobody in the cell is lost")
	done.regions.africa.threat = { "kind": "investigation", "dueMonth": 30, "severity": 1 }
	check(GameEngine.preview_action(done, "silence", "africa").reason.begins_with("Available in"), "one silencing per region a year")
	check(GameEngine.preview_action(done, "counter", "africa").ok, "obstruction is a separate favour")
	var harbinger := threatened(2)
	harbinger.specialization = "terror"
	check_eq(GameEngine.preview_action(harbinger, "silence", "africa").dread, 2 * B.SILENCE_SEVERITY_DREAD_TERROR, "Harbinger silences for a third less")
	check(not GameEngine.preview_action(start(), "silence", "africa").ok, "nobody to silence without a threat")


func test_going_public_alarms_the_world_only_the_first_time() -> void:
	var s := threatened()
	s.regions.africa.threat = null
	check_eq(GameEngine.preview_action(s, "reveal", "africa").awareness, B.REVEAL_AWARENESS)
	var public: Dictionary = GameEngine.perform_action(s, "reveal", "africa").state
	check_eq(public.awareness, B.REVEAL_AWARENESS)
	var hidden: Dictionary = GameEngine.perform_action(public, "hide", "africa").state
	hidden.elapsedMonths += 6
	var again := GameEngine.preview_action(hidden, "reveal", "africa")
	check(again.ok, "worship can go public again")
	check_eq(again.awareness, 0.0, "the world already knows this congregation")
	check(not again.risk.contains("awareness"), "and the risk text no longer promises awareness")
	check_eq(GameEngine.perform_action(hidden, "reveal", "africa").state.awareness, B.REVEAL_AWARENESS, "no second charge")


func test_every_conspicuous_action_states_its_awareness_price() -> void:
	var s := threatened()
	s.regions.africa.threat = null
	s.dread = 200.0
	check_eq(GameEngine.preview_action(s, "manifest", "africa").awareness, B.MANIFEST_AWARENESS)
	check_eq(GameEngine.preview_action(s, "prepare", "africa").awareness, B.SITE_AWARENESS)
	for id in ["recruit", "sacrifice", "conceal", "counter", "scatter"]:
		check_eq(GameEngine.preview_action(s, id, "africa").awareness, 0.0, id + " does not alarm the world")
	check_eq(GameEngine.perform_action(s, "manifest", "africa").state.awareness, B.MANIFEST_AWARENESS, "the stated price is what is charged")
	check_eq(GameEngine.perform_action(s, "prepare", "africa").state.awareness, B.SITE_AWARENESS)
