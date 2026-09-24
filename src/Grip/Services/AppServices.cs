using System.Windows;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Input;
using Grip.Core.Settings;
using Grip.Interop;
using Grip.UI;
using Grip.UI.Clipboard;
using Grip.UI.Common;
using Grip.UI.CommandBar;
using Grip.UI.Panel;
using Grip.UI.Radial;
using Grip.UI.Settings;
using Grip.UI.Shelf;

namespace Grip.Services;

/// <summary>Shows HUD notes unless the user switched them off.</summary>
public sealed class HudService
{
    private readonly SettingsService _settings;
    private HudWindow? _window;

    public HudService(SettingsService settings) => _settings = settings;

    public void Show(string text, string icon, HudTone tone = HudTone.Accent, bool force = false)
    {
        if (!force && !_settings.Current.General.ShowHud) return;
        _window ??= new HudWindow();
        _window.ShowMessage(text, icon, tone);
    }
}

/// <summary>Owns the tray panel window and its toggle behavior.</summary>
public sealed class PanelController
{
    private FlyoutWindow? _window;

    public bool IsOpen => _window?.IsVisible == true;

    public void Toggle()
    {
        _window ??= new FlyoutWindow();
        if (_window.IsVisible) _window.HidePanel();
        // A click on the tray icon first deactivates (and hides) the panel; don't bounce it back open.
        else if (DateTime.UtcNow - _window.LastHidden > TimeSpan.FromMilliseconds(350)) _window.ShowPanel();
    }

    public void Show()
    {
        _window ??= new FlyoutWindow();
        _window.ShowPanel();
    }

    public void Hide() => _window?.HidePanel();

    public void Rebuild()
    {
        if (_window?.IsVisible == true) _window.Rebuild();
    }
}

/// <summary>
/// The composition root: creates every service once, wires them together
/// and keeps them in step with the settings.
/// </summary>
public sealed class AppServices : IDisposable
{
    public SettingsService Settings { get; }
    public ThemeService Theme { get; }
    public HudService Hud { get; }
    public MessageWindow Messages { get; }
    public HotkeyService Hotkeys { get; }
    public TrayService Tray { get; }
    public KeepAwakeService KeepAwake { get; }
    public QuickToggleService Toggles { get; }
    public ClipboardService Clipboard { get; }
    public AppCatalogService AppCatalog { get; }
    public InputHookService Hooks { get; }
    public ShelfController Shelf { get; }
    public RadialController Radial { get; }
    public PanelController Panel { get; } = new();
    public SystemMonitorService Monitor { get; } = new();
    public GpuMonitorService Gpu { get; } = new();

    private ClipboardWindow? _clipboardWindow;
    private CommandBarWindow? _commandBar;
    private SettingsWindow? _settingsWindow;
    private bool _applying;

    public AppServices(bool preview = false)
    {
        Settings = new SettingsService(AppPaths.SettingsFile, readOnly: preview);
        Theme = new ThemeService();
        Hud = new HudService(Settings);
        Messages = new MessageWindow("Grip.Messages");
        Hotkeys = new HotkeyService(Messages);
        Tray = new TrayService();
        KeepAwake = new KeepAwakeService(Settings);
        Toggles = new QuickToggleService();
        Clipboard = new ClipboardService(Settings, Messages);
        AppCatalog = new AppCatalogService();
        Hooks = new InputHookService();
        Shelf = new ShelfController(Settings, Hooks);
        Radial = new RadialController(Settings, Hooks);

        LanguageService.Apply(Settings.Current.General.Language);
        Theme.Apply(Settings.Current.General.Theme);
    }

    /// <summary>Starts everything that runs in the background.</summary>
    public void Start()
    {
        KeepAwake.RecoverAfterCrash();
        Hotkeys.Pressed += (_, action) => Execute(action);
        Tray.LeftClick += (_, _) => Panel.Toggle();
        Tray.ContextMenuRequested += (_, _) => Tray.ShowMenu(TrayService.BuildMenu(Execute, KeepAwake.IsActive,
            Settings.Current.IsInstalled(FeatureIds.KeepAwake)));
        KeepAwake.Changed += (_, _) => UpdateTray();
        Settings.Changed += (_, _) => ApplySettings();
        ApplySettings();
        Tray.Show();
        UpdateTray();

        if (Settings.Current.General.LaunchAtStartup != AutostartService.IsEnabled())
            AutostartService.Set(Settings.Current.General.LaunchAtStartup);

        var conflicts = Hotkeys.Conflicts;
        if (conflicts.Count > 0)
        {
            var names = string.Join(", ", conflicts.Select(a => L.S(ActionCatalog.TitleKey(a))));
            Hud.Show(L.F("error.hotkeys", names), "Warning", HudTone.Rec, force: true);
        }
    }

