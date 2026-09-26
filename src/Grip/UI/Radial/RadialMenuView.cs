using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Grip.Core.Actions;
using Grip.Core.Localization;
using Grip.Core.Radial;
using Grip.Core.Settings;
using Grip.UI.Controls;

namespace Grip.UI.Radial;

/// <summary>
/// Draws the radial menu: a graphite ring cut into slices, one icon and
/// label per slice, the hovered slice lit in amber with its name in the hub.
/// </summary>
public sealed class RadialMenuView : FrameworkElement
{
    private IReadOnlyList<RadialItem> _items = Array.Empty<RadialItem>();
    private int _highlight = -1;
    private bool _isSubmenu;

    public double Radius { get; private set; } = 150;

    public double DeadZone => Radius * 0.34;

    public static double RadiusFor(RadialSize size) => size switch
    {
        RadialSize.Small => 132,
        RadialSize.Large => 200,
        _ => 166,
    };

    public void SetItems(IReadOnlyList<RadialItem> items, bool isSubmenu, double radius)
    {
        _items = items;
        _isSubmenu = isSubmenu;
        _highlight = -1;
        Radius = radius;
        InvalidateMeasure();
        InvalidateVisual();
    }

    public int Highlight
    {
        get => _highlight;
        set
        {
            if (_highlight == value) return;
            _highlight = value;
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize) => new(Radius * 2 + 32, Radius * 2 + 32);

    public static string LabelOf(RadialItem item)
    {
        var loc = Localizer.Instance;
        if (!string.IsNullOrWhiteSpace(item.Label)) return loc.Resolve(item.Label);
        return item.Kind switch
        {
            RadialItemKind.Action => ActionCatalog.TryGet(item.Target, out _) ? loc[ActionCatalog.TitleKey(item.Target)] : item.Target,
            RadialItemKind.App or RadialItemKind.File or RadialItemKind.Folder =>
                System.IO.Path.GetFileNameWithoutExtension(item.Target.TrimEnd('\\')) is { Length: > 0 } name ? name : item.Target,
            RadialItemKind.Url => Uri.TryCreate(item.Target, UriKind.Absolute, out var uri) ? uri.Host : item.Target,
            _ => item.Target,
        };
    }

    public static string IconOf(RadialItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Icon)) return item.Icon!;
        return item.Kind switch
        {
            RadialItemKind.Action => ActionCatalog.TryGet(item.Target, out var info) ? info.Icon : "Flash",
            RadialItemKind.App => "AppGeneric",
            RadialItemKind.File => "Document",
            RadialItemKind.Folder => "Folder",
            RadialItemKind.Url => "Globe",
            RadialItemKind.Keys => "Keyboard",
            RadialItemKind.Submenu => "Grid",
            _ => "Flash",
        };
    }

    protected override void OnRender(DrawingContext dc)
    {
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        double outer = Radius, inner = DeadZone;
        int count = Math.Max(1, _items.Count);

        Brush Res(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;
        var ring = Res("Grip.Popup");
        var line = Res("Grip.Line");
        var strong = Res("Grip.LineStrong");
        var accent = Res("Grip.Accent");
        var accentSubtle = Res("Grip.AccentSubtle");
        var text = Res("Grip.Text");
        var secondary = Res("Grip.TextSecondary");
        var hub = Res("Grip.Bg");
        var shadowColor = TryFindResource("Grip.Color.Shadow") as Color? ?? Colors.Black;

        // Soft shadow, then the ring itself.
        double spread = 12;
        var shadow = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(90, shadowColor.R, shadowColor.G, shadowColor.B), (outer - 4) / (outer + spread)),
                new GradientStop(Color.FromArgb(0, shadowColor.R, shadowColor.G, shadowColor.B), 1),
            },
        };
        dc.DrawEllipse(shadow, null, new Point(center.X, center.Y + 3), outer + spread, outer + spread);
        dc.DrawEllipse(ring, new Pen(strong, 1), center, outer, outer);

        if (_items.Count > 0)
        {
            double slice = 360.0 / count;
            if (_highlight >= 0 && _highlight < _items.Count)
            {
                double a = RadialGeometry.SliceCenter(_highlight, count);
                dc.DrawGeometry(accentSubtle, null, Slice(center, inner, outer, a - slice / 2, a + slice / 2));
                var arcPen = new Pen(accent, 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                dc.DrawGeometry(null, arcPen, Arc(center, outer - 3, a - slice / 2 + 2, a + slice / 2 - 2));
            }

            if (count > 1)
            {
                var sep = new Pen(line, 1);
                for (int i = 0; i < count; i++)
                {
                    double a = RadialGeometry.SliceCenter(i, count) + slice / 2;
                    var (x1, y1) = RadialGeometry.PointAt(center.X, center.Y, inner + 6, a);
                    var (x2, y2) = RadialGeometry.PointAt(center.X, center.Y, outer - 6, a);
                    dc.DrawLine(sep, new Point(x1, y1), new Point(x2, y2));
                }
            }

            double labelRadius = (inner + outer) / 2;
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                double a = RadialGeometry.SliceCenter(i, count);
                var (cx, cy) = RadialGeometry.PointAt(center.X, center.Y, labelRadius, a);
                bool hot = i == _highlight;
                var geometry = Icon.Resolve(IconOf(item));
                double iconSize = Radius >= 150 ? 22 : 19;
                if (geometry != null)
                {
                    dc.PushTransform(new TranslateTransform(cx - iconSize / 2, cy - iconSize / 2 - 7));
                    dc.PushTransform(new ScaleTransform(iconSize / 20, iconSize / 20));
                    dc.DrawGeometry(hot ? accent : secondary, null, geometry);
                    dc.Pop();
                    dc.Pop();
                }
                double room = count == 1 ? outer : 2 * (labelRadius - 6) * Math.Sin(Math.PI / count) - 12;
                var label = new FormattedText(LabelOf(item), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, hot ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
                    10, hot ? text : secondary, dpi)
                {
                    MaxTextWidth = Math.Max(30, Math.Min(room, inner * 1.6)),
                    MaxLineCount = 2,
                    Trimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center,
                };
                dc.DrawText(label, new Point(cx - label.MaxTextWidth / 2, cy + iconSize / 2 - 4));
            }
        }

        // The hub: dead zone showing the highlighted item's name.
        dc.DrawEllipse(hub, new Pen(strong, 1), center, inner, inner);
        string hubText = _highlight >= 0 && _highlight < _items.Count
            ? LabelOf(_items[_highlight])
            : _isSubmenu ? Localizer.Instance["radial.back"] : "";
        if (hubText.Length > 0)
        {
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var ft = new FormattedText(hubText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                12, _highlight >= 0 ? accent : secondary, dpi)
            {
                MaxTextWidth = inner * 1.7,
                MaxLineCount = 2,
                TextAlignment = TextAlignment.Center,
                Trimming = TextTrimming.CharacterEllipsis,
            };
            dc.DrawText(ft, new Point(center.X - inner * 0.85, center.Y - ft.Height / 2));
        }
        else
        {
            GripMark.Draw(dc, new Rect(center.X - inner * 0.42, center.Y - inner * 0.42, inner * 0.84, inner * 0.84), secondary, null, tile: false);
        }
    }

    private static Geometry Slice(Point c, double r0, double r1, double a0, double a1)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            var (x0, y0) = RadialGeometry.PointAt(c.X, c.Y, r1, a0);
            var (x1, y1) = RadialGeometry.PointAt(c.X, c.Y, r1, a1);
            var (x2, y2) = RadialGeometry.PointAt(c.X, c.Y, r0, a1);
            var (x3, y3) = RadialGeometry.PointAt(c.X, c.Y, r0, a0);
            bool large = a1 - a0 > 180;
            ctx.BeginFigure(new Point(x0, y0), true, true);
            ctx.ArcTo(new Point(x1, y1), new Size(r1, r1), 0, large, SweepDirection.Clockwise, true, false);
            ctx.LineTo(new Point(x2, y2), true, false);
            ctx.ArcTo(new Point(x3, y3), new Size(r0, r0), 0, large, SweepDirection.Counterclockwise, true, false);
        }
        g.Freeze();
        return g;
    }

    private static Geometry Arc(Point c, double r, double a0, double a1)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            var (x0, y0) = RadialGeometry.PointAt(c.X, c.Y, r, a0);
            var (x1, y1) = RadialGeometry.PointAt(c.X, c.Y, r, a1);
            ctx.BeginFigure(new Point(x0, y0), false, false);
            ctx.ArcTo(new Point(x1, y1), new Size(r, r), 0, a1 - a0 > 180, SweepDirection.Clockwise, true, false);
        }
        g.Freeze();
        return g;
    }
}
