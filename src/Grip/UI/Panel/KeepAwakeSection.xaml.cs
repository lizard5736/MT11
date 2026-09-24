using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Grip.Core.Energy;
using Grip.Core.Localization;
using Grip.Services;
using Grip.UI.Common;

namespace Grip.UI.Panel;

public partial class KeepAwakeSection : PanelSection
{
    private readonly KeepAwakeService _awake;

    public KeepAwakeSection()
    {
        InitializeComponent();
        _awake = S.KeepAwake;
        _awake.Changed += (_, _) => Dispatcher.BeginInvoke(Refresh);
        _awake.Tick += (_, _) => UpdateStatus();
        Localizer.Instance.LanguageChanged += (_, _) => BuildPresets();
        BuildPresets();
    }

    private void BuildPresets()
    {
        Presets.Children.Clear();
        foreach (var minutes in S.Settings.Current.KeepAwake.Presets)
        {
            var chip = new ToggleButton
            {
                Style = (Style)FindResource("Grip.Chip"),
                Content = KeepAwakeText.PresetLabel(minutes, Localizer.Instance),
                Tag = minutes,
            };
            chip.Click += (_, _) =>
            {
                _awake.Start(minutes);
                ShowStartedHud();
            };
            Presets.Children.Add(chip);
        }
        var until = new ToggleButton
        {
            Style = (Style)FindResource("Grip.Chip"),
            Content = L.S("keepAwake.preset.until"),
            Tag = "until",
        };
        until.Click += (_, _) =>
        {
            UntilRow.Visibility = UntilRow.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            if (UntilRow.Visibility == Visibility.Visible)
            {
                UntilBox.Text = DateTime.Now.AddHours(1).ToString("HH:00");
                UntilBox.Focus();
                UntilBox.SelectAll();
            }
            Refresh();
        };
        Presets.Children.Add(until);
    }

    public override void Refresh()
    {
        var settings = S.Settings.Current.KeepAwake;
        Switch.IsChecked = _awake.IsActive;
        DisplaySwitch.IsChecked = settings.KeepDisplayOn;
        LidRow.Visibility = KeepAwakeService.HasLid ? Visibility.Visible : Visibility.Collapsed;
        LidSwitch.IsChecked = settings.LidClosed;
        if (_awake.LidOverrideFailed) LidNote.Text = L.S("settings.keepAwake.lid.failed");

        foreach (ToggleButton chip in Presets.Children)
        {
            chip.IsChecked = chip.Tag switch
            {
                int minutes => _awake.IsActive && _awake.ActivePreset == minutes,
                "until" => UntilRow.Visibility == Visibility.Visible || (_awake.IsActive && _awake.ActivePreset == null),
                _ => false,
            };
        }

        Badge.SetResourceReference(Border.BackgroundProperty, _awake.IsActive ? "Grip.AccentSubtle" : "Grip.Control");
        BadgeIcon.Kind = _awake.IsActive ? "WeatherMoonFilled" : "WeatherMoon";
        BadgeIcon.SetResourceReference(Controls.Icon.ForegroundProperty, _awake.IsActive ? "Grip.Accent" : "Grip.TextSecondary");
        UpdateStatus();
    }

    private void UpdateStatus() =>
        Status.Text = KeepAwakeText.Status(_awake.Session, DateTimeOffset.Now, Localizer.Instance);

    private void OnSwitch(object sender, RoutedEventArgs e)
    {
        if (Switch.IsChecked == true)
        {
            _awake.Start(S.Settings.Current.KeepAwake.DefaultMinutes);
            ShowStartedHud();
        }
        else _awake.Stop();
        Refresh();
    }

    private void ShowStartedHud()
    {
        var session = _awake.Session;
        if (session == null) return;
        var text = session.IsIndefinite
            ? L.S("keepAwake.hud.onIndefinite")
            : L.F("keepAwake.hud.on", Localizer.Instance.Duration(session.Remaining(DateTimeOffset.Now)));
        S.Hud.Show(text, "WeatherMoonFilled", HudTone.Accent);
    }

    private void OnApplyUntil(object sender, RoutedEventArgs e) => ApplyUntil();

    private void OnUntilKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyUntil();
            e.Handled = true;
        }
    }

    private void ApplyUntil()
    {
        if (!KeepAwakeText.TryParseClock(UntilBox.Text, out var time))
        {
            UntilBox.SetResourceReference(BorderBrushProperty, "Grip.Rec");
            return;
        }
        UntilBox.ClearValue(BorderBrushProperty);
        _awake.StartUntil(time);
        UntilRow.Visibility = Visibility.Collapsed;
        ShowStartedHud();
        Refresh();
    }

    private void OnDisplay(object sender, RoutedEventArgs e) =>
        S.Settings.Update(s => s.KeepAwake.KeepDisplayOn = DisplaySwitch.IsChecked == true);

    private void OnLid(object sender, RoutedEventArgs e)
    {
        S.Settings.Update(s => s.KeepAwake.LidClosed = LidSwitch.IsChecked == true);
        Refresh();
    }
}
