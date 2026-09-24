using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;
using Grip.Core.Actions;
using Grip.Core.Calc;
using Grip.Core.Clipboard;
using Grip.Core.Localization;
using Grip.Core.Search;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Clipboard;
using Grip.UI.Common;

namespace Grip.UI.CommandBar;

/// <summary>One row in the Command Bar: either a section caption or a result.</summary>
public sealed class CommandRow
{
    public bool IsHeader { get; init; }
    public string Title { get; init; } = "";
    public string? Subtitle { get; init; }
    public string? Icon { get; init; }
    public ImageSource? Image { get; set; }
    public string? Glyph { get; init; }
    public string? Kind { get; init; }
    public int Score { get; init; }
    /// <summary>Runs the result; the window closes afterwards.</summary>
    public Action? Run { get; init; }
    /// <summary>Icon source to load lazily (shell path).</summary>
    public string? IconPath { get; init; }

    public bool HasImage => Image != null;
    public bool HasGlyph => Glyph != null;
    public bool HasIcon => Image == null && Glyph == null;

    public static CommandRow Header(string title) => new() { IsHeader = true, Title = title };
}

/// <summary>Turns a query into grouped results from every enabled source.</summary>
public sealed class CommandBarEngine
{
    private readonly AppServices _services;

    public CommandBarEngine(AppServices services) => _services = services;

    /// <summary>The window the Command Bar was opened over; results paste or act there.</summary>
    public IntPtr Target { get; set; }

    public string? CorrectedQuery { get; private set; }

    private List<WindowEntry> _windows = new();

    /// <summary>Captures the open windows once per session instead of on every keystroke.</summary>
    public void TakeSnapshot() => _windows = WindowList.Current();

    public List<CommandRow> Search(string query)
    {
        CorrectedQuery = null;
        query = query.Trim();
        var rows = Build(query);
        var s = _services.Settings.Current.CommandBar;
        if (s.FixKeyboardLayout && query.Length >= 2 && CountResults(rows) <= 1 && KeyboardLayout.Swap(query) is { } swapped)
        {
            var alternative = Build(swapped);
            if (CountResults(alternative) > CountResults(rows))
            {
                CorrectedQuery = swapped;
                rows = alternative;
            }
        }
        return rows;
    }

    private static int CountResults(List<CommandRow> rows) => rows.Count(r => !r.IsHeader && r.Kind != "web");

