using Awakening.Core.Rules;
using Xunit;

namespace Awakening.Core.Tests
{
    /// <summary>Values printed by godot/core/rng.gd at 0.4.0 (7d67097).</summary>
    public class RngTests
    {
        [Theory]
        [InlineData("greedy-c", 2186586701u, 0.9560123465489596, 4106041763u, 0.13536814018152654, 581401735u, 1, 9511856u)]
        [InlineData("dagon", 3872702462u, 0.3291574742179364, 1413720587u, 0.005272614303976297, 22645706u, 6, 4199144763u)]
        [InlineData("ñandú-🐙", 2301013234u, 0.7954191821627319, 3416299374u, 0.9410745371133089, 4041884360u, 6, 4068329007u)]
        [InlineData("", 2166136261u, 0.2739662209060043, 1176675959u, 0.31014532619155943, 1332064033u, 1, 145169569u)]
        public void MatchesGodot(string seed, uint initial, double first, uint afterFirst, double second, uint afterSecond, int die, uint afterDie)
        {
            uint state = Rng.SeedToState(seed);
            Assert.Equal(initial, state);
            RngDraw a = Rng.Next(state);
            Assert.Equal(first, a.Value);
            Assert.Equal(afterFirst, a.State);
            RngDraw b = Rng.Next(a.State);
            Assert.Equal(second, b.Value);
            Assert.Equal(afterSecond, b.State);
            state = Rng.Next(Rng.Next(b.State).State).State;
            RngIntDraw roll = Rng.NextInt(state, 1, 6);
            Assert.Equal(die, roll.Value);
            Assert.Equal(afterDie, roll.State);
        }
    }
}
