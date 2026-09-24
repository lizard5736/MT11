using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Grip.UI;
using Grip.UI.Common;
using Microsoft.Win32;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

public enum QuickToggleKind { Switch, Action }

public sealed record QuickToggleInfo(string Id, string TitleKey, string Icon, QuickToggleKind Kind);

/// <summary>
/// One-click system switches. Each one uses the setting Windows itself uses
/// (registry value plus the shell's own refresh), so Explorer stays in sync.
/// </summary>
public sealed class QuickToggleService
{
    private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const int DefViewToggleDesktopIcons = 0x7402;
    private const int DefViewRefresh = 0x7103;

    public static IReadOnlyList<QuickToggleInfo> All { get; } = new List<QuickToggleInfo>
    {
        new("darkMode", "toggle.darkMode", "DarkTheme", QuickToggleKind.Switch),
        new("desktopIcons", "toggle.desktopIcons", "Desktop", QuickToggleKind.Switch),
        new("hiddenFiles", "toggle.hiddenFiles", "Eye", QuickToggleKind.Switch),
        new("fileExtensions", "toggle.fileExtensions", "DocumentText", QuickToggleKind.Switch),
        new("lock", "toggle.lock", "LockClosed", QuickToggleKind.Action),
        new("displayOff", "toggle.displayOff", "DesktopOff", QuickToggleKind.Action),
        new("emptyBin", "toggle.emptyBin", "BinRecycle", QuickToggleKind.Action),
        new("eject", "toggle.eject", "ArrowEject", QuickToggleKind.Action),
        new("sleep", "toggle.sleep", "Sleep", QuickToggleKind.Action),
    };

    public event EventHandler? Changed;

    /// <summary>The toggles the panel shows, in the user's order.</summary>
    public IReadOnlyList<QuickToggleInfo> Visible(IReadOnlyList<string> saved)
    {
        if (saved.Count == 0) return All;
        var byId = All.ToDictionary(t => t.Id);
        return saved.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }

    public bool IsOn(string id) => id switch
    {
        "darkMode" => ReadDword(PersonalizeKey, "AppsUseLightTheme", 1) == 0,
        "desktopIcons" => DesktopIconsVisible(),
        "hiddenFiles" => ReadDword(AdvancedKey, "Hidden", 2) == 1,
        "fileExtensions" => ReadDword(AdvancedKey, "HideFileExt", 1) == 0,
        _ => false,
    };

