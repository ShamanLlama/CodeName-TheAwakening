using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI.Map
{
    /// <summary>
    /// Phase-1 spike of the survey chart: every country filled by region and each ring inked,
    /// drawn with Painter2D the way godot/map/world_map.gd draws in _draw().
    /// </summary>
    public sealed class WorldMapElement : VisualElement
    {
        static readonly Color Sea = new Color(0.106f, 0.137f, 0.157f);
        static readonly Color Ink = new Color(0.17f, 0.12f, 0.07f, 0.85f);
        static readonly Color[] Land =
        {
            new Color(0.80f, 0.72f, 0.56f), new Color(0.74f, 0.66f, 0.50f), new Color(0.83f, 0.76f, 0.60f),
            new Color(0.77f, 0.69f, 0.52f), new Color(0.81f, 0.70f, 0.53f), new Color(0.75f, 0.68f, 0.54f),
            new Color(0.79f, 0.73f, 0.58f), new Color(0.72f, 0.64f, 0.49f),
        };

        readonly Rect _world;

        public System.Action Painted;

        public WorldMapElement()
        {
            _world = Geo.Bounds();
            style.flexGrow = 1;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            Rect area = contentRect;
            if (area.width < 1 || area.height < 1) return;
            float zoom = Mathf.Min(area.width / _world.width, area.height / _world.height) * 0.96f;
            Vector2 offset = area.center - _world.center * zoom;

            Painter2D painter = context.painter2D;
            painter.fillColor = Sea;
            painter.BeginPath();
            painter.MoveTo(area.min);
            painter.LineTo(new Vector2(area.xMax, area.yMin));
            painter.LineTo(area.max);
            painter.LineTo(new Vector2(area.xMin, area.yMax));
            painter.ClosePath();
            painter.Fill();

            var regions = Geo.Regions();
            for (int r = 0; r < Geo.RegionIds.Length; r++)
            {
                painter.fillColor = Land[r];
                painter.strokeColor = Ink;
                painter.lineWidth = 0.8f;
                foreach (var country in regions[Geo.RegionIds[r]])
                    foreach (var ring in country.Rings)
                    {
                        if (ring.Length < 3) continue;
                        painter.BeginPath();
                        painter.MoveTo(ring[0] * zoom + offset);
                        for (int i = 1; i < ring.Length; i++) painter.LineTo(ring[i] * zoom + offset);
                        painter.ClosePath();
                        painter.Fill(FillRule.NonZero);
                        painter.Stroke();
                    }
            }
            Painted?.Invoke();
        }
    }
}
