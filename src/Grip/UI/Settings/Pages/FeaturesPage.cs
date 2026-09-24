using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Core.Features;
using Grip.UI.Common;
using Grip.UI.Controls;

namespace Grip.UI.Settings.Pages;

/// <summary>
/// The Features hub: install only what you use. Uninstalled features stop
/// loading and vanish from the panel, settings and shortcuts; their settings
/// stay, so reinstalling brings them back exactly as they were.
/// </summary>
public static class FeaturesPage
{
    private static Style S(string key) => (Style)Application.Current.FindResource(key);

    public static void Build(PageBuilder b)
    {
        var settings = App.Services.Settings.Current;
        var ready = FeatureCatalog.Ready.ToList();
        int installed = ready.Count(f => settings.IsInstalled(f.Id));

        // Summary card
        var summary = new DockPanel();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var installAll = new Button { Style = S("Grip.Button"), Content = L.S("hub.installAll"), IsEnabled = installed < ready.Count };
        installAll.Click += (_, _) => SetMany(ready.Select(f => f.Id), true, installAll);
        var uninstallAll = new Button { Style = S("Grip.Button"), Content = L.S("hub.uninstallAll"), IsEnabled = installed > 0, Margin = new Thickness(8, 0, 0, 0) };
        uninstallAll.Click += (_, _) =>
        {
            if (ConfirmDialog.Ask(L.S("hub.uninstallAll.confirm"), okText: L.S("hub.uninstallAll"), destructive: true, owner: Window.GetWindow(uninstallAll)))
                SetMany(ready.Select(f => f.Id), false, uninstallAll);
        };
        buttons.Children.Add(installAll);
        buttons.Children.Add(uninstallAll);
        DockPanel.SetDock(buttons, Dock.Right);
        summary.Children.Add(buttons);
        summary.Children.Add(new TextBlock
        {
            Text = L.F("hub.count", installed, ready.Count),
            Style = S("Grip.Text.Secondary"),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
        });
        b.Card();
        b.Custom(summary);

        // Presets
        b.Card("hub.presets", "hub.presets.hint");
        var presets = new UniformGrid { Columns = 3, Margin = new Thickness(-5, 0, -5, 0) };
        foreach (var preset in FeaturePreset.All) presets.Children.Add(PresetCard(preset));
        b.Custom(presets);

        // Groups
        foreach (var group in Enum.GetValues<FeatureGroup>())
        {
            var features = FeatureCatalog.InGroup(group).ToList();
            if (features.Count == 0) continue;
            b.Card("group." + group);
            foreach (var feature in features) b.Custom(FeatureRow(feature), L.S(feature.TitleKey), L.S(feature.DescriptionKey));
        }
    }

    private static Border PresetCard(FeaturePreset preset)
    {
        var card = new Border
        {
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("Grip.Control"),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(5, 0, 5, 0),
        };
        var stack = new StackPanel();
        var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        var glyph = new Icon { Kind = preset.Icon, Size = 18, Margin = new Thickness(0, 0, 8, 0) };
        glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
        head.Children.Add(glyph);
        head.Children.Add(new TextBlock { Text = L.S(preset.TitleKey), Style = S("Grip.Text.Strong") });
        stack.Children.Add(head);
        stack.Children.Add(new TextBlock { Text = L.S(preset.DescriptionKey), Style = S("Grip.Text.Secondary"), MinHeight = 48 });
        var apply = new Button { Style = S("Grip.Button"), Content = L.S("common.apply"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(14, 5, 14, 5) };
        apply.Click += (_, _) =>
        {
            App.Services.Settings.Update(s =>
            {
                foreach (var f in FeatureCatalog.Ready) s.SetInstalled(f.Id, preset.Features.Contains(f.Id));
            });
            App.Services.Hud.Show(L.F("hub.applied", L.S(preset.TitleKey)), preset.Icon, HudTone.Success);
            (Window.GetWindow(apply) as SettingsWindow)?.RequestRebuild();
        };
        stack.Children.Add(apply);
        card.Child = stack;
        return card;
    }

    private static DockPanel FeatureRow(FeatureInfo feature)
    {
        var settings = App.Services.Settings.Current;
        var row = new DockPanel();

        FrameworkElement action;
        if (feature.IsReady)
        {
            bool installed = settings.IsInstalled(feature.Id);
            var button = new Button
            {
                Style = S(installed ? "Grip.Button" : "Grip.Button.Accent"),
                Content = L.S(installed ? "common.uninstall" : "common.install"),
                MinWidth = 110,
            };
            button.Click += (_, _) => SetMany(new[] { feature.Id }, !installed, button);
            action = button;
        }
        else
        {
            var soon = new Border { Style = S("Grip.Pill"), Padding = new Thickness(10, 3, 10, 3) };
            var text = new TextBlock { Text = L.S("common.soon"), FontSize = 11, FontWeight = FontWeights.SemiBold };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextTertiary");
            soon.Child = text;
            action = soon;
        }
        action.VerticalAlignment = VerticalAlignment.Center;
        action.Margin = new Thickness(16, 0, 0, 0);
        DockPanel.SetDock(action, Dock.Right);
        row.Children.Add(action);

        var tile = new Border { Width = 38, Height = 38, CornerRadius = new CornerRadius(9), Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Top };
        tile.SetResourceReference(Border.BackgroundProperty, feature.IsReady ? "Grip.AccentSubtle" : "Grip.Control");
        var glyph = new Icon { Kind = feature.Icon, Size = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        glyph.SetResourceReference(Icon.ForegroundProperty, feature.IsReady ? "Grip.Accent" : "Grip.TextTertiary");
        tile.Child = glyph;
        row.Children.Add(tile);

        var text2 = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Opacity = feature.IsReady ? 1 : 0.7 };
        var title = new WrapPanel();
        title.Children.Add(new TextBlock { Text = L.S(feature.TitleKey), Style = S("Grip.Text.Strong"), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        if (feature.IsReady)
        {
            var chip = new Border { Style = S("Grip.Pill"), Padding = new Thickness(7, 1, 7, 2), VerticalAlignment = VerticalAlignment.Center };
            var chipText = new TextBlock { Text = L.S("energy." + feature.Energy), FontSize = 10.5 };
            chipText.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextTertiary");
            chip.Child = chipText;
            chip.ToolTip = L.S("hub.energy.tip");
            title.Children.Add(chip);
        }
        text2.Children.Add(title);
        text2.Children.Add(new TextBlock { Text = L.S(feature.DescriptionKey), Style = S("Grip.Text.Secondary"), Margin = new Thickness(0, 3, 0, 0) });
        row.Children.Add(text2);
        return row;
    }

    private static void SetMany(IEnumerable<string> ids, bool installed, FrameworkElement origin)
    {
        var list = ids.ToList();
        App.Services.Settings.Update(s =>
        {
            foreach (var id in list) s.SetInstalled(id, installed);
        });
        (Window.GetWindow(origin) as SettingsWindow)?.RequestRebuild();
    }
}
