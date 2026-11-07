extends RefCounted
## Short player-facing wording derived from the rules: first-time notes, the headline of the
## ritual timetable, threat consequences, one-line reasons an action is unavailable, and the
## account of how a run ended. Nothing here changes the rules.

const B = preload("res://core/balance.gd")

## id: [title, one sentence]. Each appears once per device, the first time it applies.
const HINTS := {
	"first_cell": ["Your cell is growing", "It recruits by itself and earns Influence. Open a neighbouring region and choose Establish a cell to spread. Every action also works while paused."],
	"threat": ["A threat is coming", "The clipping on the left counts down to a strike. Obstruct it with Influence, or accept the losses."],
	"omen": ["An omen appeared", "The violet eye marks a short-lived opportunity. Open that region to use it before it fades."],
	"doctrine": ["You can afford a doctrine", "One permanent bonus for this run, from the Doctrine button in the ledger."],
	"sacrifice": ["Ritual sites take two years", "Sacrifice followers in the Rites tab for Dread, then Prepare a site in a region you hold firmly."],
	"awareness": ["The world is noticing", "World awareness slows you and ends the run at 100%. Conceal the evidence lowers it."],
	"unsupported": ["A site stopped counting", "A struck seal stalls: its region needs more followers, or its upkeep went unpaid. Open the region to see which."],
}


## The first unseen note whose situation is on the board, or "".
static func next_hint(s: Dictionary, seen: Callable) -> String:
	if s.status in ["placing", "won", "lost"]:
		return ""
	var regions: Array = GameData.REGION_IDS.map(func(id: String) -> Dictionary: return s.regions[id])
	var checks := {
		"first_cell": true,
		"threat": regions.any(func(r: Dictionary) -> bool: return r.threat != null),
		"omen": regions.any(func(r: Dictionary) -> bool: return r.opportunity != null),
		"unsupported": regions.any(func(r: Dictionary) -> bool: return r.site.invested and site_state(r) in ["dormant", "unsupported"]),
		"awareness": s.awareness >= 30,
		"sacrifice": s.elapsedMonths >= 24 and not regions.any(func(r: Dictionary) -> bool: return r.site.invested),
		"doctrine": s.specialization == null and s.influence >= GameData.SPECIALIZATION_COST and s.elapsedMonths >= 6,
	}
	for id in ["first_cell", "threat", "unsupported", "awareness", "sacrifice", "omen", "doctrine"]:
		if checks[id] and not seen.call(id):
			return id
	return ""


## One reading of a funded site for every place that shows it (card, map, seals, checklists):
## "dormant" (upkeep unpaid), "unsupported", "ready" or "preparing". Matches Derived.ready_sites.
static func site_state(r: Dictionary) -> String:
	if r.site.dormant:
		return "dormant"
	if not Derived.supported(r):
		return "unsupported"
	return "ready" if r.site.progress >= B.SITE_MONTHS else "preparing"


## Names the timetable's most pressing problem in a few words.
static func forecast_headline(s: Dictionary) -> String:
	if s.status == "awakening":
		return "Final rite · %d of %d months" % [s.finalProgress, B.FINAL_MONTHS]
	var sites: Array = GameData.REGION_IDS.map(func(id: String) -> Dictionary: return s.regions[id]).filter(func(r: Dictionary) -> bool: return r.site.invested)
	var unsupported := sites.filter(func(r: Dictionary) -> bool: return site_state(r) in ["dormant", "unsupported"]).size()
	if Derived.net_influence_per_month(s) < 0:
		return "Upkeep exceeds income"
	if sites.size() >= B.REQUIRED_SITES:
		var remaining: Array = sites.map(func(r: Dictionary) -> int: return B.SITE_MONTHS - r.site.progress)
		remaining.sort()
		var earliest: int = maxi(B.PREPARATION_MONTHS, s.elapsedMonths + remaining[B.REQUIRED_SITES - 1]) + B.FINAL_MONTHS
		if earliest > B.DEADLINE:
			return "Sites will finish %d months too late" % (earliest - B.DEADLINE)
		if unsupported:
			return "%d site%s stopped counting" % [unsupported, "" if unsupported == 1 else "s"]
		return "The stars are right" if s.elapsedMonths >= B.PREPARATION_MONTHS else "Ritual timetable"
	if unsupported:
		return "%d site%s stopped counting" % [unsupported, "" if unsupported == 1 else "s"]
	var missing: int = B.REQUIRED_SITES - sites.size()
	if B.LATEST_SITE_START - s.elapsedMonths < 0:
		return "Too late for new sites without help"
	return "Fund %d more site%s soon" % [missing, "" if missing == 1 else "s"]


