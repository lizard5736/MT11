using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Grip.UI.Controls;

/// <summary>
/// Draws one of Grip's vector icons (Fluent UI System Icons, 20-unit grid).
/// The foreground inherits like text, so an icon inside a button follows the
/// button's color states automatically.
/// </summary>
public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(string), typeof(Icon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((Icon)d)._geometry = null));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(Icon), new FrameworkPropertyMetadata(Brushes.Gray,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(Icon),
        new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    private Geometry? _geometry;

    static Icon()
    {
        IsHitTestVisibleProperty.OverrideMetadata(typeof(Icon), new UIPropertyMetadata(false));
        SnapsToDevicePixelsProperty.OverrideMetadata(typeof(Icon), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Icon key, e.g. "Clipboard" for the resource "Icon.Clipboard".</summary>
    public string? Kind
    {
        get => (string?)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    protected override void OnRender(DrawingContext dc)
    {
        _geometry ??= Resolve(Kind);
        if (_geometry == null) return;
        double scale = Math.Min(ActualWidth, ActualHeight) / 20.0;
        double offsetX = (ActualWidth - 20 * scale) / 2;
        double offsetY = (ActualHeight - 20 * scale) / 2;
        dc.PushTransform(new TranslateTransform(offsetX, offsetY));
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawGeometry(Foreground, null, _geometry);
        dc.Pop();
        dc.Pop();
    }

    public static Geometry? Resolve(string? kind)
    {
        if (string.IsNullOrEmpty(kind)) return null;
        return Application.Current?.TryFindResource("Icon." + kind) as Geometry;
    }
}
