using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Grip.Core.Clipboard;
using Grip.Core.Settings;
using Grip.Interop;

namespace Grip.UI.Clipboard;

/// <summary>
/// The clipboard history pop-up: type to search, arrows to move, Enter to
/// paste into the app you were in, Ctrl+1…9 for the first nine entries.
/// </summary>
public partial class ClipboardWindow : Window
{
    private ClipFilter _filter = ClipFilter.All;
    private IntPtr _target;

    /// <summary>Preview mode renders without positioning or focus handling.</summary>
    public bool IsPreview { get; init; }

    public ClipboardWindow()
    {
        InitializeComponent();
        BuildFilters();
        SourceInitialized += (_, _) =>
        {
            if (IsPreview) return;
            WindowStyling.MakeToolWindow(this);
            var theme = App.Services.Theme;
            WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
        };
        Deactivated += (_, _) =>
        {
            if (!IsPreview) Hide();
        };
        PreviewKeyDown += OnKey;
        App.Services.Clipboard.HistoryChanged += (_, _) =>
        {
            if (IsVisible) Refresh(keepSelection: true);
        };
    }

    public void Toggle(IntPtr target)
    {
        if (IsVisible)
        {
            Hide();
            return;
        }
        // Never "paste back" into Grip itself.
        _target = target;
        if (ProcessInfo.ExeNameForWindow(target)?.Equals(System.IO.Path.GetFileName(ProcessInfo.CurrentExePath), StringComparison.OrdinalIgnoreCase) == true)
            _target = IntPtr.Zero;

        SearchBox.Text = "";
        _filter = ClipFilter.All;
        SyncFilterChips();
        Refresh(keepSelection: false);
        Show();
        Position();
        WindowStyling.ForceForeground(this);
        Activate();
        SearchBox.Focus();
    }

    /// <summary>Preview renderer: fills the list without showing the window.</summary>
    public void PreviewFill()
    {
        Refresh(keepSelection: false);
        var settings = App.Services.Settings.Current.Clipboard;
        PreviewColumn.Width = settings.ShowPreview ? new GridLength(300) : new GridLength(0);
    }

    private void Position()
    {
        var settings = App.Services.Settings.Current.Clipboard;
        PreviewColumn.Width = settings.ShowPreview ? new GridLength(300) : new GridLength(0);
        PreviewPane.Visibility = settings.ShowPreview ? Visibility.Visible : Visibility.Collapsed;
        Width = settings.ShowPreview ? 760 : 470;
        var cursor = Screens.CursorPosition();
        if (settings.PopupPosition == ClipboardPopupPosition.Center)
            WindowPlacement.PlaceOnMonitor(this, Screens.FromPoint(cursor), 0.4);
        else
            WindowPlacement.PlaceNear(this, cursor, below: true, gap: 8);
    }

    // ---------- filters ----------

    private void BuildFilters()
    {
        var filters = new (ClipFilter Filter, string Key, string Icon)[]
        {
            (ClipFilter.All, "clipboard.filter.all", "History"),
            (ClipFilter.Text, "clipboard.filter.text", "TextT"),
            (ClipFilter.Links, "clipboard.filter.links", "Link"),
            (ClipFilter.Images, "clipboard.filter.images", "Image"),
            (ClipFilter.Files, "clipboard.filter.files", "Document"),
            (ClipFilter.Pinned, "clipboard.filter.pinned", "Pin"),
        };
        foreach (var (filter, key, icon) in filters)
        {
            var chip = new ToggleButton { Style = (Style)FindResource("Grip.Chip"), Content = L.S(key), Tag = filter, Focusable = false };
            Ui.SetIcon(chip, icon);
            chip.Click += (_, _) =>
            {
                _filter = filter;
                SyncFilterChips();
                Refresh(keepSelection: false);
                SearchBox.Focus();
            };
            Filters.Children.Add(chip);
        }
        SyncFilterChips();
    }

    private void SyncFilterChips()
    {
        foreach (ToggleButton chip in Filters.Children) chip.IsChecked = (ClipFilter)chip.Tag == _filter;
    }

    // ---------- list ----------

    private void OnSearch(object sender, TextChangedEventArgs e) => Refresh(keepSelection: false);

