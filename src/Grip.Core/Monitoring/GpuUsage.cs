using System.Globalization;

namespace Grip.Core.Monitoring;

/// <summary>
/// Parses the CSV that <c>typeperf "\GPU Engine(*)\Utilization Percentage" -sc 2 -si 1</c>
/// prints: a header row naming one column per running GPU engine instance, then one data
/// row per sample. Two samples are asked for because this is a rate counter — PDH needs a
/// baseline, so the first data row is often invalid and the last one is the reading to use.
///
/// Read through the real typeperf.exe rather than raw PDH P/Invoke on purpose: parsing this
/// tool's text output can only fail safely (no match, a null reading), while getting the
/// native PDH_FMT_COUNTERVALUE_ITEM buffer layout wrong risks an unrecoverable crash that
/// this project has no GPU hardware to test against.
/// </summary>
public static class GpuUsage
{
    /// <summary>
    /// The busiest single engine's utilization, 0–100, across every process, adapter and
    /// engine instance. Not a sum across engines: several can run at once (3D, video
    /// decode, copy…), so adding them double-counts. "How loaded is the busiest part of
    /// the GPU right now" is a simpler, honest number than trying to reproduce exactly
    /// what Task Manager's own (undocumented) aggregation shows.
    /// </summary>
    public static double? ParsePercent(string typeperfCsv)
    {
        var lines = typeperfCsv
            .Split('\n')
            .Select(l => l.Trim('\r', '\n', ' '))
            .Where(l => l.Length > 0)
            .ToList();
        if (lines.Count < 2) return null;

        var header = SplitCsvRow(lines[0]);
        var lastData = SplitCsvRow(lines[^1]);
        if (header.Count < 2 || lastData.Count != header.Count) return null;

        double max = 0;
        bool any = false;
        for (int i = 1; i < lastData.Count; i++) // column 0 is the sample's timestamp
        {
            if (!double.TryParse(lastData[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) continue;
            any = true;
            if (value > max) max = value;
        }
        return any ? Math.Clamp(max, 0, 100) : null;
    }

    /// <summary>Splits one quoted, comma-separated typeperf row. Fields never contain a
    /// literal comma, so a plain split is enough; typeperf doubles any embedded quote.</summary>
    private static List<string> SplitCsvRow(string line) =>
        line.Split(',').Select(f => f.Trim().Trim('"').Replace("\"\"", "\"")).ToList();
}
