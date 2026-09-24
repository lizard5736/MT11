using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Core.Features;
using Grip.Core.Localization;
using Grip.Core.Search;
using Grip.Core.Settings;
using Grip.Interop;
using Grip.UI.Settings.Pages;

namespace Grip.UI.Settings;

public sealed record SettingsPageDef(
    string Id,
    string Icon,
    string Group,
    Func<AppSettings, bool> IsAvailable,
    Action<PageBuilder> Build)
{
    public string TitleKey => $"settings.page.{Id}";
    public string DescriptionKey => $"settings.page.{Id}.desc";

    public static IReadOnlyList<SettingsPageDef> All { get; } = new List<SettingsPageDef>
    {
        new("general", "Settings", "essentials", _ => true, GeneralPage.Build),
        new("features", "Apps", "essentials", _ => true, FeaturesPage.Build),
        new("access", "Shield", "essentials", _ => true, AccessPage.Build),
        new("hotkeys", "Keyboard", "essentials", _ => true, HotkeysPage.Build),
        new("clipboard", "Clipboard", "clipboard", s => s.IsInstalled(FeatureIds.ClipboardHistory) || s.IsInstalled(FeatureIds.PastePlain), ClipboardPages.BuildClipboard),
        new("links", "Link", "clipboard", s => s.IsInstalled(FeatureIds.UrlCleaner), ClipboardPages.BuildLinks),
        new("shelf", "TrayItemAdd", "clipboard", s => s.IsInstalled(FeatureIds.Shelf), ClipboardPages.BuildShelf),
        new("commandBar", "WindowConsole", "tools", s => s.IsInstalled(FeatureIds.CommandBar), ToolPages.BuildCommandBar),
        new("radial", "DataPie", "tools", s => s.IsInstalled(FeatureIds.RadialMenu), RadialPage.Build),
        new("toggles", "Grid", "tools", s => s.IsInstalled(FeatureIds.QuickToggles), ToolPages.BuildToggles),
        new("keepAwake", "WeatherMoon", "energy", s => s.IsInstalled(FeatureIds.KeepAwake), ToolPages.BuildKeepAwake),
        new("about", "Info", "about", _ => true, AboutPage.Build),
    };

    public static readonly string[] Groups = { "essentials", "clipboard", "tools", "energy" };
}