    private void Refresh(bool keepSelection)
    {
        var selectedId = (List.SelectedItem as ClipItem)?.Entry.Id;
        var entries = App.Services.Clipboard.History.Query(SearchBox.Text, _filter).Take(300).ToList();
        var items = entries.Select((e, i) => new ClipItem(e, i)).ToList();
        List.ItemsSource = items;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Text = App.Services.Clipboard.History.Count == 0 ? L.S("clipboard.empty") : L.S("clipboard.noResults");
        CountText.Text = Core.Localization.Localizer.Instance.Count("clipboard.items", App.Services.Clipboard.History.Count);

        var select = keepSelection ? items.FirstOrDefault(i => i.Entry.Id == selectedId) : null;
        List.SelectedItem = select ?? items.FirstOrDefault();
        if (List.SelectedItem != null) List.ScrollIntoView(List.SelectedItem);
        UpdatePreview();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        var item = List.SelectedItem as ClipItem;
        PreviewPane.IsEnabled = item != null;
        if (item == null)
        {
            PreviewText.Text = "";
            PreviewMeta.Text = "";
            PreviewImageHost.Visibility = Visibility.Collapsed;
            PreviewSwatch.Visibility = Visibility.Collapsed;
            return;
        }
        bool image = item.Entry.Kind == ClipKind.Image;
        PreviewImageHost.Visibility = image ? Visibility.Visible : Visibility.Collapsed;
        PreviewTextHost.Visibility = image ? Visibility.Collapsed : Visibility.Visible;
        PreviewImage.Source = image ? item.LargePreview : null;
        PreviewText.Text = item.FullText;
        PreviewText.Margin = item.HasColor ? new Thickness(0, 76, 0, 0) : new Thickness(0);
        PreviewSwatch.Visibility = item.HasColor ? Visibility.Visible : Visibility.Collapsed;
        if (item.HasColor) PreviewSwatch.Background = (Brush)new HexToBrushConverter().Convert(item.ColorHex!, typeof(Brush), null!, null!);
        PreviewMeta.Text = item.Meta;
        PinButton.Content = L.S(item.IsPinned ? "clipboard.unpin" : "clipboard.pin");
        Ui.SetIcon(PinButton, item.IsPinned ? "PinOff" : "Pin");
    }

    // ---------- keys ----------

    private void OnKey(object sender, KeyEventArgs e)
    {
        var item = List.SelectedItem as ClipItem;
        bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;
            case Key.Down:
                Move(+1);
                e.Handled = true;
                break;
            case Key.Up:
                Move(-1);
                e.Handled = true;
                break;
            case Key.PageDown:
                Move(+8);
                e.Handled = true;
                break;
            case Key.PageUp:
                Move(-8);
                e.Handled = true;
                break;
            case Key.Enter:
                if (item != null) Choose(item, plain: shift);
                e.Handled = true;
                break;
            case Key.Delete when item != null && (ctrl || SearchBox.Text.Length == 0 || !SearchBox.IsKeyboardFocused):
                App.Services.Clipboard.History.Remove(item.Entry.Id);
                e.Handled = true;
                break;
            case Key.P when ctrl && item != null:
                App.Services.Clipboard.History.SetPinned(item.Entry.Id, !item.Entry.Pinned);
                e.Handled = true;
                break;
            case Key.Tab:
                CycleFilter(shift ? -1 : +1);
                e.Handled = true;
                break;
            case >= Key.D1 and <= Key.D9 when ctrl:
            {
                int index = e.Key - Key.D1;
                if (List.Items.Count > index) Choose((ClipItem)List.Items[index], plain: shift);
                e.Handled = true;
                break;
            }
        }
    }

    private void Move(int delta)
    {
        if (List.Items.Count == 0) return;
        int index = Math.Clamp(List.SelectedIndex + delta, 0, List.Items.Count - 1);
        List.SelectedIndex = index;
        List.ScrollIntoView(List.SelectedItem);
    }

    private void CycleFilter(int delta)
    {
        var values = Enum.GetValues<ClipFilter>();
        int index = (Array.IndexOf(values, _filter) + delta + values.Length) % values.Length;
        _filter = values[index];
        SyncFilterChips();
        Refresh(keepSelection: false);
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (List.SelectedItem is ClipItem item) Choose(item, plain: Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    private void Choose(ClipItem item, bool plain)
    {
        var target = _target;
        Hide();
        _ = App.Services.Clipboard.PasteAsync(item.Entry, plain, target);
    }

    private void OnPin(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is ClipItem item) App.Services.Clipboard.History.SetPinned(item.Entry.Id, !item.Entry.Pinned);
        SearchBox.Focus();
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is ClipItem item) App.Services.Clipboard.History.Remove(item.Entry.Id);
        SearchBox.Focus();
    }
}
