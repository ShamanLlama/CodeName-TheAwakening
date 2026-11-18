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
        public static readonly Color LampDim = Hex("a39278");
        public static readonly Color Brass = Hex("bf9a55");
        public static readonly Color Ocean = Hex("0f1b1d");
        public static readonly Color OceanLine = Hex("1e3234");
        public static readonly Color Land = Hex("ab9873");
        public static readonly Color LandEdge = Hex("5e4c33");

        // Paper and the ink printed on it.
        public static readonly Color Paper = Hex("d7c7a1");
        public static readonly Color PaperLight = Hex("e4d7b5");
        public static readonly Color PaperShade = Hex("c6b38c");
        public static readonly Color PaperEdge = Hex("7d6a4b");
        public static readonly Color Text = Hex("21180e");
        public static readonly Color Muted = Hex("5a4833");
        /// <summary>Decorative ink and disabled labels: 4.7:1 on paper, 3.8:1 on shaded paper.</summary>
        public static readonly Color Faint = Hex("624f33");

        public static readonly Color Cult = Hex("34552c");
        public static readonly Color CultBright = Hex("9cbc63");
        public static readonly Color CultWash = new Color(0.2f, 0.33f, 0.17f, 0.14f);
        public static readonly Color Danger = Hex("8e1d14");
        public static readonly Color DangerBright = Hex("d9483b");
        public static readonly Color Warn = Hex("6e4608");
        public static readonly Color WarnBright = Hex("dcab4c");
        public static readonly Color Omen = Hex("553878");
        public static readonly Color OmenBright = Hex("ab8fd6");
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
