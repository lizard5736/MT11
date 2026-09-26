using System.Text.RegularExpressions;

namespace Grip.Core.Notes;

/// <summary>
/// Flips a Markdown task-list checkbox ("- [ ] foo" / "- [x] foo") by editing raw text,
/// identified by its 0-based source line rather than a character span: a toggle is always a
/// single-character substitution (space ↔ x), and the note's editor and its checkbox-bearing
/// preview are never shown at once, so no other line can shift between one click and the next.
/// </summary>
public static class TaskListToggle
{
    private static readonly Regex TaskLine = new(@"^(\s*(?:[-*+]|\d+[.)])\s+)\[([ xX])\]", RegexOptions.Compiled);

    /// <summary>Returns the updated text, or null if <paramref name="lineIndex"/> no longer
    /// points at a task item — a stale line number is a no-op, never a corrupted edit.</summary>
    public static string? Flip(string content, int lineIndex, bool desiredChecked)
    {
        int start = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            int nl = content.IndexOf('\n', start);
            if (nl < 0) return null;
            start = nl + 1;
        }
        int end = content.IndexOf('\n', start);
        if (end < 0) end = content.Length;
        string line = content[start..end];

        var m = TaskLine.Match(line);
        if (!m.Success) return null;

        char wanted = desiredChecked ? 'x' : ' ';
        if (m.Groups[2].Value[0] == wanted) return content;

        string newLine = line[..m.Groups[2].Index] + wanted + line[(m.Groups[2].Index + 1)..];
        return content[..start] + newLine + content[end..];
    }
}
