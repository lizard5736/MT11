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
}
