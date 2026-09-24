using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Grip.UI.Common;

/// <summary>
/// Shared enter/exit motion for Grip's popup windows: a quick fade with a
/// small vertical settle, matching the graphite style's calm-in / snappy-out
/// feel. Radial and Hud have their own bespoke motion and don't use this.
///
/// Every popup must position itself (WindowPlacement.MoveTo / PlaceNear /
/// PlaceOnMonitor) BEFORE calling Show(). A freshly created window paints its
/// first frame whereever Windows' default placement puts it — usually near
/// the top-left of the screen — and moving it only after that frame is
/// already on screen shows that wrong spot for an instant. Positioning while
/// still invisible (WindowPlacement already creates the HWND via
/// EnsureHandle, so DPI-aware sizing works before Show()) means the very
/// first painted frame is already in the right place.
/// </summary>
internal static class PopupMotion
{
    private const int EnterMs = 170;
    private const int ExitMs = 110;

    /// <summary>Call once, before positioning and Show(), while still invisible.</summary>
    public static void PrepareEnter(UIElement root, TranslateTransform? shift = null, double fromY = 10)
    {
        root.Opacity = 0;
        if (shift != null) shift.Y = fromY;
    }

    /// <summary>Call after Show()/Activate(), once the window is on screen in its final spot.</summary>
    public static void Enter(UIElement root, TranslateTransform? shift = null)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        root.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(EnterMs)) { EasingFunction = ease });
        shift?.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(EnterMs + 30)) { EasingFunction = ease });
    }

    /// <summary>
    /// Fades out, then calls <paramref name="onHidden"/> (normally Hide()). If something
    /// re-opens the window mid-fade (Enter runs again), the stale completion is ignored —
    /// it only ever hides while the fade it started is still the one in charge of Opacity.
    /// </summary>
    public static void Exit(UIElement root, TranslateTransform? shift, Action onHidden, double toY = 6)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(ExitMs)) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            if (root.Opacity < 0.05) onHidden();
        };
        root.BeginAnimation(UIElement.OpacityProperty, fade);
        shift?.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(toY, TimeSpan.FromMilliseconds(ExitMs)) { EasingFunction = ease });
    }
}
