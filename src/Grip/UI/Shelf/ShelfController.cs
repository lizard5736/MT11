using Grip.Core.Features;
using Grip.Core.Input;
using Grip.Interop;
using Grip.Services;
using static Grip.Interop.NativeMethods;

namespace Grip.UI.Shelf;

/// <summary>Opens the shelf from its shortcut, or when a drag is wiggled left and right.</summary>
public sealed class ShelfController
{
    private readonly SettingsService _settings;
    private readonly InputHookService _hooks;
    private readonly ShakeDetector _shake = new();
    private IDisposable? _subscription;
    private ShelfWindow? _window;

    // Hook-thread state
    private bool _leftDown;
    private int _downX, _downY;
    private bool _dragging;

    public ShelfController(SettingsService settings, InputHookService hooks)
    {
        _settings = settings;
        _hooks = hooks;
    }

    public bool IsMouseHooked => _subscription != null;

    public ShelfWindow Window
    {
        get
        {
            if (_window != null) return _window;
            _window = new ShelfWindow();
            App.Services.AppCatalog.IconLoaded += (_, _) =>
            {
                if (_window.IsVisible) _window.RefreshIcons();
            };
            return _window;
        }
    }

    public void ApplySettings()
    {
        bool want = _settings.Current.IsInstalled(FeatureIds.Shelf) && _settings.Current.Shelf.ShakeToOpen;
        if (want && _subscription == null) _subscription = _hooks.Subscribe(OnMouse);
        else if (!want && _subscription != null)
        {
            _subscription.Dispose();
            _subscription = null;
        }
        if (!_settings.Current.IsInstalled(FeatureIds.Shelf)) _window?.Hide();
    }

    public void Toggle()
    {
        if (Window.IsVisible)
        {
            Window.Hide();
            return;
        }
        Window.ShowAt(Screens.CursorPosition(), _settings.Current.Shelf.OpenNearCursor);
    }

    /// <summary>Runs on the hook thread: never swallows, only watches for a wiggle mid-drag.</summary>
    private bool OnMouse(MouseEventInfo e)
    {
        if (e.Button == MouseButtonKind.Left && e.IsDown)
        {
            _leftDown = true;
            _dragging = false;
            _downX = e.X;
            _downY = e.Y;
            _shake.Reset();
            return false;
        }
        if (e.Button == MouseButtonKind.Left && e.IsUp)
        {
            _leftDown = false;
            _dragging = false;
            return false;
        }
        if (e.Message != WM_MOUSEMOVE || !_leftDown) return false;

        if (!_dragging)
        {
            _dragging = Math.Abs(e.X - _downX) > 12 || Math.Abs(e.Y - _downY) > 12;
            if (!_dragging) return false;
        }
        if (_shake.Add(e.TimeMs, e.X))
        {
            var at = new POINT(e.X, e.Y);
            InputHookService.OnUi(() =>
            {
                if (!Window.IsVisible) Window.ShowAt(at, _settings.Current.Shelf.OpenNearCursor);
            });
        }
        return false;
    }
}