    /// <summary>Brings every service in line with the settings. Cheap; runs after each change.</summary>
    public void ApplySettings()
    {
        if (_applying) return;
        _applying = true;
        try
        {
            var s = Settings.Current;
            LanguageService.Apply(s.General.Language);
            Theme.Apply(s.General.Theme);

            var hotkeys = s.Hotkeys.ToDictionary(p => p.Key, p => Hotkey.Parse(p.Value));
            var disabled = ActionCatalog.All.Where(a => a.FeatureId != null && !s.IsInstalled(a.FeatureId)).Select(a => a.Id);
            Hotkeys.Apply(hotkeys, disabled);

            Clipboard.ApplySettings();
            Shelf.ApplySettings();
            Radial.ApplySettings();
            if (!s.IsInstalled(FeatureIds.KeepAwake) && KeepAwake.IsActive) KeepAwake.Stop();
            UpdateTray();
        }
        finally
        {
            _applying = false;
        }
    }

    private void UpdateTray()
    {
        bool dot = KeepAwake.IsActive && Settings.Current.KeepAwake.ShowInTrayIcon;
        Tray.SetStatus(dot, L.S(KeepAwake.IsActive ? "tray.tooltip.awake" : "tray.tooltip"));
    }

    // ---------- actions ----------

    public void Execute(string actionId)
    {
        try
        {
            ExecuteCore(actionId);
        }
        catch (Exception ex)
        {
            Log.Error($"Action {actionId} failed", ex);
        }
    }

    private void ExecuteCore(string actionId)
    {
        switch (actionId)
        {
            case ActionIds.OpenPanel: Panel.Show(); break;
            case ActionIds.OpenSettings: OpenSettings(null); break;
            case ActionIds.CommandBar:
                _commandBar ??= new CommandBarWindow();
                _commandBar.Toggle();
                break;
            case ActionIds.ClipboardHistory:
                _clipboardWindow ??= new ClipboardWindow();
                _clipboardWindow.Toggle(NativeMethods.GetForegroundWindow());
                break;
            case ActionIds.PastePlain: _ = Clipboard.PastePlainAsync(); break;
            case ActionIds.CleanUrl: Clipboard.CleanLinkOnClipboard(); break;
            case ActionIds.ClearClipboard: Clipboard.ClearSystemClipboard(showHud: true); break;
            case ActionIds.RadialMenu: Radial.OpenFromHotkey(); break;
            case ActionIds.Shelf: Shelf.Toggle(); break;
            case ActionIds.KeepAwakeToggle:
                KeepAwake.Toggle();
                Hud.Show(KeepAwake.IsActive
                        ? (KeepAwake.Session!.IsIndefinite ? L.S("keepAwake.hud.onIndefinite")
                            : L.F("keepAwake.hud.on", Core.Localization.Localizer.Instance.Duration(KeepAwake.Session.Remaining(DateTimeOffset.Now))))
                        : L.S("keepAwake.hud.off"),
                    KeepAwake.IsActive ? "WeatherMoonFilled" : "WeatherMoon", KeepAwake.IsActive ? HudTone.Accent : HudTone.Neutral);
                break;
            case ActionIds.LockScreen: Toggles.Run("lock"); break;
            case ActionIds.DisplayOff: Toggles.Run("displayOff"); break;
            case ActionIds.Sleep: Toggles.Run("sleep"); break;
            case ActionIds.EmptyRecycleBin: Toggles.Run("emptyBin"); break;
            case ActionIds.ToggleDarkMode: Toggles.Run("darkMode"); break;
            case ActionIds.ToggleDesktopIcons: Toggles.Run("desktopIcons"); break;
            case ActionIds.ToggleHiddenFiles: Toggles.Run("hiddenFiles"); break;
            case ActionIds.ToggleFileExtensions: Toggles.Run("fileExtensions"); break;
            case ActionIds.EjectDrives: Toggles.Run("eject"); break;
            case ActionIds.MediaPlayPause: InputSender.Tap(InputSender.VK_MEDIA_PLAY_PAUSE); break;
            case ActionIds.MediaNext: InputSender.Tap(InputSender.VK_MEDIA_NEXT_TRACK); break;
            case ActionIds.MediaPrevious: InputSender.Tap(InputSender.VK_MEDIA_PREV_TRACK); break;
            case ActionIds.VolumeUp: InputSender.Tap(InputSender.VK_VOLUME_UP); break;
            case ActionIds.VolumeDown: InputSender.Tap(InputSender.VK_VOLUME_DOWN); break;
            case ActionIds.VolumeMute: InputSender.Tap(InputSender.VK_VOLUME_MUTE); break;
            default: Log.Warn("Unknown action " + actionId); break;
        }
    }

    public void OpenSettings(string? page)
    {
        Panel.Hide();
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Open(page);
    }

    public void Dispose()
    {
        Settings.SaveNow();
        Monitor.Stop();
        Gpu.Stop();
        KeepAwake.Dispose();
        Clipboard.Dispose();
        Hooks.Dispose();
        Hotkeys.Dispose();
        Tray.Dispose();
        Messages.Dispose();
    }
}
