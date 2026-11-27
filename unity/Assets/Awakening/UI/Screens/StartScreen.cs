using System;
using System.Collections.Generic;
using Awakening.Core.Rules;
using Awakening.UI.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>The frontispiece and run setup, ported from godot/ui/start_screen.gd.</summary>
    public sealed class StartScreen : VisualElement
    {
        readonly GameController _game;
        readonly Stack _form;
        readonly TextField _seed;
        readonly Button _begin, _resume;
        readonly Stack _ruleBox;
        readonly Label _ruleTitle, _ruleBody;
        readonly Dictionary<string, Button> _difficulties = new Dictionary<string, Button>();
        string _difficulty = "normal";
        VisualElement _ask;
        static Texture2D _shadeTexture;

        public StartScreen(GameController game)
        {
            _game = game;
            name = "start-screen";
            Kit.Fill(this);
            style.backgroundColor = Palette.Desk;
            var map = new WorldMap { ShowLabels = false, pickingMode = PickingMode.Ignore };
            Kit.Fill(map);
            map.style.left = 380;
            map.style.top = map.style.bottom = 60;
            Add(map);
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };
            Kit.Fill(shade);
            shade.style.backgroundImage = ShadeTexture();
            shade.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100));
            Add(shade);
            var row = Kit.HBox(Kit.Compact ? 28 : 64);
            Kit.Fill(row);
            row.style.left = row.style.right = Kit.Compact ? 36 : 96;
            row.style.top = row.style.bottom = Kit.Compact ? 18 : 72;
            Add(row);
            var title = Kit.VBox(0);
            SheetUI.Equal(title);
            title.style.justifyContent = Justify.Center;
            title.Add(SheetUI.Label("BEING A CHRONICLE OF THE YEARS 1908 – 1926", 14, Palette.Brass, Fonts.TypeBold));
            var lines = Kit.VBox(Kit.Compact ? -16 : -30);
            lines.Add(SheetUI.Label("The Awakening", Kit.Compact ? 64 : 112, Palette.Lamp, Fonts.Display));
            lines.Add(SheetUI.Label("of Cthulhu", Kit.Compact ? 54 : 92, Palette.Brass, Fonts.DisplayItalic));
            title.Add(lines);
            title.Add(Gap(14));
            var rule = Kit.Rule(Palette.Alpha(Palette.Brass, .7f));
            rule.style.width = Kit.Compact ? 300 : 420;
            title.Add(rule);
            title.Add(Gap(14));
            title.Add(SheetUI.Label("Build the cult. Feed the rite. Wake the god.", Kit.Compact ? 21 : 26, Palette.LampDim, Fonts.DisplayItalic));
            title.Add(Gap(22));
            title.Add(SheetUI.Caption($"{Balance.PreparationMonths} months to prepare  ·  3 ritual sites  ·  1 sleeping god", Palette.LampDim));
            row.Add(title);

            int pad = Kit.Compact ? 22 : 32;
            var setup = Kit.Panel(Palette.Paper, Palette.DeskEdge, new Vector4(pad, pad, pad, pad), .16f);
            Kit.Frame(setup);
            setup.style.width = Kit.Compact ? 420 : 430;
            setup.style.flexShrink = 0;
            var scroll = SheetUI.Scroll();
            setup.Add(scroll);
            _form = Kit.VBox(Kit.Compact ? 10 : 14);
            scroll.Add(_form);
            row.Add(setup);
            RegisterCallback<GeometryChangedEvent>(_ => scroll.style.maxHeight = resolvedStyle.height - pad * 2 - 42);
            if (!string.IsNullOrEmpty(game.StorageNotice)) _form.Add(SheetUI.Body(game.StorageNotice, 15, Palette.Warn, Fonts.BodyBold));
            var saved = game.State;
            bool resume = saved != null && !GameEngine.IsOver(saved);
            if (resume)
            {
                _form.Add(SheetUI.Caption("Run in progress"));
                _form.Add(SheetUI.Label($"{Format.Date(saved.ElapsedMonths)}  ·  {GameData.Difficulties[saved.Difficulty].Name}", 22, Palette.Text, Fonts.Display));
                _resume = SheetUI.Button("Continue", game.ShowGame, true, 50);
                _form.Add(_resume);
                _form.Add(Kit.Rule());
            }
            _form.Add(SheetUI.Caption("New awakening"));
            var levels = Kit.HBox(0);
            foreach (string id in GameData.DifficultyIds)
            {
                var button = SheetUI.Button(GameData.Difficulties[id].Name, () => Pick(id), height: 42);
                button.style.fontSize = 20;
                button.tooltip = GameData.Difficulties[id].Rule;
                SheetUI.Equal(button);
                _difficulties[id] = button;
                levels.Add(button);
            }
            _form.Add(levels);
            _ruleBox = Kit.Panel(Palette.PaperLight, Palette.PaperEdge, new Vector4(12, 8, 12, 9), 0.06f, 4);
            _ruleBox.name = "difficulty-rule";
            _ruleBox.style.borderLeftWidth = 3;
            _ruleTitle = SheetUI.Caption("");
            _ruleTitle.style.whiteSpace = WhiteSpace.Normal;
            _ruleTitle.style.height = StyleKeyword.Auto;
            _ruleBody = SheetUI.Body("", Kit.Compact ? 13 : 14, Palette.Text);
            _ruleBody.name = "difficulty-rule-text";
            _ruleBox.Add(_ruleTitle);
            _ruleBox.Add(_ruleBody);
            _form.Add(_ruleBox);
            _seed = SheetUI.Field("World seed (optional)", 40, 17);
            _seed.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    e.StopImmediatePropagation();
                    Begin();
                }
            });
            _form.Add(_seed);
            if (game.Lab.Configured() || game.Lab.Corrupt || game.Lab.PendingEvents() > 0) _form.Add(new PlaytestPanel(game, true));
            _begin = SheetUI.Button("Begin", Begin, true, 50);
            _form.Add(_begin);
            if (resume) _form.Add(SheetUI.Body("Beginning anew replaces the run in progress.", 15, Palette.Muted, Fonts.Italic));
            if (Kit.Touch)
            {
                var sizes = Kit.VBox(6);
                sizes.Add(SheetUI.Caption("Interface size"));
                sizes.Add(SheetUI.SizePicker(game, 44, false));
                _form.Add(sizes);
            }
            if (game.LastResult != null)
            {
                var r = game.LastResult;
                _form.Add(SheetUI.Body($"Last run: {(r.TryGetValue("won", out var won) && won is bool yes && yes ? "awakened" : "failed")} in {Format.Date(Convert.ToInt32(r["month"]))} · seed {r["seed"]}", 15, Palette.Muted, Fonts.Italic));
            }
            Pick(_difficulty);
            SheetUI.LegacyNavigation(this);
            schedule.Execute(FocusForm);
            if (!Kit.Compact)
            {
                var credit = SheetUI.Label("Map: Natural Earth, public domain  ·  v" + Application.version, 12, Palette.Alpha(Palette.LampDim, .78f), Fonts.TypeBold);
                credit.style.position = Position.Absolute;
                credit.style.left = 96;
                credit.style.bottom = 20;
                Add(credit);
            }
        }

        static Texture2D ShadeTexture()
        {
            if (_shadeTexture != null) return _shadeTexture;
            _shadeTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                name = "Frontispiece shade", hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            var colors = new Color[256];
            for (int i = 0; i < colors.Length; i++)
            {
                float t = i / 255f;
                float alpha = t <= .45f ? Mathf.Lerp(.98f, .85f, t / .45f) : Mathf.Lerp(.85f, .35f, (t - .45f) / .55f);
                colors[i] = Palette.Alpha(Palette.Desk, alpha);
            }
            _shadeTexture.SetPixels(colors);
            _shadeTexture.Apply(false, true);
            return _shadeTexture;
        }

        static VisualElement Gap(int height) => new VisualElement { style = { height = height, flexShrink = 0 }, pickingMode = PickingMode.Ignore };

        void Pick(string id)
        {
            _difficulty = id;
            foreach (var entry in _difficulties) entry.Value.EnableInClassList("aw-on", entry.Key == id);
            DifficultyDefinition def = GameData.Difficulties[id];
            Color accent = id == "hard" ? Palette.Danger : id == "easy" ? Palette.Cult : Palette.Warn;
            _ruleBox.style.borderLeftColor = accent;
            string pace = id == "easy" ? "FASTER GROWTH · LIGHTER PRESSURE"
                : id == "hard" ? "SLOWER GROWTH · HEAVIER PRESSURE"
                : "STANDARD GROWTH AND PRESSURE";
            _ruleTitle.text = $"{def.Name.ToUpperInvariant()} · {pace}";
            _ruleTitle.style.color = accent;
            SheetUI.SetBodyText(_ruleBody, def.Rule);
        }

        void FocusForm() { if (_ask == null) (_resume ?? _begin).Focus(); }

        public void Begin()
        {
            if (_game.NeedsParticipationChoice()) AskParticipation();
            else _game.Begin(_difficulty, _seed.value);
        }

        public void AskParticipation()
        {
            if (_ask != null) return;
            _ask = new VisualElement { name = "participation-sheet" };
            Kit.Fill(_ask);
            _ask.style.backgroundColor = Palette.Alpha(Palette.Desk, .82f);
            _ask.style.alignItems = Align.Center;
            _ask.style.justifyContent = Justify.Center;
            var sheet = Kit.Panel(Palette.Paper, Palette.DeskEdge, new Vector4(40, 34, 40, 34), .16f);
            sheet.style.width = 600;
            sheet.style.maxWidth = Length.Percent(96);
            Kit.Frame(sheet);
            _ask.Add(sheet);
            var scroll = SheetUI.Scroll();
            scroll.style.maxHeight = resolvedStyle.height - 94;
            sheet.Add(scroll);
            var content = Kit.VBox(16);
            scroll.Add(content);
            content.Add(SheetUI.Label("Join the playtest?", 40, Palette.Text, Fonts.Display));
            content.Add(Kit.Rule(Palette.Alpha(Palette.Text, .6f)));
            content.Add(SheetUI.Body("This build is part of a GTC Playtest Lab round. " + PlaytestPanel.Notice, 17));
            content.Add(SheetUI.Body("Your answer is remembered on this device. Change it any time in Menu → GTC Playtest Lab.", 15, Palette.Muted, Fonts.Italic));
            var row = Kit.HBox(12);
            var join = SheetUI.Button("Join the playtest & play", () => Answer(true), true, 52);
            var skip = SheetUI.Button("Play without sharing", () => Answer(false), height: 52);
            skip.style.fontSize = 21;
            join.style.flexGrow = 1;
            skip.style.flexGrow = 1;
            row.Add(join);
            row.Add(skip);
            content.Add(row);
            Add(_ask);
            // Remove the covered form from both pointer and keyboard navigation.
            _form.pickingMode = PickingMode.Ignore;
            foreach (var control in SheetUI.Controls(_form)) control.focusable = false;
            join.schedule.Execute(join.Focus);
        }

        void Answer(bool join)
        {
            if (join) _game.SetParticipation(true);
            else _game.DeclineParticipation();
            _ask.RemoveFromHierarchy();
            _ask = null;
            _game.Begin(_difficulty, _seed.value);
        }

        public void HandleKey(KeyCode key)
        {
            if (_ask != null && key == KeyCode.Escape)
            {
                _ask.RemoveFromHierarchy();
                _ask = null;
                _form.pickingMode = PickingMode.Position;
                _form.Query<Button>().ForEach(b => b.focusable = true);
                _form.Query<Toggle>().ForEach(b => b.focusable = true);
                _seed.focusable = true;
                schedule.Execute(FocusForm);
                return;
            }
            if (_ask != null && (key == KeyCode.LeftArrow || key == KeyCode.RightArrow))
                SheetUI.HandleKey(_ask, KeyCode.Tab);
            else SheetUI.HandleKey(_ask ?? _form, key);
        }
    }
}
