using System.Collections.Generic;
using Awakening.Core.Json;
using Awakening.Core.Rules;
using UnityEngine;

namespace Awakening.UI.Map
{
    /// <summary>
    /// World geometry: Natural Earth country rings by region (built by scripts/build-earth-map.mjs)
    /// and the Natural Earth projection. Port of godot/map/geo.gd.
    /// </summary>
    public static class Geo
    {
        /// <summary>World units per projected radian. The projected world is about 1000 units wide.</summary>
        public const float Scale = 182.8f;

        public sealed class Country
        {
            public string Name;
            public List<Vector2[]> Rings = new List<Vector2[]>();
        }

        /// <summary>Where each region's name, count and ritual site sit on the map.</summary>
        public static readonly Dictionary<string, Vector2> Anchors = new Dictionary<string, Vector2>
        {
            ["north_america"] = new Vector2(-101, 44), ["latin_america"] = new Vector2(-60, -10), ["europe"] = new Vector2(16, 53),
            ["africa"] = new Vector2(20, 6), ["middle_east"] = new Vector2(46, 28), ["south_asia"] = new Vector2(78, 21),
            ["east_asia"] = new Vector2(106, 40), ["oceania"] = new Vector2(134, -25),
        };

        /// <summary>Land locations for threat markers. Mirrors src/components/threat-markers.ts.</summary>
        public static readonly Dictionary<string, Vector2[]> ThreatLocations = new Dictionary<string, Vector2[]>
        {
            ["north_america"] = new[] { new Vector2(-123, 53), new Vector2(-102, 39), new Vector2(-101, 24), new Vector2(-80, 43), new Vector2(-150, 64), new Vector2(-98, 60) },
            ["latin_america"] = new[] { new Vector2(-74, -10), new Vector2(-52, -10), new Vector2(-65, -32), new Vector2(-66, 7), new Vector2(-86, 14), new Vector2(-69, -22) },
            ["europe"] = new[] { new Vector2(2, 47), new Vector2(14, 51), new Vector2(26, 62), new Vector2(40, 56), new Vector2(-4, 40), new Vector2(20, 45) },
            ["africa"] = new[] { new Vector2(-10, 12), new Vector2(18, 5), new Vector2(30, -6), new Vector2(23, -23), new Vector2(25, 26), new Vector2(10, -1) },
            ["middle_east"] = new[] { new Vector2(44, 33), new Vector2(54, 32), new Vector2(45, 23), new Vector2(34, 39), new Vector2(44, 16), new Vector2(46, 25) },
            ["south_asia"] = new[] { new Vector2(76, 23), new Vector2(69, 30), new Vector2(89, 24), new Vector2(81, 8), new Vector2(67, 34), new Vector2(82, 27) },
            ["east_asia"] = new[] { new Vector2(105, 36), new Vector2(99, 47), new Vector2(104, 17), new Vector2(136, 36), new Vector2(117, -2), new Vector2(95, 60) },
            ["oceania"] = new[] { new Vector2(135, -25), new Vector2(146, -22), new Vector2(122, -22), new Vector2(145, -6), new Vector2(174, -39), new Vector2(130, -18) },
        };

        static Dictionary<string, List<Country>> _regions;
        static Rect? _bounds;

        /// <summary>Natural Earth projection (Šavrič, Patterson &amp; Jenny, 2011), as in d3-geo. North is up.</summary>
        public static Vector2 Project(Vector2 lonLat) => Project(lonLat.x, lonLat.y);

        public static Vector2 Project(double lon, double lat)
        {
            double lam = lon * System.Math.PI / 180.0;
            double phi = lat * System.Math.PI / 180.0;
            double phi2 = phi * phi;
            double phi4 = phi2 * phi2;
            double x = lam * (0.8707 - 0.131979 * phi2 + phi4 * (-0.013791 + phi4 * (0.003971 * phi2 - 0.001529 * phi4)));
            double y = phi * (1.007226 + phi2 * (0.015085 + phi4 * (-0.044475 + 0.028874 * phi2 - 0.005916 * phi4)));
            return new Vector2((float)x, (float)-y) * Scale;
        }

