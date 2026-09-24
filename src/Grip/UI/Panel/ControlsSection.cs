using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Grip.Core.Features;
using Grip.Core.Localization;
using Grip.Core.Settings;
using Grip.UI.Controls;

namespace Grip.UI.Panel;

/// <summary>
/// Quick on/off switches for the background behaviors, grouped like the hub,
/// with a live "on/total" count per group.
/// </summary>
public sealed class ControlsSection : PanelSection
{
    private readonly StackPanel _stack = new();

    private sealed record Row(string Feature, string Icon, Func<string> Title, Func<string> Description,
        Func<AppSettings, bool> Get, Action<AppSettings, bool> Set);

    private sealed record Group(string Id, string TitleKey, Row[] Rows);

    private static Group[] Groups => new[]
    {
        new Group("clipboard", "controls.group.clipboard", new[]
        {
            new Row(FeatureIds.ClipboardHistory, "Clipboard", () => L.S("feature.clipboardHistory.title"), () => L.S("controls.clipboardHistory.desc"),
                s => s.Clipboard.HistoryEnabled, (s, v) => s.Clipboard.HistoryEnabled = v),
            new Row(FeatureIds.ClipboardHistory, "Broom", () => L.S("controls.autoClear.title"),
                () => L.F("controls.autoClear.desc", Localizer.Instance.Duration(TimeSpan.FromSeconds(App.Services.Settings.Current.Clipboard.AutoClearSeconds))),
                s => s.Clipboard.AutoClearEnabled, (s, v) => s.Clipboard.AutoClearEnabled = v),
            new Row(FeatureIds.UrlCleaner, "Link", () => L.S("controls.autoClean.title"), () => L.S("controls.autoClean.desc"),
                s => s.UrlCleaner.AutoClean, (s, v) => s.UrlCleaner.AutoClean = v),
        }),
        new Group("tools", "controls.group.tools", new[]
        {
            new Row(FeatureIds.Shelf, "TrayItemAdd", () => L.S("controls.shake.title"), () => L.S("controls.shake.desc"),
                s => s.Shelf.ShakeToOpen, (s, v) => s.Shelf.ShakeToOpen = v),
            new Row(FeatureIds.RadialMenu, "DataPie", () => L.S("controls.radialMouse.title"),
                () => L.F("controls.radialMouse.desc", L.S("settings.radial.trigger." +
                    (App.Services.Settings.Current.RadialMenu.MouseTrigger == RadialMouseTrigger.None ? "Middle" : App.Services.Settings.Current.RadialMenu.MouseTrigger))),
                s => s.RadialMenu.MouseTrigger != RadialMouseTrigger.None,
                (s, v) => s.RadialMenu.MouseTrigger = v ? RadialMouseTrigger.Middle : RadialMouseTrigger.None),
        }),
    };

    public ControlsSection() => Content = _stack;

    public override void Refresh()
    {
        _stack.Children.Clear();
        var settings = S.Settings.Current;
        foreach (var group in Groups)
        {
            var rows = group.Rows.Where(r => settings.IsInstalled(r.Feature)).ToList();
            if (rows.Count == 0) continue;
            bool collapsed = settings.Panel.CollapsedSections.Contains("controls." + group.Id);

            var header = new DockPanel { Margin = new Thickness(2, _stack.Children.Count == 0 ? 0 : 6, 2, 8), Cursor = Cursors.Hand, Background = System.Windows.Media.Brushes.Transparent };
            var count = new TextBlock
            {
                Text = L.F("controls.count", rows.Count(r => r.Get(settings)), rows.Count),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
            };
            count.SetResourceReference(TextBlock.ForegroundProperty, "Grip.AccentText");
            DockPanel.SetDock(count, Dock.Right);
            header.Children.Add(count);
            var chevron = new Icon { Kind = collapsed ? "ChevronRight" : "ChevronDown", Size = 12, Margin = new Thickness(0, 0, 6, 0) };
            chevron.SetResourceReference(Icon.ForegroundProperty, "Grip.TextTertiary");
            header.Children.Add(chevron);
            header.Children.Add(new TextBlock
            {
                Text = L.S(group.TitleKey).ToUpper(Localizer.Instance.Culture),
                Style = (Style)FindResource("Grip.Text.Section"),
            });
            var groupId = "controls." + group.Id;
            header.MouseLeftButtonUp += (_, _) =>
            {
                S.Settings.Update(s =>
                {
                    if (!s.Panel.CollapsedSections.Remove(groupId)) s.Panel.CollapsedSections.Add(groupId);
                });
                Refresh();
            };
            _stack.Children.Add(header);
            if (collapsed) continue;

            foreach (var row in rows) _stack.Children.Add(BuildRow(row, settings));
        }
    }

    private Border BuildRow(Row row, AppSettings settings)
    {
        var card = new Border { Style = (Style)FindResource("Grip.Card"), Margin = new Thickness(0, 0, 0, 8) };
        card.SetResourceReference(Border.PaddingProperty, "Grip.Space.Card");
        var dock = new DockPanel();
        var toggle = new ToggleButton { Style = (Style)FindResource("Grip.Toggle"), IsChecked = row.Get(settings) };
        toggle.Click += (_, _) =>
        {
            S.Settings.Update(s => row.Set(s, toggle.IsChecked == true));
            Refresh();
        };
        DockPanel.SetDock(toggle, Dock.Right);
        dock.Children.Add(toggle);

        var glyph = new Icon { Kind = row.Icon, Size = 20, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 12, 0) };
        glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
        dock.Children.Add(glyph);

        var text = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        text.Children.Add(new TextBlock { Text = row.Title(), Style = (Style)FindResource("Grip.Text.Strong") });
        text.Children.Add(new TextBlock { Text = row.Description(), Style = (Style)FindResource("Grip.Text.Secondary"), Margin = new Thickness(0, 2, 0, 0) });
        dock.Children.Add(text);
        card.Child = dock;
        return card;
    }
}
