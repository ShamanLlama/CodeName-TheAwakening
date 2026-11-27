using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Accent inks for stamps.</summary>
    public enum Accent
    {
        Cult,
        Danger,
        Omen,
    }

    /// <summary>
    /// Small builders for code-constructed interface elements. Port of godot/ui/kit.gd. Sizes are
    /// in layout units of the 1600×900 canvas, the same numbers as the Godot build.
    /// </summary>
    public static class Kit
    {
        /// <summary>Touch devices pick an action first and confirm it, because a tap is also a hover.</summary>
        public static bool Touch;
        /// <summary>True when the interface size leaves a short or narrow canvas (a larger interface, a phone).</summary>
        public static bool Compact;
        /// <summary>The player asked for less motion: no pulsing markers, flickering dots or sliding sheets.</summary>
        public static bool ReducedMotion;

        public static Label Label(string text, int size = 16, Color? color = null, string weight = Fonts.Body)
        {
            if (size < 12) size = 12;
            // The compact layout runs on few, small pixels; small reading type grows by two units.
            if (Compact && size < 17) size += 2;
            var node = new Label(text) { pickingMode = PickingMode.Ignore };
            Fonts.Apply(node, (weight == Fonts.Display || weight == Fonts.DisplaySc || weight == Fonts.DisplayItalic)
                && text != null && System.Linq.Enumerable.Any(text, char.IsDigit) ? Fonts.BodyBold : weight);
            node.style.fontSize = size;
            node.style.color = color ?? Palette.Text;
            node.style.whiteSpace = WhiteSpace.NoWrap;
            return node;
        }

        /// <summary>Typewritten capitals used for field names and readout captions, as on a filed form.</summary>
        public static Label Caption(string text, Color? color = null) =>
            Label(text.ToUpperInvariant(), 12, color ?? Palette.Muted, Fonts.TypeBold);

        /// <summary>Wrapping text.</summary>
        public static Label Body(string text, int size = 15, Color? color = null, string weight = Fonts.Body)
        {
            Label node = Label(text, size, color, weight);
            node.style.whiteSpace = WhiteSpace.Normal;
            return node;
        }

        public static Button Button(string text, Action onPress = null)
        {
            var node = new Button(onPress) { text = text, focusable = false };
            node.AddToClassList("aw-button");
            return node;
        }

        /// <summary>A printed label in solid ink: the main call to action.</summary>
        public static Button Primary(string text, Action onPress = null)
        {
            var node = new Button(onPress) { text = text, focusable = false };
            node.AddToClassList("aw-primary");
            return node;
        }

        /// <summary>A quiet text link in typed capitals, for secondary destinations such as playtest status.</summary>
        public static Button Link(string text, Action onPress)
        {
            var node = new Button(onPress) { text = text, focusable = false };
            node.AddToClassList("aw-link");
            return node;
        }

        /// <summary>A rubber stamp: a ruled box in one ink, typed capitals.</summary>
        public static Button Stamp(string text, Action onPress, Accent accent)
        {
            var node = new Button(onPress) { text = text, focusable = false };
            node.AddToClassList("aw-stamp");
            node.AddToClassList("aw-stamp--" + accent.ToString().ToLowerInvariant());
            return node;
        }

        public static Stack HBox(int gap = 8) => new Stack(true, gap);

        public static Stack VBox(int gap = 8) => new Stack(false, gap);

        /// <summary>A sheet of aged paper. Its grain does not reach the text on it.</summary>
        public static Stack Panel(Color? fill = null, Color? edge = null, Vector4? pad = null, float foxing = 0.1f, int gap = 0)
        {
            var node = new Stack(false, gap);
            Box(node, fill ?? Palette.Paper, edge ?? Palette.PaperEdge, 1, pad ?? new Vector4(16, 14, 16, 14));
            AddGrain(node, foxing);
            return node;
        }

        /// <summary>Godot's StyleBoxFlat: a fill, an edge and padding (left, top, right, bottom).</summary>
        public static void Box(VisualElement node, Color fill, Color edge, int width, Vector4 pad)
        {
            node.style.backgroundColor = fill;
            node.style.borderTopColor = node.style.borderBottomColor = node.style.borderLeftColor = node.style.borderRightColor = edge;
            node.style.borderTopWidth = node.style.borderBottomWidth = node.style.borderLeftWidth = node.style.borderRightWidth = width;
            node.style.paddingLeft = pad.x;
            node.style.paddingTop = pad.y;
            node.style.paddingRight = pad.z;
            node.style.paddingBottom = pad.w;
        }

        /// <summary>Lays the paper grain under an element's children.</summary>
        public static void AddGrain(VisualElement node, float foxing = 0.1f)
        {
            var grain = new VisualElement { name = "grain", pickingMode = PickingMode.Ignore };
            Fill(grain);
            grain.style.backgroundImage = PaperGrain.Overlay(foxing);
            grain.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            grain.style.backgroundSize = new BackgroundSize(PaperGrain.Size, PaperGrain.Size);
            grain.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left);
            grain.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);
            node.hierarchy.Insert(0, grain);
        }

        /// <summary>Absolute, covering the parent.</summary>
        public static void Fill(VisualElement node)
        {
            node.style.position = Position.Absolute;
            node.style.left = node.style.top = node.style.right = node.style.bottom = 0;
        }

        public static VisualElement Spacer(bool horizontal = true)
        {
            var node = new VisualElement { pickingMode = PickingMode.Ignore };
            node.style.flexGrow = 1;
            node.style.flexShrink = 1;
            return node;
        }

        /// <summary>A printer's rule: thick over thin, or a single hairline.</summary>
        public static Rule Rule(Color? color = null, bool twin = true) => new Rule(color ?? Palette.Alpha(Palette.Text, 0.45f), twin);

        /// <summary>
        /// Ruled inner border with corner diamonds, as on a printed frontispiece. It draws inside
        /// the panel's outer edge, over its padding, under its content.
        /// </summary>
        public static void Frame(VisualElement panel, Color? color = null, float inset = 7f)
        {
            var frame = new Frame(color ?? Palette.Alpha(Palette.Text, 0.55f), inset);
            int at = panel.hierarchy.childCount > 0 && panel.hierarchy[0].name == "grain" ? 1 : 0;
            panel.hierarchy.Insert(at, frame);
        }

        public static void Clear(VisualElement node) => node.Clear();

        public static void SetVisible(VisualElement node, bool visible) =>
            node.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static bool IsVisible(VisualElement node) => node.resolvedStyle.display != DisplayStyle.None;
    }

    /// <summary>A row or column whose children are spaced by a fixed gap, like Godot's box containers.</summary>
    public class Stack : VisualElement
    {
        public readonly bool Horizontal;
        public int Gap;

        public Stack(bool horizontal, int gap)
        {
            Horizontal = horizontal;
            Gap = gap;
            style.flexDirection = horizontal ? FlexDirection.Row : FlexDirection.Column;
            style.flexShrink = 0;
            if (horizontal) style.alignItems = Align.Center;
        }

        /// <summary>Adds a child, spaced from the previous one by the gap.</summary>
        public new void Add(VisualElement child)
        {
            if (contentContainer.childCount > 0)
            {
                if (Horizontal) child.style.marginLeft = Gap;
                else child.style.marginTop = Gap;
            }
            base.Add(child);
        }
    }

    /// <summary>A printer's rule: thick over thin, or a single hairline.</summary>
    public sealed class Rule : VisualElement
    {
        public Color Color;
        public bool Twin;

        public Rule(Color color, bool twin)
        {
            Color = color;
            Twin = twin;
            pickingMode = PickingMode.Ignore;
            style.height = 5;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            Rect r = contentRect;
            float y = r.height * 0.5f;
            if (Twin)
            {
                Draw2D.Rect(context.painter2D, new Rect(0, y - 2, r.width, 1.5f), Color);
                Draw2D.Rect(context.painter2D, new Rect(0, y + 1.5f, r.width, 0.75f), Color);
            }
            else
            {
                Draw2D.Rect(context.painter2D, new Rect(0, y, r.width, 1), Color);
            }
        }
    }

    /// <summary>Ruled inner border with corner diamonds.</summary>
    public sealed class Frame : VisualElement
    {
        readonly Color _color;
        readonly float _inset;

        public Frame(Color color, float inset)
        {
            _color = color;
            _inset = inset;
            name = "frame";
            pickingMode = PickingMode.Ignore;
            Kit.Fill(this);
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            // Absolute children cover the padding box: one edge pixel inside the panel's outer edge.
            Rect outer = new Rect(-1, -1, layout.width + 2, layout.height + 2);
            Rect a = Draw2D.Grow(outer, -_inset);
            Rect b = Draw2D.Grow(outer, -_inset - 3);
            Painter2D painter = context.painter2D;
            Draw2D.StrokeRect(painter, a, _color, 1.5f);
            Draw2D.StrokeRect(painter, b, Palette.Alpha(_color, _color.a * 0.6f), 0.75f);
            foreach (Vector2 corner in new[] { a.min, new Vector2(a.xMax, a.yMin), a.max, new Vector2(a.xMin, a.yMax) })
                Draw2D.Polygon(painter, new[] { corner + new Vector2(0, -5), corner + new Vector2(5, 0), corner + new Vector2(0, 5), corner + new Vector2(-5, 0) }, _color);
        }
    }
}

