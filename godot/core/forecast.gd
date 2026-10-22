class_name Forecast
## The ritual timetable shown to the player. Mirrors src/game/forecast.ts.

const B = preload("res://core/balance.gd")


static func ritual(state: Dictionary) -> Dictionary:
	if GameEngine.is_over(state):
		return { "urgent": false, "message": state.resultReason if state.resultReason != null else "Run complete." }
	var ready := Derived.ready_sites(state)
	if state.status == "awakening":
		var remaining: int = B.FINAL_MONTHS - state.finalProgress
		var left: int = B.DEADLINE - state.elapsedMonths
		return { "urgent": ready < B.REQUIRED_SITES or left <= remaining,
			"message": "Final rite: %d supported months needed; %d months remain." % [remaining, left] + (" Restore three supported sites now." if ready < B.REQUIRED_SITES else "") }
	var sites: Array[Dictionary] = []
	for id in GameData.REGION_IDS:
		if state.regions[id].site.invested:
			sites.append(state.regions[id])
	var remaining_preparations: Array[int] = []
	for r in sites:
		remaining_preparations.append(B.SITE_MONTHS - r.site.progress)
	remaining_preparations.sort()
	if remaining_preparations.size() >= B.REQUIRED_SITES:
		var preparation := remaining_preparations[B.REQUIRED_SITES - 1]
		var earliest: int = maxi(B.PREPARATION_MONTHS, state.elapsedMonths + preparation) + B.FINAL_MONTHS
		var unsupported := 0
		for r in sites:
			if not Derived.supported(r):
				unsupported += 1
		var message: String
		if earliest > B.DEADLINE:
			message = "Preparation is too late: these sites need %d months, then the final rite needs 12. Accelerate preparation or the window closes %d months too soon." % [preparation, earliest - B.DEADLINE]
		else:
			message = ("The stars are right — begin the final rite. " if state.elapsedMonths >= B.PREPARATION_MONTHS and ready >= 3 else "Funded sites need up to %d more preparation months. " % preparation) \
				+ "The final rite needs 12 months and 100 Dread." + (" Restore follower support at %d site(s)." % unsupported if unsupported else "")
		return { "urgent": earliest > B.DEADLINE or unsupported > 0 or state.elapsedMonths >= B.PREPARATION_MONTHS, "message": message }
	var months_left: int = B.LATEST_SITE_START - state.elapsedMonths
	var timing: String
	if months_left >= 0:
		timing = ("Only " if months_left <= 24 else "") + "%d months left to start new sites without acceleration. " % months_left
	else:
		timing = "New sites now need acceleration to finish in time. "
	return { "urgent": months_left <= 24, "message": "Fund %d more ritual site(s). " % (B.REQUIRED_SITES - sites.size()) + timing + "Allow 24 months of preparation plus 12 for the final rite." }
