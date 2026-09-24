using System.ComponentModel;
using System.Diagnostics;

namespace Grip.Services;

/// <summary>
/// Terminates a process for the Monitor panel's kill buttons. Every failure mode (already
/// exited, protected/elevated, access denied) is reported back rather than thrown — a row
/// going stale between the confirm click and the kill is an expected race, not a bug.
/// </summary>
public static class ProcessControl
{
    public static bool TryKill(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill();
            return true;
        }
        catch (ArgumentException)
        {
            return true; // already exited by the time the click landed — nothing left to do
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
        {
            Log.Error($"Failed to kill process {processId}", ex);
            return false;
        }
    }
}
