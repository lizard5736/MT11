using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using static Grip.Interop.NativeMethods;

namespace Grip.Interop;

/// <summary>A monitor in physical pixels, plus its scale relative to 96 DPI.</summary>
internal readonly record struct MonitorArea(RECT Bounds, RECT Work, double Scale)
{
    public Rect WorkDip => new(Work.Left / Scale, Work.Top / Scale, Work.Width / Scale, Work.Height / Scale);
}

internal static class Screens
{
    public static POINT CursorPosition()
    {
        GetCursorPos(out var p);
        return p;
    }

    public static MonitorArea FromPoint(POINT point)
    {
        var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
        return FromHandle(monitor);
    }

    public static MonitorArea FromWindow(IntPtr hwnd) => FromHandle(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST));

    public static MonitorArea FromHandle(IntPtr monitor)
    {
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        double scale = 1;
        try
        {
            if (GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0) scale = dpiX / 96.0;
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        return new MonitorArea(info.rcMonitor, info.rcWork, scale);
    }

    /// <summary>Which edge the taskbar sits on for the monitor under a point (0 left, 1 top, 2 right, 3 bottom).</summary>
    public static int TaskbarEdge(MonitorArea area)
    {
        var w = area.Work;
        var b = area.Bounds;
        if (w.Top > b.Top) return 1;
        if (w.Left > b.Left) return 0;
        if (w.Right < b.Right) return 2;
        return 3;
    }
}

internal static class WindowPlacement
{
    /// <summary>
    /// Moves a window so its top-left lands on a physical-pixel point. Works
    /// across monitors with different scaling because it bypasses WPF's own
    /// DIP conversion (which assumes the window's current monitor).
    /// </summary>
    public static void MoveTo(Window window, int x, int y)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>The window's current size in physical pixels.</summary>
    public static (int Width, int Height) PhysicalSize(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        GetWindowRect(hwnd, out var r);
        return (r.Width, r.Height);
    }

    /// <summary>
    /// Places a window near an anchor point, kept fully inside the work area of
    /// that point's monitor. <paramref name="below"/> puts it under the anchor,
    /// otherwise it grows upward from it.
    /// </summary>
    public static void PlaceNear(Window window, POINT anchor, bool below, int gap = 12)
    {
        var area = Screens.FromPoint(anchor);
        // Hop onto the target monitor first so WPF rescales for its DPI.
        MoveTo(window, area.Work.Left + 1, area.Work.Top + 1);
        window.UpdateLayout();
        var (w, h) = PhysicalSize(window);
        int margin = (int)(gap * area.Scale);
        int x = anchor.X - w / 2;
        int y = below ? anchor.Y + margin : anchor.Y - h - margin;
        x = Math.Clamp(x, area.Work.Left + margin, Math.Max(area.Work.Left + margin, area.Work.Right - w - margin));
        y = Math.Clamp(y, area.Work.Top + margin, Math.Max(area.Work.Top + margin, area.Work.Bottom - h - margin));
        MoveTo(window, x, y);
    }

    /// <summary>Centers a window horizontally on a monitor, at a fraction of its height.</summary>
    public static void PlaceOnMonitor(Window window, MonitorArea area, double verticalFraction)
    {
        MoveTo(window, area.Work.Left + 1, area.Work.Top + 1);
        window.UpdateLayout();
        var (w, h) = PhysicalSize(window);
        int x = area.Work.Left + (area.Work.Width - w) / 2;
        int y = area.Work.Top + (int)((area.Work.Height - h) * verticalFraction);
        MoveTo(window, x, Math.Max(area.Work.Top, y));
    }
}

internal static class WindowStyling
{
    /// <summary>Keeps a window out of Alt+Tab and the taskbar.</summary>
    public static void MakeToolWindow(Window window, bool noActivate = false, bool clickThrough = false)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
        ex |= WS_EX_TOOLWINDOW;
        ex &= ~WS_EX_APPWINDOW;
        if (noActivate) ex |= WS_EX_NOACTIVATE;
        if (clickThrough) ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    /// <summary>Windows 11 rounded corners and a graphite caption for Grip's windows.</summary>
    public static void ApplyChrome(Window window, Color caption, Color text, Color border, bool dark, bool round = true)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return; // not created yet, or already closed
        try
        {
            int value = dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            if (round)
            {
                int corner = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
            }
            int captionRef = ToColorRef(caption);
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionRef, sizeof(int));
            int textRef = ToColorRef(text);
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textRef, sizeof(int));
            int borderRef = ToColorRef(border);
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderRef, sizeof(int));
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    private static int ToColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

    /// <summary>Brings a Grip window to the front even when another app owns the foreground.</summary>
    public static void ForceForeground(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        ForceForeground(hwnd);
    }

    public static void ForceForeground(IntPtr hwnd)
    {
        if (SetForegroundWindow(hwnd)) return;
        // The classic dance: briefly attach to the foreground thread's input.
        var foreground = GetForegroundWindow();
        uint foregroundThread = GetWindowThreadProcessId(foreground, out _);
        uint current = GetCurrentThreadId();
        if (foregroundThread != current && foregroundThread != 0)
        {
            AttachThreadInput(current, foregroundThread, true);
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
            AttachThreadInput(current, foregroundThread, false);
        }
        else
        {
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
        }
    }
}

internal static class ProcessInfo
{
    public static string? ExeNameForWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        GetWindowThreadProcessId(hwnd, out var pid);
        return ExeName(pid);
    }

    public static string? ExePathForWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        GetWindowThreadProcessId(hwnd, out var pid);
        return ExePath(pid);
    }

    public static string? ExeName(uint pid)
    {
        var path = ExePath(pid);
        return path == null ? null : System.IO.Path.GetFileName(path);
    }

    public static string? ExePath(uint pid)
    {
        if (pid == 0) return null;
        var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) return null;
        try
        {
            var sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;
            return QueryFullProcessImageName(handle, 0, sb, ref size) ? sb.ToString() : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    /// <summary>
    /// True when the window's process runs elevated. A process we cannot even
    /// query from a standard-rights Grip is treated as elevated too.
    /// </summary>
    public static bool IsElevated(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        GetWindowThreadProcessId(hwnd, out var pid);
        var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero) return !CurrentIsElevated;
        try
        {
            if (!OpenProcessToken(process, TOKEN_QUERY, out var token)) return !CurrentIsElevated;
            try
            {
                return GetTokenInformation(token, TokenElevation, out int elevated, sizeof(int), out _) && elevated != 0;
            }
            finally
            {
                CloseHandle(token);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }

    public static bool CurrentIsElevated { get; } = ComputeCurrentElevation();

    private static bool ComputeCurrentElevation()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static string CurrentExePath => Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "Grip.exe";
}