    public void Run(string id)
    {
        try
        {
            switch (id)
            {
                case "darkMode": SetDarkMode(!IsOn(id)); break;
                case "desktopIcons": ToggleDesktopIcons(); break;
                case "hiddenFiles": SetExplorerFlag("Hidden", IsOn(id) ? 2 : 1, id); break;
                case "fileExtensions": SetExplorerFlag("HideFileExt", IsOn(id) ? 1 : 0, id); break;
                case "lock": LockWorkStation(); break;
                case "displayOff": TurnDisplayOff(); break;
                case "emptyBin": EmptyRecycleBin(); break;
                case "eject": EjectDrives(); break;
                case "sleep": SetSuspendState(false, false, false); break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or COMException)
        {
            Log.Error($"Quick toggle {id} failed", ex);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // ---------- dark mode ----------
    public void SetDarkMode(bool dark)
    {
        using var key = Registry.CurrentUser.CreateSubKey(PersonalizeKey);
        key.SetValue("AppsUseLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
        key.SetValue("SystemUsesLightTheme", dark ? 0 : 1, RegistryValueKind.DWord);
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "ImmersiveColorSet", 0x0002, 200, out _);
        Hud(dark ? "toggle.darkMode.hud.on" : "toggle.darkMode.hud.off", "DarkTheme");
    }

    // ---------- desktop icons ----------
    private static IntPtr FindDesktopDefView()
    {
        var progman = FindWindow("Progman", null);
        var defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView != IntPtr.Zero) return defView;
        // With a wallpaper slideshow the view lives under a WorkerW window.
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            var child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (child != IntPtr.Zero && ClassName(hwnd) == "WorkerW")
            {
                found = child;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static bool DesktopIconsVisible()
    {
        var defView = FindDesktopDefView();
        if (defView == IntPtr.Zero) return ReadDword(AdvancedKey, "HideIcons", 0) == 0;
        var list = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
        return list == IntPtr.Zero ? ReadDword(AdvancedKey, "HideIcons", 0) == 0 : IsWindowVisible(list);
    }

    private void ToggleDesktopIcons()
    {
        bool wasVisible = DesktopIconsVisible();
        var defView = FindDesktopDefView();
        if (defView != IntPtr.Zero)
            SendMessage(defView, WM_COMMAND, new IntPtr(DefViewToggleDesktopIcons), IntPtr.Zero);
        else
        {
            using var key = Registry.CurrentUser.CreateSubKey(AdvancedKey);
            key.SetValue("HideIcons", wasVisible ? 1 : 0, RegistryValueKind.DWord);
            RefreshExplorer();
        }
        Hud(wasVisible ? "toggle.desktopIcons.hud.off" : "toggle.desktopIcons.hud.on", "Desktop");
    }

    // ---------- Explorer flags ----------
    private void SetExplorerFlag(string name, int value, string id)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(AdvancedKey))
            key.SetValue(name, value, RegistryValueKind.DWord);
        RefreshExplorer();
        bool on = IsOn(id);
        Hud($"toggle.{id}.hud.{(on ? "on" : "off")}", id == "hiddenFiles" ? "Eye" : "DocumentText");
    }

    /// <summary>Asks the desktop and every open Explorer window to re-read their view settings.</summary>
    public static void RefreshExplorer()
    {
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        EnumWindows((hwnd, _) =>
        {
            var cls = ClassName(hwnd);
            if (cls is "CabinetWClass" or "Progman" or "WorkerW" or "ExploreWClass")
            {
                EnumChildWindows(hwnd, (child, _) =>
                {
                    if (ClassName(child) == "SHELLDLL_DefView")
                        PostMessage(child, WM_COMMAND, new IntPtr(DefViewRefresh), IntPtr.Zero);
                    return true;
                }, IntPtr.Zero);
            }
            return true;
        }, IntPtr.Zero);
    }

    // ---------- actions ----------
    private static void TurnDisplayOff()
    {
        // A short pause so releasing the mouse button doesn't wake the screen straight away.
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, new IntPtr(SC_MONITORPOWER), new IntPtr(2));
        };
        timer.Start();
    }

    private void EmptyRecycleBin()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        if (SHQueryRecycleBin(null, ref info) == 0 && info.i64NumItems == 0)
        {
            App.Services.Hud.Show(L.S("toggle.emptyBin.alreadyEmpty"), "BinRecycle", HudTone.Neutral);
            return;
        }
        if (!ConfirmDialog.Ask(L.S("toggle.emptyBin.confirm"), okText: L.S("toggle.emptyBin"), destructive: true)) return;
        SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
        App.Services.Hud.Show(L.S("toggle.emptyBin.done"), "BinRecycle", HudTone.Success);
    }

    private static void EjectDrives()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType is DriveType.Removable or DriveType.CDRom && d.IsReady)
            .Select(d => d.Name)
            .ToList();
        if (drives.Count == 0)
        {
            App.Services.Hud.Show(L.S("toggle.eject.none"), "ArrowEject", HudTone.Neutral);
            return;
        }

        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType == null) return;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic computer = shell.NameSpace(17); // ssfDRIVES, "This PC"
            foreach (var drive in drives)
            {
                dynamic? item = computer.ParseName(drive);
                item?.InvokeVerb("Eject");
            }
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }

        // Ejection is asynchronous: look again a moment later and report honestly.
        var check = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        check.Tick += (_, _) =>
        {
            check.Stop();
            var still = DriveInfo.GetDrives().Where(d => drives.Contains(d.Name) && d.IsReady).Select(d => d.Name.TrimEnd('\\')).ToList();
            var ejected = drives.Select(d => d.TrimEnd('\\')).Except(still).ToList();
            if (still.Count > 0)
                App.Services.Hud.Show(L.F("toggle.eject.failed", string.Join(", ", still)), "ArrowEject", HudTone.Rec);
            else
                App.Services.Hud.Show(L.F("toggle.eject.done", string.Join(", ", ejected)), "ArrowEject", HudTone.Success);
        };
        check.Start();
    }

    // ---------- helpers ----------
    private static void Hud(string key, string icon) => App.Services.Hud.Show(L.S(key), icon, HudTone.Accent);

    private static int ReadDword(string path, string name, int fallback)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue(name) is int value ? value : fallback;
        }
        catch (System.Security.SecurityException)
        {
            return fallback;
        }
    }

    private static string ClassName(IntPtr hwnd)
    {
        var sb = new StringBuilder(64);
        GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
