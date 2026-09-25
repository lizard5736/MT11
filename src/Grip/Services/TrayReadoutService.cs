using System.Windows.Threading;
using Grip.Core.Monitoring;
using Grip.Interop;

namespace Grip.Services;

/// <summary>
/// A minimal, independent CPU-percent sampler for the tray icon's tooltip. Deliberately
/// not sharing SystemMonitorService's lifecycle (which only samples while the Monitor
/// panel section is open, to avoid the cost when nobody's looking): this runs whenever
/// "tray readouts" is enabled, panel open or not, and samples nothing but CPU — no
/// memory/disk/network/process enumeration, since the tooltip only ever shows one number.
/// </summary>
public sealed class TrayReadoutService
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private long _prevIdle, _prevKernel, _prevUser;

    public bool IsRunning { get; private set; }

    /// <summary>Null until the first tick after Start() has a baseline to measure from.</summary>
    public double? Latest { get; private set; }

    public event EventHandler? Sampled;

    public TrayReadoutService() => _timer.Tick += (_, _) => Sample();

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdle = idle.Ticks;
        _prevKernel = kernel.Ticks;
        _prevUser = user.Ticks;
        Latest = null;
        _timer.Start();
    }

    public void Stop()
    {
        IsRunning = false;
        _timer.Stop();
        Latest = null;
    }

    private void Sample()
    {
        try
        {
            NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user);
            Latest = CpuUsage.PercentBetween(_prevIdle, _prevKernel, _prevUser, idle.Ticks, kernel.Ticks, user.Ticks);
            _prevIdle = idle.Ticks;
            _prevKernel = kernel.Ticks;
            _prevUser = user.Ticks;
            Sampled?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error("Tray readout sample failed", ex);
        }
    }
}
