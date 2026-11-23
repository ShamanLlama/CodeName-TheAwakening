using System.Collections.Generic;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>A brass dial with a needle. Past 60% it reads in the red; at 100 the run is lost. Port of godot/ui/gauges.gd.</summary>
    public sealed class AwarenessGauge : VisualElement
    {
        double _value;

        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                MarkDirtyRepaint();
            }
        }

        public AwarenessGauge()
        {
            pickingMode = PickingMode.Ignore;
            style.width = 96;
            style.height = 52;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        static Vector2 Point(Vector2 center, float radius, float percent) =>
            center + Draw2D.FromAngle(Mathf.PI + Mathf.PI * percent / 100f) * radius;

        void Draw(MeshGenerationContext context)
        {
            Painter2D p = context.painter2D;
            Vector2 size = contentRect.size;
            var center = new Vector2(size.x * 0.5f, size.y - 6);
            float radius = Mathf.Min(size.x * 0.5f - 4, size.y - 10);
            Draw2D.Arc(p, center, radius, Mathf.PI, Mathf.PI * 2, Palette.Alpha(Palette.Text, 0.7f), 1.5f);
            Draw2D.Arc(p, center, radius - 4, Mathf.PI + Mathf.PI * 0.6f, Mathf.PI * 2, Palette.Alpha(Palette.Danger, 0.85f), 5f);
            for (int i = 0; i <= 10; i++)
            {
                Vector2 outer = Point(center, radius, i * 10f);
                Vector2 inner = Point(center, radius - (i % 5 == 0 ? 7f : 4f), i * 10f);
                Draw2D.Line(p, inner, outer, Palette.Alpha(Palette.Text, 0.75f), i % 5 == 0 ? 1.2f : 0.8f);
            }
            Vector2 tip = Point(center, radius - 3, Mathf.Clamp((float)_value, 0f, 100f));
            Draw2D.Line(p, center, tip, Palette.Text, 2f);
            Draw2D.Circle(p, center, 4.5f, Palette.Brass);
            Draw2D.Ring(p, center, 4.5f, Palette.Text);
            Draw2D.Line(p, new Vector2(center.x - radius - 2, center.y + 0.5f), new Vector2(center.x + radius + 2, center.y + 0.5f), Palette.Alpha(Palette.Text, 0.5f));
        }
    }

    /// <summary>
    /// One wax seal per required site: an outline when unfunded, filling as preparation advances,
    /// verdigris when ready, oxblood when its region no longer supports it.
    /// </summary>
    public sealed class SiteSeals : VisualElement
    {
        GameState _state;

        public GameState State
        {
            get => _state;
            set
            {
                _state = value;
                MarkDirtyRepaint();
            }
        }

        public SiteSeals()
        {
            pickingMode = PickingMode.Ignore;
            style.width = 120;
            style.height = 30;
            style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        static Vector2[] Seal(Vector2 c, float radius)
        {
            var points = new Vector2[36];
            for (int k = 0; k < 36; k++)
                points[k] = c + Draw2D.FromAngle(k * Mathf.PI * 2 / 36f) * (radius + (k % 2 == 0 ? 1.2f : -0.6f));
            return points;
        }

        static void Star(Painter2D p, Vector2 c, float radius, Color color)
        {
            var star = new Vector2[6];
            for (int k = 0; k < 6; k++) star[k] = c + Draw2D.FromAngle(-Mathf.PI / 2 + (k * 2 % 5) * Mathf.PI * 2 / 5f) * radius;
            Draw2D.Polyline(p, star, color, 1.2f);
        }

        void Draw(MeshGenerationContext context)
        {
            Painter2D p = context.painter2D;
            var sites = new List<RegionState>();
            if (_state != null)
                foreach (string id in GameData.RegionIds)
                    if (_state.Regions[id].Site.Invested) sites.Add(_state.Regions[id]);
            // Ready seals first, then by preparation; the order matches the "n / 3" caption.
            var rank = new Dictionary<string, int> { ["ready"] = 3, ["preparing"] = 2, ["unsupported"] = 1, ["dormant"] = 0 };
            sites.Sort((a, b) =>
            {
                int ra = rank[Notes.SiteState(_state, a)], rb = rank[Notes.SiteState(_state, b)];
                return ra != rb ? rb.CompareTo(ra) : b.Site.Progress.CompareTo(a.Site.Progress);
            });
            int siteMonths = _state != null ? GameEngine.SiteMonths(_state) : Balance.SiteMonths;
            for (int i = 0; i < Balance.RequiredSites; i++)
            {
                var c = new Vector2(16 + i * 38, contentRect.height * 0.5f);
                Vector2[] seal = Seal(c, 12f);
                if (i >= sites.Count)
                {
                    for (int k = 0; k < seal.Length; k += 3) Draw2D.Line(p, seal[k], seal[(k + 1) % seal.Length], Palette.Faint, 1.2f);
                    continue;
                }
                RegionState r = sites[i];
                string state = Notes.SiteState(_state, r);
                bool ready = state == "ready";
                Color color = state == "ready" ? Palette.Cult : state == "preparing" ? Palette.Omen : Palette.Danger;
                if (ready)
                {
                    Draw2D.Polygon(p, seal, color);
                    Star(p, c, 7f, Palette.PaperLight);
                }
                else
                {
                    float fill = (float)r.Site.Progress / siteMonths;
                    float level = c.y + 13 - 26 * fill;
                    List<Vector2> below = Draw2D.ClipBelow(seal, level);
                    if (below.Count >= 3) Draw2D.Polygon(p, below, Palette.Alpha(color, 0.6f));
                    Draw2D.Polyline(p, seal, color, 1.4f, true);
                    Star(p, c, 7f, Palette.Alpha(color, 0.8f));
                }
                if (state == "dormant" || state == "unsupported")
                {
                    // Struck through: stalled, readable without the colour.
                    Draw2D.Line(p, c + new Vector2(-11, 11), c + new Vector2(11, -11), Palette.PaperLight, 4f);
                    Draw2D.Line(p, c + new Vector2(-11, 11), c + new Vector2(11, -11), Palette.Danger, 2.2f);
                }
            }
        }
    }
}
