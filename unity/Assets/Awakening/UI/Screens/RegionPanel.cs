using System;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// The selected region's case file: its people, what threatens it, and what the cult can do
    /// there. Port of godot/ui/region_panel.gd. With a mouse, hovering an action explains it and
    /// clicking performs it. On touch screens a tap only opens the explanation under that action,
    /// with a button to perform it, and the file scrolls by dragging. Actions come before the
    /// statistics so their text stays in view.
    /// </summary>
    public sealed class RegionPanel : VisualElement
    {
        public static readonly (string name, string[] actions)[] Groups =
        {
            ("CULT", new[] { "recruit", "expand", "reveal", "hide", "conceal", "counter", "scatter", "silence" }),
            ("RITES", new[] { "sacrifice", "manifest", "prepare" }),
            ("OMENS", new[] { "exploit", "convert", "reveal_figure" }),
        };

        public readonly float Width = Kit.Compact ? 372f : 392f;
        public string Region = "";
        public string Group = "CULT";

        readonly GameController _game;
        readonly GameScreen _screen;
        readonly ScrollView _scroll;
        readonly Stack _body;
        readonly Label _file, _name, _stance;
        readonly VisualElement _stats;
        readonly Stack _people;
        readonly PopulationBar _population;
        readonly Flow _legend;
        readonly Stack _cards, _profile;
        readonly Stack _tabs;
        readonly Stack _actions;
        readonly Stack _detail;
        readonly Dictionary<string, (Button button, Label title, Label cost, Label aware)> _rows = new Dictionary<string, (Button, Label, Label, Label)>();
        string _detailId = "";
        string _detailShown = "";

        public RegionPanel(GameController game, GameScreen screen)
        {
            _game = game;
            _screen = screen;
            name = "region-panel";
            Kit.Box(this, Palette.Paper, Palette.DeskEdge, 1, Vector4.zero);
            Kit.AddGrain(this, 0.14f);
            float gap = Kit.Compact ? 10 : 14;
            style.position = Position.Absolute;
            style.right = gap;
            style.width = Width;
            style.top = screen.Top + gap;
            style.bottom = screen.Bottom + gap;

            _scroll = new ScrollView(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            _scroll.AddToClassList("aw-scroll");
            _scroll.style.flexGrow = 1;
            _scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            Add(_scroll);
            _body = Kit.VBox(14);
            int margin = Kit.Compact ? 14 : 18;
            _body.style.paddingLeft = _body.style.paddingRight = _body.style.paddingTop = _body.style.paddingBottom = margin;
            _scroll.Add(_body);

            Stack head = Kit.HBox(8);
            head.style.alignItems = Align.FlexStart;
            Stack titles = Kit.VBox(0);
            titles.style.flexGrow = 1;
            _file = Kit.Caption("", Palette.Faint);
            _name = Kit.Label("", Kit.Compact ? 32 : 36, Palette.Text, Fonts.Display);
            _stance = Kit.Caption("");
            titles.Add(_file);
            titles.Add(_name);
            titles.Add(_stance);
            head.Add(titles);
            head.Add(new IconButton("close", "Close  (Esc)", () => _screen.Deselect(), true));
            _body.Add(head);
            _body.Add(Kit.Rule());

            _stats = new VisualElement();
            _stats.style.flexDirection = FlexDirection.Row;
            _stats.style.flexWrap = Wrap.Wrap;

            _people = Kit.VBox(6);
            _people.Add(Kit.Caption("People"));
            _population = new PopulationBar();
            _people.Add(_population);
            _legend = new Flow(12, 4);
            _people.Add(_legend);

            _cards = Kit.VBox(10);
            _profile = Kit.VBox(10);
            _tabs = Kit.HBox(3);
            _tabs.style.alignItems = Align.Stretch;
            foreach (var group in Groups)
            {
                string groupName = group.name;
                var tab = new Button(() => SetGroup(groupName)) { text = Capitalize(groupName), focusable = false, name = groupName };
                tab.AddToClassList("aw-tab");
                _tabs.Add(tab);
            }
            _actions = Kit.VBox(0);
            _detail = Kit.VBox(6);

            // Actions and their explanation come first; statistics follow. On short screens the
            // cards (threat, omen, figure, site) also wait below the actions, so a larger interface
            // never pushes what the player can do out of view.
            if (!Kit.Compact)
            {
                _body.Add(_cards);
                _body.Add(_profile);
            }
            _body.Add(_tabs);
            _body.Add(_actions);
            _body.Add(_detail);
            if (Kit.Compact)
            {
                _body.Add(_cards);
                _body.Add(_profile);
            }
            _body.Add(_stats);
            _body.Add(_people);
            Kit.SetVisible(this, false);
        }

        static string Capitalize(string upper) => upper.Substring(0, 1) + upper.Substring(1).ToLowerInvariant();

        public bool Shown => !string.IsNullOrEmpty(Region);

        public void ShowRegion(string id)
        {
            Region = id ?? "";
            Kit.SetVisible(this, Shown);
            _detailId = "";
            if (Shown)
            {
                BuildRows();
                Refresh();
            }
        }

        void SetGroup(string groupName)
        {
            Group = groupName;
            _detailId = "";
            BuildRows();
            Refresh();
        }

        /// <summary>
        /// Connects a button so that, on touch screens, a drag that scrolls the file does not also
        /// press whatever was under the finger.
        /// </summary>
        void Tap(Button button, Action action)
        {
            if (!Kit.Touch)
            {
                button.clicked += action;
                return;
            }
            float scrolledAt = 0;
            button.RegisterCallback<PointerDownEvent>(_ => scrolledAt = _scroll.scrollOffset.y, TrickleDown.TrickleDown);
            button.clicked += () =>
            {
                if (Mathf.Abs(_scroll.scrollOffset.y - scrolledAt) < 8) action();
            };
        }

        void BuildRows()
        {
            if (_detail.parent == _actions)
            {
                _actions.Remove(_detail);
                _body.Insert(_body.IndexOf(_actions) + 1, _detail);
            }
            _actions.Clear();
            _rows.Clear();
            _detailShown = "";
            foreach (VisualElement tab in _tabs.Children()) tab.EnableInClassList("aw-on", tab.name == Group);
            foreach (string id in Groups.First(g => g.name == Group).actions)
            {
                string action = id;
                var button = new Button { focusable = false, name = id };
                button.AddToClassList("aw-row");
                Tap(button, Kit.Touch ? (Action)(() => Choose(action)) : () => _game.Act(action, Region));
                // Name on the left, price or the reason it is unavailable on the right.
                Label title = Kit.Label("", 18, Palette.Text, Fonts.Body);
                title.style.flexGrow = 1;
                title.style.flexShrink = 1;
                title.style.overflow = Overflow.Hidden;
                title.style.textOverflow = TextOverflow.Ellipsis;
                title.style.unityTextAlign = TextAnchor.MiddleLeft;
                // The price, and under it the world awareness the action adds: that is a price too.
                Stack price = Kit.VBox(0);
                price.style.alignItems = Align.FlexEnd;
                price.style.marginLeft = 10;
                price.pickingMode = PickingMode.Ignore;
                Label cost = Kit.Label("", 14, Palette.Muted, Fonts.TypeBold);
                cost.style.unityTextAlign = TextAnchor.MiddleRight;
                Label aware = Kit.Label("", 12, Palette.Danger, Fonts.TypeBold);
                aware.style.unityTextAlign = TextAnchor.MiddleRight;
                price.Add(cost);
                price.Add(aware);
                button.Add(title);
                button.Add(price);
                if (!Kit.Touch)
                    button.RegisterCallback<PointerEnterEvent>(_ =>
                    {
                        _detailId = action;
                        RefreshDetail();
                    });
                _actions.Add(button);
                _rows[id] = (button, title, cost, aware);
            }
        }

        public void Refresh()
        {
            if (!Shown || _game.State == null) return;
            GameState s = _game.State;
            RegionState r = s.Regions[Region];
            bool placing = s.Status == "placing";
            _file.text = "CASE FILE No. " + (Array.IndexOf(GameData.RegionIds, Region) + 1);
            _name.text = GameData.Regions[Region].Name;
            string stance = "NO CULT PRESENCE";
            Color stanceColor = Palette.Muted;
            if (r.Population.Followers > 0)
            {
                stance = r.Public ? "PUBLIC CONGREGATION" : "HIDDEN CELL";
                stanceColor = Palette.Cult;
            }
            if (r.Scattered)
            {
                stance = $"SCATTERED  ·  {r.ScatteredUntil - s.ElapsedMonths} MO";
                stanceColor = Palette.Warn;
            }
            if (r.BacklashUntil > s.ElapsedMonths) stance += $"  ·  BACKLASH {r.BacklashUntil - s.ElapsedMonths} MO";
            _stance.text = stance;
            _stance.style.color = stanceColor;
            FillStats(s, r);
            _population.Population = r.Population;
            FillLegend(r);
            FillCards(s, r);
            Kit.SetVisible(_profile, placing);
            Kit.SetVisible(_stats, !placing);
            Kit.SetVisible(_tabs, !placing);
            Kit.SetVisible(_actions, !placing);
            Kit.SetVisible(_detail, !placing);
            if (placing)
            {
                FillProfile(s);
                return;
            }
            foreach (var pair in _rows)
            {
                string id = pair.Key;
                var row = pair.Value;
                ActionPreview preview = GameEngine.PreviewAction(s, id, Region);
                // On touch a blocked row still opens its explanation; only the perform button is off.
                row.button.SetEnabled(preview.Ok || Kit.Touch);
                // Rows that can never apply here are left out: worship already in its state, a cell
                // that already exists, or an answer to a threat when there is none.
                bool hidden = id == "reveal" && r.Public || id == "hide" && !r.Public || id == "expand" && r.Population.Followers > 0
                    || Notes.Responses.Contains(id) && r.Threat == null;
                Kit.SetVisible(row.button, !hidden);
                var blocked = Notes.Blocker(s, id, Region, preview);
                row.button.tooltip = blocked.longText;
                // Available: ink and price. Unavailable: muted italic, and the reason in place of the price.
                row.title.text = GameData.Actions[id].Name;
                Fonts.Apply(row.title, preview.Ok ? Fonts.Body : Fonts.Italic);
                row.title.style.color = preview.Ok ? Palette.Text : Palette.Muted;
                row.cost.text = preview.Ok ? CostText(preview) : blocked.shortText;
                row.aware.text = AwarenessText(preview);
                Kit.SetVisible(row.aware, preview.Ok && preview.Awareness > 0);
                row.cost.style.color = !preview.Ok ? Palette.Warn : preview.Dread > 0 ? Palette.Omen : Palette.Text;
            }
            RefreshDetail();
        }

        void FillStats(GameState s, RegionState r)
        {
            _stats.Clear();
            var items = new (string name, string value, Color color)[]
            {
                ("Followers", Format.Count(r.Population.Followers), Palette.Cult),
                ("Recruits", Format.Rate(Derived.RecruitmentPerMonth(s, r)) + "/mo", Palette.Text),
                ("Influence", Format.Rate(Derived.RegionIncome(r)) + "/mo", Palette.Text),
                ("Exposure", (int)Math.Round(r.Exposure, MidpointRounding.AwayFromZero) + "%", r.Exposure >= 60 ? Palette.Danger : r.Exposure >= 30 ? Palette.Warn : Palette.Text),
                ("Madness", (int)Math.Round(r.Madness, MidpointRounding.AwayFromZero) + "%", r.Madness > 0 ? Palette.Omen : Palette.Text),
                ("Site needs", Format.Count(Derived.SiteRequirement(r)), Derived.Supported(r) ? Palette.Text : Palette.Muted),
            };
            var hints = new Dictionary<string, string>
            {
                ["Exposure"] = "Evidence of the cult here. Raises threat severity. Public worship keeps it above 20%, a ritual site above 40%.",
                ["Madness"] = "Left by manifestations. Speeds recruitment slightly and draws opposition. Fades over time.",
                ["Site needs"] = "Followers a ritual site here must keep: a quarter of the region's people.",
                ["Recruits"] = "Automatic recruitment each month.",
            };
            for (int i = 0; i < items.Length; i++)
            {
                Stack cell = Kit.VBox(0);
                cell.style.width = Length.Percent(33.33f);
                if (i >= 3) cell.style.marginTop = 10;
                cell.tooltip = hints.TryGetValue(items[i].name, out string hint) ? hint : "";
                cell.Add(Kit.Caption(items[i].name));
                cell.Add(Kit.Label(items[i].value, 19, items[i].color, Fonts.BodyBold));
                _stats.Add(cell);
            }
        }

        void FillLegend(RegionState r)
        {
            _legend.Clear();
            foreach (string key in PopulationBar.Order)
            {
                Stack item = Kit.HBox(5);
                var swatch = new VisualElement();
                swatch.style.width = swatch.style.height = 8;
                swatch.style.backgroundColor = PopulationBar.Colors[key];
                item.Add(swatch);
                item.Add(Kit.Label(Capitalize(key.ToUpperInvariant()) + " " + Format.Count(PopulationBar.Value(r.Population, key)), 13, Palette.Muted, Fonts.Type));
                _legend.Add(item);
            }
        }

        Stack Card(Color accent)
        {
            Stack card = Kit.Panel(Palette.PaperLight, accent, new Vector4(14, 11, 14, 12));
            card.style.borderLeftWidth = 4;
            Stack box = Kit.VBox(5);
            card.Add(box);
            _cards.Add(card);
            return box;
        }

        void FillCards(GameState s, RegionState r)
        {
            _cards.Clear();
            if (GameEngine.IsOver(s)) return;
            if (r.Threat != null)
            {
                Threat threat = r.Threat;
                Stack box = Card(Palette.Danger);
                Stack head = Kit.HBox(8);
                Label title = Kit.Body(GameData.ThreatNames[threat.Kind], 21, Palette.Danger, Fonts.DisplaySc);
                title.style.flexGrow = 1;
                title.style.flexShrink = 1;
                head.Add(title);
                head.Add(Kit.Label(Format.Months(threat.DueMonth - s.ElapsedMonths), 15, Palette.Danger, Fonts.TypeBold));
                box.Add(head);
                box.Add(Kit.Body("If ignored: " + Threats.Describe(r), 15, Palette.Text, Fonts.Italic));
                foreach (string id in Notes.Responses) box.Add(Response(s, r, id));
            }
            if (r.Opportunity != null)
            {
                Opportunity omen = r.Opportunity;
                Stack box = Card(Palette.Omen);
                Stack head = Kit.HBox(8);
                Label title = Kit.Body(GameData.OpportunityNames[omen.Kind], 21, Palette.Omen, Fonts.Display);
                title.style.flexGrow = 1;
                title.style.flexShrink = 1;
                head.Add(title);
                head.Add(Kit.Label(Format.Months(omen.ExpiresMonth - s.ElapsedMonths), 15, Palette.Omen, Fonts.TypeBold));
                box.Add(head);
                string action = omen.Kind == "nightmare" || omen.Kind == "eclipse" ? "exploit" : "convert";
                ActionPreview preview = GameEngine.PreviewAction(s, action, Region);
                Button button = Kit.Stamp(GameData.Actions[action].Name.ToUpperInvariant() + " · " + preview.Influence, null, Accent.Omen);
                Tap(button, () => _game.Act(action, Region));
                button.SetEnabled(preview.Ok);
                button.tooltip = preview.Effect + (!preview.Ok ? "\n" + preview.Reason : "");
                box.Add(Kit.Body(preview.Effect, 15, Palette.Text, Fonts.Italic));
                box.Add(WithReason(button, Notes.Blocker(s, action, Region, preview).shortText));
            }
            if (r.Figure != null)
            {
                Stack box = Card(Palette.Omen);
                string who = r.Figure.Kind == "celebrity" ? "celebrity" : "politician";
                if (r.Figure.Revealed)
                {
                    box.Add(Kit.Label("Public " + who, 20, Palette.Omen, Fonts.DisplaySc));
                }
                else
                {
                    box.Add(Kit.Label("Secret " + who, 20, Palette.Omen, Fonts.DisplaySc));
                    box.Add(Kit.Body(who == "celebrity" ? "+0.6 Influence each month." : "New threats here arrive 2 months later.", 15, Palette.Text, Fonts.Italic));
                }
            }
            if (r.Site.Invested)
            {
                string state = Notes.SiteState(r);
                Color accent = state == "ready" ? Palette.Cult : state == "preparing" ? Palette.Omen : Palette.Danger;
                Stack box = Card(accent);
                Stack head = Kit.HBox(8);
                Label title = Kit.Label("Ritual Site", 21, accent, Fonts.DisplaySc);
                title.style.flexGrow = 1;
                head.Add(title);
                head.Add(Kit.Label($"{r.Site.Progress} / {Balance.SiteMonths} MO", 15, accent, Fonts.TypeBold));
                box.Add(head);
                box.Add(new Bar(Balance.SiteMonths, accent) { Value = r.Site.Progress });
                if (state == "dormant")
                    box.Add(Kit.Body($"Dormant. Upkeep of {Js.Num(Balance.SiteUpkeep)} Influence a month is unpaid; it does not count for the rite.", 15, Palette.Danger, Fonts.Italic));
                else if (r.Scattered)
                    box.Add(Kit.Body($"Stalled. The flock is scattered for {r.ScatteredUntil - s.ElapsedMonths} more months.", 15, Palette.Danger, Fonts.Italic));
                else if (state == "unsupported")
                    box.Add(Kit.Body($"Stalled. Needs {Format.Count(Derived.SiteRequirement(r))} followers here.", 15, Palette.Danger, Fonts.Italic));
                else if (state == "ready")
                    box.Add(Kit.Body("Ready for the final rite.", 15, Palette.Cult, Fonts.Italic));
            }
        }

        /// <summary>One answer to this region's threat: its stamp, and under it what it costs besides the price, or why it cannot be used now.</summary>
        VisualElement Response(GameState s, RegionState r, string id)
        {
            ActionPreview preview = GameEngine.PreviewAction(s, id, Region);
            Button stamp = Kit.Stamp(Notes.ResponseStamp(preview, id), null, Accent.Danger);
            Tap(stamp, () => _game.Act(id, Region));
            stamp.SetEnabled(preview.Ok);
            stamp.tooltip = !preview.Ok ? preview.Reason : preview.Effect + "\n" + preview.Risk;
            stamp.style.alignSelf = Align.FlexStart;
            if (Kit.Touch) stamp.style.minHeight = 40;
            Stack column = Kit.VBox(2);
            column.Add(stamp);
            if (preview.Ok) column.Add(Kit.Body(Notes.ResponseNote(r, preview, id), 13, Palette.Muted, Fonts.Italic));
            else column.Add(Kit.Label(Notes.Blocker(s, id, Region, preview).shortText, 12, Palette.Warn, Fonts.TypeBold));
            return column;
        }

        /// <summary>A stamp, with the short reason it is unavailable underneath.</summary>
        static VisualElement WithReason(Button stamp, string reason)
        {
            stamp.style.alignSelf = Align.FlexStart;
            if (Kit.Touch) stamp.style.minHeight = 40;
            if (reason == "") return stamp;
            Stack column = Kit.VBox(4);
            column.Add(stamp);
            column.Add(Kit.Label(reason, 12, Palette.Warn, Fonts.TypeBold));
            return column;
        }

        void FillProfile(GameState s)
        {
            _profile.Clear();
            RegionDefinition def = GameData.Regions[Region];
            var facts = new (string name, double value, string hint)[]
            {
                ("Skepticism", def.Skepticism, "Share of people who deny the dream outright."),
                ("Institutions", def.InstitutionalResistance, "How hard the authorities push back."),
                ("Mysticism", def.Mysticism, "Openness to the impossible."),
            };
            foreach (var fact in facts)
            {
                Stack row = Kit.HBox(10);
                row.tooltip = fact.hint;
                Label label = Kit.Caption(fact.name);
                label.style.width = 100;
                row.Add(label);
                var bar = new Bar(100, Palette.Muted) { Value = (float)fact.value };
                bar.style.flexGrow = 1;
                row.Add(bar);
                _profile.Add(row);
            }
            var borders = def.Adjacent.Select(other => GameData.Regions[other].Name);
            _profile.Add(Kit.Body("Spreads to " + string.Join(", ", borders) + ".", 16, Palette.Muted, Fonts.Italic));
            Button start = Kit.Primary("Begin Here");
            start.tooltip = "Enter";
            Tap(start, () => _game.Place(Region));
            start.SetEnabled(s.Status == "placing");
            _profile.Add(start);
        }

        /// <summary>Every resource an action takes: "40 INF · 60 DREAD", or "" when it is free.</summary>
        static string PriceText(ActionPreview preview)
        {
            var parts = new List<string>();
            if (preview.Influence > 0) parts.Add(preview.Influence + " INF");
            if (preview.Dread > 0) parts.Add(preview.Dread + " DREAD");
            if (preview.Followers > 0) parts.Add(preview.Followers + " FOLLOWERS");
            return string.Join(" · ", parts);
        }

        static string AwarenessText(ActionPreview preview) => "+" + Js.Num(preview.Awareness) + " AWARENESS";

        static string CostText(ActionPreview preview)
        {
            string reason = preview.Reason ?? "";
            if (reason.StartsWith("Available in", StringComparison.Ordinal))
                return reason.Substring("Available in ".Length).Replace(" months.", "") + " MO";
            var parts = new List<string>();
            if (preview.Influence > 0) parts.Add(preview.Influence + " INF");
            if (preview.Dread > 0) parts.Add(preview.Dread + " DREAD");
            if (preview.Followers > 0) parts.Add(preview.Followers + " FOLLOWERS");
            return parts.Count > 0 ? string.Join("  ", parts) : "FREE";
        }

        /// <summary>Touch: a tap opens the explanation right under the chosen action; tapping it again closes it.</summary>
        void Choose(string id)
        {
            _detailId = _detailId == id ? "" : id;
            RefreshDetail();
            if (_detailId != "") ScrollTo(_rows[_detailId].button);
        }

        /// <summary>Brings the chosen action to the top of the file, so its explanation reads downwards.</summary>
        void ScrollTo(VisualElement row)
        {
            schedule.Execute(() =>
            {
                if (row.panel == null) return;
                float top = row.worldBound.y - _scroll.contentContainer.worldBound.y;
                _scroll.scrollOffset = new Vector2(0, Mathf.Max(0, top - 6));
            });
        }

        /// <summary>
        /// Keyboard control of the open file: Up and Down pick an action, Enter performs it, Left and
        /// Right change the tab. While placing, Enter begins the cult here. Returns true if handled.
        /// </summary>
        public bool HandleKey(KeyCode key)
        {
            if (!Shown || _game.State == null) return false;
            if (_game.State.Status == "placing")
            {
                if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
                {
                    _game.Place(Region);
                    return true;
                }
                return false;
            }
            var names = Groups.Select(g => g.name).ToList();
            switch (key)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                {
                    var ids = Groups.First(g => g.name == Group).actions.Where(id => _rows.ContainsKey(id) && Kit.IsVisible(_rows[id].button)).ToList();
                    if (ids.Count == 0) return true;
                    int at = ids.IndexOf(_detailId);
                    int step = key == KeyCode.DownArrow ? 1 : -1;
                    _detailId = at >= 0 ? ids[((at + step) % ids.Count + ids.Count) % ids.Count] : ids[step > 0 ? 0 : ids.Count - 1];
                    RefreshDetail();
                    ScrollTo(_rows[_detailId].button);
                    return true;
                }
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                {
                    int next = names.IndexOf(Group) + (key == KeyCode.RightArrow ? 1 : -1);
                    SetGroup(names[(next % names.Count + names.Count) % names.Count]);
                    return true;
                }
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_detailId != "" && _rows.ContainsKey(_detailId)) _game.Act(_detailId, Region);
                    return true;
            }
            return false;
        }

        void RefreshDetail()
        {
            foreach (var pair in _rows)
            {
                pair.Value.button.EnableInClassList("aw-on", Kit.Touch && pair.Key == _detailId);
                // The explained row stays marked, whether it was reached by mouse or keyboard.
                pair.Value.button.EnableInClassList("aw-marked", pair.Key == _detailId && !Kit.Touch);
            }
            ActionPreview preview = _detailId != "" && _game.State != null && _rows.ContainsKey(_detailId)
                ? GameEngine.PreviewAction(_game.State, _detailId, Region) : null;
            // The clock refreshes the file every month; rebuilding an unchanged explanation would
            // swallow a tap that lands between frames.
            string shown = preview == null ? "" : string.Join("|", _detailId, Region, preview.Ok, preview.Reason, preview.Influence, preview.Dread,
                preview.Followers, preview.Awareness, preview.Effect, preview.Risk);
            if (shown == _detailShown && _detail.childCount > 0) return;
            _detailShown = shown;
            _detail.Clear();
            if (preview == null) return;
            if (Kit.Touch)
            {
                if (_detail.parent != _actions)
                {
                    _detail.RemoveFromHierarchy();
                    _actions.Insert(_actions.IndexOf(_rows[_detailId].button) + 1, _detail);
                }
                else
                {
                    _actions.Remove(_detail);
                    _actions.Insert(_actions.IndexOf(_rows[_detailId].button) + 1, _detail);
                }
            }
            else
            {
                _detail.Add(Kit.Rule());
                _detail.Add(Kit.Label(GameData.Actions[_detailId].Name, 21, Palette.Text, Fonts.DisplaySc));
            }
            // The full price stays in view even when something else blocks the action.
            string price = PriceText(preview);
            if (price != "") _detail.Add(Kit.Label("COST  " + price, 13, preview.Dread > 0 ? Palette.Omen : Palette.Text, Fonts.TypeBold));
            // World awareness is a price too; it gets its own line so a long price never widens the file.
            if (preview.Awareness > 0) _detail.Add(Kit.Label(AwarenessText(preview).Replace("AWARENESS", "WORLD AWARENESS"), 13, Palette.Danger, Fonts.TypeBold));
            _detail.Add(Kit.Body(preview.Effect, 15, Palette.Text));
            if (!string.IsNullOrEmpty(preview.Risk)) _detail.Add(Kit.Body(preview.Risk, 15, Palette.Danger, Fonts.Italic));
            if (!preview.Ok) _detail.Add(Kit.Body(Notes.Blocker(_game.State, _detailId, Region, preview).longText, 15, Palette.Warn, Fonts.BodyBold));
            if (Kit.Touch)
            {
                string id = _detailId;
                string cost = preview.Ok ? CostText(preview) : "";
                // The perform button names the price, never the long action name, so it fits the file.
                Button perform = Kit.Primary(cost != "" ? "Perform · " + cost.ToLowerInvariant() : "Perform");
                perform.style.fontSize = 19;
                perform.style.minHeight = 52;
                Tap(perform, () => _game.Act(id, Region));
                perform.SetEnabled(preview.Ok);
                _detail.Add(perform);
                var spacer = new VisualElement();
                spacer.style.height = 6;
                _detail.Add(spacer);
            }
        }
    }

    /// <summary>Unaware, deniers, believers and followers as one stacked bar.</summary>
    public sealed class PopulationBar : VisualElement
    {
        public static readonly string[] Order = { "unaware", "deniers", "believers", "followers" };
        public static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            ["unaware"] = Palette.Hex("b4a07a"), ["deniers"] = Palette.Hex("7a6a52"), ["believers"] = Palette.Omen, ["followers"] = Palette.Cult,
        };

        Population _population;

        public Population Population
        {
            set
            {
                _population = value;
                MarkDirtyRepaint();
            }
        }

        public PopulationBar()
        {
            style.height = 10;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public static double Value(Population p, string key)
        {
            switch (key)
            {
                case "unaware": return p.Unaware;
                case "deniers": return p.Deniers;
                case "believers": return p.Believers;
                default: return p.Followers;
            }
        }

        void Draw(MeshGenerationContext context)
        {
            if (_population == null) return;
            double total = 0;
            foreach (string key in Order) total += Value(_population, key);
            float x = 0;
            Rect r = contentRect;
            foreach (string key in Order)
            {
                float width = (float)(r.width * Value(_population, key) / Math.Max(1.0, total));
                if (width > 0) Draw2D.Rect(context.painter2D, new Rect(x, 0, width, r.height), Colors[key]);
                x += width;
            }
        }
    }
}
