class_name Threats
## Threat consequences. Mirrors src/game/threats.ts. The preview and the resolution use the
## same calculation; forecasts use current population.

const B = preload("res://core/balance.gd")


static func consequences(region: Dictionary) -> Dictionary:
	var threat = region.threat
	var has := threat != null
	var followers := 0.0
	if has:
		var raid: bool = threat.kind == "raid"
		var share: float = threat.severity * (B.RAID_LOSS_SHARE if raid else B.THREAT_LOSS_SHARE)
		# A share of the cell, floored by a flat loss so small cells can still be wiped out.
		followers = minf(region.population.followers, maxf(
			threat.severity * (B.RAID_LOSS_FLOOR if raid else B.THREAT_LOSS_FLOOR),
			region.population.followers * share))
	var remaining: float = region.population.followers - followers
	var destroys_cell: bool = has and region.population.followers > 0 and remaining <= 0
	var preparation_lost := 0
	if has and threat.kind == "occult" and region.site.invested:
		preparation_lost = mini(region.site.progress, 6)
	return {
		"followers": followers, "remaining": remaining, "destroysCell": destroys_cell, "preparationLost": preparation_lost,
		"incomeLost": Derived.region_income(region) if destroys_cell else followers * B.INFLUENCE_PER_FOLLOWER,
		"exposureAdded": minf(5.0, 100.0 - region.exposure) if has else 0.0,
		"interruptsSite": has and region.site.invested and (remaining < Derived.site_requirement(region)
			or region.site.progress >= B.SITE_MONTHS and region.site.progress - preparation_lost < B.SITE_MONTHS),
	}


static func describe(region: Dictionary) -> String:
	var c := consequences(region)
	if region.threat == null:
		return "No approaching threat."
	var effects: Array[String] = ["Lose " + Js.num(ceil(c.followers)) + " followers", "income −" + Js.fixed(c.incomeLost, 2) + " Influence/month"]
	if c.preparationLost:
		effects.append("ritual −%d preparation months" % c.preparationLost)
	if c.destroysCell:
		effects.append("cell and ritual site destroyed" if region.site.invested else "cell destroyed")
	elif c.interruptsSite:
		effects.append("site cannot support the final rite")
	if c.exposureAdded:
		effects.append("Exposure +" + Js.num(c.exposureAdded))
	return " · ".join(effects) + "."
