using UnityEngine;
using UnityEngine.UIElements;

namespace Awakening.UI
{
    /// <summary>Placeholder for the port of godot/ui/start_screen.gd. The API is fixed; the page is filled in separately.</summary>
    public sealed class StartScreen : VisualElement
    {
        readonly GameController _game;

        public StartScreen(GameController game)
        {
            _game = game;
            Kit.Fill(this);
            style.backgroundColor = Palette.Desk;
            Add(Kit.Primary("Begin", () => _game.Begin("normal", "")));
        }

        public void HandleKey(KeyCode key)
        {
        }
    }
}
