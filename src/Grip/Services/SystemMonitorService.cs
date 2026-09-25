using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Grip.Core.Monitoring;
using Grip.Interop;
using Microsoft.Win32;

namespace Grip.Services;

public sealed record DriveSample(string Root, string Label, long FreeBytes, long TotalBytes)
{
    public double UsedPercent => TotalBytes <= 0 ? 0 : (TotalBytes - FreeBytes) * 100.0 / TotalBytes;
}

public sealed record ProcessSample(int ProcessId, string Name, long WorkingSetBytes);

public sealed record MonitorSnapshot(
    double CpuPercent,
    IReadOnlyList<double> CpuCorePercents,
    long MemoryUsedBytes,
    long MemoryTotalBytes,
    IReadOnlyList<DriveSample> Drives,
    double NetworkUpBytesPerSecond,
    double NetworkDownBytesPerSecond,
    long SessionBytesSent,
    long SessionBytesReceived,
    string? LocalIPv4,
    bool HasBattery,
    int BatteryPercent,
    bool BatteryCharging,
    IReadOnlyList<ProcessSample> TopProcesses)
{
    public double MemoryUsedPercent => MemoryTotalBytes <= 0 ? 0 : MemoryUsedBytes * 100.0 / MemoryTotalBytes;
}

/// <summary>
/// Samples CPU, memory, disks, network and battery on a timer. Idle unless
/// started: the Monitor panel section starts it when shown and stops it
/// when the panel closes, same as the panel's own status clock.
///
/// Everything here reads from stable, unprivileged Win32/.NET APIs —
/// nothing needs administrator rights or a driver. GetSystemTimes,
/// GlobalMemoryStatusEx, GetSystemPowerStatus, DriveInfo, NetworkInterface
/// and Process are documented; NtQuerySystemInformation (per-core load) is
/// not, but it is the same call Task Manager and every serious Windows
/// monitoring tool uses for exactly this, has been stable for decades, and
/// marshals as a plain fixed-size struct array — none of the variable-length
/// buffer risk that keeps GPU load on typeperf instead of raw PDH.
/// </summary>
public sealed class SystemMonitorService
{
    private const int HistoryLength = 60;
    private const int ProcessSampleEveryNTicks = 4; // process enumeration is the priciest step; refresh it less often

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(1500) };
    private long _prevIdle, _prevKernel, _prevUser;
    private NativeMethods.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[]? _prevCores;
    private long _prevBytesSent, _prevBytesReceived;
    private long _sessionBaselineSent = -1, _sessionBaselineReceived = -1;
    private DateTime _prevSampleTime;
    private IReadOnlyList<ProcessSample> _lastTopProcesses = Array.Empty<ProcessSample>();
    private int _tick;
    private readonly HashSet<string> _keepAliveOwners = new();

    public SampleHistory CpuHistory { get; } = new(HistoryLength);
    public SampleHistory MemoryHistory { get; } = new(HistoryLength);
    public SampleHistory NetworkHistory { get; } = new(HistoryLength);

    /// <summary>e.g. "12th Gen Intel(R) Core(TM) i7-12700K". Null if the registry value is missing.</summary>
    public string? CpuName { get; } = ReadCpuName();

    public bool IsRunning { get; private set; }

    public MonitorSnapshot? Latest { get; private set; }

    /// <summary>Fires after each sample; handlers read <see cref="Latest"/>.</summary>
    public event EventHandler? Sampled;

    public SystemMonitorService() => _timer.Tick += (_, _) => Sample();

    /// <summary>
    /// Starts sampling, or does nothing if already running. <paramref name="owner"/> tracks
    /// who wants this running — the Monitor panel section and the alerts feature can both
    /// hold it open independently, so a Stop() from one never cuts off the other.
    /// </summary>
    public void Start(string owner = "panel")
    {
        _keepAliveOwners.Add(owner);
        if (IsRunning) return;
        IsRunning = true;
        NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user);
        _prevIdle = idle.Ticks;
        _prevKernel = kernel.Ticks;
        _prevUser = user.Ticks;
        _prevCores = null; // first per-core sample after (re)starting reports 0s rather than a baseline-less spike
        (_prevBytesSent, _prevBytesReceived) = NetworkTotals();
        if (_sessionBaselineSent < 0)
        {
            _sessionBaselineSent = _prevBytesSent;
            _sessionBaselineReceived = _prevBytesReceived;
        }
        _prevSampleTime = DateTime.UtcNow;
        _tick = 0;
        Sample();
        _timer.Start();
    }

    public void Stop(string owner = "panel")
    {
        _keepAliveOwners.Remove(owner);
        if (_keepAliveOwners.Count > 0) return;
        IsRunning = false;
        _timer.Stop();
    }

    private void Sample()
    {
        try
        {
            SampleCore();
        }
        catch (Exception ex)
        {
            // One odd reading must never crash the app or take the timer down — skip this
            // tick, keep the last good snapshot on screen, and try again in 1.5s.
            Log.Error("Monitor sample failed", ex);
        }
    }

    private void SampleCore()
    {
        NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user);
        double cpu = CpuUsage.PercentBetween(_prevIdle, _prevKernel, _prevUser, idle.Ticks, kernel.Ticks, user.Ticks);
        _prevIdle = idle.Ticks;
        _prevKernel = kernel.Ticks;
        _prevUser = user.Ticks;
        CpuHistory.Add(cpu);
        var corePercents = SampleCorePercents();

        var mem = NativeMethods.MEMORYSTATUSEX.Create();
        long memUsed = 0, memTotal = 0;
        if (NativeMethods.GlobalMemoryStatusEx(ref mem))
        {
            memTotal = (long)mem.ullTotalPhys;
            memUsed = memTotal - (long)mem.ullAvailPhys;
        }
        MemoryHistory.Add(memTotal <= 0 ? 0 : memUsed * 100.0 / memTotal);

        List<DriveSample> drives;
        try
        {
            drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => new DriveSample(d.Name, string.IsNullOrEmpty(d.VolumeLabel) ? d.Name.TrimEnd('\\') : d.VolumeLabel,
                    d.TotalFreeSpace, d.TotalSize))
                .ToList();
        }
        catch (IOException) { drives = new List<DriveSample>(); }

        var now = DateTime.UtcNow;
        double seconds = Math.Max(0.1, (now - _prevSampleTime).TotalSeconds);
        var (sent, received) = NetworkTotals();
        double up = Math.Max(0, sent - _prevBytesSent) / seconds;
        double down = Math.Max(0, received - _prevBytesReceived) / seconds;
        _prevBytesSent = sent;
        _prevBytesReceived = received;
        _prevSampleTime = now;
        NetworkHistory.Add(up + down);
        long sessionSent = Math.Max(0, sent - _sessionBaselineSent);
        long sessionReceived = Math.Max(0, received - _sessionBaselineReceived);

        bool hasBattery = false;
        int batteryPercent = 0;
        bool charging = false;
        if (NativeMethods.GetSystemPowerStatus(out var power) && power.BatteryFlag != NativeMethods.BATTERY_FLAG_NO_BATTERY)
        {
            hasBattery = true;
            batteryPercent = power.BatteryLifePercent == NativeMethods.BATTERY_PERCENTAGE_UNKNOWN ? 0 : power.BatteryLifePercent;
            charging = (power.BatteryFlag & 8) != 0;
        }

        _tick++;
        if (_tick % ProcessSampleEveryNTicks == 0 || _lastTopProcesses.Count == 0) _lastTopProcesses = TopProcesses();

        Latest = new MonitorSnapshot(cpu, corePercents, memUsed, memTotal, drives, up, down, sessionSent, sessionReceived,
            LocalIPv4(), hasBattery, batteryPercent, charging, _lastTopProcesses);
        Sampled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>One load percent per logical core. Empty if the query fails — a monitor tile
    /// with no per-core row is a much smaller problem than a crash from a bad NT call.</summary>
    private double[] SampleCorePercents()
    {
        int count = Environment.ProcessorCount;
        var cores = new NativeMethods.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[count];
        int size = Marshal.SizeOf<NativeMethods.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>() * count;
        int status = NativeMethods.NtQuerySystemInformation(NativeMethods.SystemProcessorPerformanceInformation, cores, size, out _);
        if (status != 0)
        {
            _prevCores = null;
            return Array.Empty<double>();
        }

        var percents = new double[count];
        if (_prevCores != null && _prevCores.Length == count)
        {
            for (int i = 0; i < count; i++)
                percents[i] = CpuUsage.PercentBetween(
                    _prevCores[i].IdleTime, _prevCores[i].KernelTime, _prevCores[i].UserTime,
                    cores[i].IdleTime, cores[i].KernelTime, cores[i].UserTime);
        }
        _prevCores = cores;
        return percents;
    }

    private static string? ReadCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = key?.GetValue("ProcessorNameString") as string;
            return string.IsNullOrWhiteSpace(name) ? null : Regex.Replace(name.Trim(), @"\s+", " ");
        }
        catch (System.Security.SecurityException) { return null; }
    }

    private static List<ProcessSample> TopProcesses()
    {
        var result = new List<ProcessSample>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try { result.Add(new ProcessSample(process.Id, process.ProcessName, process.WorkingSet64)); }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
        }
        return result.OrderByDescending(p => p.WorkingSetBytes).Take(8).ToList();
    }

    /// <summary>Every call here is wrapped: an odd adapter, a VPN's virtual NIC or (as under Wine)
    /// a network stack that doesn't support a query at all must never take the whole sampler down.</summary>
    private static (long sent, long received) NetworkTotals()
    {
        long sent = 0, received = 0;
        foreach (var ni in AllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            try
            {
                var stats = ni.GetIPv4Statistics();
                sent += stats.BytesSent;
                received += stats.BytesReceived;
            }
            catch (NetworkInformationException) { }
        }
        return (sent, received);
    }

    private static string? LocalIPv4()
    {
        foreach (var ni in AllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            IPInterfaceProperties props;
            try { props = ni.GetIPProperties(); }
            catch (NetworkInformationException) { continue; }
            foreach (var addr in props.UnicastAddresses)
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
        }
        return null;
    }

    private static NetworkInterface[] AllNetworkInterfaces()
    {
        try { return NetworkInterface.GetAllNetworkInterfaces(); }
        catch (NetworkInformationException) { return Array.Empty<NetworkInterface>(); }
    }
}
