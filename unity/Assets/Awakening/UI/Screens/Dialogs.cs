using System;
using System.Linq;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Framed modal documents, ported from godot/ui/dialogs.gd.</summary>
    public sealed class Dialogs : VisualElement
    {
        readonly GameController _game;
        readonly GameScreen _screen;
        readonly Stack _box, _content;
        readonly ScrollView _scroll;
        readonly VisualElement _backdrop;
        bool _confirmAbandon;
        string _talentBranch = GameData.BranchIds[0];
        float _width;
        IVisualElementScheduledItem _entrance;
        public string Kind { get; private set; } = "";
        public bool IsOpen => Kind != "";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal VisualElement LayoutBox => _box;
        internal void LayoutSelectTalentBranch(string branchId)
        {
            if (Kind != "talents") return;
            _talentBranch = branchId;
            BuildTalents();
            Fit();
        }
#endif

        public Dialogs(GameController game, GameScreen screen)
        {
            _game = game;
            _screen = screen;
            name = "dialogs";
            Kit.Fill(this);
            _backdrop = new VisualElement { pickingMode = PickingMode.Ignore };
            Kit.Fill(_backdrop);
            Add(_backdrop);
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            int pad = Kit.Compact ? 24 : 40;
            _box = Kit.Panel(Palette.Paper, Palette.DeskEdge, new Vector4(pad, pad * .85f, pad, pad * .85f), .16f);
            Kit.Frame(_box);
            Add(_box);
            _scroll = SheetUI.Scroll();
            _box.Add(_scroll);
            _content = Kit.VBox(Kit.Compact ? 12 : 16);
            _scroll.Add(_content);
            RegisterCallback<GeometryChangedEvent>(_ => Fit());
            SheetUI.LegacyNavigation(this);
            Kit.SetVisible(this, false);
        }

        public void Open(string kind)
        {
            if (IsOpen) Close();
            Kind = kind;
            _confirmAbandon = false;
            _content.Clear();
            _width = 560;
            _backdrop.style.backgroundColor = Palette.Alpha(Palette.Desk, .8f);
            switch (kind)
            {
                case "menu": Menu(); break;
                case "talents":
                case "doctrine":
                    Kind = "talents";
                    Talents();
                    break;
                case "chronicle": Chronicle(); break;
                case "guide": Guide(); break;
                case "alignment": Alignment(); break;
                case "concede": Concede(); break;
                case "end": End(); break;
                case "playtest": Playtest(); break;
            }
            Kit.SetVisible(this, true);
            _screen.SetModalOpen(true);
            _game.Hold();
            Fit();
            _scroll.scrollOffset = Vector2.zero;
            schedule.Execute(() =>
            {
                Fit();
                SheetUI.FocusFirst(_content);
                if (Kind == "end") Enter();
            });
        }

        void Fit()
        {
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;
            if (float.IsNaN(width) || width <= 0) return;
            _box.style.width = Mathf.Min(_width, width - 32);
            _scroll.style.maxHeight = height - (Kit.Compact ? 24 : 40) * 1.7f - 26;
        }

        public void Close()
        {
            if (!IsOpen) return;
            _entrance?.Pause();
            _box.style.opacity = 1;
            _box.style.scale = new Scale(Vector3.one);
            Kind = "";
            Kit.SetVisible(this, false);
            _screen.SetModalOpen(false);
            _content.Clear();
            _game.Release();
        }

        public bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                if (Kind != "guide") Close();
                return true;
            }
            return SheetUI.HandleKey(_content, key);
        }

        void Enter()
        {
            if (Kit.ReducedMotion) return;
            bool won = _game.State.Status == "won";
            float duration = won ? .9f : .35f;
            float start = Time.realtimeSinceStartup;
            _box.style.opacity = 0;
            _box.style.scale = new Scale(new Vector3(.96f, .96f, 1));
            _entrance?.Pause();
            _entrance = schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.realtimeSinceStartup - start) / duration);
                _box.style.opacity = t;
                _box.style.scale = new Scale(Vector3.one * Mathf.Lerp(.96f, 1, 1 - Mathf.Pow(1 - t, 4)));
                if (t >= 1 || !IsOpen) _entrance.Pause();
            }).Every(16);
        }

        void Title(string text, Color? color = null, int size = 40)
        {
            var column = Kit.VBox(4);
            column.Add(SheetUI.Label(text, Kit.Compact ? Mathf.RoundToInt(size * .8f) : size, color ?? Palette.Text, Fonts.Display));
            column.Add(Kit.Rule(Palette.Alpha(color ?? Palette.Text, .6f)));
            _content.Add(column);
        }

        void Buttons(params Button[] buttons)
        {
            var row = Kit.HBox(10);
            foreach (var button in buttons) row.Add(button);
            _content.Add(row);
        }

        Button Primary(string text, Action press) => SheetUI.Button(text, press, true);
        Button Secondary(string text, Action press) => SheetUI.Button(text, press);

        void Guide()
        {
            _width = 620;
            Title("How the Awakening Works");
            var steps = new[]
            {
                ("Grow", "Cells recruit and earn Influence. Spread to neighbouring regions."),
                ("Sacrifice", "Followers become Dread, which pays for the rites."),
                ("Build three sites", "Each takes 24 months and needs a quarter of its region as followers."),
                ("Stay hidden", "Answer each threat: obstruct it, scatter the cell or silence the witnesses. Conceal evidence or bury the record. At 100% world awareness the run is lost."),
                ("Awaken", "From September 1924, spend 100 Dread and hold three sites for 12 months."),
            };
            for (int i = 0; i < steps.Length; i++)
            {
                var row = Kit.HBox(16);
                row.style.alignItems = Align.Stretch;
                var number = SheetUI.Label(new[] { "I", "II", "III", "IV", "V" }[i], 28, Palette.Cult, Fonts.Display);
                number.style.width = 44;
                number.style.flexShrink = 0;
                number.style.height = StyleKeyword.Auto;
                number.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(number);
                var text = Kit.VBox(0);
                SheetUI.Equal(text);
                text.Add(SheetUI.Label(steps[i].Item1, 21, Palette.Text, Fonts.DisplaySc));
                text.Add(SheetUI.Body(steps[i].Item2, 16, Palette.Muted));
                row.Add(text);
                _content.Add(row);
            }
            _content.Add(SheetUI.Body("Time runs by itself; pause whenever you like, every action works while paused. Notes appear the first time something new happens.", 15, Palette.Text, Fonts.Italic));
            if (!Kit.Touch) _content.Add(Keys());
            Buttons(Primary("Understood", () => { _game.CompleteTutorial(); Close(); }));
        }

        void Menu()
        {
            _width = Kit.Touch ? 480 : 539;
            Title("Paused");
            var column = Kit.VBox(8);
            _content.Add(column);
            column.Add(Primary("Resume", Close));
            column.Add(Secondary("How to Play", () => Open("guide")));
            column.Add(Secondary("Chronicle", () => Open("chronicle")));
            column.Add(Kit.Rule());
            column.Add(SheetUI.Caption("Sound"));
            var sound = Kit.HBox(14);
            Button toggle = null;
            var slider = new Slider(0, 100) { value = (float)_game.Settings.Volume };
            slider.AddToClassList("aw-slider");
            slider.style.flexGrow = 1;
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.style.position = Position.Absolute;
            fill.style.left = fill.style.top = fill.style.bottom = 0;
            fill.style.backgroundColor = Palette.Cult;
            fill.style.width = Length.Percent(slider.value);
            slider.Q(className: "unity-base-slider__tracker").Add(fill);
            slider.RegisterValueChangedCallback(e =>
            {
                fill.style.width = Length.Percent(e.newValue);
                _game.SetAudio(_game.Settings.Muted, e.newValue);
            });
            slider.RegisterCallback<PointerUpEvent>(_ => _game.Sfx.Play("act"));
            toggle = SheetUI.Button(_game.Settings.Muted ? "Sound Off" : "Sound On", () =>
            {
                _game.SetAudio(!_game.Settings.Muted, slider.value);
                toggle.text = _game.Settings.Muted ? "Sound Off" : "Sound On";
            }, height: 40);
            toggle.style.minWidth = 130;
            sound.Add(toggle);
            sound.Add(slider);
            column.Add(sound);
            column.Add(SheetUI.Caption("Interface size"));
            column.Add(SheetUI.SizePicker(_game, 40));
            column.Add(SheetUI.Caption("Motion"));
            var motion = Kit.HBox(0);
            foreach (bool reduced in new[] { false, true })
            {
                Button option = null;
                option = SheetUI.Button(reduced ? "Reduced" : "Full", () =>
                {
                    _game.SetReducedMotion(reduced);
                    foreach (var child in motion.Children()) child.EnableInClassList("aw-on", child == option);
                }, height: 40);
                option.EnableInClassList("aw-on", Kit.ReducedMotion == reduced);
                SheetUI.Equal(option);
                motion.Add(option);
            }
            column.Add(motion);
            column.Add(SheetUI.Caption("First-run goals"));
            var goals = Kit.HBox(0);
            foreach (bool hidden in new[] { false, true })
            {
                Button option = null;
                option = SheetUI.Button(hidden ? "Hidden" : "Shown", () =>
                {
                    _game.SetHideGoals(hidden);
                    foreach (var child in goals.Children()) child.EnableInClassList("aw-on", child == option);
                }, height: 40);
                option.EnableInClassList("aw-on", _game.Settings.HideGoals == hidden);
                SheetUI.Equal(option);
                goals.Add(option);
            }
            column.Add(goals);
            column.Add(Kit.Rule());
            var links = Kit.HBox(18);
            links.Add(SheetUI.Link("GTC PLAYTEST LAB", () => Open("playtest")));
            links.Add(SheetUI.Link("EXPORT PLAYTEST REPORT", _game.ExportReport));
            column.Add(links);
            Button abandon = null;
            Action confirm = () =>
            {
                if (_confirmAbandon) { Close(); _game.Abandon(); }
                else { _confirmAbandon = true; abandon.text = "CLICK AGAIN TO ABANDON"; }
            };
            abandon = Kit.Stamp("ABANDON THIS RUN", confirm, Accent.Danger);
            abandon.userData = confirm;
            abandon.focusable = true;
            abandon.style.height = 38;
            column.Add(abandon);
            if (!Kit.Touch) column.Add(Keys());
            if (Debug.isDebugBuild || Application.isEditor)
            {
                column.Add(SheetUI.Caption("Playtest controls · no resources granted"));
                var dev = Kit.HBox(6);
                foreach (int months in new[] { 1, 12, 120 })
                {
                    var button = SheetUI.Button($"+{months} Mo", () => { _game.DebugAdvance(months); _screen.Refresh(); }, height: 0);
                    SheetUI.Equal(button);
                    button.SetEnabled(GameEngine.IsActive(_game.State));
                    dev.Add(button);
                }
                column.Add(dev);
            }
        }

        static VisualElement Keys()
        {
            string[,] keys = { { "Space", "pause" }, { "1  2", "speed" }, { "Tab", "next region" }, { "Up Down", "pick action" }, { "Enter", "act" }, { "Left Right", "file tab" }, { "C", "chronicle" }, { "T", "talents" }, { "R", "whole world" }, { "N", "close note" }, { "Wheel", "zoom" }, { "Esc", "menu" } };
            var grid = Kit.VBox(3);
            // Godot's six-column grid uses the widest item in each column.
            float[] widths = { 54, 87, 40, 62, 77, 87 };
            for (int row = 0; row < 4; row++)
            {
                var line = Kit.HBox(10);
                for (int pair = 0; pair < 3; pair++)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        var label = SheetUI.Label(keys[row * 3 + pair, c], 13, c == 0 ? Palette.Text : Palette.Muted, c == 0 ? Fonts.TypeBold : Fonts.Type);
                        label.style.minWidth = widths[pair * 2 + c];
                        line.Add(label);
                    }
                }
                grid.Add(line);
            }
            return grid;
        }

        void Talents()
        {
            _width = Kit.Compact ? 920 : 1220;
            BuildTalents();
        }

        void BuildTalents()
        {
            var s = _game.State;
            _content.Clear();
            DifficultyDefinition diff = GameData.Difficulties[s.Difficulty];
            int total = diff.TalentSlots;
            int open = GameEngine.TalentSlotsOpen(s);
            int unused = GameEngine.TalentSlotsUnused(s);
            int taken = s.Talents.Count;
            int nextMonth = GameEngine.NextTalentSlotMonth(s);
            int cost = GameEngine.TalentCost(s);

            var headerRow = Kit.HBox(12);
            headerRow.style.alignItems = Align.Center;
            var heading = SheetUI.Label("Talents of the Cult", Kit.Compact ? 28 : 36, Palette.Text, Fonts.Display);
            heading.style.flexGrow = 1;
            heading.style.flexShrink = 1;
            headerRow.Add(heading);
            headerRow.Add(new IconButton("close", "Close  (Esc)", Close, true));
            var titleBlock = Kit.VBox(4);
            titleBlock.Add(headerRow);
            titleBlock.Add(Kit.Rule(Palette.Alpha(Palette.Text, .6f)));
            _content.Add(titleBlock);

            // Seals header: pips for every seal in this difficulty, open/unused counts, next seal date and cost.
            var sealsBar = Kit.Panel(Palette.PaperLight, unused > 0 ? Palette.Cult : Palette.PaperEdge, Kit.Compact ? new Vector4(12, 8, 12, 9) : new Vector4(14, 10, 14, 10), 0.08f, 4);
            sealsBar.name = "talent-seals-bar";
            sealsBar.style.borderLeftWidth = 4;
            sealsBar.style.borderLeftColor = unused > 0 ? Palette.Cult : Palette.PaperEdge;

            var topLine = new Flow(12, 4);
            topLine.style.alignItems = Align.Center;
            var pips = Kit.HBox(5);
            pips.style.alignItems = Align.Center;
            pips.name = "talent-seal-pips";
            for (int i = 0; i < total; i++)
            {
                int sealMonth = diff.TalentSlotMonths[i];
                bool spent = i < taken;
                bool ready = !spent && i < open;
                var pip = SheetUI.Label(spent ? "◆" : ready ? "◈" : "○", 15, spent ? Palette.Cult : ready ? Palette.Warn : Palette.Faint, Fonts.BodyBold);
                pip.pickingMode = PickingMode.Position;
                pip.tooltip = spent
                    ? $"Seal {i + 1} ({Format.Date(sealMonth)}): spent on {GameData.Talents[s.Talents[i]].Name}."
                    : ready
                        ? $"Seal {i + 1} ({Format.Date(sealMonth)}): unbroken and ready to spend."
                        : $"Seal {i + 1}: opens in {Format.Date(sealMonth)} (in {Format.Months(sealMonth - s.ElapsedMonths)}).";
                pips.Add(pip);
            }
            topLine.Add(pips);

            string unusedText = unused == 1 ? "1 UNBROKEN" : $"{unused} UNBROKEN";
            var summary = Kit.Label($"{open}/{total} SEALS OPEN  ·  {unusedText}", 12, unused > 0 ? Palette.Cult : Palette.Text, Fonts.TypeBold);
            summary.name = "talent-seals-summary";
            topLine.Add(summary);

            string timingText = nextMonth >= 0
                ? $"·  NEXT SEAL {Format.Date(nextMonth).ToUpperInvariant()} (IN {nextMonth - s.ElapsedMonths} MO)"
                : taken >= total ? "·  EVERY SEAL SPENT" : "·  ALL SEALS OPEN";
            var timing = Kit.Label(timingText, 12, Palette.Muted, Fonts.TypeBold);
            timing.name = "talent-next-seal";
            topLine.Add(timing);
            sealsBar.Add(topLine);

            string subLine = taken < total
                ? $"Next talent costs {cost} Influence (you have {Format.Count(s.Influence)}). Only {total} seals open on {diff.Name} — the tree cannot be completed in one run, and no choice is undone."
                : $"All {total} seals of this run are spent. Chosen talents are permanent.";
            bool shortOnInf = taken < total && unused > 0 && s.Influence < cost;
            sealsBar.Add(SheetUI.Body(subLine, Kit.Compact ? 13 : 14, shortOnInf ? Palette.Warn : Palette.Muted, Fonts.Italic));
            _content.Add(sealsBar);

            if (Kit.Compact)
            {
                var tabs = Kit.HBox(4);
                tabs.name = "talent-branch-tabs";
                tabs.style.alignItems = Align.Stretch;
                foreach (string branchId in GameData.BranchIds)
                {
                    string id = branchId;
                    int branchTaken = GameData.TalentIds.Count(tid => GameData.Talents[tid].Branch == id && GameEngine.HasTalent(s, tid));
                    Action pickBranch = () =>
                    {
                        _talentBranch = id;
                        BuildTalents();
                        schedule.Execute(() => { Fit(); SheetUI.FocusFirst(_content); });
                    };
                    var tab = new Button(pickBranch)
                    {
                        text = $"{GameData.Branches[id].Name} ({branchTaken}/3)",
                        focusable = true,
                        name = "talent-tab-" + id,
                        userData = pickBranch,
                    };
                    tab.AddToClassList("aw-tab");
                    tab.EnableInClassList("aw-on", _talentBranch == id);
                    tab.style.fontSize = 16;
                    tabs.Add(tab);
                }
                _content.Add(tabs);
                _content.Add(BuildBranch(s, _talentBranch, cost, wide: true));
            }
            else
            {
                var cols = Kit.HBox(12);
                cols.name = "talent-branches";
                cols.style.alignItems = Align.Stretch;
                foreach (string branchId in GameData.BranchIds)
                {
                    var col = BuildBranch(s, branchId, cost, wide: false);
                    SheetUI.Equal(col);
                    cols.Add(col);
                }
                _content.Add(cols);
            }

            Buttons(Secondary("Close", Close));
        }

        VisualElement BuildBranch(GameState s, string branchId, int cost, bool wide)
        {
            BranchDefinition branch = GameData.Branches[branchId];
            var nodes = GameData.TalentIds.Select(id => GameData.Talents[id]).Where(t => t.Branch == branchId).ToList();
            TalentDefinition root = nodes[0], midA = nodes[1], midB = nodes[2], cap = nodes[3];
            int branchTaken = nodes.Count(n => GameEngine.HasTalent(s, n.Id));
            bool rootTaken = GameEngine.HasTalent(s, root.Id);
            bool aTaken = GameEngine.HasTalent(s, midA.Id);
            bool bTaken = GameEngine.HasTalent(s, midB.Id);

            var panel = Kit.Panel(Palette.Paper, Palette.PaperEdge, new Vector4(10, 10, 10, 10), 0.06f, 6);
            panel.name = "talent-branch-" + branchId;
            panel.style.borderTopWidth = 2;
            panel.style.borderTopColor = branchTaken > 0 ? Palette.Cult : Palette.PaperEdge;

            var head = Kit.HBox(8);
            var nameLabel = Kit.Label(branch.Name, Kit.Compact ? 20 : 21, branchTaken > 0 ? Palette.Cult : Palette.Text, Fonts.DisplaySc);
            nameLabel.style.flexGrow = 1;
            nameLabel.style.flexShrink = 1;
            head.Add(nameLabel);
            head.Add(Kit.Label($"{branchTaken} / 3", 12, branchTaken > 0 ? Palette.Cult : Palette.Muted, Fonts.TypeBold));
            panel.Add(head);
            panel.Add(Kit.Body(branch.Description, 13, Palette.Muted, Fonts.Italic));

            panel.Add(BuildTalentNode(s, root, cost));

            var forkRow = Kit.VBox(2);
            forkRow.style.alignItems = Align.Center;
            forkRow.Add(new TreeConnector(fork: true, wide: wide, rootTaken, aTaken, bTaken));
            string forkCaption = aTaken || bTaken ? "TIER II · ONE CHOSEN, THE OTHER CLOSED" : "TIER II · CHOOSE ONE (CLOSES THE OTHER)";
            var forkLabel = Kit.Body(forkCaption, 10, aTaken || bTaken ? Palette.Muted : Palette.Warn, Fonts.TypeBold);
            forkLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            forkRow.Add(forkLabel);
            panel.Add(forkRow);

            if (wide)
            {
                var pair = Kit.HBox(8);
                pair.style.alignItems = Align.Stretch;
                var cardA = BuildTalentNode(s, midA, cost);
                var cardB = BuildTalentNode(s, midB, cost);
                SheetUI.Equal(cardA);
                SheetUI.Equal(cardB);
                pair.Add(cardA);
                pair.Add(cardB);
                panel.Add(pair);
            }
            else
            {
                var pair = Kit.VBox(5);
                pair.style.borderLeftWidth = 2;
                pair.style.borderLeftColor = Palette.Alpha(rootTaken ? Palette.Cult : Palette.PaperEdge, 0.55f);
                pair.style.paddingLeft = 6;
                pair.Add(BuildTalentNode(s, midA, cost));
                var orLabel = Kit.Label("— OR —", 10, Palette.Faint, Fonts.TypeBold);
                orLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                pair.Add(orLabel);
                pair.Add(BuildTalentNode(s, midB, cost));
                panel.Add(pair);
            }

            panel.Add(new TreeConnector(fork: false, wide: wide, aTaken || bTaken, aTaken, bTaken));
            panel.Add(BuildTalentNode(s, cap, cost));
            return panel;
        }

        VisualElement BuildTalentNode(GameState s, TalentDefinition t, int cost)
        {
            bool taken = GameEngine.HasTalent(s, t.Id);
            bool excluded = !taken && t.Excludes != null && GameEngine.HasTalent(s, t.Excludes);
            string reason = GameEngine.TalentReason(s, t.Id);
            bool available = reason.Length == 0;
            bool shortInf = !taken && !excluded && !available && reason.StartsWith("Need ", StringComparison.Ordinal);
            bool sealLocked = !taken && !excluded && !available && (reason.StartsWith("The next seal", StringComparison.Ordinal) || reason.StartsWith("Every seal", StringComparison.Ordinal));

            Color fill = taken || available ? Palette.PaperLight : excluded ? Palette.PaperShade : Palette.Paper;
            Color edge = taken || available ? Palette.Cult : excluded ? Palette.Alpha(Palette.Danger, 0.5f) : Palette.PaperEdge;
            Color leftBar = taken || available ? Palette.Cult : excluded ? Palette.Danger : shortInf ? Palette.Warn : Palette.PaperEdge;

            var card = Kit.Panel(fill, edge, new Vector4(10, 8, 10, 8), 0.05f, 4);
            card.name = "talent-node-" + t.Id;
            card.style.borderLeftWidth = taken ? 4 : 3;
            card.style.borderLeftColor = leftBar;
            if (taken) card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderRightWidth = 2;

            var head = Kit.HBox(6);
            string tierName = t.Tier == 1 ? "TIER I · ROOT" : t.Tier == 2 ? "TIER II · FORK" : "TIER III · CAPSTONE";
            var tierLabel = Kit.Label(tierName, 10, Palette.Faint, Fonts.TypeBold);
            tierLabel.style.flexGrow = 1;
            tierLabel.style.flexShrink = 1;
            head.Add(tierLabel);

            string badgeText = taken ? "◆ TAKEN"
                : available ? $"READY · {cost} INF"
                : excluded ? "× CLOSED"
                : shortInf ? $"NEED {cost} INF"
                : sealLocked ? "SEALED"
                : "LOCKED";
            Color badgeColor = taken || available ? Palette.Cult : excluded ? Palette.Danger : shortInf ? Palette.Warn : Palette.Muted;
            var badge = Kit.Label(badgeText, 10, badgeColor, Fonts.TypeBold);
            badge.name = "talent-state-" + t.Id;
            badge.style.flexShrink = 0;
            head.Add(badge);
            card.Add(head);

            var title = Kit.Body(t.Name, Kit.Compact ? 16 : 17, taken ? Palette.Cult : excluded ? Palette.Muted : Palette.Text, excluded ? Fonts.Italic : Fonts.DisplaySc);
            card.Add(title);

            var desc = Kit.Body(t.Description, 13, excluded ? Palette.Faint : Palette.Muted);
            desc.style.flexGrow = 1;
            card.Add(desc);

            if (available)
            {
                string talentId = t.Id;
                var pick = SheetUI.Button($"Take · {cost} Inf", () =>
                {
                    _game.TakeTalent(talentId);
                    BuildTalents();
                    schedule.Execute(() => { Fit(); SheetUI.FocusFirst(_content); });
                }, primary: true, height: Kit.Touch ? 38 : 30);
                pick.name = "take-" + t.Id;
                pick.style.fontSize = Kit.Compact ? 15 : 16;
                pick.style.paddingTop = pick.style.paddingBottom = 4;
                card.Add(pick);
            }
            else if (!taken)
            {
                var why = Kit.Body(reason, 12, excluded ? Palette.Danger : shortInf ? Palette.Warn : Palette.Muted, excluded ? Fonts.BodyBold : Fonts.Italic);
                why.name = "talent-reason-" + t.Id;
                card.Add(why);
            }
            return card;
        }

        /// <summary>Ink stem connecting tiers in the talent tree: forks into Tier II and converges into Tier III.</summary>
        sealed class TreeConnector : VisualElement
        {
            readonly bool _fork, _wide, _active, _leftActive, _rightActive;

            public TreeConnector(bool fork, bool wide, bool active, bool leftActive, bool rightActive)
            {
                _fork = fork;
                _wide = wide;
                _active = active;
                _leftActive = leftActive;
                _rightActive = rightActive;
                pickingMode = PickingMode.Ignore;
                style.height = 12;
                style.width = Length.Percent(100);
                style.flexShrink = 0;
                generateVisualContent += Draw;
            }

            void Draw(MeshGenerationContext context)
            {
                Painter2D p = context.painter2D;
                Rect r = contentRect;
                float cx = r.width * 0.5f;
                float midY = r.height * 0.5f;
                Color dim = Palette.Alpha(Palette.PaperEdge, 0.7f);
                Color lit = Palette.Cult;
                if (!_wide)
                {
                    Draw2D.Line(p, new Vector2(cx, 0), new Vector2(cx, r.height), _active ? lit : dim, _active ? 2f : 1.25f);
                    return;
                }
                float lx = r.width * 0.25f;
                float rx = r.width * 0.75f;
                if (_fork)
                {
                    Draw2D.Line(p, new Vector2(cx, 0), new Vector2(cx, midY), _active ? lit : dim, _active ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(lx, midY), new Vector2(cx, midY), _leftActive ? lit : dim, _leftActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(cx, midY), new Vector2(rx, midY), _rightActive ? lit : dim, _rightActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(lx, midY), new Vector2(lx, r.height), _leftActive ? lit : dim, _leftActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(rx, midY), new Vector2(rx, r.height), _rightActive ? lit : dim, _rightActive ? 2f : 1.25f);
                }
                else
                {
                    Draw2D.Line(p, new Vector2(lx, 0), new Vector2(lx, midY), _leftActive ? lit : dim, _leftActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(rx, 0), new Vector2(rx, midY), _rightActive ? lit : dim, _rightActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(lx, midY), new Vector2(cx, midY), _leftActive ? lit : dim, _leftActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(cx, midY), new Vector2(rx, midY), _rightActive ? lit : dim, _rightActive ? 2f : 1.25f);
                    Draw2D.Line(p, new Vector2(cx, midY), new Vector2(cx, r.height), _active ? lit : dim, _active ? 2f : 1.25f);
                }
            }
        }

        void Chronicle()
        {
            var s = _game.State;
            _width = 760;
            Title("The Chronicle");
            _content.Add(SheetUI.Caption($"Seed {s.Seed} · {GameData.Difficulties[s.Difficulty].Name} · latest first"));
            var scroll = SheetUI.Scroll();
            scroll.style.height = Kit.Compact ? 240 : 470;
            scroll.style.flexShrink = 0;
            var list = Kit.VBox(0);
            scroll.Add(list);
            _content.Add(scroll);
            foreach (var entry in s.Log.AsEnumerable().Reverse())
            {
                string classification = GameController.Classify(entry.Message);
                Color accent = classification == "threat" || classification == "loss" ? Palette.Danger : classification == "omen" ? Palette.Omen : classification == "site" || classification == "phase" ? Palette.Cult : Palette.Alpha(Palette.Text, .25f);
                var row = Kit.HBox(14);
                row.style.alignItems = Align.Stretch;
                row.style.paddingTop = row.style.paddingBottom = 7;
                var stripe = new VisualElement();
                stripe.style.backgroundColor = accent;
                stripe.style.width = 3;
                stripe.style.flexShrink = 0;
                row.Add(stripe);
                var when = SheetUI.Label(Format.DateShort(entry.Month), 13, Palette.Muted, Fonts.TypeBold);
                when.style.alignSelf = Align.Center;
                when.style.width = 80;
                when.style.flexShrink = 0;
                row.Add(when);
                var text = SheetUI.Body(entry.Message, 16);
                SheetUI.Equal(text);
                row.Add(text);
                list.Add(row);
            }
            if (s.Log.Count == 0) list.Add(SheetUI.Body("The world has not yet heard your name.", 17, Palette.Muted, Fonts.Italic));
            Buttons(Secondary("Close", Close));
        }

        void Alignment()
        {
            var s = _game.State;
            _width = 620;
            Title("The Stars Are Right", Palette.Cult);
            _content.Add(SheetUI.Body($"{Balance.Deadline - s.ElapsedMonths} months remain to complete the awakening; the alignment closes in {Format.Date(Balance.Deadline)}. Time is paused.", 17, Palette.Text, Fonts.Italic));
            var checks = Kit.VBox(8);
            _content.Add(checks);
            int sites = 0;
            foreach (string id in GameData.RegionIds)
            {
                var r = s.Regions[id];
                if (!r.Site.Invested) continue;
                sites++;
                checks.Add(Check(r.Site.Progress >= Balance.SiteMonths && Derived.Supported(r) && !r.Site.Dormant, GameData.Regions[id].Name, Derived.SiteLabel(r)));
            }
            for (int i = sites; i < Balance.RequiredSites; i++) checks.Add(Check(false, "Unfunded site", "Fund a site in a small region"));
            checks.Add(Check(s.Dread >= Balance.FinalDread, "Dread", $"{Format.Count(s.Dread)} / {Balance.FinalDread}"));
            string reason = GameEngine.AwakeningReason(s);
            bool canConcede = GameEngine.CanConcede(s);
            if (canConcede)
            {
                RiteMargin margin = Forecast.Margin(s);
                _content.Add(SheetUI.Body(Notes.RiteDoomedHeadline + ". " + Notes.RiteDoomedCause(s, margin), 16, Palette.Danger, Fonts.BodyBold));
            }
            else if (reason != "")
            {
                _content.Add(SheetUI.Body(reason, 16, Palette.Warn, Fonts.BodyBold));
            }
            var begin = Primary("Begin the Final Rite · 100 Dread", () =>
            {
                Close();
                _game.Awaken();
                if (_game.State.Status == "awakening") _game.SetSpeed(1);
            });
            begin.SetEnabled(reason == "");
            if (canConcede)
            {
                Action openConcede = () => Open("concede");
                Button pass = Kit.Stamp("LET THE STARS PASS", openConcede, Accent.Danger);
                pass.userData = openConcede;
                pass.focusable = true;
                pass.style.height = StyleKeyword.Auto;
                pass.style.minHeight = 46;
                pass.style.paddingLeft = pass.style.paddingRight = 14;
                Buttons(begin, pass, Secondary("Not Yet", Close));
            }
            else Buttons(begin, Secondary("Not Yet", Close));
        }

        void Concede()
        {
            var s = _game.State;
            RiteMargin margin = Forecast.Margin(s);
            _width = 560;
            Title("Let the Stars Pass", Palette.Danger);
            _content.Add(SheetUI.Caption($"{Format.Date(s.ElapsedMonths)} · Alignment closes {Format.Date(Balance.Deadline)}"));
            _content.Add(SheetUI.Body(Notes.RiteDoomedHeadline + ".", 18, Palette.Danger, Fonts.BodyBold));
            _content.Add(SheetUI.Body(Notes.RiteDoomedCause(s, margin), 16, Palette.Text, Fonts.Italic));
            _content.Add(SheetUI.Body("Closing the run now records a defeat and disperses the cult before the alignment ends. You can also stay and watch the remaining months pass.", 15, Palette.Muted));
            Action confirmAction = () =>
            {
                Close();
                _game.Concede();
            };
            Button confirm = Kit.Stamp("LET THE STARS PASS", confirmAction, Accent.Danger);
            confirm.userData = confirmAction;
            confirm.focusable = true;
            confirm.style.height = StyleKeyword.Auto;
            confirm.style.minHeight = 46;
            confirm.style.paddingLeft = confirm.style.paddingRight = 16;
            confirm.SetEnabled(GameEngine.CanConcede(s));
            Buttons(Secondary("Keep Watching", Close), confirm);
        }

        static VisualElement Check(bool ok, string name, string detail)
        {
            var row = Kit.HBox(12);
            var mark = SheetUI.Label(ok ? "◆" : "×", ok ? 18 : 20, ok ? Palette.Cult : Palette.Danger, Fonts.BodyBold);
            mark.style.width = 18;
            mark.style.flexShrink = 0;
            row.Add(mark);
            var title = SheetUI.Label(name, 20, Palette.Text, Fonts.DisplaySc);
            title.style.width = 170;
            title.style.flexShrink = 0;
            row.Add(title);
            var text = SheetUI.Body(detail, 16, ok ? Palette.Text : Palette.Danger, Fonts.Italic);
            SheetUI.Equal(text);
            row.Add(text);
            return row;
        }

        void End()
        {
            var s = _game.State;
            bool won = s.Status == "won";
            bool aware = s.ResultReason != null && s.ResultReason.StartsWith("The world knows", StringComparison.Ordinal);
            bool conceded = s.ResultCode == "conceded";
            var story = Notes.Ending(s);
            _width = won ? 700 : Kit.Compact ? 660 : 668;
            if (won)
            {
                _backdrop.style.backgroundColor = Palette.Alpha(Palette.OmenDeep, .9f);
                _content.Add(SheetUI.Caption(Format.Date(s.ElapsedMonths) + " · R'lyeh rises", Palette.Cult));
                Title("Cthulhu Awakens", Palette.Cult, 60);
                _content.Add(SheetUI.Body("That is not dead which can eternal lie,\nAnd with strange aeons even death may die.", 20, Palette.Muted, Fonts.DisplayItalic));
                _content.Add(SheetUI.Body(story.Cause + " The dreamer wakes.", 17, Palette.Text, Fonts.Italic));
                if (!string.IsNullOrEmpty(story.Fact)) _content.Add(SheetUI.Body(story.Fact, 16, Palette.Muted, Fonts.Italic));
            }
            else
            {
                Title(aware ? "The World Knows" : conceded ? "The Stars Passed Unanswered" : "The Alignment Has Passed", Palette.Danger);
                _content.Add(SheetUI.Caption(Format.Date(s.ElapsedMonths)));
                _content.Add(SheetUI.Body(story.Cause, 18, Palette.Text, Fonts.BodyBold));
                if (!string.IsNullOrEmpty(story.Fact)) _content.Add(SheetUI.Body(story.Fact, 16, Palette.Muted, Fonts.Italic));
                if (story.Checks.Count > 0)
                {
                    var checks = Kit.VBox(6);
                    foreach (var check in story.Checks) checks.Add(Check(check.ok, check.name, check.detail));
                    _content.Add(checks);
                }
                if (story.Advice.Count > 0)
                {
                    _content.Add(SheetUI.Caption("Next time", Palette.Cult));
                    foreach (string line in story.Advice)
                    {
                        var row = Kit.HBox(10);
                        row.style.alignItems = Align.FlexStart;
                        row.Add(SheetUI.Label("—", 16, Palette.Cult));
                        var text = SheetUI.Body(line, 16);
                        SheetUI.Equal(text);
                        row.Add(text);
                        _content.Add(row);
                    }
                }
            }
            _content.Add(Kit.Rule(Palette.Alpha(Palette.Text, .3f), false));
            var telemetry = s.Telemetry;
            var items = new[]
            {
                ("Months", s.ElapsedMonths.ToString()), ("Followers", Format.Count(Derived.TotalFollowers(s))), ("Sites held", $"{Derived.ReadySites(s)} / 3"),
                ("Sacrificed", Format.Count(telemetry.Sacrificed)), ("Lost to threats", Format.Count(telemetry.RaidLosses)),
                ("Threats answered", Notes.Responses.Sum(id => telemetry.Actions.TryGetValue(id, out var count) ? count : 0).ToString()),
            };
            var grid = Kit.VBox(10);
            int columns = Kit.Compact ? 3 : 6;
            for (int start = 0; start < items.Length; start += columns)
            {
                var row = Kit.HBox(22);
                int column = 0;
                foreach (var item in items.Skip(start).Take(columns))
                {
                    var cell = Kit.VBox(0);
                    if (Kit.Compact) cell.style.minWidth = new[] { 84f, 126f, 134.4f }[column];
                    column++;
                    cell.Add(SheetUI.Caption(item.Item1));
                    cell.Add(SheetUI.Label(item.Item2, 22, Palette.Text, Fonts.BodyBold));
                    row.Add(cell);
                }
                grid.Add(row);
            }
            _content.Add(grid);
            _content.Add(SheetUI.Caption($"Seed {s.Seed} · {GameData.Difficulties[s.Difficulty].Name}"));
            if (_game.Lab.Confirmed && _game.Lab.Active().TryGetValue("closed", out object closed) && closed is bool ended && ended)
            {
                _content.Add(SheetUI.Body("Optional comment to GTC (up to 1,000 characters). Please omit personal details.", 15, Palette.Muted));
                var comment = SheetUI.Field("What stood out during this run?", 1000);
                _content.Add(comment);
                var status = SheetUI.Caption("");
                _content.Add(Secondary("Queue Comment", () =>
                {
                    if (_game.SubmitPlaytestComment(comment.value))
                    {
                        comment.value = "";
                        status.text = "Comment saved locally for delivery.";
                    }
                    else status.text = "Comment could not be queued. Export the local record.";
                }));
                _content.Add(status);
            }
            string difficulty = s.Difficulty;
            string seed = s.Seed;
            Action retry = () => { Close(); _game.Begin(difficulty, seed); };
            Action fresh = () => { Close(); _game.Abandon(); };
            if (won) Buttons(Primary("New World", fresh), Secondary("Try this world again", retry), Secondary("View the Map", Close));
            else Buttons(Primary("Try this world again", retry), Secondary("New World", fresh), Secondary("View the Map", Close));
            var links = Kit.HBox(18);
            links.Add(SheetUI.Link("EXPORT REPORT", _game.ExportReport));
            links.Add(SheetUI.Link("GTC PLAYTEST STATUS", () => Open("playtest")));
            _content.Add(links);
        }

        void Playtest()
        {
            _width = 650;
            Title("GTC Playtest Lab");
            _content.Add(new PlaytestPanel(_game));
            Buttons(Secondary("Close", Close));
        }
    }
}
