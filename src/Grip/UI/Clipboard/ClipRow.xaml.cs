using System.Windows;
using System.Windows.Controls;

namespace Grip.UI.Clipboard;

public partial class ClipRow : UserControl
{
    public static readonly DependencyProperty ShowShortcutProperty = DependencyProperty.Register(
        nameof(ShowShortcut), typeof(bool), typeof(ClipRow),
        new PropertyMetadata(true, (d, e) =>
        {
            if (!(bool)e.NewValue) ((ClipRow)d).ShortcutCap.Visibility = Visibility.Collapsed;
        }));

    public ClipRow() => InitializeComponent();

    /// <summary>The panel hides the Ctrl+1…9 hints; the history window shows them.</summary>
    public bool ShowShortcut
    {
        get => (bool)GetValue(ShowShortcutProperty);
        set => SetValue(ShowShortcutProperty, value);
    }
}
