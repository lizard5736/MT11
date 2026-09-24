using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Grip.Core.Features;
using Grip.Core.Settings;
using Grip.Interop;
using Grip.Services;

namespace Grip.UI.Settings.Pages;

/// <summary>
/// Windows has no permission prompts like macOS; what matters here is admin
/// rights (for elevated windows), what runs in the background right now, and
/// where the data lives.
/// </summary>
public static class AccessPage
{
    public static void Build(PageBuilder b)
    {
        b.Card("access.admin.title");
        bool elevated = ProcessInfo.CurrentIsElevated;
        var status = b.Row(L.S(elevated ? "access.admin.granted" : "access.admin.normal"), L.S("access.admin.desc"),
            elevated ? null : RestartAsAdminButton(), elevated ? "CheckmarkCircleFilled" : "Shield");

        b.Card("access.background.title", "access.background.desc");
        var s = App.Services.Settings.Current;
        AddListeners(b, "access.background.events", "Clipboard", EventsListeners(s));
        AddListeners(b, "access.background.mouse", "Cursor", MouseListeners(s));
        AddListeners(b, "access.background.keyboard", "Keyboard", new List<string>());
        var shortcuts = App.Services.Settings.Current.Hotkeys
            .Where(h => App.Services.Hotkeys.StateOf(h.Key) == HotkeyState.Active)
            .Select(h => L.S(Core.Actions.ActionCatalog.TitleKey(h.Key)))
            .ToList();
        AddListeners(b, "access.background.hotkeys", "KeyboardShift", shortcuts);

        b.Card("access.data.title", "access.data.desc");
        b.Button("about.data", AppPaths.DataDir, "common.openFolder", () => OpenFolder(AppPaths.DataDir), icon: "FolderOpen", rowIcon: "Folder");
        b.Button("clipboard.title", AppPaths.ClipboardDir, "common.openFolder", () => OpenFolder(AppPaths.ClipboardDir), icon: "FolderOpen", rowIcon: "Clipboard");
    }

    private static List<string> EventsListeners(AppSettings s)
    {
        var list = new List<string>();
        if (s.IsInstalled(FeatureIds.ClipboardHistory) && s.Clipboard.HistoryEnabled) list.Add(L.S("feature.clipboardHistory.title"));
        if (s.IsInstalled(FeatureIds.UrlCleaner) && s.UrlCleaner.AutoClean) list.Add(L.S("controls.autoClean.title"));
        if (s.IsInstalled(FeatureIds.ClipboardHistory) && s.Clipboard.AutoClearEnabled) list.Add(L.S("controls.autoClear.title"));
        return list;
    }

    private static List<string> MouseListeners(AppSettings s)
    {
        var list = new List<string>();
        if (s.IsInstalled(FeatureIds.Shelf) && s.Shelf.ShakeToOpen) list.Add(L.S("controls.shake.title"));
        if (s.IsInstalled(FeatureIds.RadialMenu) && s.RadialMenu.MouseTrigger != RadialMouseTrigger.None) list.Add(L.S("controls.radialMouse.title"));
        return list;
    }

    private static void AddListeners(PageBuilder b, string titleKey, string icon, List<string> names)
    {
        var value = new TextBlock
        {
            Text = names.Count == 0 ? L.S("access.background.none") : string.Join(", ", names),
            Style = (Style)Application.Current.FindResource("Grip.Text.Secondary"),
            FontSize = 13,
            TextAlignment = TextAlignment.Right,
            MaxWidth = 380,
        };
        value.SetResourceReference(TextBlock.ForegroundProperty, names.Count == 0 ? "Grip.TextTertiary" : "Grip.AccentText");
        b.Row(L.S(titleKey), null, value, icon);
    }

    private static Button RestartAsAdminButton()
    {
        var button = new Button { Style = (Style)Application.Current.FindResource("Grip.Button"), Content = L.S("access.admin.restart") };
        Ui.SetIcon(button, "Shield");
        button.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(ProcessInfo.CurrentExePath) { UseShellExecute = true, Verb = "runas", Arguments = "--elevated-restart" });
                App.Quit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The user declined the UAC prompt; nothing to do.
            }
        };
        return button;
    }

    public static void OpenFolder(string path)
    {
        System.IO.Directory.CreateDirectory(path);
        AppCatalogService.Open(path);
    }
}
