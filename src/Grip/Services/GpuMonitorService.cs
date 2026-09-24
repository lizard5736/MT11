using System.Diagnostics;
using Grip.Core.Monitoring;

namespace Grip.Services;

/// <summary>
/// GPU load only, via the "GPU Engine" performance counters — the same, vendor-agnostic
/// ones Task Manager's GPU graphs read (NVIDIA, AMD and Intel all expose them through the
/// WDDM driver model, no vendor SDK needed). Read by spawning the stock typeperf.exe and
/// parsing its text, not by calling PDH directly: see GpuUsage for why.
///
/// Refreshed on its own background loop every 8 seconds — each read spans just over a
/// second and must never block the UI thread the way the 1.5s DispatcherTimer in
/// SystemMonitorService does. Any failure (no GPU Engine category, typeperf missing,
/// sandboxed environment, timeout) degrades to a null reading, never a crash or a
/// visible error.
/// </summary>
public sealed class GpuMonitorService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan SampleTimeout = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private double? _percent;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts != null;

    /// <summary>Null while unknown or unavailable on this machine.</summary>
    public double? Latest
    {
        get { lock (_lock) return _percent; }
    }

    public event EventHandler? Sampled;

    public void Start()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();
        _ = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            double? value = null;
            try
            {
                value = await SampleAsync(token);
            }
            catch (Exception ex)
            {
                Log.Error("GPU sample failed", ex);
            }
            lock (_lock) _percent = value;
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

    private static async Task<double?> SampleAsync(CancellationToken token)
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
        info.ArgumentList.Add(@"\GPU Engine(*)\Utilization Percentage");
        info.ArgumentList.Add("-sc");
        info.ArgumentList.Add("2");
        info.ArgumentList.Add("-si");
        info.ArgumentList.Add("1");

        using var process = new Process { StartInfo = info };
        if (!process.Start()) return null;
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
            return null;
        }

        return process.ExitCode == 0 ? GpuUsage.ParsePercent(await output) : null;
    }
}
