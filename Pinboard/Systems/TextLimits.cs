namespace Pinboard.Systems
{
    // Names typed by the player or read from a share code are tidied the same way
    // everywhere, so the save file never holds blank or over-long names.
    public static class TextLimits
    {
        // Trims a name and cuts it to the limit. Returns null when nothing is left.
        public static string Clean(string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var trimmed = name.Trim();
            return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength).TrimEnd() : trimmed;
        }
    }
}
