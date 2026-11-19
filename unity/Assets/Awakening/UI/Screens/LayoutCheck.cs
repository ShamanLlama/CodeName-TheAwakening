#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awakening.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Windowed layout regression walk, ported from godot/dev/layout_check.gd.</summary>
    internal sealed class LayoutCheck
    {
        readonly List<string> _failures = new List<string>();
        Rect _bounds, _ledgerBounds;

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
                if (parent.resolvedStyle.display == DisplayStyle.None || parent.resolvedStyle.visibility == Visibility.Hidden) return false;
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
            if (!screen.Map.LayoutGeometryValid) Fail("map has unresolved viewport or projection geometry");
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
            Rect clip = ((ScrollView)screen.LayoutThreatScroll).contentViewport.worldBound;
            foreach (var card in screen.LayoutThreatCards)
                if (Visible(card) && !Contains(clip, card.worldBound)) Fail($"threat card clipped by its container: {card.worldBound} / {clip}");
            foreach (var label in screen.Map.LayoutLabels)
            {
                if (!Visible(label)) continue;
                if (!Contains(screen.Map.worldBound, label.worldBound)) Fail($"map label {label.text} outside map viewport: {label.worldBound} / {screen.Map.worldBound}");
                foreach (var overlay in screen.VisibleMapOverlays())
                    if (Visible(overlay) && overlay.worldBound.width > 0 && overlay.worldBound.height > 0 && label.worldBound.Overlaps(overlay.worldBound))
                        Fail($"map label {label.text} intersects visible {overlay.name}: {label.worldBound} / {overlay.worldBound}");
            }
        }

        static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - 1 && inner.yMin >= outer.yMin - 1
            && inner.xMax <= outer.xMax + 1 && inner.yMax <= outer.yMax + 1;

        void LedgerText(VisualElement node)
        {
            if (!Visible(node)) return;
            if (!Contains(_ledgerBounds, node.worldBound)) Fail($"ledger content outside its bar: {node.name}, {node.worldBound} / {_ledgerBounds}");
            if (node is TextElement text && !string.IsNullOrEmpty(text.text))
            {
                Within(text, $"ledger text {text.text}");
                if (text.isElided) Fail($"ledger text truncated with ellipsis: {text.text}");
                Vector2 needed = text.MeasureTextSize(text.text, text.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                if (needed.y > text.contentRect.height + 1) Fail($"ledger text clipped vertically: {text.text}, needs {needed.y}, has {text.contentRect.height}");
                if (text.resolvedStyle.whiteSpace == WhiteSpace.NoWrap)
                {
                    needed = text.MeasureTextSize(text.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                    if (needed.x > text.contentRect.width + 1) Fail($"ledger text clipped horizontally: {text.text}, needs {needed.x}, has {text.contentRect.width}");
                }
            }
            foreach (var child in node.Children()) LedgerText(child);
        }

        internal static IEnumerator Crowd(GameController game, GameScreen screen)
        {
            if (GameEngine.IsOver(game.State)) yield break;
            foreach (string id in GameData.RegionIds)
            {
                var region = game.State.Regions[id];
                if (region.Population.Followers > 0 && region.Threat == null)
                    Director.ScheduleThreat(game.State, id, id.Length % 3 == 2);
            }
            game.Settings.Hints = new List<string>();
            screen.Map.ZoomAt(screen.Map.contentRect.size * .5f, 1.6f);
            screen.ExpandLayoutTimetable();
            screen.Refresh();
            yield return Frames(8);
        }

        IEnumerator ThreatRecovery(GameScreen screen)
        {
            float[] heights = screen.LayoutThreatHeights;
            var targets = new[] { heights[0] + 20, (heights[0] + heights[1]) / 2,
                (heights[1] + heights[2]) / 2, (heights[0] + heights[1]) / 2, heights[0] + 20 };
            var expected = new[] { 0, 1, 2, 1, 0 };
            for (int step = 0; step < targets.Length; step++)
            {
                // Change the real canvas height, rather than forcing the selected detail level.
                screen.style.height = screen.contentRect.height + targets[step] - screen.LayoutThreatAvailableHeight;
                yield return Frames(12);
                if (screen.LayoutThreatOption != expected[step]) Fail($"threat recovery step {step}: expected {expected[step]}, got {screen.LayoutThreatOption}");
                for (int frame = 0; frame < 12; frame++)
                {
                    yield return null;
                    if (screen.LayoutThreatOption != expected[step]) Fail($"threat layout oscillates at recovery step {step}");
                }
                Debug.Log($"LAYOUT_THREAT_RECOVERY: step {step}, option {screen.LayoutThreatOption}, available {screen.LayoutThreatAvailableHeight}, full2/full1/brief heights {string.Join(",", screen.LayoutThreatHeights)}");
            }
            screen.style.height = StyleKeyword.Null;
            yield return Frames(12);
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
                LogEntry longest = game.State.Log.OrderByDescending(entry => entry.Message.Length).FirstOrDefault();
                if (longest != null)
                {
                    screen.LayoutSetTicker(longest);
                    yield return Frames();
                    if (!screen.LayoutTicker.parent.tooltip.Contains(longest.Message)) Fail("ticker tooltip lost the full chronicle entry");
                    Debug.Log($"LAYOUT_CHRONICLE_LONGEST: {longest.Message.Length} characters: {longest.Message}");
                }
                _ledgerBounds = screen.LayoutLedgerBar.worldBound;
                var ledgerSlot = new Rect(_bounds.x, _bounds.yMax - screen.Bottom, _bounds.width, screen.Bottom);
                if (!Contains(ledgerSlot, _ledgerBounds)) Fail($"ledger outside its reserved bar: {_ledgerBounds} / {ledgerSlot}");
                foreach (var item in screen.LayoutLedger)
                {
                    Within(item, $"ledger item {item.name}");
                    LedgerText(item);
                }
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
                yield return Frames(8);
                if (crowded && !Kit.Compact && screen.LayoutThreatHeights.Length == 3
                    && game.State.Regions.Values.Count(region => region.Threat != null) >= 2)
                    yield return ThreatRecovery(screen);
                var kinds = new List<string> { "menu", "doctrine", "chronicle", "guide" };
                if (game.State.Status == "alignment") kinds.Add("alignment");
                if (GameEngine.IsOver(game.State)) kinds.Add("end");
                foreach (string kind in kinds)
                {
                    screen.OpenDialog(kind);
                    yield return Frames(6);
                    Within(screen.Dialogs.LayoutBox, $"dialog {kind}");
                    if (Visible(screen.LayoutBanner)) Fail($"timetable visible over open modal {kind}");
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
