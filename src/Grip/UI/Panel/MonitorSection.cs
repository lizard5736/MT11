using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Grip.Core.Features;
using Grip.Core.Localization;
using Grip.Core.Monitoring;
using Grip.Services;
using Grip.UI;
using Grip.UI.Common;
using Grip.UI.Controls;

namespace Grip.UI.Panel;

/// <summary>
/// Live tiles for whichever monitor metrics are installed: CPU, memory,
/// disks, network, battery. Starts the sampler when shown, stops it when
/// the panel closes — nothing samples while the panel is closed.
/// </summary>
public sealed class MonitorSection : PanelSection
{
    private readonly StackPanel _stack = new();
    private static SystemMonitorService Monitor => S.Monitor;

    private TextBlock? _cpuValue;
    private TextBlock? _cpuName;
    private Sparkline? _cpuGraph;
    private CoreMeter? _coreMeter;

    private TextBlock? _memValue;
    private Sparkline? _memGraph;
    private StackPanel? _memRows;

    private StackPanel? _diskRows;
    private readonly Dictionary<string, (TextBlock Text, Border Fill, ScaleTransform Scale)> _diskByRoot = new();

    private TextBlock? _netValue;
    private Sparkline? _netGraph;
    private TextBlock? _netSession;

    private TextBlock? _battValue;
    private TextBlock? _battState;

    private TextBlock? _gpuValue;
    private TextBlock? _gpuName;
    private TextBlock? _gpuStats;
    private TextBlock? _gpuState;
    private StackPanel? _gpuRows;

    private static GpuMonitorService Gpu => S.Gpu;

    public MonitorSection()
    {
        Content = _stack;
        Monitor.Sampled += OnSampled;
        Gpu.Sampled += OnGpuSampled;
    }

    public override void Refresh()
    {
        Build();
        Monitor.Start();
        Gpu.Start();
        if (Monitor.Latest is { } snapshot) Render(snapshot);
        RenderGpu(Gpu.Latest);
    }

    public override void Suspend()
    {
        Monitor.Stop();
        Gpu.Stop();
    }

    private void OnSampled(object? sender, EventArgs e)
    {
        if (Monitor.Latest is not { } snapshot) return;
        Dispatcher.BeginInvoke(() => Render(snapshot));
    }

    private void OnGpuSampled(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() => RenderGpu(Gpu.Latest));

    // ---------- building ----------

    private void Build()
    {
        _stack.Children.Clear();
        _cpuValue = null;
        _cpuName = null;
        _cpuGraph = null;
        _coreMeter = null;
        _memValue = null;
        _memGraph = null;
        _memRows = null;
        _diskRows = null;
        _diskByRoot.Clear();
        _netValue = null;
        _netGraph = null;
        _netSession = null;
        _battValue = null;
        _battState = null;
        _gpuValue = null;
        _gpuName = null;
        _gpuStats = null;
        _gpuState = null;
        _gpuRows = null;

        var s = S.Settings.Current;
        if (s.IsInstalled(FeatureIds.MonitorCpu)) _stack.Children.Add(BuildCpuCard());
        if (s.IsInstalled(FeatureIds.MonitorGpu)) _stack.Children.Add(BuildGpuCard());
        if (s.IsInstalled(FeatureIds.MonitorMemory)) _stack.Children.Add(BuildMemoryCard());
        if (s.IsInstalled(FeatureIds.MonitorDisk)) _stack.Children.Add(BuildDiskCard());
        if (s.IsInstalled(FeatureIds.MonitorNetwork)) _stack.Children.Add(BuildNetworkCard());
        if (s.IsInstalled(FeatureIds.MonitorBattery)) _stack.Children.Add(BuildBatteryCard());
    }