/// <summary>
/// Settings: a sidebar of pages grouped like the panel, a search that finds
/// any row on any page, and pages that rebuild instantly when something
/// they depend on (language, installed features) changes.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly List<SearchEntry> _index = new();
    private string _page = "general";
    private bool _rebuildPending;

    public SettingsWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyChrome();
        App.Services.Theme.ThemeChanged += OnThemeChanged;
        Localizer.Instance.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) =>
        {
            App.Services.Theme.ThemeChanged -= OnThemeChanged;
            Localizer.Instance.LanguageChanged -= OnLanguageChanged;
        };
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyChrome();

    private void OnLanguageChanged(object? sender, EventArgs e) => RequestRebuild();

    private void ApplyChrome()
    {
        var theme = App.Services.Theme;
        WindowStyling.ApplyChrome(this, theme.Color("BgAlt"), theme.Color("Text"), theme.Color("Line"), theme.IsDark, round: false);
    }

    /// <summary>Shows the window on a page (or the last one).</summary>
    public void Open(string? page)
    {
        if (page != null) _page = page;
        BuildNav();
        ShowPage(_page);
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        WindowStyling.ForceForeground(this);
        Activate();
    }

    /// <summary>Preview renderer: builds a page without showing the window.</summary>
    public void PreviewPage(string page)
    {
        _page = page;
        BuildNav();
        ShowPage(page);
    }

    /// <summary>Rebuilds nav and page on the next idle tick (coalesces bursts of changes).</summary>
    public void RequestRebuild()
    {
        if (_rebuildPending) return;
        _rebuildPending = true;
        Dispatcher.BeginInvoke(() =>
        {
            _rebuildPending = false;
            double offset = PageScroller.VerticalOffset;
            BuildNav();
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) ShowPage(_page);
            else ShowSearch(SearchBox.Text);
            PageScroller.ScrollToVerticalOffset(offset);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void BuildNav()
    {
        Nav.Children.Clear();
        var settings = App.Services.Settings.Current;
        foreach (var group in SettingsPageDef.Groups)
        {
            var pages = SettingsPageDef.All.Where(p => p.Group == group && p.IsAvailable(settings)).ToList();
            if (pages.Count == 0) continue;
            Nav.Children.Add(new TextBlock
            {
                Text = L.S("settings.nav." + group),
                Style = (Style)FindResource("Grip.Text.Caption"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(12, Nav.Children.Count == 0 ? 0 : 16, 0, 6),
            });
            foreach (var page in pages)
            {
                var item = new ToggleButton
                {
                    Style = (Style)FindResource("Grip.NavItem"),
                    Content = L.S(page.TitleKey),
                    Tag = page.Id,
                    IsChecked = page.Id == _page,
                };
                Ui.SetIcon(item, page.Icon);
                item.Click += OnNavClick;
                Nav.Children.Add(item);
            }
        }
        AboutNav.IsChecked = _page == "about";
    }

    private void OnNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: string id })
        {
            SearchBox.Text = "";
            ShowPage(id);
        }
    }

    public void ShowPage(string id)
    {
        var settings = App.Services.Settings.Current;
        var def = SettingsPageDef.All.FirstOrDefault(p => p.Id == id && p.IsAvailable(settings))
                  ?? SettingsPageDef.All[0];
        _page = def.Id;
        foreach (var item in Nav.Children.OfType<ToggleButton>()) item.IsChecked = (string)item.Tag == _page;
        AboutNav.IsChecked = _page == "about";

        PageTitle.Text = L.S(def.TitleKey);
        PageDescription.Text = L.S(def.DescriptionKey);
        PageDescription.Visibility = Visibility.Visible;
        var builder = new PageBuilder(def.Id, new List<SearchEntry>());
        def.Build(builder);
        PageContent.Content = builder.Root;
        PageScroller.ScrollToTop();
    }

    // ---------- search ----------

    private void OnSearch(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SearchBox.Text)) ShowPage(_page);
        else ShowSearch(SearchBox.Text);
    }

    private void ShowSearch(string query)
    {
        // Build every page once into a throwaway builder just to index its rows.
        _index.Clear();
        var settings = App.Services.Settings.Current;
        foreach (var def in SettingsPageDef.All.Where(p => p.IsAvailable(settings)))
        {
            var builder = new PageBuilder(def.Id, _index);
            def.Build(builder);
            _index.Add(new SearchEntry(def.Id, L.S(def.TitleKey), L.S(def.DescriptionKey), builder.Root));
        }

        var q = query.Trim();
        var matches = _index
            .Select(entry => (entry, score: Math.Max(FuzzyMatcher.Score(q, entry.Title), FuzzyMatcher.Score(q, entry.Description) * 8 / 10)))
            .Where(x => x.score >= 500)
            .OrderByDescending(x => x.score)
            .Take(30)
            .ToList();

        PageTitle.Text = L.S("settings.results");
        PageDescription.Visibility = Visibility.Collapsed;
        foreach (var item in Nav.Children.OfType<ToggleButton>()) item.IsChecked = false;

        var list = new StackPanel();
        if (matches.Count == 0)
            list.Children.Add(new TextBlock { Text = L.S("settings.noResults"), Style = (Style)FindResource("Grip.Text.Secondary"), Margin = new Thickness(2, 8, 0, 0) });

        foreach (var (entry, _) in matches)
        {
            var page = SettingsPageDef.All.First(p => p.Id == entry.PageId);
            var button = new Button { Style = (Style)FindResource("Grip.Button.Row"), Margin = new Thickness(0, 0, 0, 8) };
            var dock = new DockPanel();
            var crumb = new TextBlock { Text = L.S(page.TitleKey), Style = (Style)FindResource("Grip.Text.Caption"), VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock(crumb, Dock.Right);
            dock.Children.Add(crumb);
            var glyph = new Controls.Icon { Kind = page.Icon, Size = 18, Margin = new Thickness(0, 0, 12, 0) };
            glyph.SetResourceReference(Controls.Icon.ForegroundProperty, "Grip.Accent");
            dock.Children.Add(glyph);
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = entry.Title, Style = (Style)FindResource("Grip.Text.Strong") });
            if (!string.IsNullOrEmpty(entry.Description))
                text.Children.Add(new TextBlock { Text = entry.Description, Style = (Style)FindResource("Grip.Text.Secondary"), Margin = new Thickness(0, 2, 12, 0) });
            dock.Children.Add(text);
            button.Content = dock;
            var title = entry.Title;
            button.Click += (_, _) => JumpTo(page.Id, title);
            list.Children.Add(button);
        }
        PageContent.Content = list;
        PageScroller.ScrollToTop();
    }

    /// <summary>Opens a page and scrolls the matching row into view with a brief highlight.</summary>
    private void JumpTo(string pageId, string title)
    {
        SearchBox.TextChanged -= OnSearch;
        SearchBox.Text = "";
        SearchBox.TextChanged += OnSearch;
        var entries = new List<SearchEntry>();
        var def = SettingsPageDef.All.First(p => p.Id == pageId);
        _page = pageId;
        foreach (var item in Nav.Children.OfType<ToggleButton>()) item.IsChecked = (string)item.Tag == _page;
        AboutNav.IsChecked = _page == "about";
        PageTitle.Text = L.S(def.TitleKey);
        PageDescription.Text = L.S(def.DescriptionKey);
        PageDescription.Visibility = Visibility.Visible;
        var builder = new PageBuilder(def.Id, entries);
        def.Build(builder);
        PageContent.Content = builder.Root;
        var target = entries.FirstOrDefault(e => e.Title == title)?.Target;
        if (target == null) return;
        Dispatcher.BeginInvoke(() =>
        {
            target.BringIntoView();
            PageBuilder.Flash(target);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }
}
