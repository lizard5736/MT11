namespace Grip.Core.Features;

/// <summary>Hub sections, in display order.</summary>
public enum FeatureGroup
{
    ClipboardFiles,
    Tools,
    EnergyDisplay,
    Sound,
    Monitor,
    Windows,
    Input,
    Capture,
    Apps,
}

public enum FeatureStatus
{
    /// <summary>Built and installable.</summary>
    Ready,
    /// <summary>On the roadmap; shown in the hub as "coming soon".</summary>
    Planned,
}

/// <summary>
/// What a feature keeps alive while it is on. Static and honest by design:
/// uninstalled features load nothing at all.
/// </summary>
public enum EnergyProfile
{
    /// <summary>Nothing at rest: on-demand tools and shortcut-driven actions.</summary>
    Idle,
    /// <summary>Reacts to system notifications such as clipboard changes.</summary>
    Events,
    /// <summary>A low-level keyboard hook.</summary>
    Keyboard,
    /// <summary>A low-level mouse hook.</summary>
    Mouse,
    /// <summary>Both input hooks.</summary>
    Inputs,
    /// <summary>Samples on an interval while active or visible.</summary>
    Periodic,
}

/// <summary>Stable feature identifiers. Persisted in settings: add, never rename.</summary>
public static class FeatureIds
{
    // Clipboard and files
    public const string ClipboardHistory = "clipboardHistory";
    public const string PastePlain = "pastePlain";
    public const string UrlCleaner = "urlCleaner";
    public const string Shelf = "shelf";
    public const string ImageToFile = "imageToFile";

    // Tools
    public const string CommandBar = "commandBar";
    public const string RadialMenu = "radialMenu";
    public const string QuickToggles = "quickToggles";
    public const string QuickPanel = "quickPanel";
    public const string Scratchpad = "scratchpad";
    public const string CleaningMode = "cleaningMode";

    // Energy and display
    public const string KeepAwake = "keepAwake";
    public const string Brightness = "brightness";
    public const string BluetoothSleep = "bluetoothSleep";

    // Sound
    public const string Mixer = "mixer";
    public const string AppOutput = "appOutput";
    public const string OutputSwitcher = "outputSwitcher";
    public const string MicTools = "micTools";

    // Monitor
    public const string MonitorCpu = "monitorCpu";
    public const string MonitorGpu = "monitorGpu";
    public const string MonitorMemory = "monitorMemory";
    public const string MonitorDisk = "monitorDisk";
    public const string MonitorNetwork = "monitorNetwork";
    public const string MonitorBattery = "monitorBattery";
    public const string CpuTemperature = "cpuTemperature";
    public const string TrayReadouts = "trayReadouts";
    public const string MonitorAlerts = "monitorAlerts";

    // Windows
    public const string WindowSwitcher = "windowSwitcher";
    public const string WindowLayout = "windowLayout";
    public const string CloseProtection = "closeProtection";
    public const string QuitOnClose = "quitOnClose";

    // Keyboard and mouse
    public const string TextSnippets = "textSnippets";
    public const string SmoothScroll = "smoothScroll";
    public const string PointerAcceleration = "pointerAcceleration";
    public const string FocusFollowsMouse = "focusFollowsMouse";
    public const string ScrollDirection = "scrollDirection";
    public const string HorizontalScroll = "horizontalScroll";
    public const string MouseButtons = "mouseButtons";
    public const string ClickFilter = "clickFilter";
    public const string KeyDebounce = "keyDebounce";
    public const string SuperKey = "superKey";

    // Capture and media
    public const string Screenshot = "screenshot";
    public const string ScrollingCapture = "scrollingCapture";
    public const string ScreenRecorder = "screenRecorder";
    public const string ScreenOcr = "screenOcr";
    public const string ColorPicker = "colorPicker";
    public const string CameraPreview = "cameraPreview";
    public const string MediaTools = "mediaTools";

    // Apps
    public const string Winget = "winget";
    public const string AppUpdates = "appUpdates";
    public const string Cleaner = "cleaner";
    public const string MessengerDownloads = "messengerDownloads";
    public const string Uninstaller = "uninstaller";
    public const string PortManager = "portManager";
}

public sealed record FeatureInfo(
    string Id,
    FeatureGroup Group,
    FeatureStatus Status,
    string Icon,
    EnergyProfile Energy)
{
    public bool IsReady => Status == FeatureStatus.Ready;
    public string TitleKey => $"feature.{Id}.title";
    public string DescriptionKey => $"feature.{Id}.desc";
}

