using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// Godot's CanvasItem drawing calls (draw_rect, draw_line, draw_arc, draw_colored_polygon…)
    /// on top of Painter2D, so the ported _draw() code reads the same. Angles are radians,
    /// increasing clockwise on screen (y points down), as in Godot.
    /// </summary>
    public static class Draw2D
    {
        public static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2, r.height + by * 2);

        public static Vector2 FromAngle(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        public static void Rect(Painter2D p, Rect r, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(r.min);
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(r.max);
            p.LineTo(new Vector2(r.xMin, r.yMax));
            p.ClosePath();
            p.Fill();
        }

        public static void StrokeRect(Painter2D p, Rect r, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineJoin = LineJoin.Miter;
            p.BeginPath();
            p.MoveTo(r.min);
            p.LineTo(new Vector2(r.xMax, r.yMin));
            p.LineTo(r.max);
            p.LineTo(new Vector2(r.xMin, r.yMax));
            p.ClosePath();
            p.Stroke();
        }

        public static void Line(Painter2D p, Vector2 a, Vector2 b, Color color, float width = 1f)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineCap = LineCap.Butt;
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.Stroke();
        }

        public static void Polyline(Painter2D p, IReadOnlyList<Vector2> points, Color color, float width = 1f, bool closed = false)
        {
            if (points.Count < 2) return;
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++) p.LineTo(points[i]);
            if (closed) p.ClosePath();
            p.Stroke();
        }

        public static void Polygon(Painter2D p, IReadOnlyList<Vector2> points, Color color)
        {
            if (points.Count < 3) return;
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++) p.LineTo(points[i]);
            p.ClosePath();
            p.Fill(FillRule.NonZero);
        }

        /// <summary>An unfilled arc from <paramref name="start"/> to <paramref name="end"/> radians.</summary>
        public static void Arc(Painter2D p, Vector2 center, float radius, float start, float end, Color color, float width = 1f)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineCap = LineCap.Butt;
            p.BeginPath();
            p.Arc(center, radius, Angle.Radians(start), Angle.Radians(end), end >= start ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);
            p.Stroke();
        }

        public static void Circle(Painter2D p, Vector2 center, float radius, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.Arc(center, radius, Angle.Radians(0), Angle.Radians(Mathf.PI * 2));
            p.ClosePath();
            p.Fill();
        }

        public static void Ring(Painter2D p, Vector2 center, float radius, Color color, float width = 1f) =>
            Arc(p, center, radius, 0, Mathf.PI * 2, color, width);

        /// <summary>Keeps the part of a convex or simple polygon below (y greater than) a horizontal line.</summary>
        public static List<Vector2> ClipBelow(IReadOnlyList<Vector2> polygon, float level)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                bool ain = a.y >= level, bin = b.y >= level;
                if (ain) result.Add(a);
                if (ain != bin)
                {
                    float t = (level - a.y) / (b.y - a.y);
                    result.Add(Vector2.Lerp(a, b, t));
                }
            }
            return result;
        }
    }
}
