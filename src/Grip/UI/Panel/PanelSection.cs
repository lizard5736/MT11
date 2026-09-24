using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Grip.Core.Features;
using Grip.Core.Settings;
using Grip.UI.Controls;

namespace Grip.UI.Panel;

/// <summary>A section of the tray panel. Each one refreshes itself when the panel opens.</summary>
public abstract class PanelSection : UserControl
{
    /// <summary>Called whenever the panel is shown or the section becomes visible.</summary>
    public virtual void Refresh() { }

    /// <summary>Called when the panel hides; stop timers here.</summary>
    public virtual void Suspend() { }

    protected static Services.AppServices S => App.Services;
}

public sealed record PanelSectionDef(
    string Id,
    string TitleKey,
    string Icon,
    string? SettingsPage,
    Func<AppSettings, bool> IsAvailable,
    Func<PanelSection> Create)
{
    public static IReadOnlyList<PanelSectionDef> All { get; } = new List<PanelSectionDef>
    {
        new("keepAwake", "panel.section.keepAwake", "WeatherMoon", "keepAwake",
            s => s.IsInstalled(FeatureIds.KeepAwake), () => new KeepAwakeSection()),
        new("clipboard", "panel.section.clipboard", "Clipboard", "clipboard",
            s => s.IsInstalled(FeatureIds.ClipboardHistory), () => new ClipboardSection()),
        new("utilities", "panel.section.utilities", "Wrench", null,
            s => new[] { FeatureIds.CommandBar, FeatureIds.ClipboardHistory, FeatureIds.RadialMenu, FeatureIds.Shelf, FeatureIds.UrlCleaner }
                .Any(s.IsInstalled), () => new UtilitiesSection()),
        new("controls", "panel.section.controls", "ToggleRight", "features",
            s => new[] { FeatureIds.ClipboardHistory, FeatureIds.UrlCleaner, FeatureIds.Shelf, FeatureIds.RadialMenu }
                .Any(s.IsInstalled), () => new ControlsSection()),
        new("toggles", "panel.section.toggles", "Grid", "toggles",
            s => s.IsInstalled(FeatureIds.QuickToggles), () => new TogglesSection()),
        new("monitor", "panel.section.monitor", "DeveloperBoard", "features",
            s => new[] { FeatureIds.MonitorCpu, FeatureIds.MonitorMemory, FeatureIds.MonitorDisk, FeatureIds.MonitorNetwork, FeatureIds.MonitorBattery }
                .Any(s.IsInstalled), () => new MonitorSection()),
    };

    /// <summary>Available, not hidden, in the user's order.</summary>
    public static List<PanelSectionDef> Visible(AppSettings settings)
    {
        var order = settings.Panel.SectionOrder;
        return All
            .Where(d => d.IsAvailable(settings) && !settings.Panel.HiddenSections.Contains(d.Id))
            .OrderBy(d => order.IndexOf(d.Id) is var i && i >= 0 ? i : 100 + All.ToList().IndexOf(d))
            .ToList();
    }

    /// <summary>Every available section in order, hidden ones included (for the layout editor).</summary>
    public static List<PanelSectionDef> Ordered(AppSettings settings)
    {
        var order = settings.Panel.SectionOrder;
        return All
            .Where(d => d.IsAvailable(settings))
            .OrderBy(d => order.IndexOf(d.Id) is var i && i >= 0 ? i : 100 + All.ToList().IndexOf(d))
            .ToList();
    }
}

/// <summary>Wraps a section with its caption (and, in list mode, a collapse chevron).</summary>
public sealed class SectionHost : StackPanel
{
    private readonly Border _content;
    private readonly Icon _chevron;

    public PanelSectionDef Def { get; }
    public PanelSection Section { get; }

    public SectionHost(PanelSectionDef def, PanelSection section, bool listMode, bool collapsed, Action<bool>? onCollapse)
    {
        Def = def;
        Section = section;
        SetResourceReference(MarginProperty, "Grip.Space.Section");

        var header = new DockPanel { Margin = new Thickness(2, 0, 0, 8), Background = System.Windows.Media.Brushes.Transparent };
        _chevron = new Icon { Kind = collapsed ? "ChevronRight" : "ChevronDown", Size = 12, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        _chevron.SetResourceReference(Icon.ForegroundProperty, "Grip.TextTertiary");
        if (listMode) header.Children.Add(_chevron);

        if (def.SettingsPage != null)
        {
            var gear = new Button { Style = (Style)Application.Current.FindResource("Grip.Button.Icon"), Width = 24, Height = 24, ToolTip = L.S("panel.settings") };
            Ui.SetIcon(gear, "Options");
            gear.Click += (_, _) => App.Services.OpenSettings(def.SettingsPage);
            DockPanel.SetDock(gear, Dock.Right);
            header.Children.Add(gear);
        }

        var title = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Section"), VerticalAlignment = VerticalAlignment.Center };
        title.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding($"[{def.TitleKey}]")
        {
            Source = LocSource.Instance,
            Converter = UpperConverter.Instance,
        });
        header.Children.Add(title);
        Children.Add(header);

        _content = new Border { Child = section, Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible };
        Children.Add(_content);

        if (listMode && onCollapse != null)
        {
            header.Cursor = Cursors.Hand;
            header.MouseLeftButtonUp += (_, e) =>
            {
                if (e.OriginalSource is DependencyObject d && FindButton(d)) return;
                bool nowCollapsed = _content.Visibility == Visibility.Visible;
                _content.Visibility = nowCollapsed ? Visibility.Collapsed : Visibility.Visible;
                _chevron.Kind = nowCollapsed ? "ChevronRight" : "ChevronDown";
                onCollapse(nowCollapsed);
                if (!nowCollapsed) section.Refresh();
            };
        }
    }

    private static bool FindButton(DependencyObject d)
    {
        while (d != null)
        {
            if (d is Button) return true;
            d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        }
        return false;
    }
}
