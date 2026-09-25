using Grip.Core.Monitoring;
using Grip.UI;
using Grip.UI.Common;

namespace Grip.Services;

/// <summary>
/// Watches the Monitor's own thresholds and fires one HUD notice per rising edge — the
/// first sample that crosses into a warning, not every sample while it stays there.
///
/// Holds SystemMonitorService and GpuMonitorService open under its own "alerts" key, so a
/// warning still reaches you with the Monitor panel closed — the entire point of a notice
/// instead of a red digit the panel would otherwise be the only place to see.
/// </summary>
public sealed class MonitorAlertService
{
    private readonly SystemMonitorService _monitor;
    private readonly GpuMonitorService _gpu;
    private readonly HudService _hud;
    private readonly HashSet<string> _disksLow = new();
    private bool _cpuWasHigh, _memoryWasHigh, _batteryWasLow, _gpuWasHigh;

    public bool IsRunning { get; private set; }

    public MonitorAlertService(SystemMonitorService monitor, GpuMonitorService gpu, HudService hud)
    {
        _monitor = monitor;
        _gpu = gpu;
        _hud = hud;
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        // A fresh start shouldn't immediately fire for whatever state the machine is
        // already in — only for a threshold crossed while this has been watching.
        if (_monitor.Latest is { } snapshot) SyncBaseline(snapshot);
        _gpuWasHigh = _gpu.Latest is { } percent && percent >= MonitorWarnings.CpuHighPercent;
        _monitor.Sampled += OnSampled;
        _gpu.Sampled += OnGpuSampled;
        _monitor.Start("alerts");
        _gpu.Start("alerts");
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _monitor.Sampled -= OnSampled;
        _gpu.Sampled -= OnGpuSampled;
        _monitor.Stop("alerts");
        _gpu.Stop("alerts");
    }

    private void SyncBaseline(MonitorSnapshot snapshot)
    {
        _cpuWasHigh = MonitorWarnings.IsCpuHigh(snapshot.CpuPercent);
        _memoryWasHigh = MonitorWarnings.IsMemoryHigh(snapshot.MemoryUsedPercent);
        _batteryWasLow = snapshot.HasBattery && MonitorWarnings.IsBatteryLow(snapshot.BatteryPercent, snapshot.BatteryCharging);
        _disksLow.Clear();
        foreach (var drive in snapshot.Drives.Where(d => MonitorWarnings.IsDiskLow(100 - d.UsedPercent)))
            _disksLow.Add(drive.Root);
    }

    private void OnSampled(object? sender, EventArgs e)
    {
        if (_monitor.Latest is not { } snapshot) return;

        bool cpuHigh = MonitorWarnings.IsCpuHigh(snapshot.CpuPercent);
        if (cpuHigh && !_cpuWasHigh) Alert(L.F("monitor.alert.cpu", Math.Round(snapshot.CpuPercent)));
        _cpuWasHigh = cpuHigh;

        bool memoryHigh = MonitorWarnings.IsMemoryHigh(snapshot.MemoryUsedPercent);
        if (memoryHigh && !_memoryWasHigh) Alert(L.F("monitor.alert.memory", Math.Round(snapshot.MemoryUsedPercent)));
        _memoryWasHigh = memoryHigh;

        bool batteryLow = snapshot.HasBattery && MonitorWarnings.IsBatteryLow(snapshot.BatteryPercent, snapshot.BatteryCharging);
        if (batteryLow && !_batteryWasLow) Alert(L.F("monitor.alert.battery", snapshot.BatteryPercent));
        _batteryWasLow = batteryLow;

        foreach (var drive in snapshot.Drives)
        {
            bool low = MonitorWarnings.IsDiskLow(100 - drive.UsedPercent);
            if (low && !_disksLow.Contains(drive.Root)) Alert(L.F("monitor.alert.disk", drive.Label));
            if (low) _disksLow.Add(drive.Root);
            else _disksLow.Remove(drive.Root);
        }
    }

    private void OnGpuSampled(object? sender, EventArgs e)
    {
        bool gpuHigh = _gpu.Latest is { } percent && percent >= MonitorWarnings.CpuHighPercent;
        if (gpuHigh && !_gpuWasHigh) Alert(L.F("monitor.alert.gpu", Math.Round(_gpu.Latest!.Value)));
        _gpuWasHigh = gpuHigh;
    }

    private void Alert(string text) => _hud.Show(text, "Warning", HudTone.Rec, force: true);
}
