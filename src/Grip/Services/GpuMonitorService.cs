using System.Diagnostics;
using System.Management;
using Grip.Core.Monitoring;

namespace Grip.Services;

/// <summary>One process currently touching the GPU, resolved to a display name.</summary>
public readonly record struct GpuProcessInfo(int ProcessId, string Name, double Percent);

/// <summary>
/// GPU load, name and top consumers, via the "GPU Engine" performance counters — the same,
/// vendor-agnostic ones Task Manager's GPU graphs read (NVIDIA, AMD and Intel all expose them
/// through the WDDM driver model, no vendor SDK needed). Read by spawning the stock
/// typeperf.exe and parsing its text, not by calling PDH directly: see GpuUsage for why.
///
/// Object/counter names are localized by Windows — typeperf resolves its own command line
/// against the system's UI language, not English — so PerfCounterNames translates them first.
/// Skipping that step is exactly why GPU load silently showed nothing on a non-English system.
///
/// Refreshed on its own background loop every 8 seconds — each read spans just over a
/// second and must never block the UI thread the way the 1.5s DispatcherTimer in
/// SystemMonitorService does. Any failure (no GPU Engine category, typeperf missing,
/// sandboxed environment, timeout) degrades to a null/empty reading, never a crash or a
/// visible error.
/// </summary>
public sealed class GpuMonitorService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan SampleTimeout = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private double? _percent;
    private IReadOnlyList<GpuProcessInfo> _topProcesses = Array.Empty<GpuProcessInfo>();
    private CancellationTokenSource? _cts;
    private readonly HashSet<string> _keepAliveOwners = new();

    public bool IsRunning => _cts != null;

    /// <summary>e.g. "NVIDIA GeForce RTX 4070". Null until the one-time WMI lookup finishes, or if it fails.</summary>
    public string? GpuName { get; private set; }

    /// <summary>Null while unknown or unavailable on this machine.</summary>
    public double? Latest
    {
        get { lock (_lock) return _percent; }
    }

    /// <summary>Every process currently touching the GPU, busiest first. Empty, never null, when idle or the read failed.</summary>
    public IReadOnlyList<GpuProcessInfo> TopProcesses
    {
        get { lock (_lock) return _topProcesses; }
    }

    public event EventHandler? Sampled;

    public GpuMonitorService()
    {
        // Off the caller's thread on purpose: a process's first WMI query spins up a COM
        // apartment and can take a few hundred ms, which Start() — called from the UI thread
        // when the panel opens — must never wait on. The name never changes at runtime, so
        // this only ever needs to run once, independent of Start()/Stop().
        _ = Task.Run(() =>
        {
            try { GpuName = ReadGpuName(); }
            catch (Exception ex) { Log.Error("GPU name read failed", ex); }
        });
    }

    /// <summary><paramref name="owner"/> tracks who wants this running — see SystemMonitorService.Start.</summary>
    public void Start(string owner = "panel")
    {
        _keepAliveOwners.Add(owner);
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Stop(string owner = "panel")
    {
        _keepAliveOwners.Remove(owner);
        if (_keepAliveOwners.Count > 0) return;
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            double? percent = null;
            IReadOnlyList<GpuProcessInfo> processes = Array.Empty<GpuProcessInfo>();
            try
            {
                (percent, processes) = await SampleAsync(token);
            }
            catch (Exception ex)
            {
                Log.Error("GPU sample failed", ex);
            }
            lock (_lock)
            {
                _percent = percent;
                _topProcesses = processes;
            }
            Sampled?.Invoke(this, EventArgs.Empty);
            try
            {
                await Task.Delay(RefreshInterval, token);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    private static async Task<(double? Percent, IReadOnlyList<GpuProcessInfo> Processes)> SampleAsync(CancellationToken token)
    {
        var info = new ProcessStartInfo("typeperf.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // Two samples, one second apart: a fresh rate-type PDH counter has no valid value
        // on its first collection, so the second line is the one worth reading.
        info.ArgumentList.Add($@"\{PerfCounterNames.Localize("GPU Engine")}(*)\{PerfCounterNames.Localize("Utilization Percentage")}");
        info.ArgumentList.Add("-sc");
        info.ArgumentList.Add("2");
        info.ArgumentList.Add("-si");
        info.ArgumentList.Add("1");

        using var process = new Process { StartInfo = info };
        if (!process.Start()) return (null, Array.Empty<GpuProcessInfo>());
        var output = process.StandardOutput.ReadToEndAsync(token);

        using var timeout = new CancellationTokenSource(SampleTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            return (null, Array.Empty<GpuProcessInfo>());
        }

        if (process.ExitCode != 0) return (null, Array.Empty<GpuProcessInfo>());
        string csv = await output;
        return (GpuUsage.ParsePercent(csv), ResolveNames(GpuUsage.ParseByProcess(csv)));
    }

    /// <summary>Attaches a display name to each PID typeperf reported. A process that exited
    /// between the sample and this lookup is dropped rather than shown nameless.</summary>
    private static IReadOnlyList<GpuProcessInfo> ResolveNames(IReadOnlyList<GpuProcessUsage> byPid)
    {
        var result = new List<GpuProcessInfo>(byPid.Count);
        foreach (var usage in byPid)
        {
            try
            {
                using var process = Process.GetProcessById(usage.ProcessId);
                result.Add(new GpuProcessInfo(usage.ProcessId, process.ProcessName, usage.Percent));
            }
            catch (ArgumentException)
            {
            }
        }
        return result;
    }

    /// <summary>The first real, PCI-bus GPU WMI reports — never Windows' own software/virtual
    /// adapters, which would otherwise show up as a fake "GPU" on a machine with no real one
    /// active (headless, RDP session, a VM) or alongside the real one.</summary>
    private static string? ReadGpuName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name, PNPDeviceID FROM Win32_VideoController");
        foreach (ManagementBaseObject obj in searcher.Get())
        {
            using (obj)
            {
                if (obj["Name"] is not string name || string.IsNullOrWhiteSpace(name)) continue;
                if (IsVirtualAdapterName(name)) continue;
                bool onPciBus = obj["PNPDeviceID"] is string pnp && pnp.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase);
                if (onPciBus) return name;
            }
        }
        return null;
    }

    // These four are Windows' own non-physical adapters (WARP software rasterizer, VGA
    // fallback, Hyper-V's synthetic display, RDP's indirect display driver) — never the GPU
    // actually doing the rendering that the GPU Engine counters measure. Requiring a PCI
    // PNPDeviceID on top of this list catches any other root-enumerated adapter it misses.
    private static bool IsVirtualAdapterName(string name) =>
        name.Contains("Basic Render Driver", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Basic Display Adapter", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Hyper-V Video", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Remote Display Adapter", StringComparison.OrdinalIgnoreCase);
}
