using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Grip.Core.Settings;
using Grip.Interop;

namespace Grip.UI.Panel;

/// <summary>
/// The panel that opens from the tray icon: live status in the header, one
/// tab per installed section (or all of them in a list), and Settings /
/// layout / Quit at the bottom. It hides as soon as it loses focus.
/// </summary>
public partial class FlyoutWindow : Window
{
    private readonly Dictionary<string, PanelSection> _sections = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? _selected;
    private bool _placing;

    /// <summary>When the panel last hid, so a click on the tray icon that caused the hide doesn't reopen it.</summary>
    public DateTime LastHidden { get; private set; }

    /// <summary>Preview mode renders without positioning or focus handling.</summary>
    public bool IsPreview { get; init; }

    public FlyoutWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (IsPreview) return;
            WindowStyling.MakeToolWindow(this);
            ApplyChrome();
        };
        Deactivated += (_, _) =>
        {
            if (!IsPreview && IsVisible) HidePanel();
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                HidePanel();
                e.Handled = true;
            }
        };
        SizeChanged += (_, _) =>
        {
            if (!_placing && IsVisible && !IsPreview) Place();
        };
        _statusTimer.Tick += (_, _) => UpdateStatus();
        App.Services.Theme.ThemeChanged += OnThemeChanged;
        Closed += (_, _) =>
        {
            _statusTimer.Stop();
            App.Services.Theme.ThemeChanged -= OnThemeChanged;
        };
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyChrome();

    private void ApplyChrome()
    {
        var theme = App.Services.Theme;
        WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
    }

    // ---------- show / hide ----------

    public void ShowPanel()
    {
        Rebuild();
        UpdateStatus();
        if (!IsVisible)
        {
            Root.Opacity = 0;
            RootShift.Y = 10;
            Show();
        }
        Place();
        WindowStyling.ForceForeground(this);
        Activate();
        Animate();
        _statusTimer.Start();
        foreach (var section in VisibleSections()) section.Refresh();
    }

    public void HidePanel()
    {
        if (!IsVisible) return;
        _statusTimer.Stop();
        foreach (var section in _sections.Values) section.Suspend();
        LastHidden = DateTime.UtcNow;
        Hide();
    }

    private void Animate()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        Root.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(170)) { EasingFunction = ease });
        RootShift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
    }

    /// <summary>
    /// Sits next to the taskbar on the monitor under the pointer, like the
    /// Windows quick settings: bottom-right above a bottom taskbar, and so on.
    /// </summary>
    private void Place()
    {
        _placing = true;
        try
        {
            var cursor = Screens.CursorPosition();
            var area = Screens.FromPoint(cursor);
            WindowPlacement.MoveTo(this, area.Work.Left + 1, area.Work.Top + 1);
            Scroller.MaxHeight = Math.Max(240, area.WorkDip.Height - 190);
            UpdateLayout();
            var (w, h) = WindowPlacement.PhysicalSize(this);
            int margin = (int)(12 * area.Scale);
            int edge = Screens.TaskbarEdge(area);
            int x, y;
            switch (edge)
            {
                case 1: // top
                    x = Math.Min(cursor.X - w / 2, area.Work.Right - w - margin);
                    y = area.Work.Top + margin;
                    break;
                case 0: // left
                    x = area.Work.Left + margin;
                    y = Math.Min(cursor.Y - h / 2, area.Work.Bottom - h - margin);
                    break;
                case 2: // right
                    x = area.Work.Right - w - margin;
                    y = Math.Min(cursor.Y - h / 2, area.Work.Bottom - h - margin);
                    break;
                default: // bottom
                    x = Math.Min(cursor.X - w / 2, area.Work.Right - w - margin);
                    y = area.Work.Bottom - h - margin;
                    break;
            }
            x = Math.Max(area.Work.Left + margin, x);
            y = Math.Max(area.Work.Top + margin, y);
            WindowPlacement.MoveTo(this, x, y);
        }
        finally
        {
            _placing = false;
        }
    }

    // ---------- building ----------

    private IEnumerable<PanelSection> VisibleSections() =>
        Body.Content switch
        {
            SectionHost host => new[] { host.Section },
            StackPanel list => list.Children.OfType<SectionHost>().Select(h => h.Section),
            _ => Array.Empty<PanelSection>(),
        };

    /// <summary>Rebuilds tabs and content from the current settings.</summary>
    public void Rebuild()
    {
        var settings = App.Services.Settings.Current;
        var defs = PanelSectionDef.Visible(settings);
        bool list = settings.Panel.Layout == PanelLayoutMode.List;

        Resources["Grip.Space.Card"] = settings.Panel.Compact ? new Thickness(10) : new Thickness(14);
        Resources["Grip.Space.Section"] = settings.Panel.Compact ? new Thickness(0, 0, 0, 10) : new Thickness(0, 0, 0, 16);

        LayoutButton.Content = L.S(list ? "panel.layout.tabs" : "panel.layout.list");
        Ui.SetIcon(LayoutButton, list ? "Tabs" : "List");

        // Keep section instances alive across rebuilds so their state (search text, scroll) survives.
        foreach (var id in _sections.Keys.Where(id => defs.All(d => d.Id != id)).ToList()) _sections.Remove(id);
        PanelSection SectionFor(PanelSectionDef def)
        {
            if (!_sections.TryGetValue(def.Id, out var section))
            {
                section = def.Create();
                _sections[def.Id] = section;
            }
            if (section.Parent is Border b) b.Child = null;
            return section;
        }

        Tabs.Children.Clear();
        if (defs.Count == 0)
        {
            TabsHost.Visibility = Visibility.Collapsed;
            Body.Content = new TextBlock
            {
                Text = L.S("panel.empty"),
                Style = (Style)FindResource("Grip.Text.Secondary"),
                Margin = new Thickness(4, 8, 4, 16),
            };
            return;
        }

        if (list)
        {
            TabsHost.Visibility = Visibility.Collapsed;
            var stack = new StackPanel();
            foreach (var def in defs)
            {
                bool collapsed = settings.Panel.CollapsedSections.Contains(def.Id);
                stack.Children.Add(new SectionHost(def, SectionFor(def), listMode: true, collapsed, nowCollapsed =>
                    App.Services.Settings.Update(s =>
                    {
                        s.Panel.CollapsedSections.Remove(def.Id);
                        if (nowCollapsed) s.Panel.CollapsedSections.Add(def.Id);
                    })));
            }
            Body.Content = stack;
            return;
        }

        TabsHost.Visibility = defs.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        _selected = defs.Any(d => d.Id == settings.Panel.LastSection) ? settings.Panel.LastSection : defs[0].Id;
        foreach (var def in defs)
        {
            var tab = new ToggleButton
            {
                Style = (Style)FindResource("Grip.Tab"),
                IsChecked = def.Id == _selected,
                Tag = def.Id,
                ToolTip = L.S(def.TitleKey),
            };
            Ui.SetIcon(tab, def.Icon);
            tab.Click += (_, _) => Select(def.Id);
            Tabs.Children.Add(tab);
        }
        ShowSection(defs.First(d => d.Id == _selected), SectionFor);
        UpdateStatus();
    }

    private void Select(string id)
    {
        foreach (ToggleButton tab in Tabs.Children) tab.IsChecked = (string)tab.Tag == id;
        if (_selected == id) return;
        _selected = id;
        App.Services.Settings.Update(s => s.Panel.LastSection = id);
        var def = PanelSectionDef.All.First(d => d.Id == id);
        ShowSection(def, d =>
        {
            if (!_sections.TryGetValue(d.Id, out var section))
            {
                section = d.Create();
                _sections[d.Id] = section;
            }
            if (section.Parent is Border b) b.Child = null;
            return section;
        });
        Scroller.ScrollToTop();
    }

    private void ShowSection(PanelSectionDef def, Func<PanelSectionDef, PanelSection> sectionFor)
    {
        var section = sectionFor(def);
        Body.Content = new SectionHost(def, section, listMode: false, collapsed: false, onCollapse: null);
        section.Refresh();
    }

    // ---------- status ----------

    private void UpdateStatus()
    {
        var awake = App.Services.KeepAwake;
        if (awake.IsActive && awake.Session is { } session)
        {
            StatusDot.IsActive = true;
            StatusText.Text = session.IsIndefinite
                ? L.S("panel.status.awake")
                : L.F("panel.status.awakeUntil", session.Until!.Value.ToLocalTime().ToString("HH:mm"));
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Grip.AccentText");
            StatusPill.SetResourceReference(Border.BackgroundProperty, "Grip.AccentSubtle");
        }
        else
        {
            StatusDot.IsActive = false;
            StatusText.Text = L.S("panel.status.ready");
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextSecondary");
            StatusPill.SetResourceReference(Border.BackgroundProperty, "Grip.Control");
        }
    }

    // ---------- footer ----------

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        HidePanel();
        App.Services.OpenSettings(null);
    }

    private void OnToggleLayout(object sender, RoutedEventArgs e)
    {
        App.Services.Settings.Update(s =>
            s.Panel.Layout = s.Panel.Layout == PanelLayoutMode.List ? PanelLayoutMode.Tabs : PanelLayoutMode.List);
        Rebuild();
        foreach (var section in VisibleSections()) section.Refresh();
    }

    private void OnQuit(object sender, RoutedEventArgs e) => App.Quit();
}
