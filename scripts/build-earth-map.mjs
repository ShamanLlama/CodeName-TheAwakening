// Rebuild with: node scripts/build-earth-map.mjs
// Source and public-domain terms: src/assets/NATURAL_EARTH.md
import { readFileSync, writeFileSync } from "node:fs";
import { URL } from "node:url";
import { log } from "node:console";
const root = new URL("../", import.meta.url);
const source = JSON.parse(readFileSync(new URL("src/assets/natural-earth-countries.geojson", root), "utf8"));
const regions = Object.fromEntries(["north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania"].map(id => [id, []]));
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
      const d = pathFor(polygons.map(p => p.map(ring => clipLongitude(ring, western))));
      if (d) regions[western ? "europe" : "east_asia"].push({ name: western ? "Western Russia" : "Eastern Russia", d });
    }
  } else regions[regionFor(f.properties)].push({ name: f.properties.ADMIN, d: pathFor(polygons) });
}
for (const [id, paths] of Object.entries(regions)) if (!paths.length || paths.some(p => !p.d || /NaN|Infinity/.test(p.d))) throw new Error("Invalid geometry for " + id);
writeFileSync(new URL("src/assets/earth-regions.json", root), JSON.stringify(regions) + "\n");
log("Generated Earth map: " + Object.values(regions).flat().length + " country shapes in 8 playable regions.");
