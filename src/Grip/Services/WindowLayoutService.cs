using Grip.Core.Windows;
using Grip.Interop;

namespace Grip.Services;

/// <summary>
/// Snaps the foreground window into halves, quarters or full screen, and remembers
/// where it was so one more hotkey press puts it back. Every foreign window is moved
/// through plain SetWindowPos in physical pixels — the same call and the same
/// per-monitor DPI handling (Screens.FromWindow) already proven for Grip's own
/// windows, just aimed at whatever window currently has focus instead of at Grip.
/// </summary>
public sealed class WindowLayoutService
{
    private readonly Dictionary<IntPtr, NativeMethods.RECT> _previous = new();

    public void Apply(WindowZone zone)
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (!CanMove(hwnd)) return;

        NativeMethods.GetWindowRect(hwnd, out var current);
        _previous[hwnd] = current;

        if (zone == WindowZone.Maximize)
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
            return;
        }
        if (NativeMethods.IsZoomed(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);

        var area = Screens.FromWindow(hwnd);
        var rect = WindowZones.Compute(zone, area.Work.Left, area.Work.Top, area.Work.Width, area.Work.Height);
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, rect.X, rect.Y, rect.Width, rect.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
    }

    /// <summary>Puts the foreground window back where it was before Grip last moved it.</summary>
    public void RestorePrevious()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (!CanMove(hwnd) || !_previous.Remove(hwnd, out var rect)) return;

        if (NativeMethods.IsZoomed(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
    }

    private static bool CanMove(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        // UIPI blocks a standard-rights Grip from moving an elevated window, same as paste.
        if (ProcessInfo.IsElevated(hwnd) && !ProcessInfo.CurrentIsElevated) return false;
        return true;
    }
}
