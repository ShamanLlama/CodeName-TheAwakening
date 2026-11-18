namespace Awakening.Core.Rules
{
    /// <summary>One draw from the seeded generator: the value and the state after it.</summary>
    public readonly struct RngDraw
    {
        public readonly double Value;
        public readonly uint State;

        public RngDraw(double value, uint state)
        {
            Value = value;
            State = state;
        }
    }

    /// <summary>An integer draw and the state after it.</summary>
    public readonly struct RngIntDraw
    {
        public readonly int Value;
        public readonly uint State;

        public RngIntDraw(int value, uint state)
        {
            Value = value;
            State = state;
        }
    }

    /// <summary>
    /// Seeded xorshift32, bit-compatible with godot/core/rng.gd (and the original TypeScript).
    /// Unsigned 32-bit arithmetic wraps exactly as the GDScript masks do.
    /// </summary>
    public static class Rng
    {
        /// <summary>FNV-1a over UTF-16 code units, as JavaScript's charCodeAt sees the seed.</summary>
        public static uint SeedToState(string seed)
        {
            uint hash = 2166136261;
            foreach (char unit in seed)
            {
                hash ^= unit;
                hash = unchecked(hash * 16777619);
            }
            return hash != 0 ? hash : 1;
        }

        public static RngDraw Next(uint state)
        {
            uint x = state == 0 ? 1u : state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return new RngDraw(x / 4294967296.0, x != 0 ? x : 1u);
        }

        public static RngIntDraw NextInt(uint state, int minimum, int maximum)
        {
            RngDraw draw = Next(state);
            return new RngIntDraw((int)System.Math.Floor(draw.Value * (maximum - minimum + 1)) + minimum, draw.State);
        }
    }
}
