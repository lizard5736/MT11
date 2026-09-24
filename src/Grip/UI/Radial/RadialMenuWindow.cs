using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Grip.Core.Radial;
using Grip.Interop;

namespace Grip.UI.Radial;

/// <summary>
/// A transparent window holding the ring, centered on the pointer. It reads
/// the pointer on a timer, so selection keeps working even when the pointer
/// leaves the ring's bounds.
/// </summary>
public sealed class RadialMenuWindow : Window
{
    private readonly RadialMenuView _view = new();
    private readonly DispatcherTimer _tracker = new(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(15) };
    private readonly Stack<IReadOnlyList<RadialItem>> _stack = new();
    private NativeMethods.POINT _center;
    private double _scale = 1;

    public event Action<RadialItem>? ItemChosen;
    public event Action? Dismissed;

    public RadialMenuWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        Content = _view;
        _view.RenderTransformOrigin = new Point(0.5, 0.5);
        _view.RenderTransform = new ScaleTransform(1, 1);
        SourceInitialized += (_, _) => WindowStyling.MakeToolWindow(this);
        _tracker.Tick += (_, _) => Track();
        MouseLeftButtonUp += (_, _) => Click();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close(dismissed: true);
                e.Handled = true;
            }
        };
        Deactivated += (_, _) =>
        {
            if (IsVisible && !HoldMode) Close(dismissed: true);
        };
    }

    /// <summary>While a mouse button holds the menu open, focus changes don't close it.</summary>
    public bool HoldMode { get; set; }

    public int Highlight => _view.Highlight;

    public IReadOnlyList<RadialItem> CurrentItems => _stack.Count > 0 ? _stack.Peek() : Array.Empty<RadialItem>();

    public bool IsInSubmenu => _stack.Count > 1;

    internal void Open(IReadOnlyList<RadialItem> items, double radius, NativeMethods.POINT at, bool activate)
    {
        _stack.Clear();
        _stack.Push(items);
        _view.SetItems(items, isSubmenu: false, radius);
        _center = at;
        bool wasHidden = !IsVisible;
        if (wasHidden) _view.Opacity = 0; // Pop() below fades it in; stay invisible until positioned.
        UpdateLayout();
        var area = Screens.FromPoint(at);
        _scale = area.Scale;
        var (w, h) = WindowPlacement.PhysicalSize(this);
        // Position before Show(): otherwise the first frame paints at whatever spot
        // Windows' default placement picks (near the screen's top-left) for an instant.
        WindowPlacement.MoveTo(this, at.X - w / 2, at.Y - h / 2);
        if (wasHidden) Show();
        if (activate)
        {
            WindowStyling.ForceForeground(this);
            Activate();
            Focus();
        }
        Pop();
        _tracker.Start();
    }

    private void Pop()
    {
        var scale = (ScaleTransform)_view.RenderTransform;
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.86, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.86, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
        _view.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(110)));
    }

    private void Track()
    {
        var p = Screens.CursorPosition();
        double dx = (p.X - _center.X) / _scale;
        double dy = (p.Y - _center.Y) / _scale;
        _view.Highlight = RadialGeometry.HitTest(dx, dy, CurrentItems.Count, _view.DeadZone);
    }

    /// <summary>Acts on the highlighted slice (or goes back from a submenu when the hub is clicked).</summary>
    public void Click()
    {
        Track();
        int index = _view.Highlight;
        if (index < 0 || index >= CurrentItems.Count)
        {
            if (IsInSubmenu) Back();
            else Close(dismissed: true);
            return;
        }
        var item = CurrentItems[index];
        if (item.Kind == RadialItemKind.Submenu && item.Children.Count > 0)
        {
            _stack.Push(item.Children);
            _view.SetItems(item.Children, isSubmenu: true, _view.Radius);
            Pop();
            return;
        }
        Close(dismissed: false);
        ItemChosen?.Invoke(item);
    }

    private void Back()
    {
        _stack.Pop();
        _view.SetItems(_stack.Peek(), isSubmenu: _stack.Count > 1, _view.Radius);
        Pop();
    }

    public void Close(bool dismissed)
    {
        _tracker.Stop();
        HoldMode = false;
        // A dismiss (Escape, click on empty hub, focus lost) fades the ring out. Picking
        // an item hides instantly instead, so the action it triggers isn't held up by a wait.
        if (dismissed) CloseSoft();
        else if (IsVisible) Hide();
        if (dismissed) Dismissed?.Invoke();
    }

    private void CloseSoft()
    {
        if (!IsVisible) return;
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(110)) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            if (_view.Opacity < 0.05) Hide();
        };
        _view.BeginAnimation(OpacityProperty, fade);
        var scale = (ScaleTransform)_view.RenderTransform;
        var shrink = new DoubleAnimation(0.9, TimeSpan.FromMilliseconds(110)) { EasingFunction = ease };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
    }
}
