using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>
    /// Placeholder for the port of godot/ui/dialogs.gd (menu, doctrine, chronicle, guide, alignment,
    /// end, playtest). The API is fixed; the sheets are filled in separately.
    /// </summary>
    public sealed class Dialogs : VisualElement
    {
        readonly GameController _game;
        readonly GameScreen _screen;

        public string Kind { get; private set; } = "";
        public bool IsOpen => Kind != "";

        public Dialogs(GameController game, GameScreen screen)
        {
            _game = game;
            _screen = screen;
            Kit.Fill(this);
            Kit.SetVisible(this, false);
        }

        public void Open(string kind)
        {
            if (!IsOpen) _game.Hold();
            Kind = kind;
            Kit.SetVisible(this, true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            Kind = "";
            Kit.SetVisible(this, false);
            _game.Release();
        }

        public bool HandleKey(KeyCode key)
        {
            if (key != KeyCode.Escape) return false;
            Close();
            return true;
        }
    }
}
