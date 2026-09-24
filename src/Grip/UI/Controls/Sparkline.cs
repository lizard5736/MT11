using System.Windows;
using System.Windows.Media;

namespace Grip.UI.Controls;

/// <summary>A small filled line graph for a monitor tile: oldest value on the left, newest on the right.</summary>
public sealed class Sparkline : FrameworkElement
{
    private IReadOnlyList<double> _values = Array.Empty<double>();
    private double _max = 100;

    /// <summary>The value that reaches the top edge. 100 for a percent; for a rate, pass the series' own peak so it self-scales.</summary>
    public double Max
    {
        get => _max;
        set
        {
            if (_max.Equals(value)) return;
            _max = value;
            InvalidateVisual();
        }
    }

    public void SetValues(IReadOnlyList<double> values)
    {
        _values = values;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (_values.Count < 1 || w <= 0 || h <= 0) return;
        // One reading (just opened, nothing to trend yet) still draws — as a flat line at that value.
        var values = _values.Count == 1 ? new[] { _values[0], _values[0] } : _values;

        Brush Res(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;
        var line = Res("Grip.Accent");
        var fill = Res("Grip.AccentSubtle");

        double max = Math.Max(_max, 1);
        double step = w / (values.Count - 1);
        Point At(int i) => new(i * step, h - Math.Clamp(values[i] / max, 0, 1) * h);

        var area = new StreamGeometry();
        using (var ctx = area.Open())
        {
            ctx.BeginFigure(new Point(0, h), true, true);
            ctx.LineTo(At(0), false, false);
            for (int i = 1; i < values.Count; i++) ctx.LineTo(At(i), true, false);
            ctx.LineTo(new Point(w, h), true, false);
        }
        area.Freeze();
        dc.DrawGeometry(fill, null, area);

        var stroke = new StreamGeometry();
        using (var ctx = stroke.Open())
        {
            ctx.BeginFigure(At(0), false, false);
            for (int i = 1; i < values.Count; i++) ctx.LineTo(At(i), true, false);
        }
        stroke.Freeze();
        dc.DrawGeometry(null, new Pen(line, 1.5) { LineJoin = PenLineJoin.Round }, stroke);
    }
}
