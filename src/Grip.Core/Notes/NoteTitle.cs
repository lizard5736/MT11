namespace Grip.Core.Notes;

/// <summary>Derives a note's tab label from its own text, so there is nothing separate to
/// name or to rename. Extracted from NotepadWindow so it can be unit tested directly.</summary>
public static class NoteTitle
{
    public const int MaxLength = 28;

    /// <summary>The first non-blank line, with any leading Markdown heading marks stripped,
    /// trimmed to length. Never null or empty — falls back to <paramref name="placeholder"/>
    /// for a still-blank note.</summary>
    public static string From(string? content, string placeholder)
    {
        var line = (content ?? "")
            .Split('\n')
            .Select(l => l.Trim().TrimStart('#').Trim())
            .FirstOrDefault(l => l.Length > 0);
        if (string.IsNullOrEmpty(line)) return placeholder;
        return line.Length <= MaxLength ? line : line[..MaxLength].TrimEnd() + "…";
    }

    private static readonly char[] InvalidFileNameChars = "\\/:*?\"<>|".ToCharArray();

    /// <summary>A title made safe to use as a Windows file name: characters Explorer would
    /// reject become underscores. Never blank — an empty or all-invalid title falls back to
    /// "note" rather than leaving a save dialog with no name at all.</summary>
    public static string ToFileName(string title)
    {
        var cleaned = new string(title.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c).ToArray()).Trim();
        return cleaned.Length > 0 ? cleaned : "note";
    }
}
