using System.Windows;
using Grip.Interop;

namespace Grip.UI.Common;

/// <summary>Tells the user something failed, with details they can copy into a bug report.</summary>
public partial class ErrorWindow : Window
{
    private static ErrorWindow? _open;

    private ErrorWindow(Exception ex)
    {
        InitializeComponent();
        Details.Text = $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}";
        SourceInitialized += (_, _) =>
        {
            try
            {
                var theme = App.Services.Theme;
                WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
            }
            catch (InvalidOperationException) { }
        };
    }

    public static void ShowOnce(Exception ex)
    {
        if (_open != null) return;
        _open = new ErrorWindow(ex);
        _open.Closed += (_, _) => _open = null;
        _open.Show();
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText($"Grip {typeof(App).Assembly.GetName().Version}\n{Details.Text}");
        }
        catch (System.Runtime.InteropServices.COMException) { }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
