namespace Hearthwait
{
    /// <summary>Soft homestead colors for hover lines (Unity rich text).</summary>
    internal static class HoverStyle
    {
        // Warm amber — still waiting
        internal const string Wait = "#E8B86D";
        // Soft green — ready / harvest
        internal const string Ready = "#9CCC8A";
        // Soft rose — about to burn
        internal const string Warn = "#E08888";
        // Cool gray-blue — paused / waiting on wind
        internal const string Pause = "#9AADC0";
        // Soft cream — secondary info
        internal const string Soft = "#D2C4A8";
        internal static string Paint(string hex, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return "<color=" + hex + ">" + text + "</color>";
        }

        internal static string Waiting(string text) => Paint(Wait, text);

        internal static string Done(string text) => Paint(Ready, text);

        internal static string Warning(string text) => Paint(Warn, text);

        internal static string Paused(string text) => Paint(Pause, text);
    }
}
