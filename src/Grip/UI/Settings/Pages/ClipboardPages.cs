using System.Windows;
using System.Windows.Controls;
using Grip.Core.Actions;
using Grip.Core.Features;
using Grip.Core.Localization;
using Grip.Core.Settings;
using Grip.Core.Text;
using Grip.UI.Common;
using Grip.UI.Controls;

namespace Grip.UI.Settings.Pages;

public static class ClipboardPages
{
    private static Style S(string key) => (Style)Application.Current.FindResource(key);

    public static void BuildClipboard(PageBuilder b)
    {
        var settings = App.Services.Settings.Current;
        var loc = Localizer.Instance;

        if (settings.IsInstalled(FeatureIds.ClipboardHistory))
        {
            b.Card();
            b.Toggle("settings.clipboard.history", "settings.clipboard.history.desc", s => s.Clipboard.HistoryEnabled,
                (s, v) => s.Clipboard.HistoryEnabled = v, icon: "Clipboard");
            b.Hotkey(ActionIds.ClipboardHistory);
            b.Choice("settings.clipboard.maxItems", "settings.clipboard.maxItems.desc",
                new[] { 50, 100, 200, 500, 1000, 2000 }.Select(n => (n, loc.Count("clipboard.items", n))),
                s => s.Clipboard.MaxItems, (s, v) => s.Clipboard.MaxItems = v, icon: "History");
            b.Choice("settings.clipboard.retention", null, new[]
            {
                (0, L.S("settings.clipboard.retention.forever")),
                (1, loc.Count("time.days", 1)),
                (7, loc.Count("time.days", 7)),
                (30, loc.Count("time.days", 30)),
                (90, loc.Count("time.days", 90)),
            }, s => s.Clipboard.RetentionDays, (s, v) => s.Clipboard.RetentionDays = v, icon: "Timer");
            b.Toggle("settings.clipboard.images", null, s => s.Clipboard.SaveImages, (s, v) => s.Clipboard.SaveImages = v, icon: "Image");
            b.Toggle("settings.clipboard.files", "settings.clipboard.files.desc", s => s.Clipboard.SaveFiles, (s, v) => s.Clipboard.SaveFiles = v, icon: "Document");
            b.Toggle("settings.clipboard.keep", "settings.clipboard.keep.desc", s => s.Clipboard.KeepAfterRestart,
                (s, v) => s.Clipboard.KeepAfterRestart = v, icon: "Archive");

            b.Card("clipboard.title");
            b.Toggle("settings.clipboard.pasteOnSelect", "settings.clipboard.pasteOnSelect.desc", s => s.Clipboard.PasteOnSelect,
                (s, v) => s.Clipboard.PasteOnSelect = v, icon: "ClipboardPaste");
            b.Choice("settings.clipboard.position", null, new[]
            {
                (ClipboardPopupPosition.Cursor, L.S("settings.clipboard.position.cursor")),
                (ClipboardPopupPosition.Center, L.S("settings.clipboard.position.center")),
            }, s => s.Clipboard.PopupPosition, (s, v) => s.Clipboard.PopupPosition = v, icon: "Cursor");
            b.Toggle("settings.clipboard.preview", null, s => s.Clipboard.ShowPreview, (s, v) => s.Clipboard.ShowPreview = v, icon: "Eye");

            b.Card("settings.clipboard.ignored", "settings.clipboard.ignored.desc");
            b.Custom(ListEditor(
                () => App.Services.Settings.Current.Clipboard.IgnoredApps,
                list => App.Services.Settings.Update(s => s.Clipboard.IgnoredApps = list),
                L.S("settings.clipboard.ignored.placeholder")), L.S("settings.clipboard.ignored"), L.S("settings.clipboard.ignored.desc"));

            b.Card("settings.clipboard.autoClear", "settings.clipboard.autoClear.desc");
            b.Toggle("settings.clipboard.autoClear", null, s => s.Clipboard.AutoClearEnabled, (s, v) => s.Clipboard.AutoClearEnabled = v, icon: "Broom");
            b.Choice("settings.clipboard.autoClear.after", null,
                new[] { 15, 30, 60, 120, 300, 600, 1800 }.Select(sec => (sec, loc.Duration(TimeSpan.FromSeconds(sec)))),
                s => s.Clipboard.AutoClearSeconds, (s, v) => s.Clipboard.AutoClearSeconds = v, icon: "Timer");
            b.Toggle("settings.clipboard.clearOnLock", null, s => s.Clipboard.ClearOnLock, (s, v) => s.Clipboard.ClearOnLock = v, icon: "LockClosed");
            b.Toggle("settings.clipboard.clearOnSleep", null, s => s.Clipboard.ClearOnSleep, (s, v) => s.Clipboard.ClearOnSleep = v, icon: "Sleep");
        }

        if (settings.IsInstalled(FeatureIds.PastePlain))
        {
            b.Card("feature.pastePlain.title", "settings.clipboard.pastePlain.desc");
            b.Hotkey(ActionIds.PastePlain);
        }

        if (settings.IsInstalled(FeatureIds.ClipboardHistory))
        {
            b.Card();
            var history = App.Services.Clipboard.History;
            b.Button("settings.clipboard.clearHistory", null, "common.clear", () =>
            {
                if (ConfirmDialog.Ask(L.S("settings.clipboard.clearHistory.confirm"), okText: L.S("common.clear"), destructive: true))
                {
                    history.Clear(keepPinned: true);
                    App.Services.Hud.Show(L.S("clipboard.historyCleared"), "Broom", HudTone.Neutral);
                }
            }, destructive: true, icon: "Broom", rowIcon: "Delete");
            b.Note(L.F("settings.clipboard.stats", history.Count, history.PinnedCount));
        }
    }

