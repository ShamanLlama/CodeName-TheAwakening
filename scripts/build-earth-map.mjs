// Rebuild with: node scripts/build-earth-map.mjs
// Source and public-domain terms: src/assets/NATURAL_EARTH.md
import { readFileSync, writeFileSync } from "node:fs";
import { URL } from "node:url";
import { log } from "node:console";
const root = new URL("../", import.meta.url);
const source = JSON.parse(readFileSync(new URL("src/assets/natural-earth-countries.geojson", root), "utf8"));
const REGION_IDS = ["north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"];
const regions = Object.fromEntries(REGION_IDS.map(id => [id, []]));
// The Godot build projects raw longitude/latitude itself, so it gets unprojected outer rings.
// Only South Africa has a hole (Lesotho), and Lesotho is drawn as its own country.
const godotRegions = Object.fromEntries(REGION_IDS.map(id => [id, []]));
const round = value => Number(value.toFixed(2));
const ringsFor = polygons => polygons.map(polygon => polygon[0]).filter(ring => ring && ring.length >= 3).map(ring => ring.flatMap(([lon, lat]) => [round(lon), round(lat)]));
// Equirectangular projection, cropped below 60°S: Antarctica is not a playable region.
const point = ([lon, lat]) => [Number(((lon + 180) * 2).toFixed(2)), Number(((85 - lat) * 2).toFixed(2))];
function clipLongitude(ring, western) {
  const output = [], inside = p => western ? p[0] <= 60 : p[0] >= 60;
  for (let i = 0; i < ring.length; i++) {
    const a = ring[i], b = ring[(i + 1) % ring.length];
    if (inside(a)) output.push(a);
    if (inside(a) !== inside(b)) output.push([60, a[1] + (b[1] - a[1]) * (60 - a[0]) / (b[0] - a[0])]);
  }
  return output;
}
function pathFor(polygons) {
  return polygons.flatMap(polygon => polygon.filter(ring => ring.length >= 3).map(ring => ring.map((p, i) => (i ? "L" : "M") + point(p).join(",")).join("") + "Z")).join("");
}
function regionFor(p) {
  if (["USA", "CAN", "MEX", "GRL", "BMU"].includes(p.ADM0_A3)) return "north_america";
  if (p.CONTINENT === "North America" || p.CONTINENT === "South America") return "latin_america";
  if (p.CONTINENT === "Africa") return "africa";
  if (p.CONTINENT === "Europe") return "europe";
  if (p.SUBREGION === "Western Asia" || p.ADM0_A3 === "IRN") return "middle_east";
  if (p.SUBREGION === "Southern Asia") return "south_asia";
  if (p.CONTINENT === "Asia") return "east_asia";
  return "oceania";
}
for (const f of source.features) {
  if (f.properties.CONTINENT === "Antarctica") continue;
  const polygons = f.geometry.type === "Polygon" ? [f.geometry.coordinates] : f.geometry.coordinates;
  if (f.properties.ADM0_A3 === "RUS") {
    // Split this transcontinental country at 60°E for the game's broad regional controls.
    for (const western of [true, false]) {
      const clipped = polygons.map(p => p.map(ring => clipLongitude(ring, western)));
      const d = pathFor(clipped);
      if (d) {
        const name = western ? "Western Russia" : "Eastern Russia", id = western ? "europe" : "east_asia";
        regions[id].push({ name, d });
        godotRegions[id].push({ name, rings: ringsFor(clipped) });
      }
    }
  } else {
    regions[regionFor(f.properties)].push({ name: f.properties.ADMIN, d: pathFor(polygons) });
    godotRegions[regionFor(f.properties)].push({ name: f.properties.ADMIN, rings: ringsFor(polygons) });
  }
}
for (const [id, paths] of Object.entries(regions)) if (!paths.length || paths.some(p => !p.d || /NaN|Infinity/.test(p.d))) throw new Error("Invalid geometry for " + id);
writeFileSync(new URL("src/assets/earth-regions.json", root), JSON.stringify(regions) + "\n");
writeFileSync(new URL("godot/data/earth_regions.json", root), JSON.stringify(godotRegions) + "\n");
log("Generated Earth map: " + Object.values(regions).flat().length + " country shapes in 8 playable regions.");
