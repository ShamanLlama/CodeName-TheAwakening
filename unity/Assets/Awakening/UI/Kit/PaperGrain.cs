using System.Collections.Generic;
using UnityEngine;

namespace Awakening.UI
{
    /// <summary>
    /// Aged paper without a shipped texture: the grain, fibres and foxing of godot/ui/paper.gdshader,
    /// baked once into a tileable overlay. Godot multiplies the paper colour; UI Toolkit cannot, so
    /// the overlay lightens and darkens with white and black at low alpha and browns the stains.
    /// The lamp falloff of godot/ui/lamp.gdshader is baked the same way.
    /// </summary>
    public static class PaperGrain
    {
        public const int Size = 512;
        const float Grain = 0.08f;
        static readonly Dictionary<float, Texture2D> Cache = new Dictionary<float, Texture2D>();
        static readonly Dictionary<float, Color32[]> PixelCache = new Dictionary<float, Color32[]>();
        static Texture2D _lamp;

        public static Texture2D Overlay(float foxing)
        {
            if (Cache.TryGetValue(foxing, out Texture2D cached)) return cached;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, name = "Paper grain" };
            texture.SetPixels32(Pixels(foxing));
            texture.Apply(false, true);
            Cache[foxing] = texture;
            return texture;
        }

        /// <summary>The overlay's pixels, rows bottom-up as Texture2D stores them.</summary>
        public static Color32[] Pixels(float foxing)
        {
            if (PixelCache.TryGetValue(foxing, out Color32[] cached)) return cached;
            var pixels = new Color32[Size * Size];
            var brown = new Color(0.30f, 0.19f, 0.07f);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float speck = Hash(x, y, Size, Size) - 0.5f;
                    float fibre = Noise(x * 20f / Size, y * 256f / Size, 20, 256) - 0.5f;
                    float cloud = Noise(x * 4f / Size, y * 4f / Size, 4, 4) * 0.55f
                        + Noise(x * 15f / Size, y * 15f / Size, 15, 15) * 0.3f
                        + Noise(x * 56f / Size, y * 56f / Size, 56, 56) * 0.15f;
                    float stain = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, cloud));
                    float shift = speck * Grain + fibre * Grain * 0.7f;
                    // Grain: white lifts, black sinks, by the shader's multiplier offset.
                    Color layer = shift >= 0 ? new Color(1, 1, 1, shift * 1.2f) : new Color(0, 0, 0, -shift * 1.6f);
                    // Foxing browns the paper rather than greying it.
                    float fox = Mathf.Clamp01(stain * foxing * 2.5f) * 0.42f;
                    float alpha = fox + layer.a * (1 - fox);
                    Color rgb = alpha <= 0 ? Color.clear : (brown * fox + layer * layer.a * (1 - fox)) / alpha;
                    pixels[y * Size + x] = new Color(rgb.r, rgb.g, rgb.b, alpha);
                }
            }
            PixelCache[foxing] = pixels;
            return pixels;
        }

        /// <summary>Lamplight over the desk: warm in the middle, falling into darkness at the edges.</summary>
        public static Texture2D Lamp()
        {
            if (_lamp != null) return _lamp;
            const int w = 320, h = 180;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Texture rows run bottom-up; the shader's UV runs top-down.
                    float u = (x + 0.5f) / w, v = 1f - (y + 0.5f) / h;
                    float dx = (u - 0.46f) * 1.2f, dy = v - 0.46f;
                    float fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.9f, Mathf.Sqrt(dx * dx + dy * dy)));
                    pixels[y * w + x] = new Color(0.03f, 0.02f, 0.01f, fall * 0.62f);
                }
            }
            _lamp = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Lamp" };
            _lamp.SetPixels32(pixels);
            _lamp.Apply(false, true);
            return _lamp;
        }

        /// <summary>Hash without sine (Dave Hoskins), on lattice points wrapped to a period so the tile repeats.</summary>
        static float Hash(int x, int y, int periodX, int periodY)
        {
            x = ((x % periodX) + periodX) % periodX;
            y = ((y % periodY) + periodY) % periodY;
            float p3x = Fract(x * 0.1031f), p3y = Fract(y * 0.1031f), p3z = Fract(x * 0.1031f);
            float d = p3x * (p3y + 33.33f) + p3y * (p3z + 33.33f) + p3z * (p3x + 33.33f);
            p3x += d; p3y += d; p3z += d;
            return Fract((p3x + p3y) * p3z);
        }

        static float Noise(float px, float py, int periodX, int periodY)
        {
            int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
            float fx = px - ix, fy = py - iy;
            fx = fx * fx * (3 - 2 * fx);
            fy = fy * fy * (3 - 2 * fy);
            float a = Hash(ix, iy, periodX, periodY), b = Hash(ix + 1, iy, periodX, periodY);
            float c = Hash(ix, iy + 1, periodX, periodY), d = Hash(ix + 1, iy + 1, periodX, periodY);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fract(float value) => value - Mathf.Floor(value);
    }
}
