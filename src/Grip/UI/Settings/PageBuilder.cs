using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Controls;

namespace Grip.UI.Settings;

/// <summary>A settings row the search box can find.</summary>
public sealed record SearchEntry(string PageId, string Title, string Description, FrameworkElement Target);

/// <summary>
/// Builds settings pages from rows (switch, choice, text, shortcut, button)
/// so every page shares one look and every row lands in the search index.
/// Values are read and written through lambdas on <see cref="AppSettings"/>;
/// each write goes through <see cref="SettingsService.Update"/>.
/// </summary>
public sealed class PageBuilder
{
    private readonly StackPanel _root = new();
    private StackPanel? _card;
    private readonly string _pageId;
    private readonly List<SearchEntry> _entries;

    public PageBuilder(string pageId, List<SearchEntry> entries)
    {
        _pageId = pageId;
        _entries = entries;
    }

    public FrameworkElement Root => _root;

    private static SettingsService Settings => App.Services.Settings;
    private static AppSettings Current => Settings.Current;

    private static Style Style(string key) => (Style)Application.Current.FindResource(key);

    // ---------- structure ----------

    /// <summary>Starts a new card, optionally with a caption above it.</summary>
    public PageBuilder Card(string? titleKey = null, string? descriptionKey = null)
    {
        if (titleKey != null)
        {
            var caption = new TextBlock
            {
                Text = L.S(titleKey),
                Style = Style("Grip.Text.Subtitle"),
                FontSize = 14,
                Margin = new Thickness(2, _root.Children.Count == 0 ? 0 : 18, 0, descriptionKey == null ? 8 : 2),
            };
            _root.Children.Add(caption);
            if (descriptionKey != null)
                _root.Children.Add(new TextBlock { Text = L.S(descriptionKey), Style = Style("Grip.Text.Secondary"), Margin = new Thickness(2, 0, 0, 8) });
        }
        _card = new StackPanel();
        var border = new Border { Style = Style("Grip.Card"), Padding = new Thickness(16, 4, 16, 4), Child = _card, Margin = new Thickness(0, 0, 0, 12) };
        _root.Children.Add(border);
        return this;
    }

    private StackPanel CurrentCard
    {
        get
        {
            if (_card == null) Card();
            return _card!;
        }
    }

    /// <summary>Adds any element as a full-width row.</summary>
    public PageBuilder Custom(FrameworkElement element, string? searchTitle = null, string? searchDescription = null)
    {
        AddDivider();
        element.Margin = new Thickness(0, 10, 0, 10);
        CurrentCard.Children.Add(element);
        if (searchTitle != null) _entries.Add(new SearchEntry(_pageId, searchTitle, searchDescription ?? "", element));
        return this;
    }

    /// <summary>Adds an element outside of any card.</summary>
    public PageBuilder Raw(FrameworkElement element)
    {
        _root.Children.Add(element);
        _card = null;
        return this;
    }

    private void AddDivider()
    {
        if (CurrentCard.Children.Count > 0)
            CurrentCard.Children.Add(new Border { Style = Style("Grip.Divider") });
    }

