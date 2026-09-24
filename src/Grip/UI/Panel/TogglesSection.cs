using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Services;

namespace Grip.UI.Panel;

/// <summary>A 3-column grid of system switches and one-shot actions.</summary>
public sealed class TogglesSection : PanelSection
{
    private readonly UniformGrid _grid = new() { Columns = 3, Margin = new Thickness(-3) };

    public TogglesSection()
    {
        Content = _grid;
        S.Toggles.Changed += (_, _) =>
        {
            if (IsVisible) Dispatcher.BeginInvoke(Refresh, System.Windows.Threading.DispatcherPriority.Background);
        };
    }

    public override void Refresh()
    {
        _grid.Children.Clear();
        foreach (var toggle in S.Toggles.Visible(S.Settings.Current.QuickToggles.Visible))
        {
            ButtonBase tile = toggle.Kind == QuickToggleKind.Switch
                ? new ToggleButton { IsChecked = S.Toggles.IsOn(toggle.Id) }
                : new Button();
            tile.Style = (Style)FindResource("Grip.ToggleTile");
            // Plain string content: the tile template styles the TextBlock it generates (wrap, centre, 12 px).
            tile.Content = L.S(toggle.TitleKey);
            Ui.SetIcon(tile, toggle.Icon);
            tile.Click += (_, _) =>
            {
                if (toggle.Id is "lock" or "displayOff" or "sleep") S.Panel.Hide();
                S.Toggles.Run(toggle.Id);
            };
            _grid.Children.Add(tile);
        }
    }
}