        /// <summary>{ region id: countries with rings in world units }, parsed once per session.</summary>
        public static Dictionary<string, List<Country>> Regions()
        {
            if (_regions != null) return _regions;
            var asset = Resources.Load<TextAsset>("Data/earth_regions");
            var raw = (Dictionary<string, object>)JsonReader.Parse(asset.text);
            _regions = new Dictionary<string, List<Country>>();
            foreach (string id in GameData.RegionIds)
            {
                var countries = new List<Country>();
                foreach (Dictionary<string, object> country in (List<object>)raw[id])
                {
                    var entry = new Country { Name = (string)country["name"] };
                    foreach (List<object> values in (List<object>)country["rings"])
                    {
                        var ring = new Vector2[values.Count / 2];
                        for (int i = 0; i < ring.Length; i++) ring[i] = Project((double)values[i * 2], (double)values[i * 2 + 1]);
                        entry.Rings.Add(ring);
                    }
                    countries.Add(entry);
                }
                _regions[id] = countries;
            }
            return _regions;
        }

        public static Rect Bounds()
        {
            if (_bounds.HasValue) return _bounds.Value;
            Vector2 start = Project(-170, 84);
            Vector2 min = start, max = start;
            foreach (var countries in Regions().Values)
                foreach (var country in countries)
                    foreach (var ring in country.Rings)
                        foreach (var point in ring)
                        {
                            min = Vector2.Min(min, point);
                            max = Vector2.Max(max, point);
                        }
            _bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return _bounds.Value;
        }

        public static Vector2 Anchor(string regionId) => Project(Anchors[regionId]);

        /// <summary>Mirrors threatMapPosition: visual variation that does not consume game RNG.</summary>
        public static Vector2 ThreatPosition(string seed, string regionId, Threat threat)
        {
            Vector2[] points = ThreatLocations[regionId];
            ulong index = ((ulong)Rng.SeedToState(seed + regionId + threat.Kind) + (ulong)Mathf.FloorToInt(threat.DueMonth / 12f)) % (ulong)points.Length;
            return Project(points[index]);
        }

        public static string RegionAt(Vector2 worldPoint)
        {
            foreach (string id in GameData.RegionIds)
                foreach (var country in Regions()[id])
                    foreach (var ring in country.Rings)
                        if (PointInPolygon(worldPoint, ring)) return id;
            return "";
        }

        public static bool PointInPolygon(Vector2 point, Vector2[] ring)
        {
            bool inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                Vector2 a = ring[i], b = ring[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }

        public static float RingArea(Vector2[] ring)
        {
            float area = 0;
            for (int i = 0; i < ring.Length; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                area += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(area) * 0.5f;
        }

        /// <summary>Deterministic points scattered over a region's land, for the follower dots.</summary>
        public static List<Vector2> Scatter(string regionId, int count)
        {
            var rings = new List<Vector2[]>();
            var areas = new List<float>();
            float total = 0;
            foreach (var country in Regions()[regionId])
                foreach (var ring in country.Rings)
                {
                    float area = RingArea(ring);
                    if (area > 0.5f)
                    {
                        rings.Add(ring);
                        areas.Add(area);
                        total += area;
                    }
                }
            var points = new List<Vector2>();
            uint state = Rng.SeedToState("dots:" + regionId);
            int guard = 0;
            while (points.Count < count && guard < count * 200)
            {
                guard++;
                RngDraw roll = Rng.Next(state);
                state = roll.State;
                double pick = roll.Value * total;
                int index = 0;
                while (index < areas.Count - 1 && pick > areas[index])
                {
                    pick -= areas[index];
                    index++;
                }
                Vector2[] ring = rings[index];
                Vector2 min = ring[0], max = ring[0];
                foreach (Vector2 point in ring)
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                RngDraw rx = Rng.Next(state);
                RngDraw ry = Rng.Next(rx.State);
                state = ry.State;
                var candidate = new Vector2(min.x + (float)rx.Value * (max.x - min.x), min.y + (float)ry.Value * (max.y - min.y));
                if (PointInPolygon(candidate, ring)) points.Add(candidate);
            }
            return points;
        }
    }
}
