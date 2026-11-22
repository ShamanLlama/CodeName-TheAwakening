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

        Label _date, _countdown, _influence, _influenceRate, _dread, _followers, _talentsNote, _capstoneSub, _tickerHead, _ticker;
        readonly List<IconButton> _speed = new List<IconButton>();
        Button _talents, _capstoneInvoke, _rite, _resetView, _concede, _bannerToggle;
        Label _awarenessCaption, _sitesCaption, _bannerHead, _bannerText, _bannerCause, _riteHaltedText, _marginLine;
        AwarenessGauge _awareness;
        SiteSeals _sites;
        VisualElement _sitesBox, _tickerBox;
        Stack _capstoneBox;
        string _invokableId = "";
        Stack _threats;
        ScrollView _threatScroll;
        Stack _toasts;
        Stack _banner, _bannerHeadRow, _riteHaltedBox, _marginBox, _siteDetail, _prompt;
        Flow _marginChips;
        Bar _riteBar;
        bool _bannerExpanded;
        int _selectedSiteSlot = -1;
        string _marginShown = "";
        Stack _goals;
        string _goalsShown = "";
        Stack _note;
        string _noteId = "";
        float _noteAt;
        bool _noting;
        string _lastStatus = "";
        bool _layoutPending;
        int _threatOffset;
        // All options retain independent geometry, including while hidden. Fitting never
        // measures last frame's degraded stack, so it can recover without oscillation.
        readonly List<Stack> _threatOptions = new List<Stack>();
        int _threatOption = -1;
        float _threatAreaHeight;
        string _tickerMessage = "";
        const float TickerHeight = 42;

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
            Map.LabelObstacles = VisibleMapOverlays;
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
                case KeyCode.T:
                case KeyCode.D:
                    if (_game.State.Status != "placing") OpenDialog("talents");
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
            _date = Kit.Label("", Kit.Compact ? 23 : 25, Palette.Lamp, Fonts.Body);
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
            box.style.minWidth = Kit.Compact ? 86 : 118;
            box.tooltip = hint;
            box.Add(Kit.Caption(caption));
            Label value = Kit.Label("0", Kit.Compact ? 26 : 31, color, Fonts.BodyBold);
            Label sub = Kit.Label("", 12, Palette.Muted, Fonts.Type);
            sub.style.height = Kit.Compact ? 20 : 16;
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
            Stack row = Kit.HBox(Kit.Compact ? 11 : 22);
            row.style.flexGrow = 1;
            row.style.alignItems = Align.Center;
            bar.Add(row);
            (_influence, _influenceRate) = Readout(row, "Influence", Palette.Text,
                $"Earned each month by your followers, minus {Js.Num(Balance.SiteUpkeep)} per funded site.\nSpent on every cult action. Keep a reserve for the alignment.");
            _dread = Readout(row, "Dread", Palette.Omen, "Power for the rites. Gained only by sacrifice and omens.\nSites cost 60, 100 and 160. The final rite costs 100.").value;
            _followers = Readout(row, "Followers", Palette.Cult, "Living members of the cult, in every region.").value;
            Stack talents = Kit.VBox(3);
            talents.name = "talents";
            talents.Add(Kit.Caption("Talents"));
            _talents = Kit.Button("", () => OpenDialog("talents"));
            _talents.name = "talents-button";
            _talents.style.minWidth = Kit.Compact ? 112 : 148;
            _talents.style.fontSize = Kit.Compact ? 16 : 18;
            Fonts.Apply(_talents, Fonts.BodyBold);
            _talents.tooltip = "Open the cult's talent tree.  (T)";
            talents.Add(_talents);
            _talentsNote = Kit.Label("", 12, Palette.Warn, Fonts.TypeBold);
            _talentsNote.name = "talents-note";
            talents.Add(_talentsNote);
            row.Add(talents);

            _capstoneBox = Kit.VBox(2);
            _capstoneBox.name = "capstone-quick";
            _capstoneBox.Add(Kit.Caption("Capstone", Palette.Omen));
            Action openInvoke = () =>
            {
                if (_invokableId != "") Dialogs.OpenInvoke(_invokableId);
            };
            _capstoneInvoke = Kit.Stamp("INVOKE", openInvoke, Accent.Omen);
            _capstoneInvoke.name = "ledger-invoke-capstone";
            _capstoneInvoke.userData = openInvoke;
            _capstoneInvoke.style.fontSize = Kit.Compact ? 12 : 13;
            _capstoneInvoke.style.paddingLeft = _capstoneInvoke.style.paddingRight = Kit.Compact ? 6 : 10;
            _capstoneInvoke.style.minHeight = Kit.Touch ? 34 : 28;
            _capstoneBox.Add(_capstoneInvoke);
            _capstoneSub = Kit.Label(Kit.Compact ? "FORGETS" : "WORLD FORGETS", 10, Palette.Omen, Fonts.TypeBold);
            _capstoneBox.Add(_capstoneSub);
            Kit.SetVisible(_capstoneBox, false);
            row.Add(_capstoneBox);

            row.Add(Divider());

            Stack ticker = Kit.VBox(3);
            _tickerBox = ticker;
            ticker.style.flexGrow = 1;
            ticker.style.flexShrink = 1;
            // Like Godot's expand-fill: the chronicle takes the room left, and its text wraps in it.
            ticker.style.flexBasis = 0;
            ticker.style.minWidth = 0;
            ticker.style.justifyContent = Justify.Center;
            ticker.style.maxHeight = Bottom - 12;
            ticker.style.overflow = Overflow.Hidden;
            ticker.pickingMode = PickingMode.Position;
            ticker.tooltip = "Open the chronicle  (C)";
            ticker.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0) OpenDialog("chronicle");
            });
            _tickerHead = Kit.Caption("");
            _ticker = Kit.Body("", Kit.Compact ? 16 : 17, Palette.Text, Fonts.Italic);
            _ticker.style.whiteSpace = WhiteSpace.Normal;
            _ticker.style.maxHeight = TickerHeight;
            _ticker.style.overflow = Overflow.Hidden;
            _ticker.style.textOverflow = TextOverflow.Clip;
            _ticker.RegisterCallback<GeometryChangedEvent>(_ => FitTicker());
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
            _rite.style.flexShrink = 0;
            row.Add(_rite);
        }

        void BuildOverlays()
        {
            // Threat clippings scroll when they would reach the timetable or the ledger.
            _threatScroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _threatScroll.AddToClassList("aw-scroll");
            _threatScroll.style.position = Position.Absolute;
            _threatScroll.pickingMode = PickingMode.Ignore;
            _threatScroll.RegisterCallback<GeometryChangedEvent>(_ => Map.RefreshLabels());
            Add(_threatScroll);
            _threats = Kit.VBox(8);
            _threats.name = "threat-stack";
            _threats.style.width = Kit.Compact ? 250 : 310;
            _threatScroll.Add(_threats);
            _threats.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());

            _toasts = Kit.VBox(6);
            _toasts.style.position = Position.Absolute;
            _toasts.style.top = Top + 14;
            _toasts.style.width = 600;
            _toasts.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _toasts.style.translate = new Translate(-300, 0);
            _toasts.pickingMode = PickingMode.Ignore;
            Add(_toasts);

            // The ritual timetable and rite margin band: compact when calm, louder as margin shrinks.
            _banner = Kit.Panel(Palette.PaperLight, Palette.Warn, Kit.Compact ? new Vector4(8, 4, 8, 4) : new Vector4(14, 7, 14, 8), 0.1f, Kit.Compact ? 2 : 4);
            _banner.name = "rite-banner";
            _banner.style.position = Position.Absolute;
            _banner.style.bottom = Bottom + (Kit.Compact ? 10 : 14);
            _banner.pickingMode = PickingMode.Position;
            _bannerHeadRow = Kit.HBox(10);
            _bannerHeadRow.style.alignItems = Align.Center;
            _bannerHead = Kit.Label("", Kit.Compact ? 16 : 19, Palette.Warn, Fonts.BodyBold);
            _bannerHead.style.whiteSpace = WhiteSpace.Normal;
            _bannerHead.style.flexGrow = 1;
            _bannerHead.style.flexShrink = 1;
            _bannerHead.style.unityTextAlign = TextAnchor.MiddleLeft;
            Action concedeAction = () => OpenDialog("concede");
            _concede = Kit.Stamp("LET THE STARS PASS", concedeAction, Accent.Danger);
            _concede.name = "concede-button";
            _concede.userData = concedeAction;
            _concede.style.fontSize = Kit.Compact ? 12 : 13;
            _concede.style.flexShrink = 0;
            if (Kit.Touch) _concede.style.minHeight = 32;
            _concede.tooltip = "End this run as a loss now that the final rite can no longer finish.";
            Kit.SetVisible(_concede, false);
            Action toggleBanner = () =>
            {
                _bannerExpanded = !_bannerExpanded;
                Refresh();
            };
            _bannerToggle = Kit.Link("MORE", toggleBanner);
            _bannerToggle.userData = toggleBanner;
            _bannerToggle.style.flexShrink = 0;
            Kit.SetVisible(_bannerToggle, false);
            _bannerHeadRow.Add(_bannerHead);
            _bannerHeadRow.Add(_concede);
            _bannerHeadRow.Add(_bannerToggle);
            _banner.Add(_bannerHeadRow);
            // During the final rite the timetable becomes a progress bar that stays on screen.
            _riteBar = new Bar(Balance.FinalMonths, Palette.Cult, 8);
            Kit.SetVisible(_riteBar, false);
            _banner.Add(_riteBar);
            _riteHaltedBox = Kit.VBox(1);
            _riteHaltedBox.name = "rite-halted-box";
            _riteHaltedBox.style.backgroundColor = Palette.Alpha(Palette.Danger, 0.12f);
            _riteHaltedBox.style.borderLeftWidth = 3;
            _riteHaltedBox.style.borderLeftColor = Palette.Danger;
            _riteHaltedBox.style.paddingLeft = _riteHaltedBox.style.paddingRight = 8;
            _riteHaltedBox.style.paddingTop = _riteHaltedBox.style.paddingBottom = Kit.Compact ? 2 : 4;
            _riteHaltedText = Kit.Body("", Kit.Compact ? 11 : 13, Palette.Danger, Fonts.BodyBold);
            _riteHaltedText.name = "rite-halted-reasons";
            _riteHaltedBox.Add(_riteHaltedText);
            Kit.SetVisible(_riteHaltedBox, false);
            _banner.Add(_riteHaltedBox);
            _bannerCause = Kit.Body("", Kit.Compact ? 12 : 14, Palette.Danger, Fonts.BodyBold);
            _bannerCause.name = "rite-doomed-cause";
            Kit.SetVisible(_bannerCause, false);
            _banner.Add(_bannerCause);
            _marginBox = Kit.VBox(Kit.Compact ? 2 : 4);
            _marginBox.name = "rite-margin";
            _marginLine = Kit.Body("", Kit.Compact ? 11 : 13, Palette.Text, Fonts.TypeBold);
            _marginLine.name = "rite-margin-line";
            _marginChips = new Flow(Kit.Compact ? 4 : 6, Kit.Compact ? 2 : 4);
            _marginChips.name = "rite-margin-sites";
            _siteDetail = Kit.HBox(8);
            _siteDetail.name = "rite-site-detail";
            _siteDetail.style.alignItems = Align.Center;
            Kit.SetVisible(_siteDetail, false);
            _marginBox.Add(_marginLine);
            _marginBox.Add(_marginChips);
            _marginBox.Add(_siteDetail);
            _banner.Add(_marginBox);
            _bannerText = Kit.Body("", Kit.Compact ? 11 : 13, Palette.Muted);
            _bannerText.name = "rite-forecast-text";
            Kit.SetVisible(_bannerText, !Kit.Compact);
            _banner.Add(_bannerText);
            _banner.tooltip = "Ritual timetable and rite margin";
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
            Label sub = Kit.Body("Pick a region on the map · populous regions recruit well; small regions hold sites cheaply", 15, Palette.Muted, Fonts.Italic);
            sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            _prompt.Add(title);
            _prompt.Add(sub);
            Add(_prompt);

            _goals = Kit.Panel(Palette.PaperLight, Palette.PaperEdge, new Vector4(12, 9, 12, 10), 0.1f, 3);
            _goals.name = "first-run-goals";
            _goals.style.borderLeftWidth = 4;
            _goals.style.position = Position.Absolute;
            _goals.style.width = Kit.Compact ? 276 : 292;
            _goals.style.visibility = Visibility.Hidden;
            Kit.SetVisible(_goals, false);
            _goals.RegisterCallback<GeometryChangedEvent>(_ => { QueueLayout(); Map.RefreshLabels(); });
            Add(_goals);

            _resetView = Kit.Button("Whole world", () => Map.ResetView());
            _resetView.tooltip = "Show the whole chart  (R)";
            _resetView.style.fontSize = 17;
            _resetView.style.position = Position.Absolute;
            _resetView.RegisterCallback<GeometryChangedEvent>(_ => Map.RefreshLabels());
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

        /// <summary>The timetable owns a bottom strip; clippings and notes share the chart above it.</summary>
        void LayoutOverlays()
        {
            Rect screen = contentRect;
            if (screen.width <= 0 || _banner == null) return;
            float gap = Kit.Compact ? 10f : 14f;
            float mapRight = Selected != "" ? Panel.Width + 28 : 0;
            float availableWidth = screen.width - mapRight - gap * 2;
            _banner.style.left = gap;
            _banner.style.width = availableWidth;
            float bannerHeight = _banner.layout.height;
            if (float.IsNaN(bannerHeight)) bannerHeight = Kit.Compact ? 64 : 90;
            float reserved = Kit.IsVisible(_banner) && _banner.resolvedStyle.visibility == Visibility.Visible ? bannerHeight + gap : 0;
            Map.style.bottom = Bottom + 8 + reserved;
            var area = new Rect(gap, Top + gap, availableWidth, screen.height - Top - Bottom - gap * 2 - reserved);
            float columnWidth = (Kit.Compact ? 250 : 310) + 12;
            _threatAreaHeight = area.height;
            FitTicker();
            bool hasThreats = _threatOptions.Count > 0 && _threatOptions[0].childCount > 0;
            int option = Kit.Compact ? 1 : 0;
            while (option < _threatOptions.Count - 1 && _threatOptions[option].layout.height > area.height)
                option++;
            float threatHeight = _threatOptions.Count > 0 ? _threatOptions[option].layout.height : 0;
            if (float.IsNaN(threatHeight)) threatHeight = 0;
            _threatOption = option;
            for (int i = 0; i < _threatOptions.Count; i++)
                _threatOptions[i].style.visibility = i == option ? Visibility.Visible : Visibility.Hidden;
            _threats.style.height = threatHeight;
            _threatScroll.style.left = area.x;
            _threatScroll.style.top = area.y;
            _threatScroll.style.width = columnWidth;
            _threatScroll.style.height = threatHeight;
            _threatScroll.pickingMode = hasThreats ? PickingMode.Position : PickingMode.Ignore;
            Vector2 button = Kit.IsVisible(_resetView) ? _resetView.layout.size : Vector2.zero;
            bool hasReset = Kit.IsVisible(_resetView) && button.x > 0 && button.y > 0;
            _resetView.style.left = area.xMax - button.x;
            _resetView.style.top = area.y;
            float belowButton = area.y + (hasReset ? button.y + 8 : 0);
            var clippings = new Rect(area.x, area.y, columnWidth, threatHeight);
            var resetRect = hasReset ? new Rect(area.xMax - button.x, area.y, button.x, button.y) : Rect.zero;
            bool Blocked(Rect rect)
            {
                if (rect.width <= 0 || rect.height <= 0 || float.IsNaN(rect.width) || float.IsNaN(rect.height)) return true;
                if (rect.yMax > area.yMax || rect.yMin < area.y || rect.xMin < area.x || rect.xMax > area.xMax) return true;
                Rect padded = Draw2D.Grow(rect, 2f);
                if (hasThreats && padded.Overlaps(clippings)) return true;
                if (hasReset && padded.Overlaps(resetRect)) return true;
                return false;
            }
            Rect noteRect = Rect.zero;
            bool noteShown = false;
            if (_note != null)
            {
                var noteSize = new Vector2(_note.resolvedStyle.width, _note.layout.height);
                var spot = new Rect(new Vector2(area.x, area.yMax - noteSize.y), noteSize);
                if (Kit.Compact || Blocked(spot)) spot.position = new Vector2(area.xMax - noteSize.x, belowButton);
                noteShown = !Blocked(spot);
                _note.style.visibility = noteShown ? Visibility.Visible : Visibility.Hidden;
                if (noteShown)
                {
                    noteRect = Draw2D.Grow(spot, 2f);
                    if (!_game.HintSeen(_noteId))
                    {
                        _noting = true;
                        _game.MarkHint(_noteId);
                        _noting = false;
                    }
                    _note.style.left = spot.x;
                    _note.style.top = spot.y;
                }
            }
            if (_goals != null && Kit.IsVisible(_goals))
            {
                if (Kit.Compact && noteShown)
                {
                    _goals.style.visibility = Visibility.Hidden;
                }
                else
                {
                    var goalsSize = new Vector2(_goals.resolvedStyle.width, _goals.layout.height);
                    bool GoalsFit(Rect rect) => !Blocked(rect) && (!noteShown || !Draw2D.Grow(rect, 2f).Overlaps(noteRect));
                    Rect chosen = new Rect(new Vector2(area.xMax - goalsSize.x, belowButton), goalsSize);
                    bool goalsShown = GoalsFit(chosen);
                    if (!goalsShown && noteShown)
                    {
                        chosen.position = new Vector2(area.xMax - goalsSize.x, noteRect.yMax + 6);
                        goalsShown = GoalsFit(chosen);
                    }
                    if (!goalsShown)
                    {
                        chosen.position = new Vector2(area.x, area.yMax - goalsSize.y);
                        goalsShown = GoalsFit(chosen);
                    }
                    _goals.style.visibility = goalsShown ? Visibility.Visible : Visibility.Hidden;
                    if (goalsShown)
                    {
                        _goals.style.left = chosen.x;
                        _goals.style.top = chosen.y;
                    }
                }
            }
            Map.RefreshLabels();
            Map.schedule.Execute(Map.RefreshLabels);
        }

        internal IEnumerable<VisualElement> VisibleMapOverlays()
        {
            yield return _threatScroll;
            yield return _note;
            yield return _goals;
            yield return _resetView;
            yield return _prompt;
            yield return _toasts;
            yield return Panel;
        }

        public void SetModalOpen(bool open)
        {
            _banner.style.visibility = open ? Visibility.Hidden : Visibility.Visible;
            QueueLayout();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal IEnumerable<VisualElement> LayoutLedger => _rite.parent.Children();
        internal (VisualElement node, string name)[] LayoutOverlaysToCheck => new[]
        {
            ((VisualElement)_banner, "timetable"), ((VisualElement)_note, "note"),
            ((VisualElement)_goals, "first-run goals"),
            ((VisualElement)_resetView, "whole-world button"), ((VisualElement)_threatScroll, "threat clippings"),
        };
        internal VisualElement LayoutThreatScroll => _threatScroll;
        internal IEnumerable<VisualElement> LayoutThreatCards => _threatOptions.SelectMany(option => option.Children());
        internal VisualElement LayoutLedgerBar => _rite.parent.parent;
        internal Label LayoutTicker => _ticker;
        internal int LayoutThreatOption => _threatOption;
        internal float[] LayoutThreatHeights => _threatOptions.Select(option => option.layout.height).ToArray();
        internal float LayoutThreatAvailableHeight => _threatAreaHeight;
        internal void LayoutSetTicker(LogEntry entry) => SetTicker(entry);
        internal VisualElement LayoutBanner => _banner;
        internal void ExpandLayoutTimetable()
        {
            _bannerExpanded = true;
            Kit.SetVisible(_bannerText, true);
        }
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
            RiteStatus riteStatus = Forecast.Rite(s);
            bool riteHalted = s.Status == "awakening" && !riteStatus.Advancing && riteStatus.Progress < riteStatus.Required;
            // The deadline brightens as it nears: amber in the last three years, verdigris once aligned.
            bool near = s.Status == "playing" && Balance.PreparationMonths - month <= 36;
            _countdown.style.color = riteHalted ? Palette.DangerBright
                : s.Status == "alignment" || s.Status == "awakening" || s.Status == "won" ? Palette.CultBright
                : near ? Palette.WarnBright : Palette.LampDim;
            if (s.Status == "awakening")
            {
                string prefix = riteHalted ? "RITE HALTED" : GameEngine.InProcession(s) ? "PROCESSION" : "FINAL RITE";
                _countdown.text = $"{prefix} {riteStatus.Progress} / {riteStatus.Required} · {Balance.Deadline - month} MONTHS LEFT";
            }
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
            RefreshTalents(s, placing, over);

            LogEntry last = s.Log.Count > 0 ? s.Log[s.Log.Count - 1] : null;
            SetTicker(last);

            _awareness.Value = s.Awareness;
            _awarenessCaption.text = (Kit.Compact ? "AWARENESS  " : "WORLD AWARENESS  ") + (int)Math.Floor(s.Awareness) + "%";
            _awarenessCaption.style.color = s.Awareness >= 60 ? Palette.Danger : Palette.Muted;

            int ready = Derived.ReadySites(s);
            _sites.State = s;
            _sitesCaption.text = s.Status == "awakening"
                ? (riteHalted ? $"RITE HALTED  {riteStatus.Progress} / {riteStatus.Required}" : $"FINAL RITE  {riteStatus.Progress} / {riteStatus.Required}")
                : $"RITUAL SITES  {ready} / {Balance.RequiredSites}";
            _sitesCaption.style.color = riteHalted ? Palette.Danger : Palette.Muted;
            RitualForecast forecast = Forecast.Ritual(s);
            _sitesBox.tooltip = riteHalted
                ? "RITE HALTED — " + Notes.RiteHaltedSummary(riteStatus) + "\n" + Format.ForecastCopy(forecast.Message)
                : Format.ForecastCopy(forecast.Message);
            RefreshRite(s);
            // The alignment decision gets the chronicle's space. Full entries remain in its sheet.
            Kit.SetVisible(_tickerBox, !Kit.Compact && s.Status != "alignment");

            RefreshBanner(s, forecast, riteStatus, placing, over);
            SetModalOpen(Dialogs != null && Dialogs.IsOpen);
            Kit.SetVisible(_prompt, placing);
            RebuildThreats(s);
            Panel.Refresh();
            RefreshGoals(s);
            RefreshNote(s);
            QueueLayout();

            if (s.Status != _lastStatus)
            {
                _lastStatus = s.Status;
                if (s.Status == "alignment") OpenDialog("alignment");
                else if (GameEngine.IsOver(s)) OpenDialog("end");
            }
        }

        void RefreshBanner(GameState s, RitualForecast forecast, RiteStatus riteStatus, bool placing, bool over)
        {
            RiteMargin margin = Forecast.Margin(s);
            string level = Notes.RiteBandLevel(s, margin, forecast);
            bool visible = level != "none" && !placing && !over;
            Kit.SetVisible(_banner, visible);
            if (!visible)
            {
                _marginShown = "";
                _selectedSiteSlot = -1;
                return;
            }
            bool rite = level == "rite";
            bool doomed = level == "doomed";
            bool quiet = level == "quiet";
            bool canConcede = GameEngine.CanConcede(s);
            bool riteHalted = rite && !riteStatus.Advancing && riteStatus.Progress < riteStatus.Required;
            bool riteHealthy = margin.Fits && !riteHalted;

            Color fill = quiet ? Palette.Paper : Palette.PaperLight;
            Color edge = doomed || riteHalted ? Palette.Danger : rite ? Palette.Cult : quiet ? Palette.PaperEdge : Palette.Warn;
            int borderWidth = quiet ? 1 : 2;
            int leftWidth = doomed || riteHalted ? 6 : quiet ? 3 : 5;
            _banner.style.backgroundColor = fill;
            _banner.style.borderTopColor = _banner.style.borderBottomColor = _banner.style.borderRightColor = _banner.style.borderLeftColor = edge;
            _banner.style.borderTopWidth = _banner.style.borderBottomWidth = _banner.style.borderRightWidth = borderWidth;
            _banner.style.borderLeftWidth = leftWidth;

            Kit.SetVisible(_bannerHeadRow, !quiet || canConcede);
            _bannerHead.text = doomed ? Notes.RiteDoomedHeadline : Notes.ForecastHeadline(s);
            _bannerHead.style.color = doomed || riteHalted ? Palette.Danger : rite ? Palette.Cult : Palette.Warn;
            Kit.SetVisible(_concede, canConcede);

            Kit.SetVisible(_riteBar, rite);
            _riteBar.Max = riteStatus.Required;
            _riteBar.Value = riteStatus.Progress;
            _riteBar.FillColor = riteHealthy ? Palette.Cult : Palette.Danger;

            Kit.SetVisible(_riteHaltedBox, riteHalted);
            string haltedSummary = riteHalted ? Notes.RiteHaltedSummary(riteStatus, Kit.Compact) : "";
            if (riteHalted)
                _riteHaltedText.text = string.IsNullOrEmpty(haltedSummary)
                    ? "RITE HALTED — restore three supported sites to resume."
                    : "RITE HALTED — " + haltedSummary;

            Kit.SetVisible(_bannerCause, doomed);
            if (doomed) _bannerCause.text = Notes.RiteDoomedCause(s, margin);

            string marginText = Notes.RiteMarginLine(s, margin, Kit.Compact);
            _marginLine.text = marginText;
            _marginLine.style.color = doomed || riteHalted ? Palette.Danger : (margin.EarliestStart >= 0 && margin.Margin <= 6 && !margin.Started) ? Palette.Warn : Palette.Text;

            string forecastCopy = Format.ForecastCopy(forecast.Message);
            bool hasForecastDetail = (forecast.Urgent || rite) && !doomed && !quiet;
            Kit.SetVisible(_bannerToggle, Kit.Compact && hasForecastDetail);
            _bannerToggle.text = _bannerExpanded ? "LESS" : "MORE";
            bool showForecastText = hasForecastDetail && (!Kit.Compact || _bannerExpanded);
            Kit.SetVisible(_bannerText, showForecastText);
            _bannerText.text = forecastCopy;
            _banner.tooltip = doomed ? Notes.RiteDoomedHeadline + " — " + _bannerCause.text : riteHalted ? _riteHaltedText.text + "\n" + forecastCopy : forecastCopy;

            int dread = (int)Math.Floor(s.Dread);
            bool hasProcession = GameEngine.HasTalent(s, "great_procession");
            int procRegions = hasProcession ? GameEngine.ProcessionRegions(s) : 0;
            var keyParts = new List<string>
            {
                level, margin.Started.ToString(), margin.Fits.ToString(), margin.EarliestStart.ToString(), margin.LatestStart.ToString(),
                margin.Margin.ToString(), dread.ToString(), _selectedSiteSlot.ToString(), Kit.Compact.ToString(), s.Status,
                riteStatus.Progress.ToString(), riteStatus.Required.ToString(), riteStatus.Advancing.ToString(), haltedSummary,
                hasProcession.ToString(), procRegions.ToString(),
            };
            for (int i = 0; i < Balance.RequiredSites; i++)
            {
                if (i < margin.Sites.Count)
                {
                    SiteMargin sm = margin.Sites[i];
                    RegionState r = s.Regions[sm.Region];
                    keyParts.Add($"{sm.Region}:{sm.MonthsLeft}:{sm.Followers}/{sm.Required}:{sm.Supported}:{sm.Scattered}:{r.Site.Dormant}:{(sm.Scattered ? r.ScatteredUntil - s.ElapsedMonths : 0)}");
                }
                else keyParts.Add("empty");
            }
            string key = string.Join("|", keyParts);
            if (key == _marginShown && _marginChips.childCount > 0) return;
            _marginShown = key;

            _marginChips.Clear();
            var shownRegions = new HashSet<string>();
            for (int slot = 0; slot < Balance.RequiredSites; slot++)
            {
                int index = slot;
                Action toggleSlot = () =>
                {
                    _selectedSiteSlot = _selectedSiteSlot == index ? -1 : index;
                    Refresh();
                };
                var chip = new Button(toggleSlot) { focusable = false, name = "rite-site-" + slot };
                chip.userData = toggleSlot;
                chip.AddToClassList("aw-chip");
                chip.EnableInClassList("aw-on", _selectedSiteSlot == slot);
                if (slot < margin.Sites.Count)
                {
                    SiteMargin site = margin.Sites[slot];
                    shownRegions.Add(site.Region);
                    RegionState r = s.Regions[site.Region];
                    chip.text = Notes.SiteChipText(s, site, Kit.Compact);
                    chip.tooltip = Notes.SiteMarginDetail(s, site) + (Kit.Touch ? "" : "\nClick to pin detail.");
                    Color chipColor = r.Site.Dormant || site.Scattered || !site.Supported ? Palette.Danger : site.MonthsLeft == 0 ? Palette.Cult : Palette.Omen;
                    chip.style.color = chipColor;
                    chip.style.borderTopColor = chip.style.borderBottomColor = chip.style.borderLeftColor = chip.style.borderRightColor = Palette.Alpha(chipColor, 0.7f);
                }
                else
                {
                    chip.text = Kit.Compact ? $"○ Site {slot + 1}" : $"○ Site {slot + 1} · unfunded";
                    chip.tooltip = Notes.UnfundedSiteDetail(slot + 1, s);
                    chip.style.color = Palette.Muted;
                }
                _marginChips.Add(chip);
            }
            if (riteHalted && riteStatus.Blockers != null)
            {
                foreach (RiteBlocker blocker in riteStatus.Blockers)
                {
                    if (shownRegions.Contains(blocker.Region)) continue;
                    string target = blocker.Region;
                    Action openBlocker = () => Select(target);
                    var bChip = new Button(openBlocker) { focusable = false, name = "rite-blocker-" + blocker.Region };
                    bChip.userData = openBlocker;
                    bChip.AddToClassList("aw-chip");
                    bChip.text = $"× {GameData.Regions[blocker.Region].Name} · {blocker.Reason}";
                    bChip.tooltip = $"Halting the final rite ({Notes.RiteBlockerReason(blocker.Reason)}). Click to open case file.";
                    bChip.style.color = Palette.Danger;
                    bChip.style.borderTopColor = bChip.style.borderBottomColor = bChip.style.borderLeftColor = bChip.style.borderRightColor = Palette.Alpha(Palette.Danger, 0.7f);
                    _marginChips.Add(bChip);
                }
            }
            if (!margin.Started)
            {
                bool enoughDread = dread >= Balance.FinalDread;
                Color dreadColor = enoughDread ? Palette.Cult : (s.Status == "alignment" || level == "tight" || doomed) ? Palette.Warn : Palette.Omen;
                var dreadBadge = Kit.Label($"DREAD {dread}/{Balance.FinalDread}", 12, dreadColor, Fonts.TypeBold);
                dreadBadge.name = "rite-margin-dread";
                dreadBadge.style.fontSize = 12;
                dreadBadge.pickingMode = PickingMode.Position;
                dreadBadge.style.paddingLeft = dreadBadge.style.paddingRight = Kit.Compact ? 6 : 7;
                dreadBadge.style.paddingTop = dreadBadge.style.paddingBottom = Kit.Touch ? 3 : 2;
                dreadBadge.style.borderTopWidth = dreadBadge.style.borderBottomWidth = dreadBadge.style.borderLeftWidth = dreadBadge.style.borderRightWidth = 1;
                dreadBadge.style.borderTopColor = dreadBadge.style.borderBottomColor = dreadBadge.style.borderLeftColor = dreadBadge.style.borderRightColor = Palette.Alpha(dreadColor, 0.7f);
                dreadBadge.style.backgroundColor = Palette.Alpha(dreadColor, 0.08f);
                dreadBadge.tooltip = enoughDread
                    ? $"Dread reserved: {dread} of {Balance.FinalDread} needed for the final rite."
                    : $"Dread reserved: {dread} of {Balance.FinalDread}. Need {Balance.FinalDread - dread} more before beginning the final rite.";
                _marginChips.Add(dreadBadge);

                if (hasProcession)
                {
                    bool procMet = procRegions >= Balance.ProcessionPublicRegions;
                    Color procColor = procMet ? Palette.Cult : Palette.Warn;
                    string procText = procMet
                        ? $"PROCESSION {procRegions}/{Balance.ProcessionPublicRegions} · {Balance.ProcessionRiteMonths}m"
                        : $"PROCESSION {procRegions}/{Balance.ProcessionPublicRegions}";
                    var procBadge = Kit.Label(procText, 12, procColor, Fonts.TypeBold);
                    procBadge.name = "rite-margin-procession";
                    procBadge.style.fontSize = 12;
                    procBadge.pickingMode = PickingMode.Position;
                    procBadge.style.paddingLeft = procBadge.style.paddingRight = Kit.Compact ? 6 : 7;
                    procBadge.style.paddingTop = procBadge.style.paddingBottom = Kit.Touch ? 3 : 2;
                    procBadge.style.borderTopWidth = procBadge.style.borderBottomWidth = procBadge.style.borderLeftWidth = procBadge.style.borderRightWidth = 1;
                    procBadge.style.borderTopColor = procBadge.style.borderBottomColor = procBadge.style.borderLeftColor = procBadge.style.borderRightColor = Palette.Alpha(procColor, 0.7f);
                    procBadge.style.backgroundColor = Palette.Alpha(procColor, 0.08f);
                    procBadge.tooltip = GameEngine.TalentYieldText(s, "great_procession");
                    _marginChips.Add(procBadge);
                }
            }

            _siteDetail.Clear();
            bool showDetail = _selectedSiteSlot >= 0 && _selectedSiteSlot < Balance.RequiredSites;
            Kit.SetVisible(_siteDetail, showDetail);
            if (showDetail)
            {
                if (_selectedSiteSlot < margin.Sites.Count)
                {
                    SiteMargin site = margin.Sites[_selectedSiteSlot];
                    Label detail = Kit.Body(Notes.SiteMarginDetail(s, site), 13, Palette.Text, Fonts.Italic);
                    detail.style.flexGrow = 1;
                    detail.style.flexShrink = 1;
                    _siteDetail.Add(detail);
                    string targetRegion = site.Region;
                    Action openFile = () => Select(targetRegion);
                    Button open = Kit.Link("OPEN FILE", openFile);
                    open.userData = openFile;
                    open.style.color = Palette.Cult;
                    open.style.flexShrink = 0;
                    _siteDetail.Add(open);
                }
                else
                {
                    Label detail = Kit.Body(Notes.UnfundedSiteDetail(_selectedSiteSlot + 1, s), 13, Palette.Muted, Fonts.Italic);
                    detail.style.flexGrow = 1;
                    detail.style.flexShrink = 1;
                    _siteDetail.Add(detail);
                }
            }
        }

        /// <summary>The talents button always opens the tree, and flags unbroken seals in the ledger.</summary>
        void RefreshTalents(GameState s, bool placing, bool over)
        {
            string readyCapstone = !placing && !over
                ? GameData.InvokableTalentIds.FirstOrDefault(id => GameEngine.HasTalent(s, id) && GameEngine.InvokeReason(s, id) == "") ?? ""
                : "";
            _invokableId = readyCapstone;
            Kit.SetVisible(_capstoneBox, readyCapstone != "");
            if (readyCapstone != "")
            {
                TalentDefinition cap = GameData.Talents[readyCapstone];
                _capstoneInvoke.tooltip = $"{cap.Name} (0 Influence): −{Balance.WorldForgetsAwareness} world awareness, dissolves all threats.\n{GameEngine.InvokeRisk(s, readyCapstone)}";
                _capstoneSub.text = Kit.Compact ? "FORGETS" : "WORLD FORGETS";
            }

            _talents.SetEnabled(!placing);
            _talentsNote.text = "";
            int total = GameData.Difficulties[s.Difficulty].TalentSlots;
            int taken = s.Talents.Count;
            int unused = GameEngine.TalentSlotsUnused(s);
            int nextMonth = GameEngine.NextTalentSlotMonth(s);
            int cost = GameEngine.TalentCost(s);

            if (placing)
            {
                _talents.EnableInClassList("aw-unavailable", false);
                _talents.EnableInClassList("aw-on", false);
                _talents.text = $"0 / {total} seals";
                _talents.style.color = StyleKeyword.Null;
                _talents.tooltip = $"Talents unlock as seals open ({total} in this difficulty).  (T)";
                return;
            }

            if (unused > 0 && !over)
            {
                int shortBy = cost - (int)Math.Floor(s.Influence);
                bool affordable = shortBy <= 0;
                _talents.text = $"Choose · {cost}";
                _talents.EnableInClassList("aw-on", affordable);
                _talents.EnableInClassList("aw-unavailable", !affordable);
                _talents.style.color = StyleKeyword.Null;
                _talentsNote.text = unused == 1 ? "1 SEAL UNBROKEN" : $"{unused} SEALS UNBROKEN";
                _talentsNote.style.color = affordable ? Palette.Cult : Palette.Warn;
                _talents.tooltip = affordable
                    ? $"{unused} unbroken {(unused == 1 ? "seal" : "seals")} · next talent costs {cost} Influence  (T)"
                    : $"{unused} unbroken {(unused == 1 ? "seal" : "seals")} · need {shortBy} more Influence to take a talent  (T)";
                return;
            }

            _talents.EnableInClassList("aw-on", false);
            _talents.EnableInClassList("aw-unavailable", false);
            _talents.text = $"{taken} / {total} seals";
            _talents.style.color = taken > 0 ? Palette.Cult : StyleKeyword.Null;
            if (!over)
            {
                if (nextMonth >= 0)
                {
                    int wait = nextMonth - s.ElapsedMonths;
                    _talentsNote.text = $"SEAL IN {wait} MO";
                    _talentsNote.style.color = Palette.Muted;
                    _talents.tooltip = $"Inspect talent tree · next seal opens {Format.Date(nextMonth)} (in {Format.Months(wait)})  (T)";
                }
                else
                {
                    _talentsNote.text = "ALL SEALS SPENT";
                    _talentsNote.style.color = Palette.Muted;
                    _talents.tooltip = $"Inspect talent tree · all {total} seals spent  (T)";
                }
            }
            else
            {
                _talents.tooltip = $"Inspect the {taken} talents taken in this run  (T)";
            }
        }

        /// <summary>During the alignment the rite button stays visible; it reads as ready only when it is.</summary>
        void RefreshRite(GameState s)
        {
            Kit.SetVisible(_rite, s.Status == "alignment");
            string reason = GameEngine.AwakeningReason(s);
            bool ready = string.IsNullOrEmpty(reason);
            int riteMonths = GameEngine.RiteMonthsFor(s);
            bool procession = GameEngine.HasTalent(s, "great_procession") && riteMonths < Balance.FinalMonths;
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
                _rite.text = procession
                    ? (Kit.Compact ? $"Procession ({riteMonths}m)" : $"Begin Procession · {riteMonths} mo")
                    : (Kit.Compact ? "Begin Rite" : "Begin the Final Rite");
                _rite.tooltip = procession
                    ? $"The Great Procession ready ({GameEngine.ProcessionRegions(s)} public regions). Spend 100 Dread and hold three sites for {riteMonths} months."
                    : $"Ready. Spend 100 Dread and hold three sites for {riteMonths} months.";
                _rite.style.color = StyleKeyword.Null;
            }
        }

        void SetTicker(LogEntry entry)
        {
            _tickerHead.text = entry != null ? ("Chronicle · " + Format.Date(entry.Month)).ToUpperInvariant() : "CHRONICLE";
            _tickerMessage = entry?.Message ?? "The world sleeps. Choose where the dream begins.";
            _tickerBox.tooltip = _tickerMessage + "\nOpen the chronicle  (C)";
            FitTicker();
        }

        void FitTicker()
        {
            float width = _ticker.contentRect.width;
            if (width <= 0 || float.IsNaN(width)) return;
            bool Fits(string text) => _ticker.MeasureTextSize(text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined).y <= TickerHeight;
            string copy = _tickerMessage;
            if (!Fits(copy))
            {
                int sentence = copy.IndexOf(". ", StringComparison.Ordinal);
                if (sentence >= 0) copy = copy.Substring(0, sentence + 1);
                while (!Fits(copy) && copy.LastIndexOf(' ') > 0)
                    copy = copy.Substring(0, copy.LastIndexOf(' '));
            }
            _ticker.text = copy;
        }

        void RebuildThreats(GameState s)
        {
            _threats.Clear();
            _threatOptions.Clear();
            if (GameEngine.IsOver(s)) return;
            var ids = GameData.RegionIds.Where(id => s.Regions[id].Threat != null).ToList();
            ids.Sort((a, b) => s.Regions[a].Threat.DueMonth.CompareTo(s.Regions[b].Threat.DueMonth));
            int start = ids.Count == 0 ? 0 : _threatOffset % ids.Count;
            for (int mode = 0; mode < 3; mode++)
            {
                int limit = mode == 0 ? 2 : 1;
                var option = Kit.VBox(8);
                option.style.position = Position.Absolute;
                option.style.left = option.style.top = 0;
                option.style.width = Length.Percent(100);
                option.style.visibility = Visibility.Hidden;
                foreach (string id in ids.Skip(start).Concat(ids.Take(start)).Take(limit)) option.Add(ThreatCard(s, id, mode == 2));
                if (ids.Count > limit)
                {
                    var more = Kit.Button($"+{ids.Count - limit} more threats · review", () =>
                    {
                        _threatOffset = (start + limit) % ids.Count;
                        RebuildThreats(_game.State);
                        QueueLayout();
                    });
                    Fonts.Apply(more, Fonts.TypeBold);
                    more.style.fontSize = Kit.Compact ? 15 : 14;
                    more.style.minHeight = Kit.Touch ? 44 : 32;
                    more.tooltip = "Show the next threats, in deadline order. Select any clipping to inspect its case file.";
                    option.Add(more);
                }
                _threatOptions.Add(option);
                // Stack.Add would add an 8-unit margin to each alternative.
                _threats.hierarchy.Add(option);
                option.RegisterCallback<GeometryChangedEvent>(_ => QueueLayout());
            }
        }

        static readonly Dictionary<string, string> KindName = new Dictionary<string, string>
        {
            ["investigation"] = "Investigation", ["raid"] = "Police Raid", ["occult"] = "Occult Hunters",
        };

        /// <summary>A press clipping: the headline names the threat, the options and stamps answer it.</summary>
        VisualElement ThreatCard(GameState s, string id, bool brief)
        {
            RegionState r = s.Regions[id];
            Threat threat = r.Threat;
            int months = threat.DueMonth - s.ElapsedMonths;
            bool urgent = months <= 3;
            bool cornered = Threats.Cornered(s, id);
            var options = Threats.Options(s, id);
            Stack card = Kit.Panel(Palette.PaperLight, urgent || cornered ? Palette.Danger : Palette.PaperEdge, new Vector4(12, 9, 12, 10), 0.1f, 6);
            card.name = "threat-card-" + id;
            if (urgent || cornered) card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 2;
            if (cornered) card.style.borderLeftWidth = 5;
            card.tooltip = GameData.ThreatNames[threat.Kind] + (cornered ? " (Cornered: neither obstruction nor silencing is available.)" : "") + ".\nIf ignored: " + Threats.Describe(s, r);
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
                foreach (string effect in Notes.ThreatEffects(s, r)) text.Add(Kit.Label(effect, 15, Palette.Text, Fonts.Italic));
            }
            row.Add(text);
            Stack right = Kit.VBox(0);
            right.style.alignItems = Align.Center;
            right.Add(Kit.Label(months.ToString(), Kit.Compact ? 26 : 30, Palette.Danger, Fonts.BodyBold));
            right.Add(Kit.Caption(months != 1 ? "months" : "month", Palette.Danger));
            row.Add(right);
            if (brief)
            {
                string briefText = cornered
                    ? (Kit.Touch ? "CORNERED · tap for responses" : "CORNERED · inspect responses")
                    : (Kit.Touch ? "Tap for responses" : "Inspect responses");
                Label briefLabel = Kit.Caption(briefText, Palette.Danger);
                if (cornered) briefLabel.name = "threat-cornered-" + id;
                card.Add(briefLabel);
                return card;
            }
            if (cornered)
            {
                Label badge = Kit.Body(Notes.ThreatCorneredLabel(s, id, Kit.Compact), 11, Palette.Danger, Fonts.TypeBold);
                badge.name = "threat-cornered-" + id;
                badge.style.backgroundColor = Palette.Alpha(Palette.Danger, 0.10f);
                badge.style.borderTopWidth = badge.style.borderBottomWidth = badge.style.borderLeftWidth = badge.style.borderRightWidth = 1;
                badge.style.borderTopColor = badge.style.borderBottomColor = badge.style.borderLeftColor = badge.style.borderRightColor = Palette.Alpha(Palette.Danger, 0.65f);
                badge.style.paddingLeft = badge.style.paddingRight = 6;
                badge.style.paddingTop = badge.style.paddingBottom = 2;
                badge.tooltip = "Neither obstruction nor silencing is available now.";
                card.Add(badge);
            }
            // Three answers from Threats.Options: price and why the others are unavailable from the moment it appears.
            Stack opts = Kit.VBox(Kit.Compact ? 4 : 3);
            foreach (ThreatOption opt in options)
            {
                ActionPreview preview = GameEngine.PreviewAction(s, opt.Response, id);
                string chosen = opt.Response;
                Button stamp = Kit.Stamp(Notes.ResponseStamp(s, preview, chosen), () => _game.Act(chosen, id), Accent.Danger);
                stamp.style.fontSize = 12;
                stamp.style.paddingLeft = stamp.style.paddingRight = 7;
                stamp.style.height = Kit.Touch ? 34 : 24;
                stamp.style.flexShrink = 0;
                stamp.SetEnabled(opt.Ok);
                bool haltsRite = opt.Ok && chosen == "scatter" && Notes.RiskHaltsRite(preview);
                stamp.tooltip = opt.Ok
                    ? GameData.Actions[chosen].Name + " (" + opt.Cost + "). " + (haltsRite ? preview.Risk : Notes.ResponseNote(r, preview, chosen) + ".")
                    : opt.Reason + " (" + opt.Cost + ")";
                string detail = opt.Ok
                    ? (haltsRite ? $"HALTS RITE · {GameEngine.ScatterMonths(s)} MO"
                        : chosen == "silence" ? "+" + Js.Num(preview.Awareness) + " awareness"
                        : chosen == "scatter" ? GameEngine.ScatterMonths(s) + " mo hidden"
                        : "rests 12 mo")
                    : Notes.ThreatOptionReason(opt);
                Color detailColor = !opt.Ok ? Palette.Warn : haltsRite || chosen == "silence" ? Palette.Danger : Palette.Muted;
                string detailWeight = !opt.Ok || haltsRite ? Fonts.TypeBold : Fonts.Italic;
                Label info = Kit.Body(detail, Kit.Compact ? 11 : 12, detailColor, detailWeight);
                if (haltsRite) info.name = "threat-scatter-halts-" + id;
                if (Kit.Compact)
                {
                    Stack item = Kit.VBox(1);
                    stamp.style.alignSelf = Align.FlexStart;
                    item.Add(stamp);
                    item.Add(info);
                    opts.Add(item);
                }
                else
                {
                    Stack item = Kit.HBox(6);
                    item.style.alignItems = Align.Center;
                    info.style.flexGrow = 1;
                    info.style.flexShrink = 1;
                    info.style.minWidth = 0;
                    item.Add(stamp);
                    item.Add(info);
                    opts.Add(item);
                }
            }
            card.Add(opts);
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
            var hint = Notes.Hint(s, id);
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
            _note.RegisterCallback<GeometryChangedEvent>(_ => { QueueLayout(); Map.RefreshLabels(); });
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

        void RefreshGoals(GameState s)
        {
            if (_goals == null) return;
            GoalStep active = _game.Settings.HideGoals ? null : Notes.ActiveGoal(s);
            if (active == null)
            {
                _goalsShown = "";
                Kit.SetVisible(_goals, false);
                _goals.style.visibility = Visibility.Hidden;
                return;
            }
            var all = Notes.FirstRunGoals(s);
            int done = all.Count(g => g.Done);
            string key = string.Join("|", active.Id, active.Title, active.Detail, active.Overdue, done);
            Kit.SetVisible(_goals, true);
            if (key == _goalsShown && _goals.childCount > 0) return;
            _goalsShown = key;
            _goals.Clear();
            Color accent = active.Overdue ? Palette.Warn : Palette.Cult;
            _goals.style.borderLeftColor = accent;
            Stack head = Kit.HBox(8);
            Label cap = Kit.Caption($"First-run goals · {done}/{all.Count}", accent);
            cap.style.flexGrow = 1;
            head.Add(cap);
            Action hideAction = () => _game.SetHideGoals(true);
            Button hide = Kit.Link("HIDE", hideAction);
            hide.userData = hideAction;
            hide.tooltip = "Hide first-run goals. Restore anytime in Menu.";
            head.Add(hide);
            _goals.Add(head);
            _goals.Add(Kit.Body(active.Title, Kit.Compact ? 14 : 16, Palette.Text, Fonts.BodyBold));
            _goals.Add(Kit.Body(active.Detail, 13, active.Overdue ? Palette.Warn : Palette.Muted, Fonts.Italic));
            _goals.tooltip = string.Join("\n", all.Select(g => (g.Done ? "◆ " : "○ ") + g.Title)) + "\nHide anytime here or in Menu.";
        }
    }
}
