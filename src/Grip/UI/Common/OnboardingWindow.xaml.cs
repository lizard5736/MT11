using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Input;
using Grip.Interop;
using Grip.Services;
using Grip.UI.Controls;

namespace Grip.UI.Common;

/// <summary>First run: pick a starting set, learn the main shortcuts, find the tray icon.</summary>
public partial class OnboardingWindow : Window
{
    private string _preset = "essentials";

    public OnboardingWindow()
    {
        InitializeComponent();
        BuildPresets();
        BuildShortcuts();
        SourceInitialized += (_, _) =>
        {
            var theme = App.Services.Theme;
            WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
        };
    }

    private void BuildPresets()
    {
        foreach (var preset in FeaturePreset.All)
        {
            var card = new ToggleButton
            {
                Style = (Style)FindResource("Grip.Button.Row"),
                IsChecked = preset.Id == _preset,
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(14, 12, 14, 12),
                Tag = preset.Id,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            var stack = new StackPanel();
            var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            var glyph = new Icon { Kind = preset.Icon, Size = 18, Margin = new Thickness(0, 0, 8, 0) };
            glyph.SetResourceReference(Controls.Icon.ForegroundProperty, "Grip.Accent");
            head.Children.Add(glyph);
            head.Children.Add(new TextBlock { Text = L.S(preset.TitleKey), Style = (Style)FindResource("Grip.Text.Strong") });
            stack.Children.Add(head);
            stack.Children.Add(new TextBlock { Text = L.S(preset.DescriptionKey), Style = (Style)FindResource("Grip.Text.Secondary") });
            card.Content = stack;
            card.Click += (_, _) =>
            {
                _preset = preset.Id;
                foreach (ToggleButton other in Presets.Children) Mark(other, (string)other.Tag == _preset);
            };
            Mark(card, card.IsChecked == true);
            Presets.Children.Add(card);
        }
    }

    private static void Mark(ToggleButton card, bool selected)
    {
        card.IsChecked = selected;
        card.SetResourceReference(Control.BorderBrushProperty, selected ? "Grip.Accent" : "Grip.Line");
        card.SetResourceReference(Control.BackgroundProperty, selected ? "Grip.AccentSubtle" : "Grip.Surface");
    }

    private void BuildShortcuts()
    {
        var hotkeys = App.Services.Settings.Current.Hotkeys;
        foreach (var id in new[] { ActionIds.CommandBar, ActionIds.ClipboardHistory, ActionIds.PastePlain, ActionIds.RadialMenu, ActionIds.Shelf })
        {
            var hotkey = Hotkey.Parse(hotkeys.GetValueOrDefault(id));
            if (hotkey.IsEmpty) continue;
            var row = new DockPanel { Margin = new Thickness(0, 7, 0, 7) };
            var caps = KeyCaps.Create(hotkey);
            DockPanel.SetDock(caps, Dock.Right);
            row.Children.Add(caps);
            var info = ActionCatalog.Get(id);
            var glyph = new Icon { Kind = info.Icon, Size = 18, Margin = new Thickness(0, 0, 12, 0) };
            glyph.SetResourceReference(Controls.Icon.ForegroundProperty, "Grip.Accent");
            row.Children.Add(glyph);
            row.Children.Add(new TextBlock { Text = L.S(ActionCatalog.TitleKey(id)), Style = (Style)FindResource("Grip.Text.Body"), VerticalAlignment = VerticalAlignment.Center });
            Shortcuts.Children.Add(row);
        }
    }

    private void OnTaskbarSettings(object sender, RoutedEventArgs e) => AppCatalogService.Open("ms-settings:taskbar");

    private void OnStart(object sender, RoutedEventArgs e)
    {
        var preset = FeaturePreset.All.First(p => p.Id == _preset);
        App.Services.Settings.Update(s =>
        {
            foreach (var f in FeatureCatalog.Ready) s.SetInstalled(f.Id, preset.Features.Contains(f.Id));
            s.General.OnboardingCompleted = true;
        });
        App.Services.Settings.SaveNow();
        Close();
        App.Services.Panel.Show();
    }
}