    public static void BuildLinks(PageBuilder b)
    {
        b.Card();
        b.Toggle("settings.links.auto", "settings.links.auto.desc", s => s.UrlCleaner.AutoClean, (s, v) => s.UrlCleaner.AutoClean = v, icon: "Link");
        b.Hotkey(ActionIds.CleanUrl);
        b.Toggle("settings.links.unwrap", "settings.links.unwrap.desc", s => s.UrlCleaner.UnwrapRedirects, (s, v) => s.UrlCleaner.UnwrapRedirects = v, icon: "Open");
        b.Toggle("settings.links.search", "settings.links.search.desc", s => s.UrlCleaner.CleanSearchLinks, (s, v) => s.UrlCleaner.CleanSearchLinks = v, icon: "Search");
        b.Toggle("settings.links.hud", "settings.links.hud.desc", s => s.UrlCleaner.ShowHud, (s, v) => s.UrlCleaner.ShowHud = v, icon: "Alert");

        b.Card();
        b.Text("settings.links.extra", "settings.links.extra.desc",
            s => string.Join(", ", s.UrlCleaner.ExtraParameters),
            (s, v) => s.UrlCleaner.ExtraParameters = SplitList(v), placeholder: "ref, source");
        b.Text("settings.links.skip", "settings.links.skip.desc",
            s => string.Join(", ", s.UrlCleaner.SkipDomains),
            (s, v) => s.UrlCleaner.SkipDomains = SplitList(v), placeholder: "example.com");

        b.Card("settings.links.try");
        b.Custom(TryIt(), L.S("settings.links.try"));
    }

    public static void BuildShelf(PageBuilder b)
    {
        b.Card();
        b.Hotkey(ActionIds.Shelf);
        b.Toggle("settings.shelf.shake", "settings.shelf.shake.desc", s => s.Shelf.ShakeToOpen, (s, v) => s.Shelf.ShakeToOpen = v, icon: "Cursor");
        b.Toggle("settings.shelf.nearCursor", "settings.shelf.nearCursor.desc", s => s.Shelf.OpenNearCursor, (s, v) => s.Shelf.OpenNearCursor = v, icon: "Navigation");
        b.Toggle("settings.shelf.keep", null, s => s.Shelf.KeepItemsAfterDragOut, (s, v) => s.Shelf.KeepItemsAfterDragOut = v, icon: "Pin");
    }