## What a threat does if it lands, one plain phrase per line.
static func threat_effects(r: Dictionary) -> Array[String]:
	var c := Threats.consequences(r)
	var lines: Array[String] = ["Kills %d followers" % ceili(c.followers)]
	if c.destroysCell:
		lines[0] = "Destroys the cell" + (" and its site" if r.site.invested else "")
		return lines
	if c.preparationLost:
		lines.append("Site loses %d months" % c.preparationLost)
	if c.interruptsSite:
		lines.append("Site stops counting for the rite")
	return lines


## Why an action is unavailable: { short, long }. The engine names the first failed check,
## which is often the price; a missing cell, threat or omen is the real obstacle, so it wins.
static func blocker(s: Dictionary, id: String, region_id: String, preview: Dictionary) -> Dictionary:
	if preview.get("ok", false):
		return { "short": "", "long": "" }
	var r: Dictionary = s.regions[region_id]
	var p: Dictionary = r.population
	var found := ["", ""]
	if id != "expand" and p.followers <= 0:
		found = ["NO CELL HERE", "Establish a cult here first."]
	else:
		match id:
			"expand":
				if p.followers > 0:
					found = ["ALREADY HELD", "This region already has a cell."]
				elif Derived.total_followers(s) > 0 and not GameData.REGIONS[region_id].adjacent.any(func(o: String) -> bool: return s.regions[o].population.followers > 0):
					found = ["NOT NEXT TO A CELL", "Requires a cult in an adjacent region."]
			"counter":
				if r.threat == null:
					found = ["NO THREAT", "No approaching threat in this region."]
			"exploit":
				if r.opportunity == null or not r.opportunity.kind in ["nightmare", "eclipse"]:
					found = ["NEEDS AN OMEN", "Requires an active nightmare or eclipse."]
				elif r.opportunity.kind == "eclipse" and (not r.site.invested or r.site.progress >= B.SITE_MONTHS):
					found = ["NEEDS A SITE", "An eclipse needs an unfinished ritual site."]
			"convert":
				if r.figure != null or r.opportunity == null or not r.opportunity.kind in ["celebrity", "politician"]:
					found = ["NEEDS A FIGURE", "Requires an unconverted prominent-person opportunity."]
			"reveal_figure":
				if r.figure == null or r.figure.revealed:
					found = ["NO SECRET FIGURE", "Requires a secretly converted figure."]
				elif not r.site.invested:
					found = ["NEEDS A SITE", "Prepare a local ritual site to receive their awakening contribution."]
			"prepare":
				if r.site.invested:
					found = ["FUNDED", "This site's preparation is already funded."]
			"manifest":
				if p.deniers + p.unaware < 1:
					found = ["ALL BELIEVE", "Everyone here already believes."]
			"recruit":
				if Derived.recruitable(r) < 1:
					found = ["NO RECRUITS LEFT", "No recruits available. Use a manifestation to change disbelief."]
	if found[0] == "":
		found = [short_reason(preview), preview.get("reason", "")]
	return { "short": found[0], "long": found[1] }