public static class FeatureCatalog
{
    public static IReadOnlyList<FeatureInfo> All { get; } = new List<FeatureInfo>
    {
        // Clipboard and files
        new(FeatureIds.ClipboardHistory, FeatureGroup.ClipboardFiles, FeatureStatus.Ready, "Clipboard", EnergyProfile.Events),
        new(FeatureIds.PastePlain, FeatureGroup.ClipboardFiles, FeatureStatus.Ready, "TextClearFormatting", EnergyProfile.Idle),
        new(FeatureIds.UrlCleaner, FeatureGroup.ClipboardFiles, FeatureStatus.Ready, "Link", EnergyProfile.Events),
        new(FeatureIds.Shelf, FeatureGroup.ClipboardFiles, FeatureStatus.Ready, "TrayItemAdd", EnergyProfile.Mouse),
        new(FeatureIds.ImageToFile, FeatureGroup.ClipboardFiles, FeatureStatus.Planned, "Image", EnergyProfile.Keyboard),

        // Tools
        new(FeatureIds.CommandBar, FeatureGroup.Tools, FeatureStatus.Ready, "WindowConsole", EnergyProfile.Idle),
        new(FeatureIds.RadialMenu, FeatureGroup.Tools, FeatureStatus.Ready, "DataPie", EnergyProfile.Idle),
        new(FeatureIds.QuickToggles, FeatureGroup.Tools, FeatureStatus.Ready, "ToggleRight", EnergyProfile.Idle),
        new(FeatureIds.QuickPanel, FeatureGroup.Tools, FeatureStatus.Planned, "Grid", EnergyProfile.Idle),
        new(FeatureIds.Scratchpad, FeatureGroup.Tools, FeatureStatus.Ready, "Edit", EnergyProfile.Idle),
        new(FeatureIds.CleaningMode, FeatureGroup.Tools, FeatureStatus.Planned, "Sparkle", EnergyProfile.Idle),

        // Energy and display
        new(FeatureIds.KeepAwake, FeatureGroup.EnergyDisplay, FeatureStatus.Ready, "WeatherMoon", EnergyProfile.Idle),
        new(FeatureIds.Brightness, FeatureGroup.EnergyDisplay, FeatureStatus.Planned, "BrightnessHigh", EnergyProfile.Idle),
        new(FeatureIds.BluetoothSleep, FeatureGroup.EnergyDisplay, FeatureStatus.Planned, "Bluetooth", EnergyProfile.Events),

        // Sound
        new(FeatureIds.Mixer, FeatureGroup.Sound, FeatureStatus.Planned, "Options", EnergyProfile.Idle),
        new(FeatureIds.AppOutput, FeatureGroup.Sound, FeatureStatus.Planned, "Speaker", EnergyProfile.Idle),
        new(FeatureIds.OutputSwitcher, FeatureGroup.Sound, FeatureStatus.Planned, "HeadphonesSoundWave", EnergyProfile.Events),
        new(FeatureIds.MicTools, FeatureGroup.Sound, FeatureStatus.Planned, "MicOff", EnergyProfile.Idle),

        // Monitor
        new(FeatureIds.MonitorCpu, FeatureGroup.Monitor, FeatureStatus.Ready, "DeveloperBoard", EnergyProfile.Periodic),
        new(FeatureIds.MonitorGpu, FeatureGroup.Monitor, FeatureStatus.Ready, "Gpu", EnergyProfile.Periodic),
        new(FeatureIds.MonitorMemory, FeatureGroup.Monitor, FeatureStatus.Ready, "Ram", EnergyProfile.Periodic),
        new(FeatureIds.MonitorDisk, FeatureGroup.Monitor, FeatureStatus.Ready, "HardDrive", EnergyProfile.Periodic),
        new(FeatureIds.MonitorNetwork, FeatureGroup.Monitor, FeatureStatus.Ready, "Globe", EnergyProfile.Periodic),
        new(FeatureIds.MonitorBattery, FeatureGroup.Monitor, FeatureStatus.Ready, "Battery", EnergyProfile.Periodic),
        new(FeatureIds.CpuTemperature, FeatureGroup.Monitor, FeatureStatus.Planned, "Temperature", EnergyProfile.Periodic),
        new(FeatureIds.TrayReadouts, FeatureGroup.Monitor, FeatureStatus.Ready, "DataPie", EnergyProfile.Periodic),
        new(FeatureIds.MonitorAlerts, FeatureGroup.Monitor, FeatureStatus.Planned, "Alert", EnergyProfile.Periodic),

        // Windows
        new(FeatureIds.WindowSwitcher, FeatureGroup.Windows, FeatureStatus.Planned, "WindowMultiple", EnergyProfile.Keyboard),
        new(FeatureIds.WindowLayout, FeatureGroup.Windows, FeatureStatus.Ready, "Board", EnergyProfile.Idle),
        new(FeatureIds.CloseProtection, FeatureGroup.Windows, FeatureStatus.Planned, "Shield", EnergyProfile.Keyboard),
        new(FeatureIds.QuitOnClose, FeatureGroup.Windows, FeatureStatus.Planned, "Dismiss", EnergyProfile.Events),

        // Keyboard and mouse
        new(FeatureIds.TextSnippets, FeatureGroup.Input, FeatureStatus.Planned, "TextT", EnergyProfile.Keyboard),
        new(FeatureIds.SmoothScroll, FeatureGroup.Input, FeatureStatus.Planned, "Cursor", EnergyProfile.Mouse),
        new(FeatureIds.PointerAcceleration, FeatureGroup.Input, FeatureStatus.Planned, "Cursor", EnergyProfile.Idle),
        new(FeatureIds.FocusFollowsMouse, FeatureGroup.Input, FeatureStatus.Planned, "Cursor", EnergyProfile.Mouse),
        new(FeatureIds.ScrollDirection, FeatureGroup.Input, FeatureStatus.Planned, "ArrowSort", EnergyProfile.Mouse),
        new(FeatureIds.HorizontalScroll, FeatureGroup.Input, FeatureStatus.Planned, "ArrowSwap", EnergyProfile.Mouse),
        new(FeatureIds.MouseButtons, FeatureGroup.Input, FeatureStatus.Planned, "Cursor", EnergyProfile.Mouse),
        new(FeatureIds.ClickFilter, FeatureGroup.Input, FeatureStatus.Planned, "Cursor", EnergyProfile.Mouse),
        new(FeatureIds.KeyDebounce, FeatureGroup.Input, FeatureStatus.Planned, "Keyboard", EnergyProfile.Keyboard),
        new(FeatureIds.SuperKey, FeatureGroup.Input, FeatureStatus.Planned, "KeyboardShift", EnergyProfile.Keyboard),

        // Capture and media
        new(FeatureIds.Screenshot, FeatureGroup.Capture, FeatureStatus.Planned, "Screenshot", EnergyProfile.Idle),
        new(FeatureIds.ScrollingCapture, FeatureGroup.Capture, FeatureStatus.Planned, "Screenshot", EnergyProfile.Idle),
        new(FeatureIds.ScreenRecorder, FeatureGroup.Capture, FeatureStatus.Planned, "Record", EnergyProfile.Idle),
        new(FeatureIds.ScreenOcr, FeatureGroup.Capture, FeatureStatus.Planned, "TextT", EnergyProfile.Idle),
        new(FeatureIds.ColorPicker, FeatureGroup.Capture, FeatureStatus.Planned, "Color", EnergyProfile.Idle),
        new(FeatureIds.CameraPreview, FeatureGroup.Capture, FeatureStatus.Planned, "Camera", EnergyProfile.Idle),
        new(FeatureIds.MediaTools, FeatureGroup.Capture, FeatureStatus.Planned, "Video", EnergyProfile.Idle),

        // Apps
        new(FeatureIds.Winget, FeatureGroup.Apps, FeatureStatus.Planned, "Box", EnergyProfile.Idle),
        new(FeatureIds.AppUpdates, FeatureGroup.Apps, FeatureStatus.Planned, "ArrowDownload", EnergyProfile.Idle),
        new(FeatureIds.Cleaner, FeatureGroup.Apps, FeatureStatus.Planned, "Broom", EnergyProfile.Idle),
        new(FeatureIds.MessengerDownloads, FeatureGroup.Apps, FeatureStatus.Planned, "Folder", EnergyProfile.Idle),
        new(FeatureIds.Uninstaller, FeatureGroup.Apps, FeatureStatus.Planned, "Delete", EnergyProfile.Idle),
        new(FeatureIds.PortManager, FeatureGroup.Apps, FeatureStatus.Planned, "Globe", EnergyProfile.Idle),
    };

