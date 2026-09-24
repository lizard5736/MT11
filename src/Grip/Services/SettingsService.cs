using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Grip.Core.Localization;
using Grip.Core.Settings;
using Microsoft.Win32;

namespace Grip.Services;

/// <summary>
/// The live settings object plus a debounced save. Every change goes through
/// <see cref="Update"/> (or <see cref="Touch"/> after editing Current in
/// place), which raises <see cref="Changed"/> so services can react.
/// </summary>
public sealed class SettingsService
{
    private readonly SettingsStore _store;
    private readonly DispatcherTimer _saveTimer;

    public AppSettings Current { get; private set; }

    public event EventHandler? Changed;

    /// <summary>Set when the settings file could not be read and defaults were used.</summary>
    public string? LoadProblem { get; }

    public SettingsService(string path, bool readOnly = false)
    {
        _store = new SettingsStore(path);
        Current = _store.Load(out var problem);
        LoadProblem = problem;
        if (problem != null) Log.Warn("Settings file was unreadable, defaults loaded: " + problem);
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            if (!readOnly) SaveNow();
        };
    }

    public void Update(Action<AppSettings> change)
    {
        change(Current);
        Touch();
    }

    /// <summary>Call after mutating <see cref="Current"/> directly.</summary>
    public void Touch()
    {
        Changed?.Invoke(this, EventArgs.Empty);
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveNow()
    {
        try
        {
            _store.Save(Current);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("Could not save settings", ex);
        }
    }

    /// <summary>Swaps in a whole settings object (import, reset).</summary>
    public void Replace(AppSettings settings)
    {
        Current = SettingsStore.Normalize(settings);
        Touch();
    }

    public string Export() => SettingsStore.Serialize(Current);
}

/// <summary>Applies the graphite or light palette, following Windows when asked.</summary>
public sealed class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private AppTheme _setting = AppTheme.Dark;

    public bool IsDark { get; private set; } = true;

    public event EventHandler? ThemeChanged;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && _setting == AppTheme.System)
                Application.Current?.Dispatcher.BeginInvoke(() => Apply(_setting, force: true));
        };
    }

    public void Apply(AppTheme theme, bool force = false)
    {
        _setting = theme;
        bool dark = theme switch
        {
            AppTheme.Light => false,
            AppTheme.System => !AppsUseLightTheme(),
            _ => true,
        };
        if (!force && dark == IsDark && Application.Current.Resources.MergedDictionaries.Count > 0
            && Application.Current.Resources.MergedDictionaries[0].Source?.OriginalString.Contains(dark ? "Dark" : "Light") == true)
            return;

        IsDark = dark;
        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Grip;component/UI/Theme/Colors.{(dark ? "Dark" : "Light")}.xaml", UriKind.Absolute),
        };
        var merged = Application.Current.Resources.MergedDictionaries;
        if (merged.Count > 0) merged[0] = palette;
        else merged.Insert(0, palette);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public static bool AppsUseLightTheme() => ReadPersonalize("AppsUseLightTheme", 0) != 0;

    /// <summary>The taskbar follows the system theme, not the apps theme.</summary>
    public static bool TaskbarIsLight() => ReadPersonalize("SystemUsesLightTheme", 0) != 0;

    private static int ReadPersonalize(string name, int fallback)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue(name) is int value ? value : fallback;
        }
        catch (System.Security.SecurityException)
        {
            return fallback;
        }
    }

    public System.Windows.Media.Color Color(string key) =>
        Application.Current.TryFindResource("Grip.Color." + key) is System.Windows.Media.Color c ? c : System.Windows.Media.Colors.Gray;
}

/// <summary>Keeps the UI language in sync with the setting.</summary>
public static class LanguageService
{
    public static void Apply(AppLanguage language)
    {
        var resolved = Localizer.Resolve(language, CultureInfo.CurrentUICulture);
        Localizer.Instance.SetLanguage(resolved);
    }
}

/// <summary>Start with Windows through the per-user Run key (no admin rights needed).</summary>
public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Grip";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value
                   && value.Contains(Interop.ProcessInfo.CurrentExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue(ValueName, $"\"{Interop.ProcessInfo.CurrentExePath}\" --autostart");
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Error("Could not update autostart", ex);
        }
    }
}
