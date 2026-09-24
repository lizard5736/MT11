using System.Windows;
using System.Windows.Controls;
using Grip.Core.Input;

namespace Grip.UI.Controls;

/// <summary>Draws a shortcut as keyboard caps: [Win] [Alt] [V].</summary>
public static class KeyCaps
{
    public static FrameworkElement Create(Hotkey hotkey, bool small = false)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var part in hotkey.Parts)
        {
            var cap = new Border { Style = (Style)Application.Current.FindResource("Grip.KeyCap") };
            if (small) cap.Padding = new Thickness(5, 0, 5, 1);
            var text = new TextBlock
            {
                Text = part,
                FontSize = small ? 10 : 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextSecondary");
            cap.Child = text;
            panel.Children.Add(cap);
        }
        return panel;
    }
}