    private static readonly Dictionary<string, FeatureInfo> ById = All.ToDictionary(f => f.Id);

    public static FeatureInfo Get(string id) => ById[id];

    public static bool TryGet(string id, out FeatureInfo info) => ById.TryGetValue(id, out info!);

    public static IEnumerable<FeatureInfo> InGroup(FeatureGroup group) => All.Where(f => f.Group == group);

    public static IEnumerable<FeatureInfo> Ready => All.Where(f => f.IsReady);

    /// <summary>Installed on a clean install: every feature that is built.</summary>
    public static bool DefaultInstalled(string id) => ById.TryGetValue(id, out var f) && f.IsReady;
}

/// <summary>
/// One-click starting points for the hub. Applying a preset installs its
/// features and uninstalls the rest; nothing is deleted, every feature keeps
/// its settings and comes back with one click.
/// </summary>
public sealed record FeaturePreset(string Id, string Icon, IReadOnlyList<string> Features)
{
    public string TitleKey => $"preset.{Id}.title";
    public string DescriptionKey => $"preset.{Id}.desc";

    public static IReadOnlyList<FeaturePreset> All { get; } = new List<FeaturePreset>
    {
        new("essentials", "Star", new[]
        {
            FeatureIds.ClipboardHistory, FeatureIds.PastePlain, FeatureIds.UrlCleaner,
            FeatureIds.CommandBar, FeatureIds.QuickToggles, FeatureIds.KeepAwake,
        }),
        new("clipboard", "Clipboard", new[]
        {
            FeatureIds.ClipboardHistory, FeatureIds.PastePlain, FeatureIds.UrlCleaner, FeatureIds.Shelf,
        }),
        new("quiet", "WeatherMoon", new[]
        {
            // Nothing here listens to the keyboard, the mouse or the clipboard.
            FeatureIds.KeepAwake, FeatureIds.QuickToggles, FeatureIds.CommandBar, FeatureIds.PastePlain,
        }),
    };
}
