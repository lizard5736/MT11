using System.Globalization;
using System.Text.RegularExpressions;

namespace Grip.Core.Monitoring;

/// <summary>One process's share of GPU work, summed across every engine (3D, copy, video decode…) it's using.</summary>
public readonly record struct GpuProcessUsage(int ProcessId, double Percent);

/// <summary>
/// Parses the CSV that <c>typeperf "\&lt;GPU Engine&gt;(*)\&lt;Utilization Percentage&gt;" -sc 2 -si 1</c>
/// prints (object/counter names localized by the caller — see PerfCounterNames): a header
/// row naming one column per running GPU engine instance, then one data row per sample.
/// Two samples are asked for because this is a rate counter — PDH needs a baseline, so the
/// first data row is often invalid and the last one is the reading to use.
///
/// Read through the real typeperf.exe rather than raw PDH P/Invoke on purpose: parsing this
/// tool's text output can only fail safely (no match, a null reading), while getting the
/// native PDH_FMT_COUNTERVALUE_ITEM buffer layout wrong risks an unrecoverable crash that
/// this project has no GPU hardware to test against.
/// </summary>
public static class GpuUsage
{
    // Instance names look like "pid_10472_luid_0x00000000_0x0000f814_phys_0_eng_0_engtype_3D"
    // regardless of system language — only the object/counter name around them is localized.
    private static readonly Regex PidPattern = new(@"pid_(\d+)_", RegexOptions.Compiled);

    /// <summary>
    /// The busiest single engine's utilization, 0–100, across every process, adapter and
    /// engine instance. Not a sum across engines: several can run at once (3D, video
    /// decode, copy…), so adding them double-counts. "How loaded is the busiest part of
    /// the GPU right now" is a simpler, honest number than trying to reproduce exactly
    /// what Task Manager's own (undocumented) aggregation shows.
    /// </summary>
    public static double? ParsePercent(string typeperfCsv)
    {
        var instances = ParseInstances(typeperfCsv);
        if (instances == null || instances.Count == 0) return null;
        return Math.Clamp(instances.Max(i => i.Percent), 0, 100);
    }

    /// <summary>
    /// Every process currently using the GPU, its engines summed together, busiest first.
    /// Unlike <see cref="ParsePercent"/> this does add engines up — ranking processes
    /// against each other calls for "how much is this process doing in total", where the
    /// single-busiest-engine number would hide a process spread across several engines.
    /// </summary>
    public static IReadOnlyList<GpuProcessUsage> ParseByProcess(string typeperfCsv)
    {
        var instances = ParseInstances(typeperfCsv);
        if (instances == null) return Array.Empty<GpuProcessUsage>();
        return instances
            .Where(i => i.ProcessId > 0)
            .GroupBy(i => i.ProcessId)
            .Select(g => new GpuProcessUsage(g.Key, Math.Clamp(g.Sum(i => i.Percent), 0, 100)))
            .OrderByDescending(p => p.Percent)
            .ToList();
    }

    private readonly record struct Instance(int ProcessId, double Percent);

    private static List<Instance>? ParseInstances(string typeperfCsv)
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

        var result = new List<Instance>();
        for (int i = 1; i < lastData.Count; i++) // column 0 is the sample's timestamp
        {
            if (!double.TryParse(lastData[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) continue;
            int pid = PidPattern.Match(header[i]) is { Success: true } m ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            result.Add(new Instance(pid, value));
        }
        return result;
    }

    /// <summary>Splits one quoted, comma-separated typeperf row. Fields never contain a
    /// literal comma, so a plain split is enough; typeperf doubles any embedded quote.</summary>
    private static List<string> SplitCsvRow(string line) =>
        line.Split(',').Select(f => f.Trim().Trim('"').Replace("\"\"", "\"")).ToList();
}
