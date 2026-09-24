using System.Windows;
using System.Windows.Controls;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Input;
using Grip.UI.Controls;

namespace Grip.UI.Panel;

/// <summary>Tiles for the on-demand tools, each showing its shortcut.</summary>
public sealed class UtilitiesSection : PanelSection
{
    private readonly StackPanel _stack = new();

    private static readonly (string Feature, string Action, string Icon, string TitleKey, string DescKey)[] Tools =
    {
        (FeatureIds.CommandBar, ActionIds.CommandBar, "WindowConsole", "action.commandBar", "utilities.commandBar.desc"),
        (FeatureIds.ClipboardHistory, ActionIds.ClipboardHistory, "Clipboard", "action.clipboard", "utilities.clipboard.desc"),
        (FeatureIds.RadialMenu, ActionIds.RadialMenu, "DataPie", "action.radial", "utilities.radial.desc"),
        (FeatureIds.Shelf, ActionIds.Shelf, "TrayItemAdd", "action.shelf", "utilities.shelf.desc"),
        (FeatureIds.UrlCleaner, ActionIds.CleanUrl, "Link", "action.cleanUrl", "utilities.cleanUrl.desc"),
    };

    public UtilitiesSection()
    {
        Content = _stack;
    }

    public override void Refresh()
    {
        _stack.Children.Clear();
        var settings = S.Settings.Current;
        foreach (var tool in Tools)
        {
            if (!settings.IsInstalled(tool.Feature)) continue;
            var hotkey = Hotkey.Parse(settings.Hotkeys.GetValueOrDefault(tool.Action));
            _stack.Children.Add(Tile(tool.Icon, L.S(tool.TitleKey), L.S(tool.DescKey), hotkey, () =>
            {
                S.Panel.Hide();
                S.Execute(tool.Action);
            }));
        }
    }

    /// <summary>A utility tile: icon badge, title, description, shortcut caps and a chevron.</summary>
    public static Button Tile(string icon, string title, string description, Hotkey hotkey, Action onClick)
    {
        var button = new Button { Style = (Style)Application.Current.FindResource("Grip.Button.Row"), Margin = new Thickness(0, 0, 0, 8) };
        button.Click += (_, _) => onClick();

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var badge = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(9), VerticalAlignment = VerticalAlignment.Top };
        badge.SetResourceReference(Border.BackgroundProperty, "Grip.AccentSubtle");
        var glyph = new Icon { Kind = icon, Size = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
        badge.Child = glyph;
        grid.Children.Add(badge);

        var text = new StackPanel { Margin = new Thickness(12, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, Style = (Style)Application.Current.FindResource("Grip.Text.Strong") });
        text.Children.Add(new TextBlock
        {
            Text = description,
            Style = (Style)Application.Current.FindResource("Grip.Text.Secondary"),
            Margin = new Thickness(0, 2, 0, 0),
        });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (!hotkey.IsEmpty) right.Children.Add(KeyCaps.Create(hotkey, small: true));
        var chevron = new Icon { Kind = "ChevronRight", Size = 14, Margin = new Thickness(6, 0, 0, 0) };
        chevron.SetResourceReference(Icon.ForegroundProperty, "Grip.TextTertiary");
        right.Children.Add(chevron);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);

        button.Content = grid;
        return button;
    }
}
