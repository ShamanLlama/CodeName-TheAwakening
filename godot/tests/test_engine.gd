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
		s.regions[id].site = { "invested": true, "progress": 24 }
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
