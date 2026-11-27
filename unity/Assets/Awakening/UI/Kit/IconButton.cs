using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// A square button whose glyph is drawn in code: pause, play, fast, menu or close. Brass on
    /// the dark desk by default; ink when it sits on paper. Port of godot/ui/icon_button.gd.
    /// </summary>
    public sealed class IconButton : Button
    {
        public readonly string Glyph;
        public readonly bool OnPaper;
        bool _active;
        bool _hovered;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                MarkDirtyRepaint();
            }
        }

        public IconButton(string glyph, string hint, Action onPress, bool paper = false) : base(onPress)
        {
            Glyph = glyph;
            OnPaper = paper;
            tooltip = hint;
            focusable = false;
            text = "";
            Vector2 size = Kit.Touch && Kit.Compact ? new Vector2(52, 50) : Kit.Touch ? new Vector2(48, 44) : new Vector2(40, 36);
            style.width = size.x;
            style.height = size.y;
            style.flexShrink = 0;
            style.backgroundColor = Color.clear;
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 0;
            style.marginLeft = style.marginRight = style.marginTop = style.marginBottom = 0;
            style.paddingLeft = style.paddingRight = style.paddingTop = style.paddingBottom = 0;
            AddToClassList("aw-icon");
            RegisterCallback<PointerEnterEvent>(_ => { _hovered = true; MarkDirtyRepaint(); });
            RegisterCallback<PointerLeaveEvent>(_ => { _hovered = false; MarkDirtyRepaint(); });
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            Painter2D p = context.painter2D;
            Rect r = contentRect;
            Color color;
            if (OnPaper) color = !enabledSelf ? Palette.Faint : _hovered ? Palette.Text : Palette.Muted;
            else color = _active ? Palette.Brass : !enabledSelf ? Palette.Alpha(Palette.LampDim, 0.74f) : _hovered ? Palette.Lamp : Palette.LampDim;
            if (_active)
            {
                Draw2D.Rect(p, new Rect(0, 0, r.width, r.height), Palette.Alpha(Palette.Brass, 0.1f));
                Draw2D.Rect(p, new Rect(4, r.height - 3, r.width - 8, 1.5f), Palette.Brass);
                Draw2D.Rect(p, new Rect(4, r.height - 0.75f, r.width - 8, 0.75f), Palette.Brass);
            }
            Vector2 c = r.size * 0.5f;
            switch (Glyph)
            {
                case "pause":
                    Draw2D.Rect(p, new Rect(c + new Vector2(-6, -7), new Vector2(4, 14)), color);
                    Draw2D.Rect(p, new Rect(c + new Vector2(2, -7), new Vector2(4, 14)), color);
                    break;
                case "play":
                    Draw2D.Polygon(p, new[] { c + new Vector2(-5, -8), c + new Vector2(7, 0), c + new Vector2(-5, 8) }, color);
                    break;
                case "fast":
                    Draw2D.Polygon(p, new[] { c + new Vector2(-10, -7), c, c + new Vector2(-10, 7) }, color);
                    Draw2D.Polygon(p, new[] { c + new Vector2(0, -7), c + new Vector2(10, 0), c + new Vector2(0, 7) }, color);
                    break;
                case "menu":
                    foreach (int y in new[] { -6, 0, 6 }) Draw2D.Rect(p, new Rect(c + new Vector2(-9, y - 1), new Vector2(18, 2)), color);
                    break;
                case "close":
                    Draw2D.Line(p, c + new Vector2(-6, -6), c + new Vector2(6, 6), color, 2f);
                    Draw2D.Line(p, c + new Vector2(6, -6), c + new Vector2(-6, 6), color, 2f);
                    break;
            }
        }
    }
}
