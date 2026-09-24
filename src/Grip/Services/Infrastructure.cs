using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Grip.Services;

/// <summary>Where Grip keeps its files. Everything stays on this PC.</summary>
public static class AppPaths
{
    /// <summary>Roaming settings: %APPDATA%\Grip.</summary>
    public static string DataDir { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Grip");

    /// <summary>Machine-local data (history, images, logs): %LOCALAPPDATA%\Grip.</summary>
    public static string LocalDir { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Grip");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string ClipboardDir => Path.Combine(LocalDir, "clipboard");
    public static string ClipboardFile => Path.Combine(ClipboardDir, "history.json");
    public static string ClipboardImagesDir => Path.Combine(ClipboardDir, "images");
    public static string LogsDir => Path.Combine(LocalDir, "logs");

    /// <summary>Redirects all data for previews and tests.</summary>
    public static void UseRoot(string root)
    {
        DataDir = Path.Combine(root, "data");
        LocalDir = Path.Combine(root, "local");
    }

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(LocalDir);
        Directory.CreateDirectory(ClipboardImagesDir);
        Directory.CreateDirectory(LogsDir);
    }
}

/// <summary>A tiny rolling file log. Keeps the last few days, never throws.</summary>
public static class Log
{
    private static readonly object Gate = new();
    private static string? _file;

    public static void Init()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogsDir);
            _file = Path.Combine(AppPaths.LogsDir, $"grip-{DateTime.Now:yyyyMMdd}.log");
            foreach (var old in new DirectoryInfo(AppPaths.LogsDir).GetFiles("grip-*.log")
                         .OrderByDescending(f => f.Name).Skip(7))
                old.Delete();
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex == null ? message : $"{message}\n{ex}");

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}";
        Debug.WriteLine(line);
        if (_file == null) return;
        lock (Gate)
        {
            try
            {
                File.AppendAllText(_file, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

/// <summary>
/// One Grip per user session. A second launch pokes the first one (which
/// opens its panel) and exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\Grip.SingleInstance.3f1c";
    private const string EventName = @"Local\Grip.Activate.3f1c";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;
    private RegisteredWaitHandle? _registration;

    public bool IsFirst { get; private set; }

    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out bool created);
        IsFirst = created;
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }

    /// <summary>Waits for the previous Grip to exit (used when restarting as administrator).</summary>
    public bool WaitForOwnership(TimeSpan timeout)
    {
        if (IsFirst) return true;
        try
        {
            IsFirst = _mutex.WaitOne(timeout);
        }
        catch (AbandonedMutexException)
        {
            IsFirst = true;
        }
        return IsFirst;
    }

    public void SignalFirstInstance() => _activate.Set();

    public void ListenForActivation(Action onActivate)
    {
        _registration = ThreadPool.RegisterWaitForSingleObject(_activate, (_, _) => onActivate(), null, -1, executeOnlyOnce: false);
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        _activate.Dispose();
        if (IsFirst)
        {
            try { _mutex.ReleaseMutex(); }
            catch (ApplicationException) { }
        }
        _mutex.Dispose();
    }
}
