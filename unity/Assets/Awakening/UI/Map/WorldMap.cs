using System;
using System.Collections.Generic;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;
using MeasureMode = UnityEngine.UIElements.VisualElement.MeasureMode;
using PointerType = UnityEngine.UIElements.PointerType;

namespace Awakening.UI.Map
{
    /// <summary>
    /// The strategy map, drawn as an old survey chart: a dark sea inside a ruled neatline, land in
    /// foxed parchment with inked borders. Port of godot/map/world_map.gd. Land and borders redraw
    /// on change; follower dots, threat markers, omens and ritual sites redraw on a clock. The wheel
    /// or a pinch zooms, a drag pans, and a click or tap without a drag picks a region.
    /// </summary>
    public sealed class WorldMap : VisualElement
    {
        public const int MaxDots = 260;
        /// <summary>Where Johansen found R'lyeh, in The Call of Cthulhu.</summary>
        static readonly Vector2 Rlyeh = new Vector2(-126.72f, -47.15f);
        static readonly Vector2 LatRange = new Vector2(-58, 84);
        public const float MaxViewScale = 4f;
        /// <summary>A press that moves further than this, in pixels, pans instead of picking.</summary>
        const float DragThreshold = 8f;
        static readonly Color CultInk = Palette.Hex("1f3519");

        public event Action<string> RegionPressed;
        public event Action<string> RegionHovered;
        /// <summary>True while the view is zoomed or panned away from the whole world.</summary>
        public event Action<bool> ViewChanged;

        public GameState State;
        public string Selected = "";
        public string Hovered = "";
        public bool ShowLabels = true;

        readonly VisualElement _sea, _land, _grain, _borders, _dots, _fx, _labels;
        readonly Rect _world;
        readonly Dictionary<string, List<Vector2>> _dotPoints = new Dictionary<string, List<Vector2>>();
        readonly Dictionary<string, List<Vector2>> _outlines = new Dictionary<string, List<Vector2>>();
        readonly Dictionary<string, Color> _regionColor = new Dictionary<string, Color>();
        readonly List<(Vector2 at, float radius, string region)> _hits = new List<(Vector2, float, string)>();
        readonly Dictionary<string, (Label title, Label count)> _regionLabels = new Dictionary<string, (Label, Label)>();
        readonly List<Label> _threatLabels = new List<Label>();
        readonly Label _north, _rlyehName, _rlyehCoords;

        float _zoom = 1f;
        Vector2 _offset;
        float _viewScale = 1f;
        Vector2 _viewCenter;
        float _time;
        float _sinceDots;

        // Pointer state.
        Vector2 _pressAt;
        bool _pressing, _dragging, _moved, _gesture;
        readonly Dictionary<int, Vector2> _touches = new Dictionary<int, Vector2>();
        float _pinchDistance;

        // reset_view animation.
        float _tween = -1f;
        float _fromScale;
        Vector2 _fromCenter;

        static readonly Dictionary<string, List<Vector2>> DotCache = new Dictionary<string, List<Vector2>>();
        static readonly Dictionary<string, List<Vector2>> OutlineCache = new Dictionary<string, List<Vector2>>();

        public WorldMap()
        {
            style.overflow = Overflow.Hidden;
            pickingMode = PickingMode.Position;
            focusable = false;
            Rect bounds = Geo.Bounds();
            _world = new Rect(bounds.x - 8, bounds.y - 4, bounds.width + 16, bounds.height + 8);
            _viewCenter = _world.center;
            foreach (string id in GameData.RegionIds)
            {
                if (!DotCache.ContainsKey(id))
                {
                    DotCache[id] = Geo.Scatter(id, MaxDots);
                    OutlineCache[id] = Outline(id);
                }
                _dotPoints[id] = DotCache[id];
                _outlines[id] = OutlineCache[id];
                _regionColor[id] = Palette.Land;
            }
            _sea = Layer(DrawSea);
            _land = Layer(DrawLand);
            _grain = new VisualElement { pickingMode = PickingMode.Ignore, name = "land-grain" };
            _grain.style.position = Position.Absolute;
            _grain.style.backgroundImage = LandGrain.Texture(_world);
            _grain.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            hierarchy.Add(_grain);
            _borders = Layer(DrawBorders);
            _dots = Layer(DrawDots);
            _fx = Layer(DrawFx);
            _labels = new VisualElement { pickingMode = PickingMode.Ignore, name = "labels" };
            Kit.Fill(_labels);
            hierarchy.Add(_labels);
            foreach (string id in GameData.RegionIds)
            {
                Label title = MapLabel(Fonts.DisplaySc, Palette.Text);
                Label count = MapLabel(Fonts.TypeBold, Palette.Cult);
                _regionLabels[id] = (title, count);
            }
            _north = MapLabel(Fonts.Display, Palette.Alpha(Palette.Brass, 0.55f), 16, 0);
            _north.text = "N";
            _rlyehName = MapLabel(Fonts.DisplayItalic, Palette.OmenBright, 15, 0);
            _rlyehName.text = "R'lyeh";
            _rlyehCoords = MapLabel(Fonts.Type, Palette.OmenBright, 11, 0);
            _rlyehCoords.text = "47°9′S 126°43′W";

            RegisterCallback<GeometryChangedEvent>(_ => Layout());
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(e => { _touches.Remove(e.pointerId); _pressing = false; });
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(""));
            RegisterCallback<WheelEvent>(OnWheel);
            schedule.Execute(Tick).Every(16);
        }

