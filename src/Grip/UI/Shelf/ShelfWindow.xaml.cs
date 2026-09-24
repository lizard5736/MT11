using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Grip.Core.Localization;
using Grip.Core.Text;
using Grip.Interop;
using Grip.Services;
using Grip.UI.Common;

namespace Grip.UI.Shelf;

public enum ShelfItemKind { File, Folder, Text, Link }

/// <summary>Something parked on the shelf.</summary>
public sealed class ShelfItem
{
    public ShelfItemKind Kind { get; init; }
    public string Value { get; init; } = "";
    public ImageSource? Image { get; set; }

    public bool IsFile => Kind is ShelfItemKind.File or ShelfItemKind.Folder;
    public bool HasImage => Image != null;

    public string Name => Kind switch
    {
        ShelfItemKind.File or ShelfItemKind.Folder => Path.GetFileName(Value.TrimEnd('\\')) is { Length: > 0 } n ? n : Value,
        ShelfItemKind.Link => Uri.TryCreate(Value, UriKind.Absolute, out var uri) ? uri.Host : L.S("shelf.link"),
        _ => Core.Clipboard.ClipText.Preview(Value, 40),
    };

    public string Icon => Kind switch
    {
        ShelfItemKind.Folder => "Folder",
        ShelfItemKind.Link => "Link",
        ShelfItemKind.Text => "TextT",
        _ => "Document",
    };

    public string Tooltip => Kind == ShelfItemKind.Text ? Core.Clipboard.ClipText.Preview(Value, 300) : Value;

    public DataObject ToData()
    {
        var data = new DataObject();
        if (IsFile)
        {
            var list = new StringCollection { Value };
            data.SetFileDropList(list);
        }
        else
        {
            data.SetText(Value);
        }
        return data;
    }
}

/// <summary>
/// A parking spot for drag and drop: drop files, text or links on it, then
/// drag them back out wherever they need to go. It never steals focus, so
/// a drag in progress keeps going.
/// </summary>
public partial class ShelfWindow : Window
{
    private readonly ObservableCollection<ShelfItem> _items = new();
    private Point _dragStart;
    private ShelfItem? _dragItem;

    public bool IsPreview { get; init; }

    public bool Pinned => PinToggle.IsChecked == true;

