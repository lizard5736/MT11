using System.Windows;
using System.Windows.Controls;
using Grip.Core.Actions;
using Grip.Core.Clipboard;
using Grip.Core.Input;
using Grip.UI.Clipboard;
using Grip.UI.Common;

namespace Grip.UI.Panel;

/// <summary>The latest clipboard entries right in the panel: click to copy again.</summary>
public partial class ClipboardSection : PanelSection
{
    private const int MaxRows = 8;

    public ClipboardSection()
    {
        InitializeComponent();
        S.Clipboard.HistoryChanged += (_, _) =>
        {
            if (IsVisible) Refresh();
        };
    }

    public override void Refresh()
    {
        var settings = S.Settings.Current.Clipboard;
        DisabledPanel.Visibility = settings.HistoryEnabled ? Visibility.Collapsed : Visibility.Visible;
        HistoryPanel.Visibility = settings.HistoryEnabled ? Visibility.Visible : Visibility.Collapsed;
        if (!settings.HistoryEnabled) return;

        var items = S.Clipboard.History.Query(SearchBox.Text, ClipFilter.All).Take(MaxRows).Select(e => new ClipItem(e)).ToList();
        Items.ItemsSource = items;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Text = string.IsNullOrWhiteSpace(SearchBox.Text) ? L.S("clipboard.empty") : L.S("clipboard.noResults");

        var hotkey = Hotkey.Parse(S.Settings.Current.Hotkeys.GetValueOrDefault(ActionIds.ClipboardHistory));
        OpenHistoryButton.Content = hotkey.IsEmpty ? L.S("clipboard.openHistory") : $"{L.S("clipboard.openHistory")}   {hotkey}";
    }

    private void OnSearch(object sender, TextChangedEventArgs e) => Refresh();

    private static ClipItem? ItemOf(object sender) => sender switch
    {
        FrameworkElement { Tag: ClipItem item } => item,
        FrameworkElement { DataContext: ClipItem item } => item,
        _ => null,
    };

    private void OnItemClick(object sender, RoutedEventArgs e) => Copy(ItemOf(sender));

    private void OnCopy(object sender, RoutedEventArgs e) => Copy(ItemOf(sender));

    private void Copy(ClipItem? item)
    {
        if (item == null) return;
        if (S.Clipboard.SetEntry(item.Entry))
        {
            S.Clipboard.History.Promote(item.Entry.Id, DateTimeOffset.Now);
            S.Hud.Show(L.S("common.copied"), "Copy", HudTone.Success);
        }
    }

    private void OnPin(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item) S.Clipboard.History.SetPinned(item.Entry.Id, !item.Entry.Pinned);
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (ItemOf(sender) is { } item) S.Clipboard.History.Remove(item.Entry.Id);
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        if (ConfirmDialog.Ask(L.S("settings.clipboard.clearHistory.confirm"), okText: L.S("common.clear"), destructive: true))
        {
            S.Clipboard.History.Clear(keepPinned: true);
            S.Hud.Show(L.S("clipboard.historyCleared"), "Broom", HudTone.Neutral);
        }
    }

    private void OnOpenHistory(object sender, RoutedEventArgs e)
    {
        S.Panel.Hide();
        S.Execute(ActionIds.ClipboardHistory);
    }

    private void OnEnable(object sender, RoutedEventArgs e)
    {
        S.Settings.Update(s => s.Clipboard.HistoryEnabled = true);
        Refresh();
    }
}
