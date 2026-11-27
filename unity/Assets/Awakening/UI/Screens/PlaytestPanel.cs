using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Optional participation and the durable local record, as in playtest_panel.gd.</summary>
    public sealed class PlaytestPanel : Stack
    {
        public const string Notice = "Share choices (including difficulty and world seed), resource spending, progress and session times with GTC. Random ID per round; no name or email. Detailed records are kept for six months. Keep personal details out of your seed. You can play without sharing.";

        public PlaytestPanel(GameController game, bool small = false) : base(false, 8)
        {
            Add(SheetUI.Caption("GTC Playtest Lab · optional"));
            Add(SheetUI.Body(Notice, small ? 14 : 16, Palette.Muted));
            if (!small) Add(SheetUI.Body("Your choice is remembered on this device until you change it here. Stop clears unsent records on this device; it does not erase records already received. Contact the GTC operator to manage those records. Local exports stay under your control.", 15, Palette.Muted));
            var choice = new Toggle { text = "I agree to share this playtest with GTC", name = "PlaytestParticipation", value = game.Lab.Confirmed };
            choice.AddToClassList("aw-check");
            choice.AddToClassList("aw-participation");
            choice.style.minHeight = 36;
            var checkmark = choice.Q(className: "unity-toggle__checkmark");
            checkmark.generateVisualContent += context =>
            {
                if (!choice.value) return;
                var r = checkmark.contentRect;
                float inset = Mathf.Floor((r.width + 4) * .28f) - 2;
                Draw2D.Rect(context.painter2D, new Rect(inset, inset, r.width - inset * 2, r.height - inset * 2), choice.enabledInHierarchy ? Palette.Cult : Palette.Faint);
            };
            choice.SetEnabled(!game.Blocked && game.Lab.Configured() && !game.Lab.Corrupt);
            var status = SheetUI.Body(game.PlaytestStatus(), small ? 14 : 15, Palette.Muted);
            choice.RegisterValueChangedCallback(e =>
            {
                checkmark.MarkDirtyRepaint();
                game.SetParticipation(e.newValue);
                choice.SetValueWithoutNotify(game.Lab.Confirmed);
                checkmark.MarkDirtyRepaint();
                SheetUI.SetBodyText(status, game.PlaytestStatus());
            });
            Add(choice);
            Add(status);
            if (!small || game.Lab.Corrupt)
            {
                Add(SheetUI.Button("Export Local Save & Playtest Record", game.ExportPlaytestRecord, height: 0));
                Add(SheetUI.Button("Stop & Clear Unsent Records", () =>
                {
                    game.SetParticipation(false);
                    choice.SetValueWithoutNotify(false);
                    checkmark.MarkDirtyRepaint();
                    SheetUI.SetBodyText(status, game.PlaytestStatus());
                }, height: 0));
            }
            schedule.Execute(() => SheetUI.SetBodyText(status, game.PlaytestStatus())).Every(1000);
        }
    }
}
