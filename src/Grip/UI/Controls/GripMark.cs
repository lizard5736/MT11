using System.Windows;
using System.Windows.Media;

namespace Grip.UI.Controls;

/// <summary>
/// The Grip mark: a "G" drawn as a 290° ring with a crossbar and a tally dot
/// in its opening (the REC light). Shared by the panel header, the settings
/// window and the tray icon renderer, so every size uses the same geometry.
/// </summary>
public sealed class GripMark : FrameworkElement
{
    public static readonly DependencyProperty MarkBrushProperty = DependencyProperty.Register(
        nameof(MarkBrush), typeof(Brush), typeof(GripMark),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DotBrushProperty = DependencyProperty.Register(
        nameof(DotBrush), typeof(Brush), typeof(GripMark),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x3A)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowDotProperty = DependencyProperty.Register(
        nameof(ShowDot), typeof(bool), typeof(GripMark),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TileBrushProperty = DependencyProperty.Register(
        nameof(TileBrush), typeof(Brush), typeof(GripMark),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PixelImageProperty = DependencyProperty.Register(
        nameof(PixelImage), typeof(ImageSource), typeof(GripMark),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    static GripMark()
    {
        IsHitTestVisibleProperty.OverrideMetadata(typeof(GripMark), new UIPropertyMetadata(false));
    }

    public Brush MarkBrush { get => (Brush)GetValue(MarkBrushProperty); set => SetValue(MarkBrushProperty, value); }
    public Brush DotBrush { get => (Brush)GetValue(DotBrushProperty); set => SetValue(DotBrushProperty, value); }
    public bool ShowDot { get => (bool)GetValue(ShowDotProperty); set => SetValue(ShowDotProperty, value); }
    /// <summary>When set, the mark sits on a rounded graphite tile like the app icon.</summary>
    public Brush? TileBrush { get => (Brush?)GetValue(TileBrushProperty); set => SetValue(TileBrushProperty, value); }

    /// <summary>When set, draws this hand-pixelled bitmap instead of the procedural ring —
    /// the DOOM theme's own take on the mark, bound via DynamicResource so it appears only
    /// while that theme is active.</summary>
    public ImageSource? PixelImage { get => (ImageSource?)GetValue(PixelImageProperty); set => SetValue(PixelImageProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        double side = double.IsInfinity(availableSize.Width) ? 24 : Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(side)) side = 24;
        return new Size(side, side);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0) return;
        var origin = new Point((ActualWidth - side) / 2, (ActualHeight - side) / 2);
        if (TileBrush != null)
        {
            dc.DrawRoundedRectangle(TileBrush, null, new Rect(origin, new Size(side, side)), side * 0.22, side * 0.22);
            var inset = new Rect(origin.X + side * 0.1, origin.Y + side * 0.1, side * 0.8, side * 0.8);
            if (PixelImage != null) DrawPixelImage(dc, inset);
            else Draw(dc, inset, MarkBrush, ShowDot ? DotBrush : null, tile: true);
        }
        else
        {
            var square = new Rect(origin, new Size(side, side));
            if (PixelImage != null) DrawPixelImage(dc, square);
            else Draw(dc, square, MarkBrush, ShowDot ? DotBrush : null, tile: false);
        }
    }

    private void DrawPixelImage(DrawingContext dc, Rect square)
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        dc.DrawImage(PixelImage, square);
    }

    /// <summary>
    /// Draws the mark into a square. Without a tile the ring fills more of the
    /// square, which keeps it legible at tray-icon sizes.
    /// </summary>
    public static void Draw(DrawingContext dc, Rect square, Brush mark, Brush? dot, bool tile)
    {
        double s = square.Width;
        double cx = square.X + s / 2, cy = square.Y + s / 2;
        double radius = s * (tile ? 0.30 : 0.33);
        double stroke = s * (tile ? 0.13 : 0.19);

        var pen = new Pen(mark, stroke) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
        pen.Freeze();

        // Ring from 0° (3 o'clock) clockwise to 290°, leaving the upper-right opening.
        var start = PointOnCircle(cx, cy, radius, 0);
        var end = PointOnCircle(cx, cy, radius, 290);
        var ring = new StreamGeometry();
        using (var ctx = ring.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(end, new Size(radius, radius), 0, true, SweepDirection.Clockwise, true, false);
        }
        ring.Freeze();
        dc.DrawGeometry(null, pen, ring);

        // Crossbar from near the center out to the ring's outer edge.
        double half = stroke / 2;
        dc.DrawRectangle(mark, null, new Rect(cx + stroke * 0.25, cy - half, radius + half - stroke * 0.25, stroke));

        if (dot != null)
        {
            var center = PointOnCircle(cx, cy, radius, 325);
            double r = stroke * 0.62;
            dc.DrawEllipse(dot, null, center, r, r);
        }
    }

    /// <summary>Screen coordinates: 0° points right and angles grow clockwise.</summary>
    private static Point PointOnCircle(double cx, double cy, double r, double degrees)
    {
        double rad = degrees * Math.PI / 180;
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }
}
