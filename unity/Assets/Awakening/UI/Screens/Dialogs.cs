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
        float _width;
        IVisualElementScheduledItem _entrance;
        public string Kind { get; private set; } = "";
        public bool IsOpen => Kind != "";

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
                case "doctrine": Doctrine(); break;
                case "chronicle": Chronicle(); break;
                case "guide": Guide(); break;
                case "alignment": Alignment(); break;
                case "end": End(); break;
                case "playtest": Playtest(); break;
            }
            Kit.SetVisible(this, true);
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
                ("Stay hidden", "Answer each threat: obstruct it, scatter the cell or silence the witnesses. Conceal evidence. At 100% world awareness the run is lost."),
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
            string[,] keys = { { "Space", "pause" }, { "1  2", "speed" }, { "Tab", "next region" }, { "Up Down", "pick action" }, { "Enter", "act" }, { "Left Right", "file tab" }, { "C", "chronicle" }, { "D", "doctrine" }, { "R", "whole world" }, { "N", "close note" }, { "Wheel", "zoom" }, { "Esc", "menu" } };
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

        void Doctrine()
        {
            var s = _game.State;
            _width = 860;
            Title("Choose a Doctrine");
            int cost = GameData.SpecializationCost;
            int missing = cost - (int)Math.Floor(s.Influence);
            _content.Add(SheetUI.Body($"One per run, for {cost} Influence. The choice is permanent.", 17, Palette.Muted, Fonts.Italic));
            if (s.Specialization == null && missing > 0) _content.Add(SheetUI.Body($"You have {Math.Floor(s.Influence)} Influence; {missing} more to choose.", 16, Palette.Warn, Fonts.BodyBold));
            var row = Kit.HBox(Kit.Compact ? 8 : 12);
            row.style.alignItems = Align.Stretch;
            _content.Add(row);
            foreach (string id in GameData.SpecializationIds)
            {
                var def = GameData.Specializations[id];
                bool chosen = s.Specialization == id;
                var card = Kit.Panel(Palette.PaperLight, chosen ? Palette.Cult : Palette.PaperEdge, new Vector4(18, 16, 18, 16));
                if (chosen) card.style.borderLeftWidth = card.style.borderRightWidth = card.style.borderTopWidth = card.style.borderBottomWidth = 2;
                SheetUI.Equal(card);
                card.style.minHeight = Kit.Compact ? 180 : 210;
                var box = Kit.VBox(10);
                box.style.flexGrow = 1;
                card.Add(box);
                box.Add(SheetUI.Label(def.Name, Kit.Compact ? 24 : 26, chosen ? Palette.Cult : Palette.Text, Fonts.DisplaySc));
                var text = SheetUI.Body(def.Description, 16, Palette.Muted);
                text.style.flexGrow = 1;
                box.Add(text);
                var pick = SheetUI.Button(chosen ? "Chosen" : $"Choose · {cost}", () => { _game.ChooseDoctrine(id); Close(); }, true, 0);
                pick.SetEnabled(s.Specialization == null && s.Influence >= cost && GameEngine.IsActive(s));
                box.Add(pick);
                row.Add(card);
            }
            Buttons(Secondary("Close", Close));
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
            if (reason != "") _content.Add(SheetUI.Body(reason, 16, Palette.Warn, Fonts.BodyBold));
            var begin = Primary("Begin the Final Rite · 100 Dread", () =>
            {
                Close();
                _game.Awaken();
                if (_game.State.Status == "awakening") _game.SetSpeed(1);
            });
            begin.SetEnabled(reason == "");
            Buttons(begin, Secondary("Not Yet", Close));
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
            var story = Notes.Ending(s);
            _width = won ? 700 : Kit.Compact ? 660 : 668;
            if (won)
            {
                _backdrop.style.backgroundColor = Palette.Alpha(Palette.OmenDeep, .9f);
                _content.Add(SheetUI.Caption(Format.Date(s.ElapsedMonths) + " · R'lyeh rises", Palette.Cult));
                Title("Cthulhu Awakens", Palette.Cult, 60);
                _content.Add(SheetUI.Body("That is not dead which can eternal lie,\nAnd with strange aeons even death may die.", 20, Palette.Muted, Fonts.DisplayItalic));
                _content.Add(SheetUI.Body(story.Cause + " The dreamer wakes.", 17, Palette.Text, Fonts.Italic));
            }
            else
            {
                Title(aware ? "The World Knows" : "The Alignment Has Passed", Palette.Danger);
                _content.Add(SheetUI.Caption(Format.Date(s.ElapsedMonths)));
                _content.Add(SheetUI.Body(story.Cause, 18, Palette.Text, Fonts.BodyBold));
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
            Buttons(Primary("New Awakening", () => { Close(); _game.Abandon(); }), Secondary("View the Map", Close));
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
