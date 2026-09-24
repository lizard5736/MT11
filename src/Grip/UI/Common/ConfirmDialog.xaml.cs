using System.Windows;
using Grip.Interop;

namespace Grip.UI.Common;

/// <summary>A graphite replacement for MessageBox, so confirmations match the rest of Grip.</summary>
public partial class ConfirmDialog : Window
{
    private ConfirmDialog(string title, string body, string okText, bool destructive)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;
        BodyText.Visibility = string.IsNullOrEmpty(body) ? Visibility.Collapsed : Visibility.Visible;
        OkButton.Content = okText;
        if (destructive)
        {
            OkButton.Style = (Style)FindResource("Grip.Button.Danger");
            Glyph.Kind = "Warning";
            Glyph.Foreground = (System.Windows.Media.Brush)FindResource("Grip.Rec");
        }
        SourceInitialized += (_, _) => WindowStyling.ApplyChrome(this,
            App.Services.Theme.Color("Bg"), App.Services.Theme.Color("Text"), App.Services.Theme.Color("LineStrong"),
            App.Services.Theme.IsDark);
        Loaded += (_, _) => WindowStyling.ForceForeground(this);
    }

    public static bool Ask(string title, string body = "", string? okText = null, bool destructive = false, Window? owner = null)
    {
        var dialog = new ConfirmDialog(title, body, okText ?? L.S("common.ok"), destructive);
        if (owner is { IsVisible: true })
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        return dialog.ShowDialog() == true;
    }

    private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
