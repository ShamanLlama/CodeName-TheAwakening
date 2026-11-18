namespace Awakening.Core.Json
{
    /// <summary>A validated JSON value whose original bytes must survive a save rewrite.</summary>
    internal sealed class RawJson
    {
        internal readonly string Text;
        internal RawJson(string text)
        {
            JsonReader.Parse(text);
            Text = text;
        }
    }
}
