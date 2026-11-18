using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;
using Awakening.Core.Session;
using Awakening.UI.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// The strategy screen: a survey chart on a dark desk, between a slim top bar and the ledger,
    /// with press clippings for threats on the left, telegrams at the top, the selected region's
    /// case file on the right, a note for first-time situations, and dialogs. Port of
    /// godot/ui/game_screen.gd.
    /// </summary>
    public sealed class GameScreen : VisualElement
    {
        // Bar heights; slimmer in the compact layout so the chart keeps its room.
        public readonly float Top = Kit.Compact ? 48f : 58f;
        public readonly float Bottom = Kit.Compact ? 86f : 104f;

        readonly GameController _game;
        public readonly WorldMap Map;
        public readonly RegionPanel Panel;
        public readonly Dialogs Dialogs;
        public string Selected = "";

        Label _date, _countdown, _influence, _influenceRate, _dread, _followers, _doctrineNote, _tickerHead, _ticker;
        readonly List<IconButton> _speed = new List<IconButton>();
        Button _doctrine, _rite, _resetView;
        Label _awarenessCaption, _sitesCaption, _bannerHead, _bannerText;
        AwarenessGauge _awareness;
        SiteSeals _sites;
        VisualElement _sitesBox, _tickerBox;
        Stack _threats;
        ScrollView _threatScroll;
        Stack _toasts;
        Stack _banner, _prompt;
        Bar _riteBar;
        Stack _note;
        string _noteId = "";
        float _noteAt;
        bool _noting;
        string _lastStatus = "";
        bool _layoutPending;

        public GameScreen(GameController game)
        {
            _game = game;
            name = "game-screen";
            Kit.Fill(this);
            style.backgroundColor = Palette.Desk;

            Map = new WorldMap();
            Map.style.position = Position.Absolute;
            Map.style.left = 0;
            Map.style.right = 0;
            Map.style.top = Top + 8;
            Map.style.bottom = Bottom + 8;
            Add(Map);
            Map.RegionPressed += Select;
            Map.ViewChanged += OnViewChanged;
            var lamp = new VisualElement { pickingMode = PickingMode.Ignore, name = "lamp" };
            Kit.Fill(lamp);
            lamp.style.backgroundImage = PaperGrain.Lamp();
            lamp.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            Add(lamp);
            BuildTop();
            BuildBottom();
            BuildOverlays();
            Panel = new RegionPanel(game, this);
            Add(Panel);
            Dialogs = new Dialogs(game, this);
            Add(Dialogs);
            _game.Changed += Refresh;
            _game.Events += OnEvents;
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                _game.Changed -= Refresh;
                _game.Events -= OnEvents;
            });
            RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
            _lastStatus = game.State.Status;
            Refresh();
            if (!game.TutorialComplete) OpenDialog("guide");
            else if (game.State.Status == "alignment") OpenDialog("alignment");
            else if (GameEngine.IsOver(game.State)) OpenDialog("end");
        }

        public void Select(string id)
        {
            Selected = id;
            Panel.ShowRegion(id);
            // Keep the whole world visible beside the panel.
            Map.style.right = Panel.Width + 28;
            Map.SetState(_game.State, Selected);
            QueueLayout();
        }

        public void Deselect()
        {
            Selected = "";
            Panel.ShowRegion("");
            Map.style.right = 0;
            Map.SetState(_game.State, Selected);
            QueueLayout();
        }

        public void OpenDialog(string kind) => Dialogs.Open(kind);

        /// <summary>Screen shortcuts; returns true when the key was used.</summary>
        public bool HandleKey(KeyCode key, bool shift)
        {
            if (Dialogs.IsOpen) return Dialogs.HandleKey(key);
            switch (key)
            {
                case KeyCode.Escape:
                    if (Selected != "") Deselect();
                    else OpenDialog("menu");
                    return true;
                case KeyCode.Tab:
                    CycleRegion(shift ? -1 : 1);
                    return true;
                case KeyCode.C:
                    OpenDialog("chronicle");
                    return true;
                case KeyCode.D:
                    if (_game.State.Status != "placing") OpenDialog("doctrine");
                    return true;
                case KeyCode.H:
                    OpenDialog("guide");
                    return true;
                case KeyCode.R:
                    Map.ResetView();
                    return true;
                case KeyCode.N:
                    if (_note == null) return false;
                    DismissNote();
                    return true;
                default:
                    return Selected != "" && Panel.HandleKey(key);
            }
        }

        /// <summary>Tab walks the regions in map order; the cult's own regions come first.</summary>
        void CycleRegion(int step)
        {
            GameState s = _game.State;
            var ids = GameData.RegionIds.ToList();
            if (s.Status != "placing")
                ids = ids.Where(id => s.Regions[id].Population.Followers > 0).Concat(ids.Where(id => s.Regions[id].Population.Followers <= 0)).ToList();
            int at = ids.IndexOf(Selected);
            Select(at >= 0 ? ids[((at + step) % ids.Count + ids.Count) % ids.Count] : ids[step > 0 ? 0 : ids.Count - 1]);
        }

        // --- Layout ------------------------------------------------------------------------

        /// <summary>The top bar is the dark edge of the desk; the bottom one is a strip of ledger paper.</summary>
        Stack Bar(bool top)
        {
            var bar = new Stack(true, 0);
            bar.name = top ? "top-bar" : "ledger";
            int side = Kit.Compact ? 14 : 20;
            Kit.Box(bar, top ? Palette.DeskRaised : Palette.Paper, top ? Palette.Brass : Palette.DeskEdge, 0, new Vector4(side, 0, side - 6, 0));
            if (top) bar.style.borderBottomWidth = 2;
            else
            {
                bar.style.borderTopWidth = 3;
                Kit.AddGrain(bar);
            }
            bar.style.position = Position.Absolute;
            bar.style.left = 0;
            bar.style.right = 0;
            if (top)
            {
                bar.style.top = 0;
                bar.style.height = Top;
            }
            else
            {
                bar.style.bottom = 0;
                bar.style.height = Bottom;
            }
            bar.style.alignItems = Align.Center;
            Add(bar);
            return bar;
        }

        void BuildTop()
        {
            Stack bar = Bar(true);
            Stack left = Kit.HBox(10);
            left.style.flexGrow = 1;
            left.style.flexBasis = 0;
            left.style.alignItems = Align.FlexEnd;
            left.Add(Kit.Label("The Awakening", Kit.Compact ? 22 : 24, Palette.Lamp, Fonts.DisplaySc));
            if (!Kit.Compact) left.Add(Kit.Label("of Cthulhu", 22, Palette.Brass, Fonts.DisplayItalic));
            bar.Add(left);
            Stack center = Kit.VBox(-2);
            center.style.alignItems = Align.Center;
            _date = Kit.Label("", Kit.Compact ? 23 : 25, Palette.Lamp, Fonts.Display);
            _countdown = Kit.Caption("", Palette.LampDim);
            center.Add(_date);
            center.Add(_countdown);
            bar.Add(center);
            Stack right = Kit.HBox(2);
            right.style.flexGrow = 1;
            right.style.flexBasis = 0;
            right.style.justifyContent = Justify.FlexEnd;
            var speeds = new[] { ("pause", "Pause  (Space)", 0), ("play", "Normal speed  (1)", 1), ("fast", "Double speed  (2)", 2) };
            foreach (var (glyph, hint, speed) in speeds)
            {
                var button = new IconButton(glyph, hint, () => _game.SetSpeed(speed));
                right.Add(button);
                _speed.Add(button);
            }
            var gap = new VisualElement();
            gap.style.width = 18;
            right.Add(gap);
            right.Add(new IconButton("menu", "Menu  (Esc)", () => OpenDialog("menu")));
            bar.Add(right);
        }

        (Label value, Label sub) Readout(Stack parent, string caption, Color color, string hint)
        {
            Stack box = Kit.VBox(0);
            box.name = caption.ToLowerInvariant();
            box.style.justifyContent = Justify.Center;
            box.style.minWidth = Kit.Compact ? 92 : 118;
            box.tooltip = hint;
            box.Add(Kit.Caption(caption));
            Label value = Kit.Label("0", Kit.Compact ? 26 : 31, color, Fonts.BodyBold);
            Label sub = Kit.Label("", 12, Palette.Muted, Fonts.Type);
            box.Add(value);
            box.Add(sub);
            parent.Add(box);
            return (value, sub);
        }

        VisualElement Divider()
        {
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.width = 1;
            line.style.height = Kit.Compact ? 56 : 64;
            line.style.backgroundColor = Palette.Alpha(Palette.Text, 0.35f);
            line.style.flexShrink = 0;
            return line;
        }

        void BuildBottom()
        {
            Stack bar = Bar(false);
            Stack row = Kit.HBox(Kit.Compact ? 14 : 24);
            row.style.flexGrow = 1;
            bar.Add(row);
            (_influence, _influenceRate) = Readout(row, "Influence", Palette.Text,
                $"Earned each month by your followers, minus {Js.Num(Balance.SiteUpkeep)} per funded site.\nSpent on every cult action. Keep a reserve for the alignment.");
            _dread = Readout(row, "Dread", Palette.Omen, "Power for the rites. Gained only by sacrifice and omens.\nSites cost 60, 100 and 160. The final rite costs 100.").value;
            _followers = Readout(row, "Followers", Palette.Cult, "Living members of the cult, in every region.").value;
            Stack doctrine = Kit.VBox(3);
            doctrine.name = "doctrine";
            doctrine.Add(Kit.Caption("Doctrine"));
            _doctrine = Kit.Button("", () => OpenDialog("doctrine"));
            _doctrine.style.minWidth = Kit.Compact ? 124 : 150;
            _doctrine.style.fontSize = 18;
            _doctrine.tooltip = "One permanent bonus per run.  (D)";
            doctrine.Add(_doctrine);
            _doctrineNote = Kit.Label("", 12, Palette.Warn, Fonts.TypeBold);
            doctrine.Add(_doctrineNote);
            row.Add(doctrine);
            row.Add(Divider());

            Stack ticker = Kit.VBox(3);
            _tickerBox = ticker;
            ticker.style.flexGrow = 1;
            ticker.style.flexShrink = 1;
            // Like Godot's expand-fill: the chronicle takes the room left, and its text wraps in it.
            ticker.style.flexBasis = 0;
            ticker.style.minWidth = 0;
            ticker.style.justifyContent = Justify.Center;
            ticker.pickingMode = PickingMode.Position;
            ticker.tooltip = "Open the chronicle  (C)";
            ticker.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0) OpenDialog("chronicle");
            });
            _tickerHead = Kit.Caption("");
            _ticker = Kit.Body("", Kit.Compact ? 16 : 17, Palette.Text, Fonts.Italic);
            _ticker.style.maxHeight = (Kit.Compact ? 16 : 17) * 2.6f;
            _ticker.style.overflow = Overflow.Hidden;
            _ticker.style.textOverflow = TextOverflow.Ellipsis;
            ticker.Add(_tickerHead);
            ticker.Add(_ticker);
            row.Add(ticker);
            VisualElement second = Divider();
            row.Add(second);
            // Short screens drop the ticker; the chronicle stays a key, a menu entry and the telegrams.
            if (Kit.Compact)
            {
                Kit.SetVisible(ticker, false);
                Kit.SetVisible(second, false);
            }

            Stack awareness = Kit.VBox(2);
            awareness.name = "awareness";
            awareness.style.alignItems = Align.Center;
            awareness.tooltip = "How much of the world knows. It climbs with the share of humanity that follows you, with public worship and with ritual sites.\nIt slows income and recruitment. At 100% the run is lost.";
            _awarenessCaption = Kit.Caption("");
            _awareness = new AwarenessGauge();
            awareness.Add(_awarenessCaption);
            awareness.Add(_awareness);
            row.Add(awareness);

            Stack sites = Kit.VBox(6);
            sites.name = "sites";
            _sitesBox = sites;
            _sitesCaption = Kit.Caption("");
            _sites = new SiteSeals();
            sites.Add(_sitesCaption);
            sites.Add(_sites);
            row.Add(sites);

            _rite = Kit.Primary("Final Rite", () => OpenDialog("alignment"));
            row.Add(_rite);
        }

        void BuildOverlays()
        {
            // Threat clippings scroll when they would reach the timetable or the ledger.
            _threatScroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _threatScroll.AddToClassList("aw-scroll");
            _threatScroll.style.position = Position.Absolute;
            _threatScroll.pickingMode = PickingMode.Ignore;
            Add(_threatScroll);
            _threats = Kit.VBox(8);
            _threats.style.width = Kit.Compact ? 250 : 310;
            _threatScroll.Add(_threats);

            _toasts = Kit.VBox(6);
            _toasts.style.position = Position.Absolute;
            _toasts.style.top = Top + 14;
            _toasts.style.width = 600;
            _toasts.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _toasts.style.translate = new Translate(-300, 0);
            _toasts.pickingMode = PickingMode.Ignore;
            Add(_toasts);

            // The ritual timetable: a headline that names the problem, then the detail.
            _banner = Kit.Panel(Palette.PaperLight, Palette.Warn, new Vector4(16, 9, 16, 10), 0.1f, 2);
            _banner.style.position = Position.Absolute;
            _banner.style.bottom = Bottom + (Kit.Compact ? 10 : 14);
            // On short screens only the headline shows; a click or tap unfolds the detail.
            _banner.pickingMode = Kit.Compact ? PickingMode.Position : PickingMode.Ignore;
            _banner.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!Kit.Compact || e.button != 0) return;
                Kit.SetVisible(_bannerText, !Kit.IsVisible(_bannerText));
                Refresh();
            });
            _bannerHead = Kit.Label("", 20, Palette.Warn, Fonts.DisplaySc);
            _bannerHead.style.unityTextAlign = TextAnchor.MiddleCenter;
            _bannerText = Kit.Body("", 14, Palette.Text);
            _bannerText.style.unityTextAlign = TextAnchor.MiddleCenter;
            Kit.SetVisible(_bannerText, !Kit.Compact);
            _banner.Add(_bannerHead);
            // During the final rite the timetable becomes a progress bar that stays on screen.
            _riteBar = new Bar(Balance.FinalMonths, Palette.Cult, 8);
            Kit.SetVisible(_riteBar, false);
            _banner.Add(_riteBar);
            _banner.Add(_bannerText);
            Add(_banner);
            _banner.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());

            _prompt = Kit.Panel(Palette.Paper, Palette.PaperEdge, new Vector4(26, 16, 26, 16));
            Kit.Frame(_prompt, Palette.Alpha(Palette.Cult, 0.6f), 5f);
            _prompt.style.position = Position.Absolute;
            _prompt.style.top = Top + 22;
            _prompt.style.width = 580;
            _prompt.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _prompt.style.translate = new Translate(-290, 0);
            _prompt.pickingMode = PickingMode.Ignore;
            Label title = Kit.Label("Choose where the dream takes root", 27, Palette.Cult, Fonts.Display);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            Label sub = Kit.Label("Pick a region on the map · small regions are easier to hold", 15, Palette.Muted, Fonts.Italic);
            sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            _prompt.Add(title);
            _prompt.Add(sub);
            Add(_prompt);

            _resetView = Kit.Button("Whole world", () => Map.ResetView());
            _resetView.tooltip = "Show the whole chart  (R)";
            _resetView.style.fontSize = 17;
            _resetView.style.position = Position.Absolute;
            Kit.SetVisible(_resetView, false);
            Add(_resetView);
        }

        void QueueLayout()
        {
            if (_layoutPending) return;
            _layoutPending = true;
            schedule.Execute(() =>
            {
                _layoutPending = false;
                LayoutOverlays();
            });
        }

        /// <summary>
        /// Overlays share the visible chart without covering one another or the region's file: the
        /// timetable at the bottom centre, the threat clippings down the left (scrolling above the
        /// timetable), the whole-world button at the top right, and a note at the lower left or, when
        /// that would cover the timetable or the clippings, at the top right below the button.
        /// </summary>
        void LayoutOverlays()
        {
            Rect screen = contentRect;
            if (screen.width <= 0 || _banner == null) return;
            float gap = Kit.Compact ? 10f : 14f;
            float mapRight = Selected != "" ? Panel.Width + 28 : 0;
            var area = new Rect(gap, Top + gap, screen.width - mapRight - gap * 2f, screen.height - Top - Bottom - gap * 2f);
            // The timetable is centred over the chart, but moves right of the threat clippings when
            // both are shown; only if that leaves it too narrow do the clippings stop above it.
            float columnWidth = Mathf.Max(_threats.resolvedStyle.width, _threats.layout.width) + 10f;
            bool hasThreats = _threats.childCount > 0;
            float left = area.x;
            if (hasThreats) left = area.x + columnWidth + 8f;
            float width = Mathf.Min(620f, area.xMax - left);
            float centred = area.x + (area.width - Mathf.Min(620f, area.width)) * 0.5f;
            if (!hasThreats || width < 320f)
            {
                left = centred;
                width = Mathf.Min(620f, area.width);
            }
            else
            {
                left = Mathf.Max(left, centred);
                width = Mathf.Min(620f, area.xMax - left);
            }
            _banner.style.left = left;
            _banner.style.width = width;
            bool bannerShown = Kit.IsVisible(_banner);
            float bannerHeight = _banner.layout.height;
            var bannerRect = new Rect(left, area.yMax - bannerHeight, width, bannerHeight);
            Vector2 button = Kit.IsVisible(_resetView) ? _resetView.layout.size : Vector2.zero;
            _resetView.style.left = area.xMax - button.x;
            _resetView.style.top = area.y;
            float belowButton = area.y + (Kit.IsVisible(_resetView) ? button.y + 8f : 0f);
            var column = new Rect(area.position, new Vector2(columnWidth, area.height));
            if (bannerShown && column.Overlaps(bannerRect)) column.height = bannerRect.y - 8f - area.y;
            _threatScroll.style.left = column.x;
            _threatScroll.style.top = column.y;
            _threatScroll.style.width = column.width;
            _threatScroll.style.height = Mathf.Min(_threats.layout.height, Mathf.Max(0f, column.height));
            _threatScroll.pickingMode = hasThreats ? PickingMode.Position : PickingMode.Ignore;
            if (_note == null) return;
            var noteSize = new Vector2(_note.resolvedStyle.width, _note.layout.height);
            var spot = new Rect(new Vector2(area.x, area.yMax - noteSize.y), noteSize);
            var clippings = new Rect(column.x, column.y, column.width, _threatScroll.resolvedStyle.height);
            bool Blocked(Rect rect) => bannerShown && rect.Overlaps(bannerRect) || hasThreats && rect.Overlaps(clippings)
                || rect.yMax > area.yMax + 1 || rect.x < area.x - 1;
            if (Kit.Compact || Blocked(spot)) spot.position = new Vector2(area.xMax - noteSize.x, belowButton);
            // With no free corner the note waits, still unread, until the chart has room for it.
            bool shown = !Blocked(spot) && noteSize.y > 0;
            _note.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            if (shown && !_game.HintSeen(_noteId))
            {
                _noting = true;
                _game.MarkHint(_noteId);
                _noting = false;
            }
            _note.style.left = spot.x;
            _note.style.top = spot.y;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IEnumerable<VisualElement> LayoutLedger => _rite.parent.Children();
        internal (VisualElement node, string name)[] LayoutOverlaysToCheck => new[]
        {
            ((VisualElement)_banner, "timetable"), ((VisualElement)_note, "note"),
            ((VisualElement)_resetView, "whole-world button"), ((VisualElement)_threatScroll, "threat clippings"),
        };
        internal VisualElement LayoutThreatScroll => _threatScroll;
        internal void ExpandLayoutTimetable() => Kit.SetVisible(_bannerText, true);
#endif

        void OnViewChanged(bool freeView)
        {
            Kit.SetVisible(_resetView, freeView);
            QueueLayout();
        }

        // --- Refresh -----------------------------------------------------------------------

        public void Refresh()
        {
            if (_game.State == null) return;
            GameState s = _game.State;
            Map.SetState(s, Selected);
            bool placing = s.Status == "placing";
            bool over = GameEngine.IsOver(s);
            int month = s.ElapsedMonths;
            _date.text = Format.Date(month);
            switch (s.Status)
            {
                case "placing": _countdown.text = $"{Balance.PreparationMonths} MONTHS TO THE ALIGNMENT"; break;
                case "playing": _countdown.text = $"{Balance.PreparationMonths - month} MONTHS TO THE ALIGNMENT"; break;
                case "alignment":
                case "awakening": _countdown.text = $"THE STARS ARE RIGHT · {Balance.Deadline - month} MONTHS LEFT"; break;
                default: _countdown.text = s.Status == "won" ? "CTHULHU AWAKENS" : "THE RUN HAS ENDED"; break;
            }
            // The deadline brightens as it nears: amber in the last three years, verdigris once aligned.
            bool near = s.Status == "playing" && Balance.PreparationMonths - month <= 36;
            _countdown.style.color = s.Status == "alignment" || s.Status == "awakening" || s.Status == "won" ? Palette.CultBright : near ? Palette.WarnBright : Palette.LampDim;
            if (s.Status == "awakening") _countdown.text = $"FINAL RITE {s.FinalProgress} / {Balance.FinalMonths} · {Balance.Deadline - month} MONTHS LEFT";
            for (int i = 0; i < _speed.Count; i++)
            {
                _speed[i].Active = s.Speed == i && !placing && !over;
                _speed[i].SetEnabled(!placing && !over);
            }

            _influence.text = Format.Count(s.Influence);
            double upkeep = Derived.SiteUpkeep(s);
            double net = Derived.NetInfluencePerMonth(s);
            _influenceRate.text = Kit.Compact ? Format.Rate(net) + " NET / MO" : Format.Rate(net) + " / MONTH" + (upkeep > 0 ? " · SITES −" + Js.Num(upkeep) : "");
            _influenceRate.style.color = net < 0 ? Palette.Danger : Palette.Muted;
            _dread.text = Format.Count(s.Dread);
            _followers.text = Format.Count(Derived.TotalFollowers(s));
            RefreshDoctrine(s, placing, over);

            LogEntry last = s.Log.Count > 0 ? s.Log[s.Log.Count - 1] : null;
            _tickerHead.text = last != null ? ("Chronicle · " + Format.Date(last.Month)).ToUpperInvariant() : "CHRONICLE";
            _ticker.text = last?.Message ?? "The world sleeps. Choose where the dream begins.";

            _awareness.Value = s.Awareness;
            _awarenessCaption.text = (Kit.Compact ? "AWARENESS  " : "WORLD AWARENESS  ") + (int)Math.Floor(s.Awareness) + "%";
            _awarenessCaption.style.color = s.Awareness >= 60 ? Palette.Danger : Palette.Muted;

            int ready = Derived.ReadySites(s);
            _sites.State = s;
            _sitesCaption.text = s.Status == "awakening" ? $"FINAL RITE  {s.FinalProgress} / {Balance.FinalMonths}" : $"RITUAL SITES  {ready} / {Balance.RequiredSites}";
            RitualForecast forecast = Forecast.Ritual(s);
            _sitesBox.tooltip = forecast.Message;
            RefreshRite(s);

            bool rite = s.Status == "awakening";
            Kit.SetVisible(_banner, (forecast.Urgent || rite) && !placing && !over);
            _bannerHead.text = Notes.ForecastHeadline(s) + (Kit.Compact && !Kit.IsVisible(_bannerText) ? "  · more" : "");
            _bannerHead.style.color = rite && Derived.ReadySites(s) >= Balance.RequiredSites ? Palette.Cult : Palette.Warn;
            _bannerText.text = forecast.Message;
            Kit.SetVisible(_riteBar, rite);
            _riteBar.Value = s.FinalProgress;
            Kit.SetVisible(_prompt, placing);
            RebuildThreats(s);
            Panel.Refresh();
            RefreshNote(s);
            QueueLayout();

            if (s.Status != _lastStatus)
            {
                _lastStatus = s.Status;
                if (s.Status == "alignment") OpenDialog("alignment");
                else if (GameEngine.IsOver(s)) OpenDialog("end");
            }
        }

        /// <summary>The doctrine button always opens the choices, but says plainly when they cannot be bought.</summary>
        void RefreshDoctrine(GameState s, bool placing, bool over)
        {
            _doctrine.SetEnabled(!placing && !over);
            _doctrineNote.text = "";
            if (s.Specialization != null)
            {
                _doctrine.text = GameData.Specializations[s.Specialization].Name;
                _doctrine.style.color = Palette.Cult;
                return;
            }
            _doctrine.text = "Choose · " + GameData.SpecializationCost;
            int shortBy = GameData.SpecializationCost - (int)Math.Floor(s.Influence);
            bool affordable = shortBy <= 0;
            _doctrine.style.color = affordable ? Palette.Text : Palette.Muted;
            if (!affordable && !placing && !over) _doctrineNote.text = shortBy + " MORE INFLUENCE";
        }

        /// <summary>During the alignment the rite button stays visible; it reads as ready only when it is.</summary>
        void RefreshRite(GameState s)
        {
            Kit.SetVisible(_rite, s.Status == "alignment");
            string reason = GameEngine.AwakeningReason(s);
            bool ready = string.IsNullOrEmpty(reason);
            _rite.EnableInClassList("aw-primary", ready);
            _rite.EnableInClassList("aw-button", !ready);
            if (!ready)
            {
                _rite.text = Kit.Compact ? "Final Rite" : "Final Rite · not ready";
                _rite.tooltip = reason;
                _rite.style.color = Palette.Muted;
                _rite.style.paddingLeft = _rite.style.paddingRight = 18;
                _rite.style.paddingTop = 9;
                _rite.style.paddingBottom = 10;
                _rite.style.fontSize = 21;
            }
            else
            {
                _rite.text = Kit.Compact ? "Begin Rite" : "Begin the Final Rite";
                _rite.tooltip = "Ready. Spend 100 Dread and hold three sites for 12 months.";
                _rite.style.color = StyleKeyword.Null;
            }
        }

        void RebuildThreats(GameState s)
        {
            _threats.Clear();
            if (GameEngine.IsOver(s)) return;
            var ids = GameData.RegionIds.Where(id => s.Regions[id].Threat != null).ToList();
            ids.Sort((a, b) => s.Regions[a].Threat.DueMonth.CompareTo(s.Regions[b].Threat.DueMonth));
            foreach (string id in ids) _threats.Add(ThreatCard(s, id));
        }

        static readonly Dictionary<string, string> KindName = new Dictionary<string, string>
        {
            ["investigation"] = "Investigation", ["raid"] = "Police Raid", ["occult"] = "Occult Hunters",
        };

        /// <summary>A press clipping: the headline names the threat, the stamps answer it.</summary>
        VisualElement ThreatCard(GameState s, string id)
        {
            RegionState r = s.Regions[id];
            Threat threat = r.Threat;
            int months = threat.DueMonth - s.ElapsedMonths;
            bool urgent = months <= 3;
            Stack card = Kit.Panel(Palette.PaperLight, urgent ? Palette.Danger : Palette.PaperEdge, new Vector4(12, 9, 12, 10), 0.1f, 6);
            if (urgent) card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 2;
            card.tooltip = GameData.ThreatNames[threat.Kind] + ".\nIf ignored: " + Threats.Describe(r);
            card.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0 && !(e.target is Button)) Select(id);
            });
            Stack row = Kit.HBox(10);
            row.style.alignItems = Align.FlexStart;
            card.Add(row);
            Stack text = Kit.VBox(3);
            text.style.flexGrow = 1;
            text.style.flexShrink = 1;
            text.Add(Kit.Label(KindName[threat.Kind], Kit.Compact ? 19 : 21, Palette.Danger, Fonts.DisplaySc));
            // Short screens keep the clipping to a headline; its consequences are in the region's file.
            if (Kit.Compact) text.Add(Kit.Caption(GameData.Regions[id].Name, Palette.Muted));
            else
            {
                text.Add(Kit.Caption(GameData.Regions[id].Name + " · if ignored", Palette.Muted));
                foreach (string effect in Notes.ThreatEffects(r)) text.Add(Kit.Label(effect, 15, Palette.Text, Fonts.Italic));
            }
            row.Add(text);
            Stack right = Kit.VBox(0);
            right.style.alignItems = Align.Center;
            right.Add(Kit.Label(months.ToString(), Kit.Compact ? 26 : 30, Palette.Danger, Fonts.BodyBold));
            right.Add(Kit.Caption(months != 1 ? "months" : "month", Palette.Danger));
            row.Add(right);
            // Three answers, each paid in a different currency. The region's file says what else each costs.
            var stamps = new Flow(6, 6);
            foreach (string action in Notes.Responses)
            {
                ActionPreview preview = GameEngine.PreviewAction(s, action, id);
                string chosen = action;
                Button stamp = Kit.Stamp(Notes.ResponseStamp(preview, action), () => _game.Act(chosen, id), Accent.Danger);
                stamp.style.fontSize = 13;
                stamp.SetEnabled(preview.Ok);
                stamp.tooltip = preview.Ok ? GameData.Actions[action].Name + ". " + Notes.ResponseNote(r, preview, action) + "." : preview.Reason;
                if (Kit.Touch) stamp.style.minHeight = 40;
                stamps.Add(stamp);
            }
            card.Add(stamps);
            return card;
        }

        void OnEvents(List<GameEvent> entries)
        {
            foreach (GameEvent entry in entries)
            {
                string kind = entry.Refused ? "refused" : GameController.Classify(entry.Message);
                if (kind == "info" || kind == "faded") continue;
                Toast(entry.Message, kind);
            }
        }

        /// <summary>A telegram strip: typed capitals on yellowed paper, marked in the colour of its news.</summary>
        void Toast(string message, string kind)
        {
            var accents = new Dictionary<string, Color>
            {
                ["threat"] = Palette.Danger, ["loss"] = Palette.Danger, ["omen"] = Palette.Omen, ["site"] = Palette.Cult,
                ["phase"] = Palette.Cult, ["end"] = Palette.Text, ["refused"] = Palette.Warn,
            };
            Color accent = accents.TryGetValue(kind, out Color found) ? found : Palette.Muted;
            Stack toast = Kit.Panel(Palette.Hex("e3d5a6"), Palette.Alpha(Palette.PaperEdge, 0.8f), new Vector4(12, 8, 14, 9));
            toast.pickingMode = PickingMode.Ignore;
            Stack row = Kit.HBox(12);
            row.style.alignItems = Align.Stretch;
            var stripe = new VisualElement();
            stripe.style.width = 4;
            stripe.style.backgroundColor = accent;
            row.Add(stripe);
            Label text = Kit.Body(message.ToUpperInvariant(), 14, Palette.Text, Fonts.Type);
            text.style.flexGrow = 1;
            text.style.flexShrink = 1;
            row.Add(text);
            toast.Add(row);
            _toasts.Add(toast);
            while (_toasts.childCount > 3) _toasts.RemoveAt(0);
            float shown = kind != "refused" ? 4.5f : 2.5f;
            toast.style.opacity = Kit.ReducedMotion ? 1f : 0f;
            float started = Time.unscaledTime;
            IVisualElementScheduledItem fade = null;
            fade = toast.schedule.Execute(() =>
            {
                float t = Time.unscaledTime - started;
                float fadeIn = Kit.ReducedMotion ? 0f : 0.18f;
                float fadeOut = Kit.ReducedMotion ? 0.01f : 0.5f;
                if (t < fadeIn) toast.style.opacity = t / fadeIn;
                else if (t < fadeIn + shown) toast.style.opacity = 1f;
                else if (t < fadeIn + shown + fadeOut) toast.style.opacity = 1f - (t - fadeIn - shown) / fadeOut;
                else
                {
                    fade.Pause();
                    toast.RemoveFromHierarchy();
                }
            }).Every(16);
        }

        // --- First-time notes --------------------------------------------------------------

        /// <summary>
        /// One short note the first time a situation comes up, in the lower-left corner of the chart.
        /// It stays until dismissed and never pauses the clock.
        /// </summary>
        void RefreshNote(GameState s)
        {
            // Marking a note as seen saves the game, which refreshes this screen; ignore that echo.
            if (_noting) return;
            bool showing = _note != null;
            string id = Notes.NextHint(s, h => _game.HintSeen(h) || h == _noteId);
            if (id == "") return;
            // A note left open for a while gives way to the next one instead of holding it back.
            if (showing)
            {
                if (Time.unscaledTime - _noteAt < 12f) return;
                _note.RemoveFromHierarchy();
                _note = null;
            }
            _noting = true;
            _noteAt = Time.unscaledTime;
            _noteId = id;
            var hint = Notes.Hints[id];
            _note = Kit.Panel(Palette.PaperLight, Palette.Cult, new Vector4(16, 12, 14, 12), 0.1f, 4);
            _note.style.borderLeftWidth = 4;
            _note.style.position = Position.Absolute;
            _note.style.width = Kit.Compact ? 340 : 380;
            _note.style.visibility = Visibility.Hidden;
            Stack head = Kit.HBox(8);
            head.style.alignItems = Align.FlexStart;
            Stack titles = Kit.VBox(0);
            titles.style.flexGrow = 1;
            titles.style.flexShrink = 1;
            titles.Add(Kit.Caption("Note", Palette.Cult));
            titles.Add(Kit.Label(hint.title, 20, Palette.Text, Fonts.DisplaySc));
            head.Add(titles);
            head.Add(new IconButton("close", "Dismiss", DismissNote, true));
            _note.Add(head);
            _note.Add(Kit.Body(hint.text, 15, Palette.Text));
            Button ok = Kit.Link(Kit.Touch ? "GOT IT" : "GOT IT  (N)", DismissNote);
            ok.style.color = Palette.Cult;
            _note.Add(ok);
            Insert(IndexOf(Panel), _note);
            _note.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
            _noting = false;
            QueueLayout();
            if (!Kit.ReducedMotion)
            {
                _note.style.opacity = 0;
                float started = Time.unscaledTime;
                Stack fading = _note;
                IVisualElementScheduledItem fade = null;
                fade = fading.schedule.Execute(() =>
                {
                    float t = (Time.unscaledTime - started) / 0.25f;
                    fading.style.opacity = Mathf.Clamp01(t);
                    if (t >= 1) fade.Pause();
                }).Every(16);
            }
        }

        void DismissNote()
        {
            string id = _noteId;
            _note?.RemoveFromHierarchy();
            _note = null;
            _noteId = "";
            if (id != "") _game.MarkHint(id);
            schedule.Execute(Refresh);
        }
    }
}
