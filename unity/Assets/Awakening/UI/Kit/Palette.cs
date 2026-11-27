using UnityEngine;

namespace Awakening.UI
{
    /// <summary>
    /// Colours for the whole game: a cult's archive on a lamp-lit desk, 1908–1926. Port of
    /// godot/ui/palette.gd. Documents are paper printed in ink; the desk, the sea and the bars
    /// around them are dark. One accent per meaning: verdigris for the cult, oxblood for danger,
    /// bruised violet for omens and Dread. Each accent has an ink for paper and a brighter tone
    /// for the dark.
    /// </summary>
    public static class Palette
    {
        // The desk, the sea, and text that sits on them.
        public static readonly Color Desk = Hex("0d0b08");
        public static readonly Color DeskRaised = Hex("1c1711");
        public static readonly Color DeskEdge = Hex("3b3022");
        public static readonly Color Lamp = Hex("ebdfc2");
        public static readonly Color LampDim = Hex("b8a78c");
        public static readonly Color Brass = Hex("c9a55d");
        public static readonly Color Ocean = Hex("0f1b1d");
        public static readonly Color OceanLine = Hex("1e3234");
        public static readonly Color Land = Hex("ab9873");
        public static readonly Color LandEdge = Hex("5e4c33");

        // Paper and the ink printed on it.
        public static readonly Color Paper = Hex("d7c7a1");
        public static readonly Color PaperLight = Hex("e4d7b5");
        public static readonly Color PaperShade = Hex("c6b38c");
        public static readonly Color PaperEdge = Hex("7d6a4b");
        public static readonly Color Text = Hex("19120a");
        public static readonly Color Muted = Hex("423322");
        /// <summary>Decorative ink and disabled labels: 6.5:1 on paper, 5.3:1 on shaded paper.</summary>
        public static readonly Color Faint = Hex("4a3b26");

        public static readonly Color Cult = Hex("24421e");
        public static readonly Color CultBright = Hex("9cbc63");
        public static readonly Color CultWash = new Color(36f / 255f, 66f / 255f, 30f / 255f, 0.14f);
        public static readonly Color Danger = Hex("7a140d");
        public static readonly Color DangerBright = Hex("f06859");
        public static readonly Color Warn = Hex("5c3704");
        public static readonly Color WarnBright = Hex("dcab4c");
        public static readonly Color Omen = Hex("432864");
        public static readonly Color OmenBright = Hex("b69ce0");
        public static readonly Color OmenDeep = Hex("231832");

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
            return color;
        }

        public static Color Alpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        /// <summary>Godot's Color.darkened: towards black by <paramref name="amount"/>.</summary>
        public static Color Darkened(Color color, float amount) =>
            new Color(color.r * (1f - amount), color.g * (1f - amount), color.b * (1f - amount), color.a);

        /// <summary>Godot's Color.lightened: towards white by <paramref name="amount"/>.</summary>
        public static Color Lightened(Color color, float amount) =>
            new Color(color.r + (1f - color.r) * amount, color.g + (1f - color.g) * amount, color.b + (1f - color.b) * amount, color.a);
    }
}
