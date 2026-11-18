using System.Collections.Generic;
using Awakening.Core.Rules;
using UnityEngine;

namespace Awakening.UI.Map
{
    /// <summary>
    /// The foxed parchment of the land (godot/map/world_map.gd gives its Polygon2D land the paper
    /// shader at foxing 0.3). Painter2D cannot fill with a texture, so the grain is baked once per
    /// session into a world-space overlay that is transparent over the sea, and the map stretches
    /// it over the land it draws in flat colour.
    /// </summary>
    public static class LandGrain
    {
        const int Width = 1400;
        static Texture2D _texture;

        public static Texture2D Texture(Rect world)
        {
            if (_texture != null) return _texture;
            int height = Mathf.CeilToInt(Width * world.height / world.width);
            var mask = new bool[Width * height];
            float sx = Width / world.width, sy = height / world.height;
            var crossings = new List<float>();
            foreach (string id in GameData.RegionIds)
                foreach (var country in Geo.Regions()[id])
                    foreach (Vector2[] ring in country.Rings)
                        Fill(ring, world, sx, sy, height, mask, crossings);
            Color32[] grain = PaperGrain.Pixels(0.3f);
            var pixels = new Color32[Width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < Width; x++)
                {
                    // Texture rows run bottom-up; the mask rows run top-down like the screen.
                    if (!mask[y * Width + x]) continue;
                    pixels[(height - 1 - y) * Width + x] = grain[(y % PaperGrain.Size) * PaperGrain.Size + x % PaperGrain.Size];
                }
            _texture = new Texture2D(Width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Land grain" };
            _texture.SetPixels32(pixels);
            _texture.Apply(false, true);
            return _texture;
        }

        /// <summary>Scanline fill with the even-odd rule, so each ring marks its own interior.</summary>
        static void Fill(Vector2[] ring, Rect world, float sx, float sy, int height, bool[] mask, List<float> crossings)
        {
            if (ring.Length < 3) return;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector2 p in ring)
            {
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            int rowFrom = Mathf.Max(0, Mathf.FloorToInt((minY - world.y) * sy));
            int rowTo = Mathf.Min(height - 1, Mathf.CeilToInt((maxY - world.y) * sy));
            for (int row = rowFrom; row <= rowTo; row++)
            {
                float y = world.y + (row + 0.5f) / sy;
                crossings.Clear();
                for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                {
                    Vector2 a = ring[i], b = ring[j];
                    if ((a.y > y) != (b.y > y)) crossings.Add(a.x + (y - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                crossings.Sort();
                for (int k = 0; k + 1 < crossings.Count; k += 2)
                {
                    int from = Mathf.Max(0, Mathf.CeilToInt((crossings[k] - world.x) * sx - 0.5f));
                    int to = Mathf.Min(Width - 1, Mathf.FloorToInt((crossings[k + 1] - world.x) * sx - 0.5f));
                    for (int x = from; x <= to; x++) mask[row * Width + x] = true;
                }
            }
        }
    }
}
