using System.IO;
using Microsoft.Win32;

namespace Grip.Services;

/// <summary>
/// Performance counter names are localized — a Russian Windows doesn't call
/// anything "GPU Engine". typeperf.exe always resolves the names in its
/// command line against the system's own language, so before building that
/// command line Grip translates the English object/counter names it knows
/// into whatever this machine actually calls them.
///
/// Windows keeps that mapping in the registry, no PDH API needed: the "009"
/// key holds the English names, "CurrentLanguage" holds today's language,
/// both as a flat (index, name, index, name, …) list under the same indices.
/// Matching an English name to its index in "009" and reading the same
/// index back from "CurrentLanguage" gives the localized name. On an
/// English system the two lists are the same, so this is also just a
/// (slower) identity lookup there — it isn't a Russian-only special case.
/// </summary>
internal static class PerfCounterNames
{
    private const string Base = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib";

    private static readonly Lazy<Dictionary<string, string>?> English = new(() => ReadPairs("009"));
    private static readonly Lazy<Dictionary<string, string>?> Local = new(() => ReadPairs("CurrentLanguage"));

    /// <summary>This system's name for an English perf object/counter name; the English
    /// name unchanged if anything about the lookup fails (correct on an English system,
    /// and a typeperf call that returns no data beats one that throws on a lookup miss).</summary>
    public static string Localize(string englishName)
    {
        var english = English.Value;
        var local = Local.Value;
        if (english == null || local == null) return englishName;
        foreach (var (index, name) in english)
        {
            if (string.Equals(name, englishName, StringComparison.OrdinalIgnoreCase) && local.TryGetValue(index, out var localized))
                return localized;
        }
        return englishName;
    }

    private static Dictionary<string, string>? ReadPairs(string languageKey)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"{Base}\{languageKey}");
            if (key?.GetValue("Counter") is not string[] lines) return null;
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < lines.Length; i += 2) map[lines[i]] = lines[i + 1];
            return map;
        }
        catch (System.Security.SecurityException) { return null; }
        catch (IOException) { return null; }
    }
}