namespace Awakening.UI
{
    /// <summary>A thin horizontal gauge: Godot's ProgressBar with a flat fill.</summary>
    public sealed class Bar : VisualElement
    {
        readonly VisualElement _fill;
        public float Max;
        float _value;

        public Bar(float max, Color fill, float height = 6f)
        {
            Max = max;
            pickingMode = PickingMode.Ignore;
            style.height = height;
            style.flexShrink = 0;
            style.backgroundColor = Palette.Alpha(Palette.Text, 0.12f);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.style.height = Length.Percent(100);
            _fill.style.backgroundColor = fill;
            _fill.style.width = Length.Percent(0);
            Add(_fill);
        }

        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                _fill.style.width = Length.Percent(Max <= 0 ? 0 : Mathf.Clamp01(value / Max) * 100f);
            }
        }

        public Color FillColor
        {
            set => _fill.style.backgroundColor = value;
        }
    }

    /// <summary>A row that wraps onto new lines, like Godot's HFlowContainer.</summary>
    public sealed class Flow : VisualElement
    {
        readonly int _h, _v;

        public Flow(int horizontal, int vertical)
        {
            _h = horizontal;
            _v = vertical;
            style.flexDirection = FlexDirection.Row;
            style.flexWrap = Wrap.Wrap;
            style.alignItems = Align.FlexStart;
            style.marginRight = -horizontal;
            style.marginBottom = -vertical;
        }

        public new void Add(VisualElement child)
        {
            child.style.marginRight = _h;
            child.style.marginBottom = _v;
            base.Add(child);
        }
    }
}
