using System.Windows;
using System.Windows.Threading;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Input;
using Grip.Core.Radial;
using Grip.Core.Settings;
using Grip.Interop;
using Grip.Services;
using Grip.UI.Common;
using static Grip.Interop.NativeMethods;

namespace Grip.UI.Radial;

/// <summary>
/// Opens the radial menu from its shortcut or a held mouse button, and runs
/// the chosen item. A quick tap of the mouse button (no selection) is passed
/// through, so the button keeps its normal job.
/// </summary>
public sealed class RadialController
{
    private readonly SettingsService _settings;
    private readonly InputHookService _hooks;
    private RadialMenuWindow? _window;
    private IDisposable? _mouseSubscription;
    private IntPtr _previousForeground;

    // Hotkey hold tracking
    private readonly DispatcherTimer _releaseWatch = new() { Interval = TimeSpan.FromMilliseconds(20) };
    private int _hotkeyVk;
    private DateTime _openedAt;

    // Mouse trigger state (touched from the hook thread)
    private volatile bool _mouseHeld;
    private long _downTime;
    private int _downX, _downY;
    private MouseButtonKind _triggerButton;

    public RadialController(SettingsService settings, InputHookService hooks)
    {
        _settings = settings;
        _hooks = hooks;
        _releaseWatch.Tick += (_, _) => WatchHotkeyRelease();
    }

    private RadialMenuSettings S => _settings.Current.RadialMenu;

    public bool IsMouseHooked => _mouseSubscription != null;

    public void ApplySettings()
    {
        bool want = _settings.Current.IsInstalled(FeatureIds.RadialMenu) && S.MouseTrigger != RadialMouseTrigger.None;
        _triggerButton = S.MouseTrigger switch
        {
            RadialMouseTrigger.XButton1 => MouseButtonKind.XButton1,
            RadialMouseTrigger.XButton2 => MouseButtonKind.XButton2,
            _ => MouseButtonKind.Middle,
        };
        if (want && _mouseSubscription == null) _mouseSubscription = _hooks.Subscribe(OnMouse);
        else if (!want && _mouseSubscription != null)
        {
            _mouseSubscription.Dispose();
            _mouseSubscription = null;
        }
    }

    private RadialProfile ActiveProfile =>
        S.Profiles.FirstOrDefault(p => p.Id == S.ActiveProfileId) ?? S.Profiles.FirstOrDefault() ?? RadialProfile.CreateDefault();

    private RadialMenuWindow Window
    {
        get
        {
            if (_window != null) return _window;
            _window = new RadialMenuWindow();
            _window.ItemChosen += Run;
            return _window;
        }
    }

    public void OpenFromHotkey()
    {
        if (Window.IsVisible)
        {
            Window.Close(dismissed: true);
            return;
        }
        _previousForeground = GetForegroundWindow();
        var hotkey = Hotkey.Parse(_settings.Current.Hotkeys.GetValueOrDefault(ActionIds.RadialMenu));
        _hotkeyVk = hotkey.Key;
        _openedAt = DateTime.UtcNow;
        Window.HoldMode = false;
        Window.Open(ActiveProfile.Items, RadialMenuView.RadiusFor(S.Size), Screens.CursorPosition(), activate: true);
        if (S.ReleaseToSelect && _hotkeyVk != 0) _releaseWatch.Start();
    }

    /// <summary>Release-to-select: letting go of the shortcut over a slice runs it.</summary>
    private void WatchHotkeyRelease()
    {
        if (_window?.IsVisible != true)
        {
            _releaseWatch.Stop();
            return;
        }
        if ((GetAsyncKeyState(_hotkeyVk) & 0x8000) != 0) return;
        _releaseWatch.Stop();
        // A quick tap leaves the menu open for clicking; a hold picks on release.
        if (DateTime.UtcNow - _openedAt < TimeSpan.FromMilliseconds(280)) return;
        if (_window.Highlight >= 0) _window.Click();
    }

    // ---------- mouse trigger (hook thread) ----------

    private bool OnMouse(MouseEventInfo e)
    {
        if (e.Button != _triggerButton) return false;
        if (e.IsDown)
        {
            _mouseHeld = true;
            _downTime = e.TimeMs;
            _downX = e.X;
            _downY = e.Y;
            var at = new POINT(e.X, e.Y);
            InputHookService.OnUi(() =>
            {
                _previousForeground = GetForegroundWindow();
                Window.HoldMode = true;
                Window.Open(ActiveProfile.Items, RadialMenuView.RadiusFor(S.Size), at, activate: false);
            });
            return true;
        }
        if (e.IsUp && _mouseHeld)
        {
            _mouseHeld = false;
            long held = e.TimeMs - _downTime;
            bool moved = Math.Abs(e.X - _downX) > 12 || Math.Abs(e.Y - _downY) > 12;
            var button = e.Button!.Value;
            InputHookService.OnUi(() => ReleaseMouse(held, moved, button));
            return true;
        }
        return false;
    }

    private void ReleaseMouse(long heldMs, bool moved, MouseButtonKind button)
    {
        if (_window == null) return;
        if (_window.Highlight >= 0)
        {
            _window.Click();
            if (_window.IsVisible) _window.HoldMode = false; // a submenu opened: keep it for clicking
            return;
        }
        _window.Close(dismissed: true);
        // A plain click with that button: hand it back to the app under the pointer.
        if (heldMs < 300 && !moved) MouseClicker.Click(button);
    }

    // ---------- running items ----------

    private void Run(RadialItem item)
    {
        try
        {
            switch (item.Kind)
            {
                case RadialItemKind.Action:
                    App.Services.Execute(item.Target);
                    break;
                case RadialItemKind.App:
                case RadialItemKind.File:
                case RadialItemKind.Folder:
                case RadialItemKind.Url:
                    if (!AppCatalogService.Open(item.Target, item.Arguments))
                        App.Services.Hud.Show(L.F("radial.failed", RadialMenuView.LabelOf(item)), "Warning", HudTone.Rec);
                    break;
                case RadialItemKind.Keys:
                    SendKeysToPrevious(Hotkey.Parse(item.Target));
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Radial item failed", ex);
        }
    }

    private void SendKeysToPrevious(Hotkey hotkey)
    {
        if (hotkey.IsEmpty) return;
        var target = _previousForeground;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (target != IntPtr.Zero && IsWindow(target)) WindowStyling.ForceForeground(target);
            InputSender.SendHotkey(hotkey);
        };
        timer.Start();
    }
}

/// <summary>Replays a mouse click with SendInput (tagged so Grip's own hook ignores it).</summary>
internal static class MouseClicker
{
    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080, MOUSEEVENTF_XUP = 0x0100;

    public static void Click(MouseButtonKind button)
    {
        (uint down, uint up, uint data) = button switch
        {
            MouseButtonKind.XButton1 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, 1u),
            MouseButtonKind.XButton2 => (MOUSEEVENTF_XDOWN, MOUSEEVENTF_XUP, 2u),
            _ => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP, 0u),
        };
        var inputs = new[]
        {
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = down, mouseData = data, dwExtraInfo = InputSender.GripInputTag } } },
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = up, mouseData = data, dwExtraInfo = InputSender.GripInputTag } } },
        };
        SendInput((uint)inputs.Length, inputs, INPUT.Size);
    }
}
