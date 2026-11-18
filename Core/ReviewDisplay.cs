namespace UnattendLocal.Core;

internal static class ReviewDisplay
{
    // Keep untrusted file names and event fields on one bounded display row.
    internal static string Field(string text)
    {
        string value = new(text.Take(512).Select(c => char.IsControl(c) || char.GetUnicodeCategory(c) ==
            System.Globalization.UnicodeCategory.Format || c is '\u2028' or '\u2029' ? ' ' : c).ToArray());
        return text.Length > 512 ? value + "…" : value;
    }
}