    /// <summary>A card shell: icon badge, title, a mono value on the right, and a body for the rest.</summary>
    private static (Border Root, StackPanel Body, TextBlock Value) Card(string icon, string title)
    {
        var root = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 8),
        };
        root.SetResourceReference(Border.BackgroundProperty, "Grip.Surface");
        root.SetResourceReference(Border.BorderBrushProperty, "Grip.Line");

        var outer = new StackPanel();
        root.Child = outer;

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var badge = new Border { Width = 26, Height = 26, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 8, 0) };
        badge.SetResourceReference(Border.BackgroundProperty, "Grip.AccentSubtle");
        var glyph = new Icon { Kind = icon, Size = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
        badge.Child = glyph;
        header.Children.Add(badge);

        var titleText = new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.FindResource("Grip.Text.Strong"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(titleText, 1);
        header.Children.Add(titleText);

        var value = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Mono"), VerticalAlignment = VerticalAlignment.Center };
        value.SetResourceReference(TextBlock.ForegroundProperty, "Grip.Text");
        Grid.SetColumn(value, 2);
        header.Children.Add(value);

        outer.Children.Add(header);
        var body = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        outer.Children.Add(body);
        return (root, body, value);
    }

    /// <summary>One process row for the memory/GPU lists: name + detail on the left, a kill button on the right.</summary>
    private static FrameworkElement ProcessRow(int pid, string name, string detail)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = name,
            Style = (Style)Application.Current.FindResource("Grip.Text.Secondary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        text.Children.Add(new TextBlock { Text = detail, Style = (Style)Application.Current.FindResource("Grip.Text.Caption") });
        grid.Children.Add(text);

        var kill = new Button
        {
            Style = (Style)Application.Current.FindResource("Grip.Button.Icon"),
            Width = 24,
            Height = 24,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = L.S("monitor.process.kill"),
        };
        Ui.SetIcon(kill, "Dismiss");
        Grid.SetColumn(kill, 1);
        kill.Click += (_, _) => OnKillProcess(pid, name);
        grid.Children.Add(kill);

        return grid;
    }

    private static void OnKillProcess(int pid, string name)
    {
        if (!ConfirmDialog.Ask(L.F("monitor.process.kill.confirm", name), okText: L.S("monitor.process.kill"), destructive: true)) return;
        if (!ProcessControl.TryKill(pid)) S.Hud.Show(L.F("monitor.process.kill.failed", name), "Warning", HudTone.Rec);
    }

    private static void RenderProcessRows(StackPanel? host, IEnumerable<(int Pid, string Name, string Detail)> rows)
    {
        if (host == null) return;
        host.Children.Clear();
        foreach (var row in rows) host.Children.Add(ProcessRow(row.Pid, row.Name, row.Detail));
    }

    private FrameworkElement BuildCpuCard()
    {
        var (root, body, value) = Card("DeveloperBoard", L.S("feature.monitorCpu.title"));
        _cpuValue = value;
        _cpuName = new TextBlock
        {
            Style = (Style)Application.Current.FindResource("Grip.Text.Caption"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = Monitor.CpuName ?? "",
            Visibility = Monitor.CpuName == null ? Visibility.Collapsed : Visibility.Visible,
        };
        body.Children.Add(_cpuName);
        _cpuGraph = new Sparkline { Height = 28, Max = 100, Margin = new Thickness(0, 6, 0, 0) };
        body.Children.Add(_cpuGraph);
        _coreMeter = new CoreMeter { Height = 20, Margin = new Thickness(0, 6, 0, 0) };
        body.Children.Add(_coreMeter);
        return root;
    }

    private FrameworkElement BuildGpuCard()
    {
        var (root, body, value) = Card("Gpu", L.S("feature.monitorGpu.title"));
        _gpuValue = value;
        _gpuName = new TextBlock
        {
            Style = (Style)Application.Current.FindResource("Grip.Text.Caption"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = Gpu.GpuName ?? "",
            Visibility = Gpu.GpuName == null ? Visibility.Collapsed : Visibility.Visible,
        };
        body.Children.Add(_gpuName);
        _gpuStats = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Caption"), Margin = new Thickness(0, 4, 0, 0) };
        body.Children.Add(_gpuStats);
        _gpuState = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Caption"), Margin = new Thickness(0, 4, 0, 0) };
        body.Children.Add(_gpuState);
        _gpuRows = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        body.Children.Add(_gpuRows);
        return root;
    }

    private FrameworkElement BuildMemoryCard()
    {
        var (root, body, value) = Card("Ram", L.S("feature.monitorMemory.title"));
        _memValue = value;
        _memGraph = new Sparkline { Height = 28, Max = 100 };
        body.Children.Add(_memGraph);
        _memRows = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        body.Children.Add(_memRows);
        return root;
    }

    private FrameworkElement BuildDiskCard()
    {
        var (root, body, value) = Card("HardDrive", L.S("feature.monitorDisk.title"));
        value.Visibility = Visibility.Collapsed; // per-drive detail lives in the rows below, not the header
        _diskRows = new StackPanel();
        body.Children.Add(_diskRows);
        return root;
    }

    private (FrameworkElement Root, TextBlock Text, Border Fill, ScaleTransform Scale) DiskRow(string label)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        var header = new DockPanel();
        var text = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Caption") };
        DockPanel.SetDock(text, Dock.Right);
        header.Children.Add(text);
        header.Children.Add(new TextBlock { Text = label, Style = (Style)Application.Current.FindResource("Grip.Text.Secondary") });
        stack.Children.Add(header);

        var track = new Border { Height = 5, CornerRadius = new CornerRadius(2.5), Margin = new Thickness(0, 4, 0, 0) };
        track.SetResourceReference(Border.BackgroundProperty, "Grip.Line");
        var fill = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(2.5),
            RenderTransformOrigin = new Point(0, 0.5),
        };
        fill.SetResourceReference(Border.BackgroundProperty, "Grip.Accent");
        var scale = new ScaleTransform(0, 1);
        fill.RenderTransform = scale;
        track.Child = fill;
        stack.Children.Add(track);
        return (stack, text, fill, scale);
    }

    private FrameworkElement BuildNetworkCard()
    {
        var (root, body, value) = Card("Globe", L.S("feature.monitorNetwork.title"));
        _netValue = value;
        _netGraph = new Sparkline { Height = 24 };
        body.Children.Add(_netGraph);
        _netSession = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Caption"), Margin = new Thickness(0, 6, 0, 0) };
        body.Children.Add(_netSession);
        return root;
    }

    private FrameworkElement BuildBatteryCard()
    {
        var (root, body, value) = Card("Battery", L.S("feature.monitorBattery.title"));
        _battValue = value;
        _battState = new TextBlock { Style = (Style)Application.Current.FindResource("Grip.Text.Caption") };
        body.Children.Add(_battState);
        return root;
    }

    // ---------- rendering a sample ----------

    private void Render(MonitorSnapshot snapshot)
    {
        bool ru = Localizer.Instance.Language == UiLanguage.Ru;
        var culture = Localizer.Instance.Culture;

        if (_cpuValue != null)
        {
            _cpuValue.Text = $"{Math.Round(snapshot.CpuPercent)}%";
            Warn(_cpuValue, MonitorWarnings.IsCpuHigh(snapshot.CpuPercent));
            _cpuGraph!.SetValues(Monitor.CpuHistory.Values);
            _coreMeter?.SetValues(snapshot.CpuCorePercents);
        }

        if (_memValue != null)
        {
            _memValue.Text = $"{ByteFormat.Size(snapshot.MemoryUsedBytes, culture, ru)} / {ByteFormat.Size(snapshot.MemoryTotalBytes, culture, ru)}";
            Warn(_memValue, MonitorWarnings.IsMemoryHigh(snapshot.MemoryUsedPercent));
            _memGraph!.SetValues(Monitor.MemoryHistory.Values);
            RenderProcessRows(_memRows, snapshot.TopProcesses.Take(5)
                .Select(p => (p.ProcessId, p.Name, ByteFormat.Size(p.WorkingSetBytes, culture, ru))));
        }

        if (_diskRows != null) RenderDisks(_diskRows, snapshot.Drives, culture, ru);

        if (_netValue != null)
        {
            string up = ByteFormat.Rate(snapshot.NetworkUpBytesPerSecond, culture, ru);
            string down = ByteFormat.Rate(snapshot.NetworkDownBytesPerSecond, culture, ru);
            _netValue.Text = $"↓{down}  ↑{up}";
            _netGraph!.Max = Math.Max(1, Monitor.NetworkHistory.Max);
            _netGraph.SetValues(Monitor.NetworkHistory.Values);
            string session = L.F("monitor.network.session",
                ByteFormat.Size(snapshot.SessionBytesReceived, culture, ru), ByteFormat.Size(snapshot.SessionBytesSent, culture, ru));
            _netSession!.Text = snapshot.LocalIPv4 == null ? session : $"{session}  ·  {snapshot.LocalIPv4}";
        }

        if (_battValue != null)
        {
            if (snapshot.HasBattery)
            {
                _battValue.Text = $"{snapshot.BatteryPercent}%";
                Warn(_battValue, MonitorWarnings.IsBatteryLow(snapshot.BatteryPercent, snapshot.BatteryCharging));
                _battState!.Text = L.S(snapshot.BatteryCharging ? "monitor.battery.charging" : "monitor.battery.onBattery");
            }
            else
            {
                _battValue.Text = "";
                _battState!.Text = L.S("monitor.battery.none");
            }
        }
    }

    private void RenderGpu(double? percent)
    {
        if (_gpuValue == null) return;
        bool ru = Localizer.Instance.Language == UiLanguage.Ru;
        var culture = Localizer.Instance.Culture;

        if (_gpuName != null)
        {
            _gpuName.Text = Gpu.GpuName ?? "";
            _gpuName.Visibility = Gpu.GpuName == null ? Visibility.Collapsed : Visibility.Visible;
        }

        if (percent is { } value)
        {
            _gpuValue.Text = $"{Math.Round(value)}%";
            Warn(_gpuValue, value >= MonitorWarnings.CpuHighPercent);
            _gpuState!.Text = "";
        }
        else
        {
            _gpuValue.Text = "";
            _gpuState!.Text = L.S("monitor.gpu.unavailable");
        }

        if (_gpuStats != null)
        {
            var temperature = Gpu.TemperatureCelsius;
            var memory = Gpu.MemoryInfo;
            var parts = new List<string>();
            if (temperature is { } t) parts.Add($"{Math.Round(t)}°C");
            if (memory is { } m) parts.Add($"{ByteFormat.Size(m.Used, culture, ru)} / {ByteFormat.Size(m.Total, culture, ru)}");
            _gpuStats.Text = string.Join("  ·  ", parts);
            _gpuStats.Visibility = parts.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            WarnCaption(_gpuStats, temperature is { } warnT && MonitorWarnings.IsGpuTemperatureHigh(warnT));
        }

        RenderProcessRows(_gpuRows, Gpu.TopProcesses.Take(3).Select(p => (p.ProcessId, p.Name, $"{Math.Round(p.Percent)}%")));
    }

    private void RenderDisks(StackPanel diskRows, IReadOnlyList<DriveSample> drives, System.Globalization.CultureInfo culture, bool ru)
    {
        if (!drives.Select(d => d.Root).SequenceEqual(_diskByRoot.Keys))
        {
            diskRows.Children.Clear();
            _diskByRoot.Clear();
            foreach (var drive in drives)
            {
                var row = DiskRow(drive.Label);
                diskRows.Children.Add(row.Root);
                _diskByRoot[drive.Root] = (row.Text, row.Fill, row.Scale);
            }
        }
        foreach (var drive in drives)
        {
            if (!_diskByRoot.TryGetValue(drive.Root, out var refs)) continue;
            refs.Text.Text = L.F("monitor.disk.free", ByteFormat.Size(drive.FreeBytes, culture, ru));
            refs.Scale.ScaleX = Math.Clamp(drive.UsedPercent / 100.0, 0, 1);
            refs.Fill.SetResourceReference(Border.BackgroundProperty,
                MonitorWarnings.IsDiskLow(100 - drive.UsedPercent) ? "Grip.Rec" : "Grip.Accent");
        }
    }

    private static void Warn(TextBlock text, bool isWarning) =>
        text.SetResourceReference(TextBlock.ForegroundProperty, isWarning ? "Grip.Rec" : "Grip.Text");

    /// <summary>Same idea as <see cref="Warn"/>, but for a Grip.Text.Caption block: clearing
    /// back to the style's own muted color instead of hardcoding the primary text color, so
    /// a non-warning caption doesn't turn as bright as a headline value.</summary>
    private static void WarnCaption(TextBlock text, bool isWarning)
    {
        if (isWarning) text.SetResourceReference(TextBlock.ForegroundProperty, "Grip.Rec");
        else text.ClearValue(TextBlock.ForegroundProperty);
    }
}