## A few typed words for why an action cannot be taken, shown where its cost would be.
static func short_reason(preview: Dictionary) -> String:
	var reason: String = preview.get("reason", "")
	if reason.begins_with("Available in"):
		return "IN " + reason.trim_prefix("Available in ").trim_suffix(" months.").trim_suffix(" month.") + " MO"
	if reason.begins_with("Need ") and reason.ends_with(" Influence."):
		return "NEED " + reason.trim_prefix("Need ").trim_suffix(" Influence.") + " INF"
	if reason.begins_with("Need ") and reason.ends_with(" Dread."):
		return "NEED " + reason.trim_prefix("Need ").trim_suffix(" Dread.") + " DREAD"
	if reason.begins_with("Need ") and reason.ends_with(" local followers."):
		return "NEED " + reason.trim_prefix("Need ").trim_suffix(" local followers.") + " HERE"
	if reason.begins_with("Maintain at least"):
		return "NEED " + reason.trim_prefix("Maintain at least ").trim_suffix(" local followers.") + " HERE"
	for entry in [
		["Establish a cult here", "NO CELL HERE"], ["No recruits available", "NO RECRUITS LEFT"],
		["already has a cell", "ALREADY HELD"], ["adjacent region", "NOT NEXT TO A CELL"],
		["Nothing to conceal", "NOTHING TO HIDE"], ["already believes", "ALL BELIEVE"],
		["already funded", "FUNDED"], ["Too late", "TOO LATE"], ["No approaching threat", "NO THREAT"],
		["nightmare or eclipse", "NEEDS AN OMEN"], ["eclipse needs", "NEEDS A SITE"],
		["prominent-person", "NEEDS A FIGURE"], ["secretly converted", "NO SECRET FIGURE"],
		["receive their awakening", "NEEDS A SITE"], ["already public", "ALREADY PUBLIC"],
		["already underground", "ALREADY HIDDEN"],
	]:
		if reason.contains(entry[0]):
			return entry[1]
	return "UNAVAILABLE"


## How a finished run ended: { cause, checks: [[ok, name, detail]], advice: [String] }.
## The cause is the most basic failure; advice covers up to two of the problems found.
static func ending(s: Dictionary) -> Dictionary:
	var result := { "cause": "", "checks": [], "advice": [] }
	if s.status == "won":
		result.cause = "Three sites held through the final rite."
		return result
	var regions: Array = GameData.REGION_IDS.map(func(id: String) -> Dictionary: return s.regions[id])
	var sites: Array = regions.filter(func(r: Dictionary) -> bool: return r.site.invested)
	if s.awareness >= 100:
		result.cause = "World awareness reached 100%. The world found the cult before the stars were right."
		result.advice.append("Conceal the evidence before awareness passes half; it costs more as awareness rises.")
		result.advice.append("Public worship, sites and a very large cult all raise awareness. Grow only as large as the rite needs.")
		return result
	for id in GameData.REGION_IDS:
		var r: Dictionary = s.regions[id]
		if r.site.invested:
			var ready: bool = r.site.progress >= B.SITE_MONTHS and Derived.supported(r) and not r.site.dormant
			result.checks.append([ready, GameData.REGIONS[id].name, Derived.site_label(r)])
	for i in maxi(0, B.REQUIRED_SITES - sites.size()):
		result.checks.append([false, "Unfunded site", "Never funded"])
	var started: bool = s.finalProgress > 0
	result.checks.append([started, "Final rite", "%d of %d months" % [s.finalProgress, B.FINAL_MONTHS]])
	var issues: Array = []
	if started:
		issues.append(["The rite began but lost a supported site before its 12 months were done.",
			"During the rite the world hunts your sites. Keep Influence to obstruct every threat against them."])
	if sites.size() < B.REQUIRED_SITES:
		var next_cost: int = B.SITE_DREAD_COSTS[mini(sites.size(), B.SITE_DREAD_COSTS.size() - 1)]
		issues.append(["Only %d of 3 ritual sites were funded." % sites.size(),
			"Fund sites earlier. Each needs 24 months, then the rite needs 12; the last safe start is %s." % Format.date(B.LATEST_SITE_START)
			+ (" Bank Dread by sacrifice: the next site costs %d." % next_cost if s.dread < next_cost else "")])
	if sites.any(func(r: Dictionary) -> bool: return not Derived.supported(r)):
		issues.append(["%d of 3 sites were ready; others lost the followers they need." % Derived.ready_sites(s),
			"Build sites in small regions, where a quarter of the people is easier to hold, and obstruct threats there."])
	if sites.any(func(r: Dictionary) -> bool: return Derived.supported(r) and r.site.progress < B.SITE_MONTHS):
		issues.append(["The sites were not finished in time.",
			"Start preparation by %s, or speed it with eclipses and revealed figures." % Format.date(B.LATEST_SITE_START)])
	if not started and s.dread < B.FINAL_DREAD:
		issues.append(["The rite needed 100 Dread.", "Bank 100 Dread before September 1924; sacrifice and nightmares give Dread."])
	if issues.is_empty():
		issues.append(["The sites were ready, but the rite was not begun in time.", "Begin the final rite as soon as the stars are right; it needs 12 months."])
	result.cause = issues[0][0]
	for issue in issues.slice(0, 2):
		result.advice.append(issue[1])
	return result
