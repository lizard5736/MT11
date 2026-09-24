using System.Globalization;
using System.Text;

namespace Grip.Core.Search;

public static class TextNormalizer
{
    /// <summary>Lower-case, "ё" folded into "е", runs of whitespace collapsed.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var sb = new StringBuilder(text.Length);
        bool space = false;
        foreach (var raw in text)
        {
            char ch = char.ToLowerInvariant(raw);
            if (ch == 'ё') ch = 'е';
            if (char.IsWhiteSpace(ch))
            {
                space = sb.Length > 0;
                continue;
            }
            if (space) { sb.Append(' '); space = false; }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}

/// <summary>
/// Scores how well a query matches a title, the way launchers do: exact beats
/// prefix, prefix beats word start, word start beats acronym, and a loose
/// in-order subsequence is the last resort.
/// </summary>
public static class FuzzyMatcher
{
    public const int NoMatch = 0;

    public static int Score(string query, string candidate)
    {
        var q = TextNormalizer.Normalize(query);
        var c = TextNormalizer.Normalize(candidate);
        if (q.Length == 0 || c.Length == 0) return NoMatch;

        if (c == q) return 1000;
        if (c.StartsWith(q, StringComparison.Ordinal)) return 900 - Math.Min(100, c.Length - q.Length);

        int wordStart = IndexOfWordStart(c, q);
        if (wordStart >= 0) return 800 - Math.Min(100, wordStart);

        if (IsAcronym(q, c)) return 700;

        int index = c.IndexOf(q, StringComparison.Ordinal);
        if (index >= 0) return 600 - Math.Min(100, index);

        int gaps = SubsequenceGaps(q, c);
        if (gaps >= 0 && q.Length >= 2) return Math.Max(1, 400 - gaps * 10);

        return NoMatch;
    }

    /// <summary>Best score across a title and extra keywords (keywords weigh a little less).</summary>
    public static int Score(string query, string title, IEnumerable<string>? keywords)
    {
        int best = Score(query, title);
        if (keywords != null)
            foreach (var keyword in keywords)
                best = Math.Max(best, Score(query, keyword) * 9 / 10);
        return best;
    }

    private static int IndexOfWordStart(string c, string q)
    {
        int from = 0;
        while (true)
        {
            int index = c.IndexOf(q, from, StringComparison.Ordinal);
            if (index < 0) return -1;
            if (index == 0 || !char.IsLetterOrDigit(c[index - 1])) return index;
            from = index + 1;
        }
    }

    /// <summary>"vsc" → "Visual Studio Code", "дз" → "Диспетчер задач".</summary>
    private static bool IsAcronym(string q, string c)
    {
        if (q.Length < 2 || q.Contains(' ')) return false;
        var initials = new StringBuilder();
        bool atStart = true;
        for (int i = 0; i < c.Length; i++)
        {
            char ch = c[i];
            if (!char.IsLetterOrDigit(ch)) { atStart = true; continue; }
            if (atStart) initials.Append(ch);
            atStart = false;
        }
        return initials.ToString().StartsWith(q, StringComparison.Ordinal);
    }

    /// <summary>Characters of q in order inside c; returns the count of skipped characters or -1.</summary>
    private static int SubsequenceGaps(string q, string c)
    {
        int qi = 0, gaps = 0, lastMatch = -1;
        for (int ci = 0; ci < c.Length && qi < q.Length; ci++)
        {
            if (c[ci] == q[qi])
            {
                if (lastMatch >= 0) gaps += ci - lastMatch - 1;
                lastMatch = ci;
                qi++;
            }
        }
        return qi == q.Length ? gaps : -1;
    }
}

/// <summary>
/// Fixes text typed in the wrong keyboard layout: "ыуеештпы" ↔ "settings",
/// "ntktuhfv" ↔ "телеграм". Uses the standard ЙЦУКЕН and QWERTY positions.
/// </summary>
public static class KeyboardLayout
{
    private const string En = "`qwertyuiop[]asdfghjkl;'zxcvbnm,./~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?";
    private const string Ru = "ёйцукенгшщзхъфывапролджэячсмитьбю.ЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ,";

    private static readonly Dictionary<char, char> EnToRu = Build(En, Ru);
    private static readonly Dictionary<char, char> RuToEn = Build(Ru, En);

    private static Dictionary<char, char> Build(string from, string to)
    {
        var map = new Dictionary<char, char>();
        for (int i = 0; i < from.Length && i < to.Length; i++)
            map.TryAdd(from[i], to[i]);
        return map;
    }

    /// <summary>Returns the text as if typed in the other layout, or null when nothing would change.</summary>
    public static string? Swap(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        int ru = 0, en = 0;
        foreach (var ch in text)
        {
            if (ch is >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё') ru++;
            else if (ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z') en++;
        }
        if (ru == 0 && en == 0) return null;
        var map = ru >= en ? RuToEn : EnToRu;
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text) sb.Append(map.TryGetValue(ch, out var swapped) ? swapped : ch);
        var result = sb.ToString();
        return string.Equals(result, text, StringComparison.Ordinal) ? null : result;
    }

    public static bool LooksCyrillic(string text) =>
        text.Any(ch => ch is >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё');

    internal static string Invariant(string s) => s.ToLower(CultureInfo.InvariantCulture);
}
