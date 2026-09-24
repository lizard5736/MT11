using System.Windows;
using System.Windows.Media;

namespace Grip.UI;

/// <summary>A small status light: amber while something is running, dim otherwise.</summary>
public sealed class StatusDot : FrameworkElement
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(StatusDot),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var key = IsActive ? "Grip.Accent" : "Grip.TextTertiary";
        var brush = TryFindResource(key) as Brush ?? Brushes.Gray;
        double r = Math.Min(ActualWidth, ActualHeight) / 2;
        if (IsActive)
        {
            var glow = brush.Clone();
            glow.Opacity = 0.25;
            dc.DrawEllipse(glow, null, new Point(ActualWidth / 2, ActualHeight / 2), r + 2, r + 2);
        }
        dc.DrawEllipse(brush, null, new Point(ActualWidth / 2, ActualHeight / 2), r, r);
    }
}
