namespace Awakening.Core.Playtest
{
    /// <summary>The capture's view of a game, without a dependency on its rules.</summary>
    public sealed class CaptureState
    {
        public string Seed;
        public string Difficulty;
        public string Ruleset;
        public string Status;
        public int ElapsedMonths;
        public int NextLogId;
        public bool IsOver;
    }
}
