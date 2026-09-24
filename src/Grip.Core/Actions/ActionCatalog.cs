using Grip.Core.Features;
using Grip.Core.Input;

namespace Grip.Core.Actions;

/// <summary>Stable action identifiers. Persisted in hotkeys and radial menus: add, never rename.</summary>
public static class ActionIds
{
    // Grip windows and tools
    public const string OpenPanel = "grip.panel";
    public const string OpenSettings = "grip.settings";
    public const string CommandBar = "grip.commandBar";
    public const string ClipboardHistory = "grip.clipboard";
    public const string PastePlain = "grip.pastePlain";
    public const string CleanUrl = "grip.cleanUrl";
    public const string ClearClipboard = "grip.clearClipboard";
    public const string RadialMenu = "grip.radial";
    public const string Shelf = "grip.shelf";
    public const string KeepAwakeToggle = "grip.keepAwake";

    // System
    public const string LockScreen = "sys.lock";
    public const string DisplayOff = "sys.displayOff";
    public const string Sleep = "sys.sleep";
    public const string EmptyRecycleBin = "sys.emptyRecycleBin";
    public const string ToggleDarkMode = "sys.darkMode";
    public const string ToggleDesktopIcons = "sys.desktopIcons";
    public const string ToggleHiddenFiles = "sys.hiddenFiles";
    public const string ToggleFileExtensions = "sys.fileExtensions";
    public const string EjectDrives = "sys.ejectDrives";

    // Media keys
    public const string MediaPlayPause = "media.playPause";
    public const string MediaNext = "media.next";
    public const string MediaPrevious = "media.previous";
    public const string VolumeUp = "media.volumeUp";
    public const string VolumeDown = "media.volumeDown";
    public const string VolumeMute = "media.mute";

    // Window layout
    public const string SnapLeft = "win.snapLeft";
    public const string SnapRight = "win.snapRight";
    public const string SnapMaximize = "win.snapMaximize";
    public const string SnapRestore = "win.snapRestore";
    public const string SnapTopLeft = "win.snapTopLeft";
    public const string SnapTopRight = "win.snapTopRight";
    public const string SnapBottomLeft = "win.snapBottomLeft";
    public const string SnapBottomRight = "win.snapBottomRight";
    public const string SnapLeftThird = "win.snapLeftThird";
    public const string SnapCenterThird = "win.snapCenterThird";
    public const string SnapRightThird = "win.snapRightThird";
    public const string SnapLeftTwoThirds = "win.snapLeftTwoThirds";
    public const string SnapRightTwoThirds = "win.snapRightTwoThirds";
}

public enum ActionCategory { Grip, System, Media, Window }

public sealed record ActionInfo(
    string Id,
    ActionCategory Category,
    string Icon,
    string? FeatureId,
    string DefaultHotkey = "",
    bool CanHaveHotkey = true);

