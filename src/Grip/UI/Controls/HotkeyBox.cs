using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Grip.Core.Actions;
using Grip.Core.Input;
using Grip.Services;

namespace Grip.UI.Controls;

/// <summary>
/// Records a shortcut: click, press the keys, done. Esc cancels, Backspace
/// clears. While recording, Grip's own global shortcuts are released so
/// pressing one records it instead of triggering it.
/// </summary>
public sealed class HotkeyBox : UserControl
{
    private readonly Button _field = new();
    private readonly Button _clear = new();
    private readonly TextBlock _status = new();
    private readonly Border _frame = new();
    private readonly ContentControl _display = new();
    private bool _recording;
    private Hotkey _value;
    private string? _actionId;

    public event EventHandler<Hotkey>? ValueChanged;

    public HotkeyBox()
    {
        _frame.CornerRadius = new CornerRadius(7);
        _frame.BorderThickness = new Thickness(1);
        _frame.MinWidth = 170;
        _frame.Height = 32;
        _frame.Padding = new Thickness(10, 0, 10, 0);
        _frame.SetResourceReference(Border.BackgroundProperty, "Grip.Control");
        _frame.SetResourceReference(Border.BorderBrushProperty, "Grip.Line");
        _display.VerticalAlignment = VerticalAlignment.Center;
        _display.HorizontalAlignment = HorizontalAlignment.Left;
        _frame.Child = _display;

        _field.Style = (Style)Application.Current.FindResource("Grip.Button.Ghost");
        _field.Padding = new Thickness(0);
        _field.Content = _frame;
        _field.Click += (_, _) => StartRecording();

        _clear.Style = (Style)Application.Current.FindResource("Grip.Button.Icon");
        _clear.Width = 30;
        _clear.Height = 30;
        _clear.Margin = new Thickness(4, 0, 0, 0);
        _clear.ToolTip = L.S("hotkeys.clear");
        Ui.SetIcon(_clear, "Dismiss");
        _clear.Click += (_, _) => SetValue(Hotkey.None);

        var line = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        line.Children.Add(_field);
        line.Children.Add(_clear);

        _status.FontSize = 11;
        _status.HorizontalAlignment = HorizontalAlignment.Right;
        _status.Margin = new Thickness(0, 4, 34, 0);
        _status.Visibility = Visibility.Collapsed;

        var stack = new StackPanel();
        stack.Children.Add(line);
        stack.Children.Add(_status);
        Content = stack;

        PreviewKeyDown += OnPreviewKeyDown;
        LostKeyboardFocus += (_, e) =>
        {
            if (_recording && !IsKeyboardFocusWithin) StopRecording();
        };
        Loaded += (_, _) =>
        {
            App.Services.Hotkeys.StatesChanged += OnStatesChanged;
            Render();
        };
        Unloaded += (_, _) =>
        {
            App.Services.Hotkeys.StatesChanged -= OnStatesChanged;
            if (_recording) StopRecording();
        };
    }

    private void OnStatesChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(Render);

    /// <summary>When set, the box edits that action's shortcut in the settings.</summary>
    public string? ActionId
    {
        get => _actionId;
        set
        {
            _actionId = value;
            if (value != null)
                _value = Hotkey.Parse(App.Services.Settings.Current.Hotkeys.GetValueOrDefault(value));
            Render();
        }
    }

    public Hotkey Value
    {
        get => _value;
        set
        {
            _value = value;
            Render();
        }
    }

    private void SetValue(Hotkey hotkey)
    {
        _value = hotkey;
        if (_actionId != null)
            App.Services.Settings.Update(s => s.Hotkeys[_actionId] = hotkey.ToString());
        ValueChanged?.Invoke(this, hotkey);
        Render();
    }

    private void StartRecording()
    {
        if (_recording) return;
        _recording = true;
        App.Services.Hotkeys.Suspend();
        _field.Focus();
        Keyboard.Focus(_field);
        Render();
    }

    private void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        App.Services.Hotkeys.Resume();
        Render();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_recording) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.ImeProcessed) key = e.ImeProcessedKey;

        var modifiers = HotkeyModifiers.None;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) modifiers |= HotkeyModifiers.Ctrl;
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) modifiers |= HotkeyModifiers.Alt;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) modifiers |= HotkeyModifiers.Shift;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) modifiers |= HotkeyModifiers.Win;

        if (key == Key.Escape && modifiers == HotkeyModifiers.None)
        {
            StopRecording();
            return;
        }
        if ((key == Key.Back || key == Key.Delete) && modifiers == HotkeyModifiers.None)
        {
            SetValue(Hotkey.None);
            StopRecording();
            return;
        }

        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0 || VirtualKeys.IsModifierKey(vk)) return; // wait for the main key

        var hotkey = new Hotkey(modifiers, vk);
        if (!hotkey.IsUsableGlobally)
        {
            ShowStatus(L.S("hotkeys.invalid"), "Grip.Rec");
            return;
        }
        StopRecording();
        SetValue(hotkey);
    }

    private void Render()
    {
        _frame.SetResourceReference(Border.BorderBrushProperty, _recording ? "Grip.Accent" : "Grip.Line");
        if (_recording)
        {
            var text = new TextBlock { Text = L.S("hotkeys.recording"), FontSize = 12 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Grip.AccentText");
            _display.Content = text;
        }
        else if (_value.IsEmpty)
        {
            var text = new TextBlock { Text = L.S("hotkeys.none"), FontSize = 12 };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextTertiary");
            _display.Content = text;
        }
        else
        {
            _display.Content = KeyCaps.Create(_value);
        }
        _clear.Visibility = _value.IsEmpty || _recording ? Visibility.Hidden : Visibility.Visible;

        if (_recording || _actionId == null)
        {
            if (!_recording) _status.Visibility = Visibility.Collapsed;
            return;
        }
        var hotkeys = App.Services.Hotkeys;
        switch (hotkeys.StateOf(_actionId))
        {
            case HotkeyState.Conflict:
                ShowStatus(L.S("hotkeys.conflict"), "Grip.Rec");
                break;
            case HotkeyState.Duplicate:
                var owner = hotkeys.DuplicateOwner(_actionId);
                ShowStatus(L.F("hotkeys.duplicate", owner == null ? "?" : L.S(ActionCatalog.TitleKey(owner))), "Grip.Rec");
                break;
            case HotkeyState.FeatureOff:
                ShowStatus(L.S("hotkeys.featureOff"), "Grip.TextTertiary");
                break;
            default:
                _status.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void ShowStatus(string text, string brushKey)
    {
        _status.Text = text;
        _status.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
        _status.Visibility = Visibility.Visible;
    }
}
