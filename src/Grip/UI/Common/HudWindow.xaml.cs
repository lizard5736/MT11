using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Grip.Interop;

namespace Grip.UI.Common;

public enum HudTone { Accent, Success, Rec, Neutral }

/// <summary>
/// A short on-screen note above the taskbar ("Link cleaned"). It never takes
/// focus and lets clicks pass through.
/// </summary>
public partial class HudWindow : Window
{
    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromMilliseconds(1700) };

    public HudWindow()
    {
        InitializeComponent();
        _hideTimer.Tick += (_, _) => FadeOut();
        SourceInitialized += (_, _) => WindowStyling.MakeToolWindow(this, noActivate: true, clickThrough: true);
    }

    public void ShowMessage(string text, string icon, HudTone tone)
    {
        Message.Text = text;
        Glyph.Kind = icon;
        Glyph.Foreground = (Brush)FindResource(tone switch
        {
            HudTone.Success => "Grip.Success",
            HudTone.Rec => "Grip.Rec",
            HudTone.Neutral => "Grip.TextSecondary",
            _ => "Grip.Accent",
        });

        if (!IsVisible)
        {
            Root.Opacity = 0;
            Shift.Y = 8;
            Show();
        }
        UpdateLayout();
        var area = Screens.FromPoint(Screens.CursorPosition());
        var (w, h) = WindowPlacement.PhysicalSize(this);
        int x = area.Work.Left + (area.Work.Width - w) / 2;
        int y = area.Work.Bottom - h - (int)(56 * area.Scale);
        WindowPlacement.MoveTo(this, x, y);

        Animate(1, 0, 160);
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    /// <summary>Preview renderer: fills the pill without showing or animating the window.</summary>
    public void ShowMessageForPreview(string text, string icon, HudTone tone)
    {
        Message.Text = text;
        Glyph.Kind = icon;
        Glyph.Foreground = (Brush)FindResource(tone == HudTone.Success ? "Grip.Success" : "Grip.Accent");
        Root.Opacity = 1;
        Shift.Y = 0;
    }

    private void FadeOut()
    {
        _hideTimer.Stop();
        var fade = Animate(0, 6, 240);
        fade.Completed += (_, _) =>
        {
            if (Root.Opacity < 0.05) Hide();
        };
    }

    private DoubleAnimation Animate(double opacity, double y, int ms)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease };
        Root.BeginAnimation(OpacityProperty, fade);
        Shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, TimeSpan.FromMilliseconds(ms)) { EasingFunction = ease });
        return fade;
    }
}
