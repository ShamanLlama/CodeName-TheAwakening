using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// Type roles. body: Old Standard, a 1900s textbook face. display: IM Fell, printed
    /// antiquarian titles. type: Courier Prime, the typewriter in the case files.
    /// </summary>
    public static class Fonts
    {
        public const string Body = "body";
        public const string BodyBold = "body_bold";
        public const string Italic = "italic";
        public const string Display = "display";
        public const string DisplayItalic = "display_italic";
        public const string DisplaySc = "display_sc";
        public const string Type = "type";
        public const string TypeBold = "type_bold";

        static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            [Body] = "OldStandard-Regular", [BodyBold] = "OldStandard-Bold", [Italic] = "OldStandard-Italic",
            [Display] = "IMFellEnglish-Regular", [DisplayItalic] = "IMFellEnglish-Italic", [DisplaySc] = "IMFellEnglishSC-Regular",
            [Type] = "CourierPrime-Regular", [TypeBold] = "CourierPrime-Bold",
        };

        static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>();

        public static Font Get(string role)
        {
            if (!Cache.TryGetValue(role, out Font font))
            {
                font = Resources.Load<Font>("Fonts/" + Files[role]);
                Cache[role] = font;
            }
            return font;
        }

        /// <summary>Sets the role's face on an element; small capitals get a point of tracking, as in Godot.</summary>
        public static void Apply(VisualElement element, string role)
        {
            element.style.unityFontDefinition = FontDefinition.FromFont(Get(role));
            element.style.letterSpacing = role == DisplaySc ? 1 : 0;
        }
    }
}
