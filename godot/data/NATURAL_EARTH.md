# Earth map provenance

Source: Natural Earth, Admin 0 countries, 1:110 million scale, release v5.1.2.

- Dataset: https://github.com/nvkelso/natural-earth-vector/blob/v5.1.2/geojson/ne_110m_admin_0_countries.geojson
- Download: https://raw.githubusercontent.com/nvkelso/natural-earth-vector/v5.1.2/geojson/ne_110m_admin_0_countries.geojson
- Terms: https://www.naturalearthdata.com/about/terms-of-use/
- Retrieved: 2026-09-06.

Natural Earth places its vector and raster map data in the public domain, permitting personal, educational, and commercial use and modification. Attribution is optional; this game credits Natural Earth beneath the map.

The unmodified GeoJSON is retained in natural-earth-countries.geojson. Run `node scripts/build-earth-map.mjs` from the project root to regenerate earth-regions.json. Only generated SVG path data ships in the browser bundle; no map network service or API key is required.

Adaptations: an equirectangular projection; coordinates rounded to two decimal places; Antarctica omitted; country shapes grouped into the eight gameplay regions. Russia is divided at 60°E between Europe and East Asia. Iran belongs to West Asia; Central and Southeast Asia share the broad East Asia gameplay region. North America groups the United States, Canada, Mexico, Greenland, and Bermuda; Central America, the Caribbean, and South America use Latin America. These groupings are gameplay abstractions. West Asia retains the internal region key `middle_east` for save compatibility.

Natural Earth's borders are its source cartography; the game does not simulate individual country control. Small islands absent at this map scale remain accessible through their broader gameplay region.
