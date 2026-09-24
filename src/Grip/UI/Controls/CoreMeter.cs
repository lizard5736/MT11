using System.Linq;
using System.Windows;
using System.Windows.Media;
using Grip.Core.Monitoring;

namespace Grip.UI.Controls;

/// <summary>
/// One thin vertical bar per logical core, height = that core's current load — a compact
/// stand-in for a dozen-plus numeric labels (the same trade-off Task Manager's own
/// per-core view makes). A tooltip carries the exact numbers for anyone who hovers.
/// </summary>
public sealed class CoreMeter : FrameworkElement
{
    private IReadOnlyList<double> _percents = Array.Empty<double>();

    public void SetValues(IReadOnlyList<double> percents)
    {
        _percents = percents;
        ToolTip = percents.Count == 0 ? null : string.Join("   ", percents.Select((p, i) => $"#{i + 1} {Math.Round(p)}%"));
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        int n = _percents.Count;
        if (n < 1 || w <= 0 || h <= 0) return;

        Brush Res(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;
        var normal = Res("Grip.Accent");
        var hot = Res("Grip.Rec");

        const double gap = 2;
        double slot = w / n;
        double barWidth = Math.Max(1, slot - gap);
        for (int i = 0; i < n; i++)
        {
            double percent = Math.Clamp(_percents[i], 0, 100);
            double barHeight = Math.Max(2, h * percent / 100.0);
            var rect = new Rect(i * slot, h - barHeight, barWidth, barHeight);
            dc.DrawRoundedRectangle(MonitorWarnings.IsCpuHigh(percent) ? hot : normal, null, rect, 1.5, 1.5);
        }
    }
}