    private List<CommandRow> Build(string query)
    {
        var rows = new List<CommandRow>();
        var settings = _services.Settings.Current;
        var s = settings.CommandBar;
        var loc = Localizer.Instance;
        if (query.Length == 0) return rows;

        void Section(string titleKey, IEnumerable<CommandRow> items, int limit)
        {
            var list = items.Where(r => r.Score > 0).OrderByDescending(r => r.Score).Take(limit).ToList();
            if (list.Count == 0) return;
            rows.Add(CommandRow.Header(L.S(titleKey)));
            rows.AddRange(list);
        }

        // Calculator and units come first: they answer the question outright.
        if (s.Calculator && Calculator.LooksLikeMath(query) && Calculator.TryEvaluate(query, out var value))
        {
            var shown = Calculator.Format(value, loc.Culture);
            var copy = Calculator.Format(value, loc.Culture, grouping: false);
            Section("commandBar.section.calc", new[]
            {
                new CommandRow
                {
                    Title = "= " + shown, Subtitle = query + "   ·   " + L.S("commandBar.copyResult"), Icon = "Calculator", Kind = "calc",
                    Score = 1000, Run = () => CopyText(copy),
                },
            }, 1);
        }
        if (s.Units && UnitConverter.TryConvert(query, out var conversion))
        {
            var result = UnitConverter.FormatValue(conversion.Result, loc.Culture);
            Section("commandBar.section.units", new[]
            {
                new CommandRow
                {
                    Title = UnitConverter.Describe(conversion, loc.Language),
                    Subtitle = L.S("commandBar.copyResult"), Icon = "Ruler", Kind = "units", Score = 1000,
                    Run = () => CopyText(result.Replace(" ", "")),
                },
            }, 1);
        }

        bool emojiMode = query.StartsWith(':') || query.StartsWith("emoji ", StringComparison.OrdinalIgnoreCase)
                         || query.StartsWith("эмодзи ", StringComparison.OrdinalIgnoreCase);
        if (s.Emoji && emojiMode)
        {
            var term = query.TrimStart(':');
            int space = term.IndexOf(' ');
            if (!query.StartsWith(':') && space >= 0) term = term[(space + 1)..];
            AddEmoji(rows, term, 24);
            return rows;
        }

        if (s.SearchApps)
        {
            Section("commandBar.section.apps", _services.AppCatalog.Apps.Select(app => new CommandRow
            {
                Title = app.Name,
                Subtitle = L.S("commandBar.kind.app"),
                IconPath = app.ShellPath,
                Image = _services.AppCatalog.Icon(app.ShellPath),
                Icon = "AppGeneric",
                Kind = "app",
                Score = FuzzyMatcher.Score(query, app.Name),
                Run = () =>
                {
                    if (!AppCatalogService.Launch(app)) Failed(app.Name);
                },
            }), 6);
        }

        if (s.SearchWindows)
        {
            Section("commandBar.section.windows", _windows.Select(w => new CommandRow
            {
                Title = w.Title,
                Subtitle = w.ProcessName,
                IconPath = w.ExePath,
                Image = w.ExePath == null ? null : _services.AppCatalog.Icon(w.ExePath),
                Icon = "Window",
                Kind = "window",
                Score = Math.Max(FuzzyMatcher.Score(query, w.Title), FuzzyMatcher.Score(query, w.ProcessName ?? "") * 9 / 10),
                Run = () => WindowList.Activate(w.Handle),
            }), 5);
        }

        Section("commandBar.section.actions", ActionCatalog.All
            .Where(a => a.FeatureId == null || settings.IsInstalled(a.FeatureId))
            .Where(a => a.Id != ActionIds.CommandBar)
            .Select(a => new CommandRow
            {
                Title = L.S(ActionCatalog.TitleKey(a.Id)),
                Subtitle = L.S("action.category." + a.Category),
                Icon = a.Icon,
                Kind = "action",
                Score = FuzzyMatcher.Score(query, L.S(ActionCatalog.TitleKey(a.Id))),
                Run = () => _services.Execute(a.Id),
            }), 5);

        if (s.SearchSettings)
        {
            Section("commandBar.section.settings", WindowsSettingsCatalog.All.Select(entry => new CommandRow
            {
                Title = entry.Name(loc.Language),
                Subtitle = entry.IsSettingsUri ? L.S("commandBar.kind.setting") : entry.Target,
                Icon = entry.Icon,
                Kind = "setting",
                Score = FuzzyMatcher.Score(query, entry.Name(loc.Language), entry.Keywords.Append(entry.Name(loc.Language == UiLanguage.Ru ? UiLanguage.En : UiLanguage.Ru))),
                Run = () =>
                {
                    if (!AppCatalogService.Open(entry.Target)) Failed(entry.Name(loc.Language));
                },
            }), 5);
        }

        Section("commandBar.section.scripts", s.Scripts.Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Command))
            .Select(script => new CommandRow
            {
                Title = script.Name,
                Subtitle = (script.Command + " " + script.Arguments).Trim(),
                Icon = "WindowConsole",
                Kind = "script",
                Score = FuzzyMatcher.Score(query, script.Name),
                Run = () =>
                {
                    if (!AppCatalogService.Open(script.Command, script.Arguments)) Failed(script.Name);
                },
            }), 5);

        if (s.SearchClipboard && settings.IsInstalled(Core.Features.FeatureIds.ClipboardHistory) && query.Length >= 2)
        {
            var target = Target;
            Section("commandBar.section.clipboard", _services.Clipboard.History.Query(query, ClipFilter.All).Take(4).Select((entry, i) =>
            {
                var item = new ClipItem(entry);
                return new CommandRow
                {
                    Title = item.Title,
                    Subtitle = item.Meta,
                    Icon = item.KindIcon,
                    Image = item.Thumbnail,
                    Kind = "clipboard",
                    Score = 500 - i,
                    Run = () => _ = _services.Clipboard.PasteAsync(entry, false, target),
                };
            }), 4);
        }

        // Nothing else matched: emoji keywords are worth a look.
        if (s.Emoji && CountResults(rows) == 0 && query.Length >= 3) AddEmoji(rows, query, 12);

        if (s.WebSearch)
        {
            var engine = s.SearchEngine;
            rows.Add(CommandRow.Header(L.S("commandBar.section.web")));
            rows.Add(new CommandRow
            {
                Title = L.F("commandBar.webSearch", query),
                Subtitle = L.F("commandBar.webSearch.in", EngineName(engine)),
                Icon = "Globe",
                Kind = "web",
                Score = 1,
                Run = () => AppCatalogService.Open(SearchUrl(engine, query)),
            });
        }
        return rows;
    }

    private void AddEmoji(List<CommandRow> rows, string term, int limit)
    {
        var matches = EmojiCatalog.Search(term, limit).ToList();
        if (matches.Count == 0) return;
        var target = Target;
        var language = Localizer.Instance.Language;
        rows.Add(CommandRow.Header(L.S("commandBar.section.emoji")));
        rows.AddRange(matches.Select(m => new CommandRow
        {
            Title = m.Entry.Name(language),
            Subtitle = string.Join(", ", (language == UiLanguage.Ru ? m.Entry.KeywordsRu : m.Entry.KeywordsEn).Skip(1).Take(3)),
            Glyph = m.Entry.Emoji,
            Kind = "emoji",
            Score = m.Score,
            Run = () => _ = _services.Clipboard.PasteTextAsync(m.Entry.Emoji, target),
        }));
    }

    private void CopyText(string text)
    {
        if (_services.Clipboard.SetText(text)) _services.Hud.Show(L.S("common.copied") + ": " + text, "Copy", HudTone.Success);
    }

    private void Failed(string what) => _services.Hud.Show(L.F("commandBar.runFailed", what), "Warning", HudTone.Rec);

    public static string EngineName(WebSearchEngine engine) => engine switch
    {
        WebSearchEngine.Yandex => Localizer.Instance.Language == UiLanguage.Ru ? "Яндексе" : "Yandex",
        WebSearchEngine.DuckDuckGo => "DuckDuckGo",
        WebSearchEngine.Bing => "Bing",
        _ => "Google",
    };

    public static string SearchUrl(WebSearchEngine engine, string query)
    {
        var q = Uri.EscapeDataString(query);
        return engine switch
        {
            WebSearchEngine.Yandex => "https://yandex.ru/search/?text=" + q,
            WebSearchEngine.DuckDuckGo => "https://duckduckgo.com/?q=" + q,
            WebSearchEngine.Bing => "https://www.bing.com/search?q=" + q,
            _ => "https://www.google.com/search?q=" + q,
        };
    }
}