    private static List<string> SplitList(string text) =>
        text.Split(new[] { ',', ';', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>A live "paste a link, see it cleaned" box.</summary>
    private static FrameworkElement TryIt()
    {
        var stack = new StackPanel();
        var input = new TextBox { Style = S("Grip.TextBox") };
        TextBoxHelper.SetPlaceholder(input, L.S("settings.links.try.placeholder"));
        TextBoxHelper.SetIcon(input, "Link");
        var output = new TextBlock { Style = S("Grip.Text.Mono"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(2, 10, 2, 0) };
        var verdict = new TextBlock { Style = S("Grip.Text.Secondary"), Margin = new Thickness(2, 6, 2, 0) };
        input.TextChanged += (_, _) =>
        {
            var text = input.Text.Trim();
            if (text.Length == 0)
            {
                output.Text = "";
                verdict.Text = "";
                return;
            }
            if (!UrlCleaner.IsHttpUrl(text))
            {
                output.Text = "";
                verdict.Text = L.S("settings.links.try.invalid");
                verdict.SetResourceReference(TextBlock.ForegroundProperty, "Grip.TextTertiary");
                return;
            }
            var result = UrlCleaner.Clean(text, App.Services.Clipboard.CleanerOptions());
            output.Text = result.Url;
            output.SetResourceReference(TextBlock.ForegroundProperty, result.Changed ? "Grip.AccentText" : "Grip.TextSecondary");
            var parts = new List<string>();
            if (result.Unwrapped) parts.Add(L.S("settings.links.try.unwrapped"));
            if (result.RemovedCount > 0) parts.Add(L.F("settings.links.try.removed", string.Join(", ", result.RemovedNames.Distinct())));
            verdict.Text = parts.Count > 0 ? string.Join(" · ", parts) : L.S("settings.links.try.clean");
            verdict.SetResourceReference(TextBlock.ForegroundProperty, result.Changed ? "Grip.Success" : "Grip.TextTertiary");
        };
        stack.Children.Add(input);
        stack.Children.Add(output);
        stack.Children.Add(verdict);
        return stack;
    }

    /// <summary>An add/remove list of short strings (app names).</summary>
    public static FrameworkElement ListEditor(Func<List<string>> get, Action<List<string>> set, string placeholder)
    {
        var root = new StackPanel();
        var chips = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        var input = new TextBox { Style = S("Grip.TextBox"), Width = 260 };
        TextBoxHelper.SetPlaceholder(input, placeholder);
        var add = new Button { Style = S("Grip.Button"), Content = L.S("common.add"), Margin = new Thickness(8, 0, 0, 0) };
        Ui.SetIcon(add, "Add");

        void Fill()
        {
            chips.Children.Clear();
            foreach (var item in get())
            {
                var chip = new Border { Style = S("Grip.Pill"), Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(10, 3, 4, 3) };
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(new TextBlock { Text = item, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
                var remove = new Button { Style = S("Grip.Button.Icon"), Width = 20, Height = 20, Margin = new Thickness(4, 0, 0, 0), ToolTip = L.S("common.remove") };
                Ui.SetIcon(remove, "Dismiss");
                var captured = item;
                remove.Click += (_, _) =>
                {
                    set(get().Where(x => !x.Equals(captured, StringComparison.OrdinalIgnoreCase)).ToList());
                    Fill();
                };
                row.Children.Add(remove);
                chip.Child = row;
                chips.Children.Add(chip);
            }
        }

        void Add()
        {
            var value = input.Text.Trim();
            if (value.Length == 0) return;
            var list = get().ToList();
            if (!list.Contains(value, StringComparer.OrdinalIgnoreCase)) list.Add(value);
            set(list);
            input.Text = "";
            Fill();
        }

        add.Click += (_, _) => Add();
        input.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) Add();
        };
        Fill();
        root.Children.Add(chips);
        var line = new StackPanel { Orientation = Orientation.Horizontal };
        line.Children.Add(input);
        line.Children.Add(add);
        root.Children.Add(line);
        return root;
    }
}
