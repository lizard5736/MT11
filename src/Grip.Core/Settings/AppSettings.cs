using Grip.Core.Features;

namespace Grip.Core.Settings;

/// <summary>
/// Everything Grip remembers, persisted as JSON in %APPDATA%\Grip\settings.json.
/// New properties must have sensible defaults: older files simply lack them.
/// </summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public GeneralSettings General { get; set; } = new();
    public PanelSettings Panel { get; set; } = new();

    /// <summary>Feature id → installed. Missing ids fall back to the catalog default.</summary>
    public Dictionary<string, bool> Features { get; set; } = new();

    /// <summary>Action id → shortcut text ("Win+Alt+V"); empty string means "no shortcut".</summary>
    public Dictionary<string, string> Hotkeys { get; set; } = new();

    public ClipboardSettings Clipboard { get; set; } = new();
    public UrlCleanerSettings UrlCleaner { get; set; } = new();
    public KeepAwakeSettings KeepAwake { get; set; } = new();
    public QuickTogglesSettings QuickToggles { get; set; } = new();
    public CommandBarSettings CommandBar { get; set; } = new();
    public RadialMenuSettings RadialMenu { get; set; } = new();
    public ShelfSettings Shelf { get; set; } = new();

    public bool IsInstalled(string featureId) =>
        Features.TryGetValue(featureId, out var installed)
            ? installed && FeatureCatalog.TryGet(featureId, out var info) && info.IsReady
            : FeatureCatalog.DefaultInstalled(featureId);

    public void SetInstalled(string featureId, bool installed) => Features[featureId] = installed;
}

public enum AppLanguage { System, Russian, English }

public enum AppTheme { Dark, Light, System }

public enum PanelLayoutMode { Tabs, List }

public sealed class GeneralSettings
{
    public AppLanguage Language { get; set; } = AppLanguage.System;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool LaunchAtStartup { get; set; } = true;
    public bool ShowHud { get; set; } = true;
    public bool OnboardingCompleted { get; set; }
}

public sealed class PanelSettings
{
    public PanelLayoutMode Layout { get; set; } = PanelLayoutMode.Tabs;
    public bool Compact { get; set; }
    public List<string> SectionOrder { get; set; } = new();
    public List<string> HiddenSections { get; set; } = new();
    public List<string> CollapsedSections { get; set; } = new();
    public string? LastSection { get; set; }
}

public enum ClipboardPopupPosition { Cursor, Center }

public sealed class ClipboardSettings
{
    public bool HistoryEnabled { get; set; } = true;
    public int MaxItems { get; set; } = 200;
    /// <summary>0 keeps unpinned entries until the size limit pushes them out.</summary>
    public int RetentionDays { get; set; }
    public bool SaveImages { get; set; } = true;
    public bool SaveFiles { get; set; } = true;
    public bool KeepAfterRestart { get; set; } = true;
    /// <summary>Enter pastes into the app you came from; off means it only copies.</summary>
    public bool PasteOnSelect { get; set; } = true;
    public ClipboardPopupPosition PopupPosition { get; set; } = ClipboardPopupPosition.Cursor;
    public bool ShowPreview { get; set; } = true;
    public List<string> IgnoredApps { get; set; } = new()
    {
        "KeePass.exe", "KeePassXC.exe", "1Password.exe", "Bitwarden.exe", "Dashlane.exe", "LastPass.exe",
    };

    public bool AutoClearEnabled { get; set; }
    public int AutoClearSeconds { get; set; } = 60;
    public bool ClearOnLock { get; set; }
    public bool ClearOnSleep { get; set; }
}

public sealed class UrlCleanerSettings
{
    public bool AutoClean { get; set; }
    public bool UnwrapRedirects { get; set; } = true;
    public bool CleanSearchLinks { get; set; } = true;
    public bool ShowHud { get; set; } = true;
    public List<string> ExtraParameters { get; set; } = new();
    public List<string> SkipDomains { get; set; } = new();
}

public sealed class KeepAwakeSettings
{
    public bool KeepDisplayOn { get; set; } = true;
    /// <summary>0 means "until you turn it off".</summary>
    public int DefaultMinutes { get; set; }
    public bool ShowInTrayIcon { get; set; } = true;
    public bool LidClosed { get; set; }
    public List<int> Presets { get; set; } = new() { 0, 15, 30, 60, 120, 240 };
}

public sealed class QuickTogglesSettings
{
    public List<string> Visible { get; set; } = new();
}

public enum WebSearchEngine { Google, Yandex, DuckDuckGo, Bing }

public sealed class CommandBarSettings
{
    public bool SearchApps { get; set; } = true;
    public bool SearchWindows { get; set; } = true;
    public bool SearchSettings { get; set; } = true;
    public bool SearchClipboard { get; set; } = true;
    public bool Calculator { get; set; } = true;
    public bool Units { get; set; } = true;
    public bool Emoji { get; set; } = true;
    public bool WebSearch { get; set; } = true;
    public WebSearchEngine SearchEngine { get; set; } = WebSearchEngine.Google;
    public bool FixKeyboardLayout { get; set; } = true;
    public List<ScriptEntry> Scripts { get; set; } = new();
}

public sealed class ScriptEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Arguments { get; set; } = "";
}

public enum RadialMouseTrigger { None, Middle, XButton1, XButton2 }

public enum RadialSize { Small, Medium, Large }

public sealed class RadialMenuSettings
{
    public RadialMouseTrigger MouseTrigger { get; set; } = RadialMouseTrigger.None;
    public RadialSize Size { get; set; } = RadialSize.Medium;
    /// <summary>Releasing the shortcut runs the highlighted item, like a pie menu.</summary>
    public bool ReleaseToSelect { get; set; } = true;
    public string ActiveProfileId { get; set; } = "main";
    public List<Radial.RadialProfile> Profiles { get; set; } = new();
}

public sealed class ShelfSettings
{
    public bool ShakeToOpen { get; set; } = true;
    public bool KeepItemsAfterDragOut { get; set; }
    public bool OpenNearCursor { get; set; } = true;
}
