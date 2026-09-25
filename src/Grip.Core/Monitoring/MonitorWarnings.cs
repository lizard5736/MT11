namespace Grip.Core.Monitoring;

/// <summary>
/// Thresholds for when a monitor tile switches to its warning color. Plain
/// functions so the panel and (later) the alerts feature agree on one rule.
/// </summary>
public static class MonitorWarnings
{
    public const double CpuHighPercent = 90;
    public const double MemoryHighPercent = 90;
    public const double DiskLowFreePercent = 5;
    public const double BatteryLowPercent = 10;
    public const double GpuHighTemperatureCelsius = 85;

    public static bool IsCpuHigh(double percent) => percent >= CpuHighPercent;

    public static bool IsMemoryHigh(double usedPercent) => usedPercent >= MemoryHighPercent;

    public static bool IsDiskLow(double freePercent) => freePercent <= DiskLowFreePercent;

    public static bool IsGpuTemperatureHigh(double celsius) => celsius >= GpuHighTemperatureCelsius;

    /// <summary>Low and not charging — plugged in at 5% isn't a warning, unplugged at 5% is.</summary>
    public static bool IsBatteryLow(double percent, bool charging) => !charging && percent <= BatteryLowPercent;
}
