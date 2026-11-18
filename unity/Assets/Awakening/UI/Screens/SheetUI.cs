using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Shared document controls and legacy-input focus handling for the modal sheets.</summary>
    internal static class SheetUI
    {
        // Godot's FontVariation includes its fallback ascent/descent in every line. The same
        // TTFs in TextCore use their own tighter metrics; retain the reference document spacing.
        static int LineHeight(int size) => Mathf.CeilToInt(size * 1.069f) + Mathf.CeilToInt(size * .293f);

        public static Label Label(string text, int size = 16, Color? color = null, string weight = Fonts.Body)
        {
            int actual = Kit.Compact && size < 17 ? size + 2 : size;
            var label = Kit.Label(text, size, color, weight);
            label.enableRichText = false;
            label.style.height = LineHeight(actual);
            label.style.flexShrink = 0;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        public static Label Caption(string text, Color? color = null) => Label(text.ToUpperInvariant(), 12, color ?? Palette.Muted, Fonts.TypeBold);

        public static Label Body(string text, int size = 15, Color? color = null, string weight = Fonts.Body)
        {
            int actual = Kit.Compact && size < 17 ? size + 2 : size;
            var label = Kit.Body("", size, color, weight);
            label.userData = actual;
            SetBodyText(label, text);
            label.enableRichText = true;
            label.style.minHeight = LineHeight(actual);
            label.RegisterCallback<AttachToPanelEvent>(_ => label.schedule.Execute(() =>
            {
                // Keep the reference's first-line height as well as its inter-line leading.
                float natural = label.MeasureTextSize(BodyPrefix(actual) + "Ag", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
                label.style.paddingBottom = Mathf.Max(0, LineHeight(actual) - natural);
            }));
            return label;
        }

        static string BodyPrefix(int size) => $"<size={size}><line-height={LineHeight(size) + 3}>";

        public static void SetBodyText(Label label, string text)
        {
            // Preserve literal player text while the outer tag sets the reference line leading.
            string literal = text.Replace("<", "<noparse><</noparse>");
            label.text = BodyPrefix((int)label.userData) + literal;
        }

        public static Button Button(string text, Action action, bool primary = false, float height = 46)
        {
            var button = primary ? Kit.Primary(text, action) : Kit.Button(text, action);
            button.userData = action;
            button.focusable = true;
            button.style.minHeight = height;
            return button;
        }

        public static Button Link(string text, Action action)
        {
            var button = Kit.Link(text, action);
            button.userData = action;
            button.focusable = true;
            return button;
        }

        public static ScrollView Scroll()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.AddToClassList("aw-scroll");
            scroll.style.flexShrink = 1;
            scroll.style.minHeight = 0;
            scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            return scroll;
        }

        public static TextField Field(string placeholder, int maxLength, int fontSize = 16)
        {
            var field = new TextField { maxLength = maxLength };
            field.textEdition.placeholder = placeholder;
            field.AddToClassList("aw-field");
            field.style.minHeight = 42;
            field.Q(className: "unity-base-text-field__input").style.fontSize = fontSize;
            field.style.marginLeft = field.style.marginRight = field.style.marginTop = field.style.marginBottom = 0;
            return field;
        }

        public static VisualElement SizePicker(GameController game, int height, bool tooltips = true)
        {
            var row = Kit.HBox(0);
            var available = game.AvailableUiScales();
            foreach (float scale in GameController.UiScales)
            {
                var option = Button($"{Mathf.RoundToInt(scale * 100)}%", () => game.SetUiScale(scale), height: height);
                option.EnableInClassList("aw-on", Math.Abs(game.Settings.UiScale - scale) < .001);
                option.SetEnabled(available.Contains(scale));
                if (tooltips && !available.Contains(scale)) option.tooltip = "Too large for this window.";
                Equal(option);
                row.Add(option);
            }
            return row;
        }

        public static void Equal(VisualElement element)
        {
            element.style.flexGrow = 1;
            element.style.flexBasis = 0;
            element.style.minWidth = 0;
        }

        public static IEnumerable<VisualElement> Controls(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                if ((child is Button || child is TextField || child is Slider || child is Toggle) && child.focusable && child.enabledInHierarchy)
                    yield return child;
                else
                    foreach (var control in Controls(child)) yield return control;
            }
        }

        // The controller owns keyboard input. Prevent Toolkit's navigation from activating twice.
        public static void LegacyNavigation(VisualElement root)
        {
            root.RegisterCallback<NavigationSubmitEvent>(e => { e.StopImmediatePropagation(); }, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(e => { e.StopImmediatePropagation(); }, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(e => { e.StopImmediatePropagation(); }, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(e =>
            {
                var target = e.target as VisualElement;
                bool field = target is TextField || target?.GetFirstAncestorOfType<TextField>() != null;
                if (e.keyCode == KeyCode.Tab || (!field && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)))
                    e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
        }

        public static void FocusFirst(VisualElement root)
        {
            var controls = Controls(root).ToList();
            (controls.FirstOrDefault(c => c.ClassListContains("aw-primary")) ?? controls.FirstOrDefault())?.Focus();
        }

        public static bool HandleKey(VisualElement root, KeyCode key)
        {
            var controls = Controls(root).ToList();
            if (controls.Count == 0) return true;
            var focused = root.panel?.focusController?.focusedElement as VisualElement;
            var current = controls.FirstOrDefault(c => c == focused || (focused != null && c.Contains(focused)));
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space)
            {
                if (current is Button button && button.userData is Action press) press();
                else if (current is Toggle toggle) toggle.value = !toggle.value;
                return true;
            }
            if ((key == KeyCode.LeftArrow || key == KeyCode.RightArrow) && current is Slider slider)
            {
                slider.value += key == KeyCode.LeftArrow ? -1 : 1;
                return true;
            }
            if (key == KeyCode.Tab)
            {
                int delta = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
                int at = controls.IndexOf(current);
                controls[(at + delta + controls.Count) % controls.Count].Focus();
                return true;
            }
            Vector2 direction = key == KeyCode.LeftArrow ? Vector2.left : key == KeyCode.RightArrow ? Vector2.right : key == KeyCode.UpArrow ? Vector2.up : key == KeyCode.DownArrow ? Vector2.down : Vector2.zero;
            if (direction == Vector2.zero) return true;
            if (current == null) { FocusFirst(root); return true; }
            // UI coordinates grow downwards.
            direction.y = -direction.y;
            var next = controls.Where(c => c != current).Select(c => (control: c, offset: c.worldBound.center - current.worldBound.center))
                .Where(c => Vector2.Dot(c.offset, direction) > 1)
                .OrderBy(c => Vector2.Dot(c.offset, direction) + Math.Abs(c.offset.x * direction.y - c.offset.y * direction.x) * 4)
                .FirstOrDefault();
            next.control?.Focus();
            return true;
        }
    }
}