public static class ActionCatalog
{
    public static IReadOnlyList<ActionInfo> All { get; } = new List<ActionInfo>
    {
        new(ActionIds.OpenPanel, ActionCategory.Grip, "Navigation", null),
        new(ActionIds.OpenSettings, ActionCategory.Grip, "Settings", null),
        new(ActionIds.CommandBar, ActionCategory.Grip, "WindowConsole", FeatureIds.CommandBar, "Alt+Space"),
        new(ActionIds.ClipboardHistory, ActionCategory.Grip, "Clipboard", FeatureIds.ClipboardHistory, "Win+Alt+V"),
        new(ActionIds.PastePlain, ActionCategory.Grip, "TextClearFormatting", FeatureIds.PastePlain, "Ctrl+Alt+Shift+V"),
        new(ActionIds.CleanUrl, ActionCategory.Grip, "Link", FeatureIds.UrlCleaner),
        new(ActionIds.ClearClipboard, ActionCategory.Grip, "Broom", FeatureIds.ClipboardHistory),
        new(ActionIds.RadialMenu, ActionCategory.Grip, "DataPie", FeatureIds.RadialMenu, "Win+Alt+Q"),
        new(ActionIds.Shelf, ActionCategory.Grip, "TrayItemAdd", FeatureIds.Shelf, "Win+Alt+S"),
        new(ActionIds.KeepAwakeToggle, ActionCategory.Grip, "WeatherMoon", FeatureIds.KeepAwake),

        new(ActionIds.LockScreen, ActionCategory.System, "LockClosed", null),
        new(ActionIds.DisplayOff, ActionCategory.System, "DesktopOff", null),
        new(ActionIds.Sleep, ActionCategory.System, "Sleep", null),
        new(ActionIds.EmptyRecycleBin, ActionCategory.System, "BinRecycle", FeatureIds.QuickToggles),
        new(ActionIds.ToggleDarkMode, ActionCategory.System, "DarkTheme", FeatureIds.QuickToggles),
        new(ActionIds.ToggleDesktopIcons, ActionCategory.System, "Desktop", FeatureIds.QuickToggles),
        new(ActionIds.ToggleHiddenFiles, ActionCategory.System, "Eye", FeatureIds.QuickToggles),
        new(ActionIds.ToggleFileExtensions, ActionCategory.System, "Document", FeatureIds.QuickToggles),
        new(ActionIds.EjectDrives, ActionCategory.System, "ArrowEject", FeatureIds.QuickToggles),

        new(ActionIds.MediaPlayPause, ActionCategory.Media, "Play", null, CanHaveHotkey: false),
        new(ActionIds.MediaNext, ActionCategory.Media, "Next", null, CanHaveHotkey: false),
        new(ActionIds.MediaPrevious, ActionCategory.Media, "Previous", null, CanHaveHotkey: false),
        new(ActionIds.VolumeUp, ActionCategory.Media, "Speaker", null, CanHaveHotkey: false),
        new(ActionIds.VolumeDown, ActionCategory.Media, "SpeakerLow", null, CanHaveHotkey: false),
        new(ActionIds.VolumeMute, ActionCategory.Media, "SpeakerMute", null, CanHaveHotkey: false),

        new(ActionIds.SnapLeft, ActionCategory.Window, "LayoutSplitLeft", FeatureIds.WindowLayout, "Win+Alt+Left"),
        new(ActionIds.SnapRight, ActionCategory.Window, "LayoutSplitRight", FeatureIds.WindowLayout, "Win+Alt+Right"),
        new(ActionIds.SnapMaximize, ActionCategory.Window, "Maximize", FeatureIds.WindowLayout, "Win+Alt+Up"),
        new(ActionIds.SnapRestore, ActionCategory.Window, "WindowRestore", FeatureIds.WindowLayout, "Win+Alt+Down"),
        new(ActionIds.SnapTopLeft, ActionCategory.Window, "LayoutQuarters", FeatureIds.WindowLayout, "Win+Alt+U"),
        new(ActionIds.SnapTopRight, ActionCategory.Window, "LayoutQuarters", FeatureIds.WindowLayout, "Win+Alt+I"),
        new(ActionIds.SnapBottomLeft, ActionCategory.Window, "LayoutQuarters", FeatureIds.WindowLayout, "Win+Alt+J"),
        new(ActionIds.SnapBottomRight, ActionCategory.Window, "LayoutQuarters", FeatureIds.WindowLayout, "Win+Alt+K"),
        new(ActionIds.SnapLeftThird, ActionCategory.Window, "LayoutSplitLeft", FeatureIds.WindowLayout, "Win+Alt+,"),
        new(ActionIds.SnapRightThird, ActionCategory.Window, "LayoutSplitRight", FeatureIds.WindowLayout, "Win+Alt+."),
        new(ActionIds.SnapCenterThird, ActionCategory.Window, "LayoutQuarters", FeatureIds.WindowLayout),
        new(ActionIds.SnapLeftTwoThirds, ActionCategory.Window, "LayoutSplitLeft", FeatureIds.WindowLayout),
        new(ActionIds.SnapRightTwoThirds, ActionCategory.Window, "LayoutSplitRight", FeatureIds.WindowLayout),
    };

    private static readonly Dictionary<string, ActionInfo> ById = All.ToDictionary(a => a.Id);

    public static bool TryGet(string id, out ActionInfo info) => ById.TryGetValue(id, out info!);

    public static ActionInfo Get(string id) => ById[id];

    /// <summary>Localization key of an action title: "action.commandBar", "action.sys.lock", ...</summary>
    public static string TitleKey(string id) =>
        "action." + (id.StartsWith("grip.", StringComparison.Ordinal) ? id["grip.".Length..] : id);

    public static Hotkey DefaultHotkey(string id) =>
        ById.TryGetValue(id, out var info) ? Hotkey.Parse(info.DefaultHotkey) : Hotkey.None;
}
