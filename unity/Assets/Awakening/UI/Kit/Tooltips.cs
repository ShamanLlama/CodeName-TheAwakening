using UnityEngine;
using UnityEngine.UIElements;
using MeasureMode = UnityEngine.UIElements.VisualElement.MeasureMode;
using PointerType = UnityEngine.UIElements.PointerType;

namespace Awakening.UI
{
    /// <summary>
    /// Runtime tooltips. UI Toolkit shows an element's <c>tooltip</c> only in the Editor, so this
    /// watches the pointer over a root and shows the nearest ancestor's tooltip on paper after a
    /// short pause, as Godot does. Touch screens never hover, so they get none.
    /// </summary>
    public sealed class Tooltips
    {
        const long DelayMs = 500;
        readonly VisualElement _root;
        readonly Label _tip;
        VisualElement _owner;
        Vector2 _pointer;
        IVisualElementScheduledItem _pending;

        public Tooltips(VisualElement root)
        {
            _root = root;
            _tip = new Label { pickingMode = PickingMode.Ignore, name = "tooltip" };
            _tip.AddToClassList("aw-tooltip");
            Fonts.Apply(_tip, Fonts.Body);
            _tip.style.display = DisplayStyle.None;
            root.Add(_tip);
            root.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(_ => Hide(), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(_ => Hide());
        }

        void OnMove(PointerMoveEvent e)
        {
            if (e.pointerType == PointerType.touch) return;
            _pointer = e.position;
            VisualElement owner = Find(e.target as VisualElement);
            if (owner == _owner)
            {
                if (_tip.style.display == DisplayStyle.Flex) Position();
                return;
            }
            Hide();
            _owner = owner;
            if (owner == null) return;
            _pending = _root.schedule.Execute(Show).StartingIn(DelayMs);
        }

        static VisualElement Find(VisualElement element)
        {
            for (VisualElement at = element; at != null; at = at.parent)
                if (!string.IsNullOrEmpty(at.tooltip)) return at;
            return null;
        }

        void Show()
        {
            if (_owner == null || _owner.panel == null || string.IsNullOrEmpty(_owner.tooltip)) return;
            _tip.text = _owner.tooltip;
            _tip.style.display = DisplayStyle.Flex;
            _tip.BringToFront();
            Position();
        }

        void Position()
        {
            Vector2 local = _root.WorldToLocal(_pointer);
            Vector2 size = _tip.MeasureTextSize(_tip.text, 360 - 24, MeasureMode.AtMost, 0, MeasureMode.Undefined) + new Vector2(26, 20);
            Rect area = _root.contentRect;
            float x = Mathf.Min(local.x + 14, area.width - size.x - 4);
            float y = local.y + 22;
            if (y + size.y > area.height - 4) y = local.y - size.y - 8;
            _tip.style.left = Mathf.Max(4, x);
            _tip.style.top = Mathf.Max(4, y);
        }

        public void Hide()
        {
            _pending?.Pause();
            _pending = null;
            _owner = null;
            _tip.style.display = DisplayStyle.None;
        }
    }
}