    /// <summary>Title and description on the left, a control on the right.</summary>
    public DockPanel Row(string title, string? description, FrameworkElement? control, string? icon = null)
    {
        AddDivider();
        var row = new DockPanel { Margin = new Thickness(0, 12, 0, 12), LastChildFill = true };
        if (control != null)
        {
            control.VerticalAlignment = VerticalAlignment.Center;
            control.Margin = new Thickness(16, 0, 0, 0);
            DockPanel.SetDock(control, Dock.Right);
            row.Children.Add(control);
        }
        if (icon != null)
        {
            var glyph = new Icon { Kind = icon, Size = 20, Margin = new Thickness(0, 1, 14, 0), VerticalAlignment = VerticalAlignment.Top };
            glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
            row.Children.Add(glyph);
        }
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, Style = Style("Grip.Text.Body"), TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrEmpty(description))
            text.Children.Add(new TextBlock { Text = description, Style = Style("Grip.Text.Secondary"), Margin = new Thickness(0, 3, 0, 0) });
        row.Children.Add(text);
        CurrentCard.Children.Add(row);
        _entries.Add(new SearchEntry(_pageId, title, description ?? "", row));
        return row;
    }

    // ---------- row types ----------

    public ToggleButton Toggle(string titleKey, string? descriptionKey, Func<AppSettings, bool> get, Action<AppSettings, bool> set,
        string? icon = null, Action? after = null)
    {
        var toggle = new ToggleButton { Style = Style("Grip.Toggle"), IsChecked = get(Current) };
        toggle.Click += (_, _) =>
        {
            Settings.Update(s => set(s, toggle.IsChecked == true));
            after?.Invoke();
        };
        Row(L.S(titleKey), descriptionKey == null ? null : L.S(descriptionKey), toggle, icon);
        return toggle;
    }

    public ComboBox Choice<T>(string titleKey, string? descriptionKey, IEnumerable<(T Value, string Label)> options,
        Func<AppSettings, T> get, Action<AppSettings, T> set, string? icon = null, double width = 200, Action? after = null)
    {
        var combo = ComboFor(options, get(Current), value =>
        {
            Settings.Update(s => set(s, value));
            after?.Invoke();
        }, width);
        Row(L.S(titleKey), descriptionKey == null ? null : L.S(descriptionKey), combo, icon);
        return combo;
    }

    public static ComboBox ComboFor<T>(IEnumerable<(T Value, string Label)> options, T selected, Action<T> onChange, double width = 200)
    {
        var combo = new ComboBox { Style = Style("Grip.ComboBox"), Width = width };
        foreach (var (value, label) in options)
        {
            var item = new ComboBoxItem { Content = label, Tag = value };
            combo.Items.Add(item);
            if (EqualityComparer<T>.Default.Equals(value, selected)) combo.SelectedItem = item;
        }
        if (combo.SelectedItem == null && combo.Items.Count > 0) combo.SelectedIndex = 0;
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem { Tag: T value }) onChange(value);
        };
        return combo;
    }

    public TextBox Text(string titleKey, string? descriptionKey, Func<AppSettings, string> get, Action<AppSettings, string> set,
        string? placeholder = null, double width = 260)
    {
        var box = new TextBox { Style = Style("Grip.TextBox"), Width = width, Text = get(Current) };
        if (placeholder != null) TextBoxHelper.SetPlaceholder(box, placeholder);
        box.LostKeyboardFocus += (_, _) => Settings.Update(s => set(s, box.Text.Trim()));
        box.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) Settings.Update(s => set(s, box.Text.Trim()));
        };
        Row(L.S(titleKey), descriptionKey == null ? null : L.S(descriptionKey), box);
        return box;
    }

    public HotkeyBox Hotkey(string actionId, string? titleOverride = null)
    {
        var box = new HotkeyBox { ActionId = actionId };
        Row(titleOverride ?? L.S(Core.Actions.ActionCatalog.TitleKey(actionId)), null, box);
        return box;
    }

    public Button Button(string titleKey, string? descriptionKey, string buttonTextKey, Action onClick,
        bool destructive = false, bool accent = false, string? icon = null, string? rowIcon = null)
    {
        var button = new Button
        {
            Style = Style(destructive ? "Grip.Button.Danger" : accent ? "Grip.Button.Accent" : "Grip.Button"),
            Content = L.S(buttonTextKey),
        };
        if (icon != null) Ui.SetIcon(button, icon);
        button.Click += (_, _) => onClick();
        Row(L.S(titleKey), descriptionKey == null ? null : L.S(descriptionKey), button, rowIcon);
        return button;
    }

    public TextBlock Note(string text)
    {
        var note = new TextBlock { Text = text, Style = Style("Grip.Text.Secondary"), Margin = new Thickness(0, 10, 0, 10) };
        AddDivider();
        CurrentCard.Children.Add(note);
        return note;
    }

    /// <summary>Visual cue when search jumps to a row.</summary>
    public static void Flash(FrameworkElement element)
    {
        var brush = (Brush)Application.Current.FindResource("Grip.AccentSubtle");
        if (element is System.Windows.Controls.Panel panel)
        {
            var original = panel.Background;
            panel.Background = brush;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                panel.Background = original;
            };
            timer.Start();
        }
    }
}