    public ShelfWindow()
    {
        InitializeComponent();
        Items.ItemsSource = _items;
        _items.CollectionChanged += (_, _) => Sync();
        SourceInitialized += (_, _) =>
        {
            if (IsPreview) return;
            WindowStyling.MakeToolWindow(this);
            var theme = App.Services.Theme;
            WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
        };
        DragEnter += OnDragOver;
        DragOver += OnDragOver;
        DragLeave += (_, _) => Frame.SetResourceReference(Border.BorderBrushProperty, "Grip.Line");
        Drop += OnDrop;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) CloseSoft();
        };
        Sync();
    }

    public IList<ShelfItem> ItemsList => _items;

    private void Sync()
    {
        DropZone.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ItemsScroller.Visibility = _items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        CountText.Text = Localizer.Instance.Count("shelf.items", _items.Count);
    }

    /// <summary>Shows the shelf without taking focus, at a physical-pixel point.</summary>
    internal void ShowAt(NativeMethods.POINT anchor, bool nearCursor)
    {
        bool wasHidden = !IsVisible;
        if (wasHidden) PopupMotion.PrepareEnter(Frame, FrameShift);
        UpdateLayout();
        var area = Screens.FromPoint(anchor);
        var (w, h) = WindowPlacement.PhysicalSize(this);
        int margin = (int)(16 * area.Scale);
        int x, y;
        if (nearCursor)
        {
            x = anchor.X + (int)(40 * area.Scale);
            y = anchor.Y - h / 2;
            if (x + w > area.Work.Right - margin) x = anchor.X - w - (int)(40 * area.Scale);
        }
        else
        {
            x = area.Work.Right - w - margin;
            y = area.Work.Top + (area.Work.Height - h) / 2;
        }
        x = Math.Clamp(x, area.Work.Left + margin, area.Work.Right - w - margin);
        y = Math.Clamp(y, area.Work.Top + margin, area.Work.Bottom - h - margin);
        // Position before Show(): otherwise the first frame paints at whatever spot
        // Windows' default placement picks (near the screen's top-left) for an instant.
        WindowPlacement.MoveTo(this, x, y);
        if (wasHidden)
        {
            Show();
            PopupMotion.Enter(Frame, FrameShift);
        }
    }

    /// <summary>Fades out and hides — for the user dismissing the shelf, not for dragging its last item out.</summary>
    private void CloseSoft()
    {
        if (!IsVisible) return;
        PopupMotion.Exit(Frame, FrameShift, Hide);
    }

    public void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (_items.Any(i => i.IsFile && i.Value.Equals(path, StringComparison.OrdinalIgnoreCase))) continue;
            var item = new ShelfItem { Kind = Directory.Exists(path) ? ShelfItemKind.Folder : ShelfItemKind.File, Value = path };
            item.Image = App.Services.AppCatalog.Icon(path);
            _items.Add(item);
        }
        Items.Items.Refresh();
    }

    public void AddTextItem(string text)
    {
        text = text.Trim();
        if (text.Length == 0) return;
        _items.Add(new ShelfItem { Kind = UrlCleaner.IsHttpUrl(text) ? ShelfItemKind.Link : ShelfItemKind.Text, Value = text });
    }

    public void RefreshIcons()
    {
        foreach (var item in _items.Where(i => i.IsFile && i.Image == null)) item.Image = App.Services.AppCatalog.Icon(item.Value);
        Items.Items.Refresh();
    }

    // ---------- dropping in ----------

    private void OnDragOver(object sender, DragEventArgs e)
    {
        bool accept = e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.UnicodeText);
        e.Effects = accept ? (e.AllowedEffects.HasFlag(DragDropEffects.Copy) ? DragDropEffects.Copy : DragDropEffects.Link) : DragDropEffects.None;
        Frame.SetResourceReference(Border.BorderBrushProperty, accept ? "Grip.Accent" : "Grip.Line");
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        Frame.SetResourceReference(Border.BorderBrushProperty, "Grip.Line");
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
        else if (e.Data.GetData(DataFormats.UnicodeText) is string text) AddTextItem(text);
        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    // ---------- dragging out ----------

    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragItem = (sender as FrameworkElement)?.DataContext as ShelfItem;
    }

    private void OnItemMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem == null) return;
        var delta = e.GetPosition(this) - _dragStart;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var item = _dragItem;
        _dragItem = null;
        var result = DragDrop.DoDragDrop((DependencyObject)sender, item.ToData(), DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
        if (result != DragDropEffects.None && !App.Services.Settings.Current.Shelf.KeepItemsAfterDragOut)
        {
            _items.Remove(item);
            if (_items.Count == 0 && !Pinned) CloseSoft();
        }
    }

    // ---------- commands ----------

    private static ShelfItem? ItemOf(object sender) => (sender as FrameworkElement)?.DataContext as ShelfItem;

    private void OnShowInFolder(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { IsFile: true } item)
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.Value}\"");
    }

    private void OnCopyItem(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item)
        {
            try { System.Windows.Clipboard.SetDataObject(item.ToData(), true); }
            catch (System.Runtime.InteropServices.COMException) { }
        }
    }

    private void OnRemoveItem(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item) _items.Remove(item);
    }

    private void OnClear(object sender, RoutedEventArgs e) => _items.Clear();

    private void OnClose(object sender, RoutedEventArgs e) => CloseSoft();

    private void OnPin(object sender, RoutedEventArgs e) => Ui.SetIcon(PinToggle, Pinned ? "PinFilled" : "Pin");

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && fe.TemplatedParent is System.Windows.Controls.Primitives.ButtonBase) return;
        try { DragMove(); }
        catch (InvalidOperationException) { }
    }
}