        VisualElement Layer(Action<MeshGenerationContext> painter)
        {
            var layer = new VisualElement { pickingMode = PickingMode.Ignore };
            Kit.Fill(layer);
            layer.generateVisualContent += painter;
            hierarchy.Add(layer);
            return layer;
        }

        Label MapLabel(string font, Color color, int size = 18, float halo = 3.2f)
        {
            Label label = Kit.Label("", size, color, font);
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.top = 0;
            if (halo > 0)
            {
                label.style.unityTextOutlineWidth = halo;
                label.style.unityTextOutlineColor = Palette.Alpha(Palette.PaperLight, 0.93f);
            }
            label.style.display = DisplayStyle.None;
            _labels.Add(label);
            return label;
        }

        public void SetState(GameState next, string selectedId)
        {
            State = next;
            Selected = selectedId ?? "";
            Recolor();
            _borders.MarkDirtyRepaint();
            _dots.MarkDirtyRepaint();
        }

        void Tick(TimerState timer)
        {
            float delta = Mathf.Min(timer.deltaTime / 1000f, 0.1f);
            // With reduced motion the clock stands still: markers keep their resting pose.
            if (!Kit.ReducedMotion) _time += delta;
            if (_tween >= 0)
            {
                _tween = Mathf.Min(1f, _tween + delta / 0.3f);
                float t = 1f - Mathf.Pow(1f - _tween, 4); // quartic ease-out
                _viewScale = Mathf.Lerp(_fromScale, 1f, t);
                _viewCenter = Vector2.Lerp(_fromCenter, _world.center, t);
                Layout();
                if (_tween >= 1f) _tween = -1f;
            }
            _sinceDots += delta;
            if (_sinceDots >= 0.08f)
            {
                _sinceDots = 0;
                _dots.MarkDirtyRepaint();
            }
            UpdateLabels();
            _fx.MarkDirtyRepaint();
        }

        /// <summary>
        /// Text on the chart is made of labels, positioned here before the frame is drawn: UI Toolkit
        /// does not allow changing elements while a layer draws its mesh.
        /// </summary>
        void UpdateLabels()
        {
            if (contentRect.width <= 0) return;
            Vector2 compass = ToScreen(Geo.Project(-24, -44));
            float radius = Mathf.Clamp(FitZoom() * 26f, 22f, 40f);
            Place(_north, compass + new Vector2(0, -radius - 5), 0.5f, 1f);
            Vector2 rlyeh = ToScreen(Geo.Project(Rlyeh));
            Color color = RlyehColor();
            _rlyehName.style.color = color;
            _rlyehCoords.style.color = Palette.Alpha(color, color.a * 0.8f);
            Place(_rlyehName, rlyeh + new Vector2(11, 4), 0f, 0.8f);
            Place(_rlyehCoords, rlyeh + new Vector2(11, 17), 0f, 0.8f);
            int threatLabel = 0;
            if (State != null && !GameEngine.IsOver(State))
            {
                foreach (string id in GameData.RegionIds)
                {
                    Threat threat = State.Regions[id].Threat;
                    if (threat == null) continue;
                    while (_threatLabels.Count <= threatLabel) _threatLabels.Add(MapLabel(Fonts.TypeBold, Palette.Danger, 14, 2.4f));
                    Label text = _threatLabels[threatLabel++];
                    text.text = (threat.DueMonth - State.ElapsedMonths) + " MO";
                    Place(text, ToScreen(Geo.ThreatPosition(State.Seed, id, threat)) + new Vector2(0, -15), 0.5f, 1f);
                }
            }
            for (int i = threatLabel; i < _threatLabels.Count; i++) _threatLabels[i].style.display = DisplayStyle.None;
            PlaceLabels();
        }

        bool Stirring => State != null && (State.Status == "alignment" || State.Status == "awakening" || State.Status == "won");

