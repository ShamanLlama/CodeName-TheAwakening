#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Windowed layout regression walk, ported from godot/dev/layout_check.gd.</summary>
    internal sealed class LayoutCheck
    {
        readonly List<string> _failures = new List<string>();
        Rect _bounds;

        static IEnumerator Frames(int count = 4)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        void Fail(string what)
        {
            if (!_failures.Contains(what)) _failures.Add(what);
        }

        static bool Visible(VisualElement node)
        {
            if (node == null || node.panel == null) return false;
            for (var parent = node; parent != null; parent = parent.parent)
                if (parent.resolvedStyle.display == DisplayStyle.None) return false;
            return node.resolvedStyle.visibility == Visibility.Visible;
        }

        void Within(VisualElement node, string what)
        {
            if (!Visible(node)) return;
            Rect rect = node.worldBound;
            // Godot deliberately checks left, right and bottom (not the top edge).
            if (rect.xMin < _bounds.xMin - 1 || rect.xMax > _bounds.xMax + 1 || rect.yMax > _bounds.yMax + 1)
                Fail($"{what} outside the canvas: {rect}, canvas {_bounds.size}");
            if (float.IsNaN(rect.x) || float.IsNaN(rect.y) || float.IsNaN(rect.width) || float.IsNaN(rect.height))
                Fail($"{what} has unresolved geometry: {rect}");
        }

        void Overlays(GameScreen screen, string id)
        {
            var shown = new List<(VisualElement node, string name)>();
            foreach (var pair in screen.LayoutOverlaysToCheck)
            {
                if (!Visible(pair.node) || pair.node.worldBound.height <= 0) continue;
                shown.Add(pair);
                Within(pair.node, pair.name);
                if (id != "" && Visible(screen.Panel) && pair.node.worldBound.Overlaps(screen.Panel.worldBound))
                    Fail($"{pair.name} under the file of {id}");
            }
            for (int i = 0; i < shown.Count; i++)
                for (int j = i + 1; j < shown.Count; j++)
                    if (shown[i].node.worldBound.Overlaps(shown[j].node.worldBound))
                        Fail($"{shown[i].name} covers {shown[j].name}{(id == "" ? "" : $" (file of {id} open)")}: {shown[i].node.worldBound} / {shown[j].node.worldBound}");
            if (screen.LayoutThreatScroll.worldBound.yMax > _bounds.yMax - screen.Bottom + 1)
                Fail("threat clippings reach the ledger");
        }

        internal static IEnumerator Crowd(GameController game, GameScreen screen)
        {
            if (GameEngine.IsOver(game.State)) yield break;
            foreach (string id in GameData.RegionIds)
            {
                var region = game.State.Regions[id];
                if (region.Population.Followers > 0 && region.Threat == null)
                    region.Threat = new Threat { Kind = new[] { "investigation", "raid", "occult" }[id.Length % 3], DueMonth = game.State.ElapsedMonths + 3, Severity = 1 };
            }
            game.Settings.Hints = new List<string>();
            screen.Map.ZoomAt(screen.Map.contentRect.size * .5f, 1.6f);
            screen.ExpandLayoutTimetable();
            screen.Refresh();
            yield return Frames(8);
        }

        public static IEnumerator Run(GameController game, bool crowded) => new LayoutCheck().Walk(game, crowded);

        IEnumerator Walk(GameController game, bool crowded)
        {
            // StageShot has already used the same scenario harness as screenshots.
            yield return Frames(20);
            if (!(game.CurrentScreen is GameScreen screen) || game.State == null)
            {
                Fail("layout check requires a staged game scenario");
            }
            else
            {
                _bounds = screen.worldBound;
                Debug.Log($"LAYOUT_CHECK_PROFILE: {Screen.width}x{Screen.height}, canvas {_bounds.size}, scale {game.Settings.UiScale}, touch {Kit.Touch}, crowded {crowded}, status {game.State.Status}");
                if (crowded && !GameEngine.IsOver(game.State))
                {
                    yield return Crowd(game, screen);
                }
                game.State.Speed = 0;
                screen.Dialogs.Close();
                yield return Frames();
                foreach (var item in screen.LayoutLedger) Within(item, $"ledger item {item.name}");
                Overlays(screen, "");
                foreach (string id in GameData.RegionIds)
                {
                    screen.Select(id);
                    yield return Frames(2);
                    Overlays(screen, id);
                    if (game.State.Status == "placing")
                    {
                        Within(screen.Panel, $"file {id}");
                        continue;
                    }
                    foreach (var group in RegionPanel.Groups)
                    {
                        screen.Panel.LayoutSetGroup(group.name);
                        yield return Frames(2);
                        foreach (string action in group.actions)
                        {
                            if (!screen.Panel.LayoutActionVisible(action)) continue;
                            screen.Panel.LayoutExplain(action);
                            yield return Frames(2);
                            Within(screen.Panel, $"file {id} {group.name}/{action}");
                            if (screen.Panel.LayoutContentWidth > screen.Panel.Width + 1)
                                Fail($"file {id} {group.name}/{action} wider than {screen.Panel.Width}: {screen.Panel.LayoutContentWidth}");
                        }
                    }
                }
                screen.Deselect();
                var kinds = new List<string> { "menu", "doctrine", "chronicle", "guide" };
                if (game.State.Status == "alignment") kinds.Add("alignment");
                if (GameEngine.IsOver(game.State)) kinds.Add("end");
                foreach (string kind in kinds)
                {
                    screen.OpenDialog(kind);
                    yield return Frames(6);
                    Within(screen.Dialogs.LayoutBox, $"dialog {kind}");
                    screen.Dialogs.Close();
                }
            }
            foreach (string failure in _failures) Debug.Log("LAYOUT_CHECK: " + failure);
            Debug.Log(_failures.Count == 0 ? "LAYOUT_CHECK_PASSED" : $"LAYOUT_CHECK_FAILED {_failures.Count}");
            Application.Quit(_failures.Count == 0 ? 0 : 1);
        }
    }
}
#endif
