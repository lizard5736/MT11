using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Grip.Interop;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

public sealed record AppEntry(string Name, string AppId)
{
    public string ShellPath => @"shell:AppsFolder\" + AppId;
}

/// <summary>
/// The Start menu's app list (desktop and Store apps), loaded in the
/// background and refreshed at most every few minutes. Icons load lazily.
/// </summary>
public sealed class AppCatalogService
{
    private readonly Dictionary<string, BitmapSource?> _icons = new(StringComparer.OrdinalIgnoreCase);
    private List<AppEntry> _apps = new();
    private DateTime _loadedAt = DateTime.MinValue;
    private Task? _loading;

    public IReadOnlyList<AppEntry> Apps => _apps;

    public event EventHandler? Updated;

    /// <summary>Preview renderer: a fixed app list instead of the real Start menu.</summary>
    public void SetForPreview(IEnumerable<AppEntry> apps)
    {
        _apps = apps.ToList();
        _loadedAt = DateTime.UtcNow;
    }

    /// <summary>Starts a background refresh when the list is missing or stale.</summary>
    public void Refresh(bool force = false)
    {
        if (_loading is { IsCompleted: false }) return;
        if (!force && DateTime.UtcNow - _loadedAt < TimeSpan.FromMinutes(5) && _apps.Count > 0) return;
        _loading = ShellInterop.RunSta(() =>
            {
                try
                {
                    return ShellInterop.EnumerateStartApps();
                }
                catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidCastException)
                {
                    Log.Warn("Start menu apps could not be listed: " + ex.Message);
                    return new List<ShellInterop.ShellApp>();
                }
            })
            .ContinueWith(t =>
            {
                if (t.IsFaulted) return;
                _apps = t.Result
                    .Where(a => !IsUninstaller(a))
                    .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new AppEntry(g.Key, g.First().AppId))
                    .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                _loadedAt = DateTime.UtcNow;
                Updated?.Invoke(this, EventArgs.Empty);
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static bool IsUninstaller(ShellInterop.ShellApp app)
    {
        var id = app.AppId.ToLowerInvariant();
        var name = app.Name.ToLowerInvariant();
        return id.EndsWith("unins000.exe", StringComparison.Ordinal) || id.EndsWith("uninstall.exe", StringComparison.Ordinal)
               || name.StartsWith("uninstall ", StringComparison.Ordinal) || name.StartsWith("удалить ", StringComparison.Ordinal)
               || name.StartsWith("удаление ", StringComparison.Ordinal);
    }

    /// <summary>Returns a cached icon, or null while it loads (then raises <see cref="IconLoaded"/>).</summary>
    public BitmapSource? Icon(string parsingName)
    {
        lock (_icons)
        {
            if (_icons.TryGetValue(parsingName, out var icon)) return icon;
            _icons[parsingName] = null;
        }
        ShellInterop.RunSta(() => ShellInterop.GetIcon(parsingName, 32)).ContinueWith(t =>
        {
            if (t.IsFaulted || t.Result == null) return;
            lock (_icons) _icons[parsingName] = t.Result;
            IconLoaded?.Invoke(this, parsingName);
        }, TaskScheduler.FromCurrentSynchronizationContext());
        return null;
    }

    public event EventHandler<string>? IconLoaded;

    public static bool Launch(AppEntry app)
    {
        try
        {
            Process.Start(new ProcessStartInfo(app.ShellPath) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            Log.Warn($"Could not launch {app.Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Opens a file, folder, program or link the way Explorer would.</summary>
    public static bool Open(string target, string? arguments = null)
    {
        try
        {
            var info = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(target)) { UseShellExecute = true };
            if (!string.IsNullOrWhiteSpace(arguments)) info.Arguments = arguments;
            Process.Start(info);
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            Log.Warn($"Could not open {target}: {ex.Message}");
            return false;
        }
    }
}

public sealed record WindowEntry(IntPtr Handle, string Title, string? ExePath, string? ProcessName);

/// <summary>Open top-level windows, the way Alt+Tab sees them.</summary>
public static class WindowList
{
    public static List<WindowEntry> Current()
    {
        var result = new List<WindowEntry>();
        var own = (uint)Environment.ProcessId;
        var shell = GetShellWindow();
        EnumWindows((hwnd, _) =>
        {
            if (hwnd == shell || !IsWindowVisible(hwnd)) return true;
            long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            bool appWindow = (ex & WS_EX_APPWINDOW) != 0;
            if ((ex & WS_EX_TOOLWINDOW) != 0 && !appWindow) return true;
            if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero && !appWindow) return true;
            if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            int length = GetWindowTextLength(hwnd);
            if (length == 0) return true;
            var sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == own) return true;
            var path = ProcessInfo.ExePath(pid);
            result.Add(new WindowEntry(hwnd, sb.ToString(), path, path == null ? null : Path.GetFileNameWithoutExtension(path)));
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static void Activate(IntPtr hwnd)
    {
        if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
        WindowStyling.ForceForeground(hwnd);
    }
}