        Color RlyehColor()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * (Stirring ? 2.4f : 0.6f));
            return Palette.Alpha(Palette.OmenBright, (Stirring ? 0.6f : 0.42f) + 0.25f * pulse);
        }

        // --- View -------------------------------------------------------------------------

        float FitZoom()
        {
            Rect r = contentRect;
            return Mathf.Min(r.width / _world.width, r.height / _world.height);
        }

        void Layout()
        {
            Rect r = contentRect;
            if (r.width <= 0 || r.height <= 0) return;
            _zoom = FitZoom() * _viewScale;
            _viewCenter = ClampCenter(_viewCenter, _zoom);
            _offset = r.size * 0.5f - _viewCenter * _zoom;
            Vector2 min = ToScreen(_world.min);
            _grain.style.left = min.x;
            _grain.style.top = min.y;
            _grain.style.width = _world.width * _zoom;
            _grain.style.height = _world.height * _zoom;
            _sea.MarkDirtyRepaint();
            _land.MarkDirtyRepaint();
            _borders.MarkDirtyRepaint();
            _dots.MarkDirtyRepaint();
        }

        /// <summary>Keeps the world covering the view where it is big enough to, and centred where not.</summary>
        Vector2 ClampCenter(Vector2 center, float zoom)
        {
            Vector2 half = contentRect.size * 0.5f / zoom;
            for (int axis = 0; axis < 2; axis++)
            {
                float low = _world.min[axis] + half[axis];
                float high = _world.max[axis] - half[axis];
                center[axis] = low <= high ? Mathf.Clamp(center[axis], low, high) : _world.center[axis];
            }
            return center;
        }

        public bool IsFreeView => _viewScale > 1.01f;

        /// <summary>Zooms by <paramref name="factor"/> keeping the world point under <paramref name="at"/> still.</summary>
        public void ZoomAt(Vector2 at, float factor)
        {
            Vector2 before = ToWorld(at);
            _viewScale = Mathf.Clamp(_viewScale * factor, 1f, MaxViewScale);
            float zoom = FitZoom() * _viewScale;
            _viewCenter = before + (contentRect.size * 0.5f - at) / zoom;
            _tween = -1f;
            Layout();
            ViewChanged?.Invoke(IsFreeView);
        }

        public void PanBy(Vector2 pixels)
        {
            _viewCenter -= pixels / _zoom;
            Layout();
        }

        public void ResetView()
        {
            if (Kit.ReducedMotion || panel == null)
            {
                _viewScale = 1f;
                _viewCenter = _world.center;
                _tween = -1f;
                Layout();
            }
            else
            {
                _fromScale = _viewScale;
                _fromCenter = _viewCenter;
                _tween = 0f;
            }
            ViewChanged?.Invoke(false);
        }

        public Vector2 ToScreen(Vector2 world) => world * _zoom + _offset;

        Vector2 ToWorld(Vector2 local) => (local - _offset) / _zoom;

        // --- Input ------------------------------------------------------------------------

        void OnPointerDown(PointerDownEvent e)
        {
            if (e.pointerType == PointerType.touch)
            {
                _touches[e.pointerId] = e.localPosition;
                // A pinch never ends in a pick; the flag clears on the next press.
                if (_touches.Count >= 2)
                {
                    _gesture = true;
                    _pinchDistance = PairDistance();
                    return;
                }
            }
            if (e.button != 0) return;
            _pressing = true;
            _dragging = false;
            _moved = false;
            _gesture = _touches.Count >= 2;
            _pressAt = e.localPosition;
            this.CapturePointer(e.pointerId);
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            Vector2 position = e.localPosition;
            if (e.pointerType == PointerType.touch && _touches.ContainsKey(e.pointerId) && _touches.Count >= 2)
            {
                Vector2 before = PairMiddle();
                _touches[e.pointerId] = position;
                float distance = PairDistance();
                if (_pinchDistance > 0 && distance > 0) ZoomAt(PairMiddle(), distance / _pinchDistance);
                _pinchDistance = distance;
                PanBy(PairMiddle() - before);
                return;
            }
            if (e.pointerType == PointerType.touch) _touches[e.pointerId] = position;
            if (_pressing && !_gesture)
            {
                if (!_moved && Vector2.Distance(position, _pressAt) > DragThreshold)
                {
                    _moved = true;
                    _dragging = IsFreeView;
                }
                if (_dragging)
                {
                    PanBy(e.deltaPosition);
                    return;
                }
            }
            if (e.pointerType != PointerType.touch) SetHovered(Pick(position));
        }

        void OnPointerUp(PointerUpEvent e)
        {
            bool touch = e.pointerType == PointerType.touch;
            if (touch) _touches.Remove(e.pointerId);
            if (this.HasPointerCapture(e.pointerId)) this.ReleasePointer(e.pointerId);
            if (e.button != 0 && !touch) return;
            bool picked = _pressing && !_moved && !_gesture;
            _pressing = false;
            _dragging = false;
            if (touch && _touches.Count == 0) _gesture = false;
            string id = picked ? Pick(e.localPosition) : "";
            if (!string.IsNullOrEmpty(id)) RegionPressed?.Invoke(id);
        }

        void OnWheel(WheelEvent e)
        {
            // A trackpad's two-finger scroll arrives as a small horizontal or vertical delta.
            ZoomAt(e.localMousePosition, e.delta.y < 0 ? 1.15f : 1f / 1.15f);
            e.StopPropagation();
        }

        float PairDistance()
        {
            if (_touches.Count < 2) return 0;
            var points = new List<Vector2>(_touches.Values);
            return Vector2.Distance(points[0], points[1]);
        }

        Vector2 PairMiddle()
        {
            if (_touches.Count < 2) return Vector2.zero;
            var points = new List<Vector2>(_touches.Values);
            return (points[0] + points[1]) * 0.5f;
        }

        void SetHovered(string id)
        {
            if (id == Hovered) return;
            Hovered = id;
            Recolor();
            _borders.MarkDirtyRepaint();
            RegionHovered?.Invoke(id);
        }

        /// <summary>Markers win over the land beneath them.</summary>
        string Pick(Vector2 local)
        {
            foreach (var hit in _hits)
                if (Vector2.Distance(local, hit.at) <= hit.radius) return hit.region;
            return Geo.RegionAt(ToWorld(local));
        }

        // --- Colour -----------------------------------------------------------------------

        float Share(string id)
        {
            if (State == null) return 0;
            RegionState r = State.Regions[id];
            return (float)(r.Population.Followers / Math.Max(1.0, Derived.Capacity(r)));
        }

        void Recolor()
        {
            foreach (string id in GameData.RegionIds)
            {
                Color color = Palette.Land;
                if (State != null)
                {
                    RegionState r = State.Regions[id];
                    if (r.Population.Followers > 0)
                    {
                        float tint = Mathf.Clamp01(0.25f + Share(id) * 1.6f);
                        // The cult soaks into the paper like a verdigris stain.
                        color = Color.Lerp(Palette.Land, Palette.Hex(r.Public ? "6f8a52" : "8b9466"), tint);
                    }
                }
                if (id == Hovered) color = Palette.Lightened(color, 0.08f);
                if (id == Selected) color = Palette.Lightened(color, 0.14f);
                _regionColor[id] = color;
            }
            _land.MarkDirtyRepaint();
        }

        // --- Drawing ----------------------------------------------------------------------

        List<Vector2> Meridian(float lon, float fromLat, float toLat)
        {
            var line = new List<Vector2>(37);
            for (int k = 0; k <= 36; k++) line.Add(ToScreen(Geo.Project(lon, Mathf.Lerp(fromLat, toLat, k / 36f))));
            return line;
        }

        List<Vector2> Parallel(float lat, float fromLon = -180, float toLon = 180)
        {
            var line = new List<Vector2>(61);
            for (int k = 0; k <= 60; k++) line.Add(ToScreen(Geo.Project(Mathf.Lerp(fromLon, toLon, k / 60f), lat)));
            return line;
        }

        /// <summary>The sea inside the chart's outline, its graticule, a ruled neatline and a compass rose.</summary>
        void DrawSea(MeshGenerationContext context)
        {
            if (contentRect.width <= 0) return;
            Painter2D p = context.painter2D;
            var outline = Meridian(-180, LatRange.x, LatRange.y);
            outline.AddRange(Parallel(LatRange.y));
            outline.AddRange(Meridian(180, LatRange.y, LatRange.x));
            outline.AddRange(Parallel(LatRange.x, 180, -180));
            Draw2D.Polygon(p, outline, Palette.Ocean);
            for (int lon = -150; lon <= 150; lon += 30) Draw2D.Polyline(p, Meridian(lon, LatRange.x, LatRange.y), Palette.OceanLine);
            foreach (int lat in new[] { -40, -20, 0, 20, 40, 60, 80 })
                Draw2D.Polyline(p, Parallel(lat), lat == 0 ? Palette.Lightened(Palette.OceanLine, 0.12f) : Palette.OceanLine);
            Draw2D.Polyline(p, outline, Palette.Alpha(Palette.Brass, 0.75f), 2f, true);
            Vector2 center = ToScreen(Vector2.zero);
            var outer = new List<Vector2>(outline.Count);
            foreach (Vector2 point in outline) outer.Add(point + (point - center).normalized * 5f);
            Draw2D.Polyline(p, outer, Palette.Alpha(Palette.Brass, 0.4f), 1f, true);
            DrawCompass(p, ToScreen(Geo.Project(-24, -44)), Mathf.Clamp(FitZoom() * 26f, 22f, 40f));
        }

        void DrawCompass(Painter2D p, Vector2 at, float radius)
        {
            Color ink = Palette.Alpha(Palette.Brass, 0.55f);
            Draw2D.Ring(p, at, radius * 0.72f, ink);
            Draw2D.Ring(p, at, radius * 0.66f, Palette.Alpha(ink, 0.3f));
            for (int k = 0; k < 8; k++)
            {
                float angle = -Mathf.PI / 2 + k * Mathf.PI * 2 / 8f;
                float length = k % 2 == 0 ? radius : radius * 0.55f;
                Vector2 tip = at + Draw2D.FromAngle(angle) * length;
                Vector2 left = at + Draw2D.FromAngle(angle - Mathf.PI / 2) * radius * 0.09f;
                Vector2 right = at + Draw2D.FromAngle(angle + Mathf.PI / 2) * radius * 0.09f;
                Draw2D.Polygon(p, new[] { tip, left, at }, ink);
                Draw2D.Polygon(p, new[] { tip, at, right }, Palette.Alpha(ink, 0.25f));
            }
        }

        void DrawLand(MeshGenerationContext context)
        {
            if (contentRect.width <= 0) return;
            Painter2D p = context.painter2D;
            foreach (string id in GameData.RegionIds)
            {
                p.fillColor = _regionColor[id];
                foreach (var country in Geo.Regions()[id])
                    foreach (var ring in country.Rings)
                    {
                        if (ring.Length < 3) continue;
                        p.BeginPath();
                        p.MoveTo(ToScreen(ring[0]));
                        for (int i = 1; i < ring.Length; i++) p.LineTo(ToScreen(ring[i]));
                        p.ClosePath();
                        p.Fill(FillRule.NonZero);
                    }
            }
        }

        /// <summary>Segments that belong to exactly one country ring of the region: its outer edge.</summary>
        static List<Vector2> Outline(string id)
        {
            var count = new Dictionary<(long, long, long, long), int>();
            var segments = new Dictionary<(long, long, long, long), (Vector2, Vector2)>();
            foreach (var country in Geo.Regions()[id])
                foreach (var ring in country.Rings)
                    for (int i = 0; i < ring.Length; i++)
                    {
                        Vector2 a = ring[i], b = ring[(i + 1) % ring.Length];
                        long ax = Mathf.RoundToInt(a.x * 50), ay = Mathf.RoundToInt(a.y * 50);
                        long bx = Mathf.RoundToInt(b.x * 50), by = Mathf.RoundToInt(b.y * 50);
                        if (ax == bx && ay == by) continue;
                        bool aFirst = ax < bx || ax == bx && ay < by;
                        var key = aFirst ? (ax, ay, bx, by) : (bx, by, ax, ay);
                        count[key] = count.TryGetValue(key, out int n) ? n + 1 : 1;
                        segments[key] = (a, b);
                    }
            var lines = new List<Vector2>();
            foreach (var pair in count)
                if (pair.Value == 1)
                {
                    lines.Add(segments[pair.Key].Item1);
                    lines.Add(segments[pair.Key].Item2);
                }
            return lines;
        }

        void Multiline(Painter2D p, List<Vector2> lines, Color color, float width)
        {
            p.strokeColor = color;
            p.lineWidth = width;
            p.lineCap = LineCap.Butt;
            p.BeginPath();
            for (int i = 0; i + 1 < lines.Count; i += 2)
            {
                p.MoveTo(ToScreen(lines[i]));
                p.LineTo(ToScreen(lines[i + 1]));
            }
            p.Stroke();
        }

        void DrawBorders(MeshGenerationContext context)
        {
            if (contentRect.width <= 0) return;
            Painter2D p = context.painter2D;
            foreach (string id in GameData.RegionIds)
            {
                // Country borders inside a region are faint; the region's own edge is inked.
                p.strokeColor = Palette.Alpha(Palette.LandEdge, id == Selected ? 0.45f : 0.7f);
                p.lineWidth = 1f;
                p.lineJoin = LineJoin.Round;
                p.BeginPath();
                foreach (var country in Geo.Regions()[id])
                    foreach (var ring in country.Rings)
                    {
                        if (ring.Length < 2) continue;
                        p.MoveTo(ToScreen(ring[0]));
                        for (int i = 1; i < ring.Length; i++) p.LineTo(ToScreen(ring[i]));
                        p.ClosePath();
                    }
                p.Stroke();
            }
            foreach (string id in GameData.RegionIds) Multiline(p, _outlines[id], Palette.LandEdge, 1.2f);
            foreach (string id in new[] { Hovered, Selected })
                if (!string.IsNullOrEmpty(id))
                    Multiline(p, _outlines[id], Palette.Alpha(Palette.Text, id == Selected ? 0.9f : 0.5f), id == Selected ? 2.6f : 1.6f);
            DrawRoutes(p);
        }

        /// <summary>Dashed routes from the selected cell to the regions it can spread to.</summary>
        void DrawRoutes(Painter2D p)
        {
            if (State == null || string.IsNullOrEmpty(Selected) || State.Regions[Selected].Population.Followers <= 0) return;
            foreach (string other in GameData.Regions[Selected].Adjacent)
            {
                Vector2 a = Geo.Anchors[Selected], b = Geo.Anchors[other];
                bool occupied = State.Regions[other].Population.Followers > 0;
                Color color = occupied ? Palette.Cult : Palette.Text;
                if (Mathf.Abs(a.x - b.x) > 180f)
                {
                    // Across the Pacific: leave one edge of the map and re-enter at the other.
                    var edgeA = new Vector2(a.x < 0 ? -180f : 180f, Mathf.Lerp(a.y, b.y, 0.5f));
                    var edgeB = new Vector2(-edgeA.x, edgeA.y);
                    DashedArc(p, ToScreen(Geo.Project(a)), ToScreen(Geo.Project(edgeA)), color);
                    DashedArc(p, ToScreen(Geo.Project(edgeB)), ToScreen(Geo.Project(b)), color);
                }
                else
                {
                    DashedArc(p, ToScreen(Geo.Project(a)), ToScreen(Geo.Project(b)), color);
                }
            }
        }

        static void DashedArc(Painter2D p, Vector2 from, Vector2 to, Color color)
        {
            Vector2 d = to - from;
            Vector2 control = (from + to) * 0.5f + new Vector2(d.y, -d.x).normalized * d.magnitude * 0.18f;
            var points = new Vector2[33];
            for (int i = 0; i <= 32; i++)
            {
                float t = i / 32f;
                points[i] = Vector2.Lerp(Vector2.Lerp(from, control, t), Vector2.Lerp(control, to, t), t);
            }
            // A pale underline keeps the inked route legible over the dark sea.
            for (int pass = 0; pass < 2; pass++)
            {
                p.strokeColor = pass == 0 ? Palette.Alpha(Palette.PaperLight, 0.55f) : color;
                p.lineWidth = pass == 0 ? 3.4f : 1.4f;
                p.lineCap = LineCap.Butt;
                p.BeginPath();
                for (int i = 0; i + 1 < points.Length; i += 2)
                {
                    p.MoveTo(points[i]);
                    p.LineTo(points[i + 1]);
                }
                p.Stroke();
            }
        }

        /// <summary>Follower dots, as small quads written straight into the mesh: thousands of them.</summary>
        void DrawDots(MeshGenerationContext context)
        {
            if (State == null || contentRect.width <= 0) return;
            float dotSize = Mathf.Clamp(_zoom * 1.1f, 1.2f, 3.4f);
            var quads = new List<(Vector2 at, float half, Color32 color)>();
            Rect view = contentRect;
            foreach (string id in GameData.RegionIds)
            {
                RegionState r = State.Regions[id];
                if (r.Population.Followers <= 0) continue;
                int count = Mathf.Clamp(Mathf.RoundToInt(MaxDots * Mathf.Clamp01(Share(id) * 1.6f)), 4, MaxDots);
                List<Vector2> points = _dotPoints[id];
                for (int i = 0; i < Mathf.Min(count, points.Count); i++)
                {
                    Vector2 at = ToScreen(points[i]);
                    if (!view.Contains(at)) continue;
                    float flicker = 0.75f + 0.25f * Mathf.Sin(_time * 1.7f + i * 2.39f);
                    if (r.Public)
                    {
                        quads.Add((at, dotSize * 2.4f * 0.8f, Palette.Alpha(Palette.Cult, 0.12f * flicker)));
                        quads.Add((at, dotSize * 0.85f, Palette.Alpha(CultInk, 0.95f * flicker)));
                    }
                    else
                    {
                        quads.Add((at, dotSize * 0.75f, Palette.Alpha(CultInk, 0.6f * flicker)));
                    }
                }
            }
            if (quads.Count == 0) return;
            MeshWriteData mesh = context.Allocate(quads.Count * 4, quads.Count * 6);
            ushort index = 0;
            foreach (var quad in quads)
            {
                float h = quad.half;
                mesh.SetNextVertex(new Vertex { position = new Vector3(quad.at.x - h, quad.at.y - h, Vertex.nearZ), tint = quad.color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(quad.at.x + h, quad.at.y - h, Vertex.nearZ), tint = quad.color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(quad.at.x + h, quad.at.y + h, Vertex.nearZ), tint = quad.color });
                mesh.SetNextVertex(new Vertex { position = new Vector3(quad.at.x - h, quad.at.y + h, Vertex.nearZ), tint = quad.color });
                mesh.SetNextIndex(index);
                mesh.SetNextIndex((ushort)(index + 1));
                mesh.SetNextIndex((ushort)(index + 2));
                mesh.SetNextIndex(index);
                mesh.SetNextIndex((ushort)(index + 2));
                mesh.SetNextIndex((ushort)(index + 3));
                index += 4;
            }
        }

        void DrawFx(MeshGenerationContext context)
        {
            _hits.Clear();
            if (contentRect.width <= 0) return;
            Painter2D p = context.painter2D;
            DrawRlyeh(p);
            if (State != null)
            {
                bool over = GameEngine.IsOver(State);
                foreach (string id in GameData.RegionIds)
                {
                    RegionState r = State.Regions[id];
                    if (r.Site.Invested) DrawSite(p, id, r);
                    if (!over && r.Opportunity != null) DrawOmen(p, id);
                    if (!over && r.Threat != null) DrawThreat(p, id, r.Threat);
                }
            }
        }

        /// <summary>One small mark in the South Pacific, which stirs once the stars are right.</summary>
        void DrawRlyeh(Painter2D p)
        {
            Vector2 at = ToScreen(Geo.Project(Rlyeh));
            bool stirring = Stirring;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * (stirring ? 2.4f : 0.6f));
            Color color = RlyehColor();
            Draw2D.Line(p, at + new Vector2(-4, -4), at + new Vector2(4, 4), color, 1.2f);
            Draw2D.Line(p, at + new Vector2(4, -4), at + new Vector2(-4, 4), color, 1.2f);
            Draw2D.Ring(p, at, 7f + pulse * (stirring ? 5f : 2f), Palette.Alpha(color, color.a * 0.5f));
        }

        void DrawSite(Painter2D p, string id, RegionState r)
        {
            Vector2 at = ToScreen(Geo.Anchor(id)) + new Vector2(0, 32);
            float progress = r.Site.Progress / (float)Balance.SiteMonths;
            string state = Notes.SiteState(r);
            bool ready = state == "ready";
            bool supported = state == "ready" || state == "preparing";
            Color color = !supported ? Palette.DangerBright : ready ? Palette.CultBright : Palette.OmenBright;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * (!supported ? 5f : 2f));
            Draw2D.Circle(p, at, 13f, Palette.Alpha(Palette.Text, 0.88f));
            Draw2D.Ring(p, at, 11f, Palette.Alpha(color, 0.25f), 1.5f);
            if (progress > 0) Draw2D.Arc(p, at, 11f, -Mathf.PI / 2, -Mathf.PI / 2 + Mathf.PI * 2 * progress, color, 2.5f);
            // A slowly turning pentagram: five points joined by every second vertex.
            float spin = _time * 0.35f - Mathf.PI / 2;
            var star = new Vector2[6];
            for (int k = 0; k < 6; k++) star[k] = at + Draw2D.FromAngle(spin + (k * 2 % 5) * Mathf.PI * 2 / 5f) * 7f;
            Draw2D.Polyline(p, star, Palette.Alpha(color, ready || !supported ? 0.6f + 0.4f * pulse : 0.85f), 1.3f);
            if (!supported)
            {
                // A struck-through seal: stalled, readable without the colour.
                Draw2D.Line(p, at + new Vector2(-11, 11), at + new Vector2(11, -11), Palette.PaperLight, 4f);
                Draw2D.Line(p, at + new Vector2(-11, 11), at + new Vector2(11, -11), color, 2.2f);
            }
            _hits.Add((at, 14f, id));
        }

        void DrawOmen(Painter2D p, string id)
        {
            Vector2 at = ToScreen(Geo.Anchor(id)) + new Vector2(38, -26 + Mathf.Sin(_time * 2.2f) * 2.5f);
            float glow = 0.5f + 0.5f * Mathf.Sin(_time * 3f);
            Draw2D.Circle(p, at, 15f, Palette.Alpha(Palette.Omen, 0.14f + 0.1f * glow));
            var diamond = new[] { at + new Vector2(0, -10), at + new Vector2(10, 0), at + new Vector2(0, 10), at + new Vector2(-10, 0) };
            Draw2D.Polygon(p, diamond, Palette.OmenDeep);
            Draw2D.Polyline(p, diamond, Palette.OmenBright, 1.6f, true);
            // An eye: the omen is watching back.
            var lid = new List<Vector2>(18);
            for (int k = 0; k <= 8; k++)
            {
                float t = k / 8f;
                lid.Add(at + new Vector2(Mathf.Lerp(-5.5f, 5.5f, t), -Mathf.Sin(t * Mathf.PI) * 3.2f));
            }
            for (int k = 0; k <= 8; k++)
            {
                float t = 1f - k / 8f;
                lid.Add(at + new Vector2(Mathf.Lerp(-5.5f, 5.5f, t), Mathf.Sin(t * Mathf.PI) * 3.2f));
            }
            Draw2D.Polyline(p, lid, Palette.OmenBright, 1.2f);
            Draw2D.Circle(p, at, 1.6f, Palette.OmenBright);
            _hits.Add((at, 14f, id));
        }

        void DrawThreat(Painter2D p, string id, Threat threat)
        {
            Vector2 at = ToScreen(Geo.ThreatPosition(State.Seed, id, threat));
            int months = threat.DueMonth - State.ElapsedMonths;
            bool urgent = months <= 3;
            Color color = Palette.Danger;
            float phase = Mathf.Repeat(_time * (urgent ? 1.8f : 0.8f), 1f);
            Draw2D.Ring(p, at, 6f + phase * 14f, Palette.Alpha(color, 0.8f * (1f - phase)), urgent ? 2f : 1.5f);
            Draw2D.Circle(p, at, 7.5f, Palette.PaperLight);
            Draw2D.Ring(p, at, 7.5f, color, 2f);
            Draw2D.Circle(p, at, 3.4f, color);
            _hits.Add((at, 14f, id));
        }

        /// <summary>
        /// Positions a label so that <paramref name="anchor"/> sits at the given fraction of its width
        /// and height (Godot draws text from its baseline; 0.8 of the height approximates it).
        /// </summary>
        static void Place(Label label, Vector2 anchor, float alignX, float alignY)
        {
            Vector2 size = label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            label.style.translate = new Translate(anchor.x - size.x * alignX, anchor.y - size.y * alignY);
            label.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Region names and follower counts on a pale halo, so borders never cross the letters. Type
        /// grows a little as the chart is zoomed. Labels are placed in order of importance; one that
        /// would cover another moves just above or below its anchor.
        /// </summary>
        void PlaceLabels()
        {
            if (State == null || !ShowLabels)
            {
                foreach (var pair in _regionLabels.Values)
                {
                    pair.title.style.display = DisplayStyle.None;
                    pair.count.style.display = DisplayStyle.None;
                }
                return;
            }
            float grow = Mathf.Clamp(Mathf.Sqrt(_viewScale), 1f, 1.5f);
            int sizeTitle = Mathf.RoundToInt((FitZoom() < 1f ? 18 : 20) * grow);
            int sizeCount = Mathf.RoundToInt((FitZoom() < 1f ? 15 : 17) * grow);
            var order = new List<string>(GameData.RegionIds);
            order.Sort((a, b) => LabelRank(b).CompareTo(LabelRank(a)));
            // Markers are placed first, so a name moves off a site, an omen or a threat's countdown.
            List<Rect> placed = MarkerRects();
            int markers = placed.Count;
            foreach (string id in order)
            {
                (Label title, Label count) = _regionLabels[id];
                title.text = GameData.Regions[id].Name;
                double followers = State.Regions[id].Population.Followers;
                count.text = followers > 0 ? Format.Count(followers) : "";
                title.style.fontSize = sizeTitle;
                count.style.fontSize = sizeCount;
                Vector2 titleSize = title.MeasureTextSize(title.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
                Vector2 countSize = count.text.Length > 0 ? count.MeasureTextSize(count.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined) : Vector2.zero;
                float height = sizeTitle * 0.85f + (count.text.Length > 0 ? sizeCount * 1.05f : 0f);
                float width = Mathf.Max(titleSize.x, countSize.x);
                Vector2 at = ToScreen(Geo.Anchor(id));
                float best = 0, bestOverlap = float.PositiveInfinity;
                foreach (float shift in new[] { 0f, height + 2f, -(height + 2f), (height + 2f) * 2f, -(height + 2f) * 2f })
                {
                    var rect = new Rect(at.x - width * 0.5f, at.y - sizeTitle * 0.85f + shift, width, height);
                    float overlap = 0;
                    for (int k = 0; k < placed.Count; k++)
                    {
                        // Covering a marker costs more than touching another name.
                        overlap += Intersection(rect, placed[k]) * (k < markers ? 3f : 1f);
                    }
                    if (overlap < bestOverlap)
                    {
                        bestOverlap = overlap;
                        best = shift;
                    }
                    if (overlap <= 0) break;
                }
                Vector2 origin = at + new Vector2(0, best);
                placed.Add(Draw2D.Grow(new Rect(origin.x - width * 0.5f, origin.y - sizeTitle * 0.85f, width, height), 3));
                bool focus = id == Selected || id == Hovered;
                title.style.color = focus ? Palette.Text : Palette.Alpha(Palette.Text, 0.86f);
                Place(title, origin + new Vector2(0, -4), 0.5f, 0.8f);
                if (count.text.Length > 0) Place(count, origin + new Vector2(0, sizeCount * 0.9f), 0.5f, 0.8f);
                else count.style.display = DisplayStyle.None;
            }
        }

        static float Intersection(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return w > 0 && h > 0 ? w * h : 0;
        }

        /// <summary>Screen rectangles of the markers drawn over the land this frame.</summary>
        List<Rect> MarkerRects()
        {
            var rects = new List<Rect>();
            bool over = GameEngine.IsOver(State);
            foreach (string id in GameData.RegionIds)
            {
                RegionState r = State.Regions[id];
                Vector2 anchor = ToScreen(Geo.Anchor(id));
                if (r.Site.Invested) rects.Add(new Rect(anchor + new Vector2(-14, 18), new Vector2(28, 28)));
                if (!over && r.Opportunity != null) rects.Add(new Rect(anchor + new Vector2(23, -41), new Vector2(30, 30)));
                if (!over && r.Threat != null)
                {
                    Vector2 at = ToScreen(Geo.ThreatPosition(State.Seed, id, r.Threat));
                    rects.Add(new Rect(at + new Vector2(-24, -28), new Vector2(48, 38)));
                }
            }
            return rects;
        }

        /// <summary>The open region first, then the hovered one, then the cult's regions by size.</summary>
        double LabelRank(string id)
        {
            if (id == Selected) return 1e9;
            if (id == Hovered) return 1e8;
            return State.Regions[id].Population.Followers;
        }
    }
}
