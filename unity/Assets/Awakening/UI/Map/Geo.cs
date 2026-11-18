using System.Collections.Generic;
using Awakening.Core.Json;
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

        public static readonly string[] RegionIds =
        {
            "north_america", "latin_america", "europe", "africa", "middle_east", "south_asia", "east_asia", "oceania",
        };

        public sealed class Country
        {
            public string Name;
            public List<Vector2[]> Rings = new List<Vector2[]>();
        }

        static Dictionary<string, List<Country>> _regions;

        /// <summary>Natural Earth projection (Šavrič, Patterson &amp; Jenny, 2011), as in d3-geo. North is up.</summary>
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
            foreach (string id in RegionIds)
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
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var countries in Regions().Values)
                foreach (var country in countries)
                    foreach (var ring in country.Rings)
                        foreach (var point in ring)
                        {
                            min = Vector2.Min(min, point);
                            max = Vector2.Max(max, point);
                        }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
