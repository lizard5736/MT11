namespace Grip.Core.Monitoring;

/// <summary>
/// Total CPU load between two whole-system time snapshots, the same way
/// Task Manager computes it: idle time versus everything else. Values are
/// 100-nanosecond ticks, matching Win32's GetSystemTimes.
/// </summary>
public static class CpuUsage
{
    /// <summary>
    /// Percent busy between two snapshots. Kernel time already includes idle
    /// time on Windows, so total work is kernel + user, and busy is total minus idle.
    /// </summary>
    public static double PercentBetween(long idle0, long kernel0, long user0, long idle1, long kernel1, long user1)
    {
        long idleDelta = idle1 - idle0;
        long totalDelta = (kernel1 - kernel0) + (user1 - user0);
        if (totalDelta <= 0) return 0;
        double busy = totalDelta - idleDelta;
        return Math.Clamp(busy * 100.0 / totalDelta, 0, 100);
    }
}
