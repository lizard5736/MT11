using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Grip.Core.Actions;
using Grip.Interop;
using Grip.UI;
using Grip.UI.Controls;
using Forms = System.Windows.Forms;

namespace Grip.Services;

/// <summary>
/// The tray icon: left click toggles the panel, right click opens a short
/// menu. The icon is drawn at runtime from the Grip mark so it is always
/// crisp at the current scale and follows the taskbar's light or dark theme.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private System.Drawing.Icon? _currentIcon;
    private Window? _menuAnchor;
    private bool _dotVisible;

    public event EventHandler? LeftClick;
    public event EventHandler? ContextMenuRequested;

    public TrayService()
    {
        _icon = new Forms.NotifyIcon { Text = "Grip", Visible = false };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) LeftClick?.Invoke(this, EventArgs.Empty);
            else if (e.Button == Forms.MouseButtons.Right) ContextMenuRequested?.Invoke(this, EventArgs.Empty);
        };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnPreferenceChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
    }

    public void Show()
    {
        Render();
        _icon.Visible = true;
    }

    public void SetStatus(bool dotVisible, string tooltip)
    {
        _icon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
        if (_dotVisible == dotVisible && _currentIcon != null) return;
        _dotVisible = dotVisible;
        Render();
    }

    private void OnPreferenceChanged(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e) =>
        Application.Current?.Dispatcher.BeginInvoke(Render);

    private void OnDisplayChanged(object? sender, EventArgs e) =>
        Application.Current?.Dispatcher.BeginInvoke(Render);

    private void Render()
    {
        int size = Math.Max(16, NativeMethods.GetSystemMetrics(49 /* SM_CXSMICON */));
        bool lightTaskbar = ThemeService.TaskbarIsLight();
        var mark = new SolidColorBrush(lightTaskbar ? Color.FromRgb(0x1A, 0x1A, 0x1A) : Colors.White);
        var dot = new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20));
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            GripMark.Draw(dc, new Rect(0, 0, size, size), mark, _dotVisible ? dot : null, tile: false);

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var icon = ToIcon(bitmap);
        _icon.Icon = icon;
        if (_currentIcon != null)
        {
            NativeMethods.DestroyIcon(_currentIcon.Handle);
            _currentIcon.Dispose();
        }
        _currentIcon = icon;
    }

    private static System.Drawing.Icon ToIcon(BitmapSource source)
    {
        var straight = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int w = straight.PixelWidth, h = straight.PixelHeight, stride = w * 4;
        var pixels = new byte[stride * h];
        straight.CopyPixels(pixels, stride, 0);
        using var bmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        bmp.UnlockBits(data);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    /// <summary>
    /// Opens a WPF context menu at the pointer. The menu needs a foreground
    /// window of ours to close properly on an outside click, so a tiny
    /// invisible anchor window takes focus first.
    /// </summary>
    public void ShowMenu(ContextMenu menu)
    {
        _menuAnchor ??= CreateAnchor();
        var cursor = Screens.CursorPosition();
        _menuAnchor.Show();
        WindowPlacement.MoveTo(_menuAnchor, cursor.X, cursor.Y);
        WindowStyling.ForceForeground(_menuAnchor);
        menu.PlacementTarget = _menuAnchor;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.Closed += (_, _) => _menuAnchor?.Hide();
        menu.IsOpen = true;
    }

    private static Window CreateAnchor()
    {
        var anchor = new Window
        {
            Width = 1,
            Height = 1,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            ShowActivated = true,
        };
        anchor.SourceInitialized += (_, _) => WindowStyling.MakeToolWindow(anchor);
        return anchor;
    }

    public void Dispose()
    {
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnPreferenceChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        _icon.Visible = false;
        _icon.Dispose();
        if (_currentIcon != null)
        {
            NativeMethods.DestroyIcon(_currentIcon.Handle);
            _currentIcon.Dispose();
        }
        _menuAnchor?.Close();
    }

    /// <summary>Builds the right-click menu.</summary>
    public static ContextMenu BuildMenu(Action<string> run, bool keepAwakeActive, bool keepAwakeInstalled)
    {
        var menu = new ContextMenu();
        MenuItem Item(string text, string icon, string action)
        {
            var item = new MenuItem { Header = text };
            Ui.SetIcon(item, icon);
            item.Click += (_, _) => run(action);
            return item;
        }
        menu.Items.Add(Item(L.S("tray.menu.panel"), "Navigation", ActionIds.OpenPanel));
        if (App.Services.Settings.Current.IsInstalled(Core.Features.FeatureIds.CommandBar))
            menu.Items.Add(Item(L.S("action.commandBar"), "WindowConsole", ActionIds.CommandBar));
        if (App.Services.Settings.Current.IsInstalled(Core.Features.FeatureIds.ClipboardHistory))
            menu.Items.Add(Item(L.S("action.clipboard"), "Clipboard", ActionIds.ClipboardHistory));
        if (keepAwakeInstalled)
        {
            var awake = Item(L.S("keepAwake.title"), "WeatherMoon", ActionIds.KeepAwakeToggle);
            awake.IsCheckable = false;
            awake.IsChecked = keepAwakeActive;
            menu.Items.Add(awake);
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(L.S("tray.menu.settings"), "Settings", ActionIds.OpenSettings));
        var quit = new MenuItem { Header = L.S("tray.menu.quit") };
        Ui.SetIcon(quit, "Power");
        quit.Click += (_, _) => App.Quit();
        menu.Items.Add(quit);
        return menu;
    }
}
