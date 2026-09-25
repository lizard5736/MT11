using System.Globalization;
using Grip.Core.Actions;
using Grip.Core.Calc;
using Grip.Core.Clipboard;
using Grip.Core.Energy;
using Grip.Core.Features;
using Grip.Core.Input;
using Grip.Core.Localization;
using Grip.Core.Monitoring;
using Grip.Core.Notes;
using Grip.Core.Radial;
using Grip.Core.Search;
using Grip.Core.Settings;
using Grip.Core.Text;
using Grip.Core.Windows;

namespace Grip.Core.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData("Win+Alt+V", HotkeyModifiers.Win | HotkeyModifiers.Alt, 'V')]
    [InlineData("ctrl + shift + space", HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, 0x20)]
    [InlineData("Alt+F4", HotkeyModifiers.Alt, 0x73)]
    [InlineData("Ctrl+Alt+Shift+V", HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 'V')]
    public void Parses(string text, HotkeyModifiers modifiers, int key)
    {
        Assert.True(Hotkey.TryParse(text, out var hotkey));
        Assert.Equal(modifiers, hotkey.Modifiers);
        Assert.Equal(key, hotkey.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+A+B")]
    [InlineData("Ctrl+Nope")]
    public void RejectsInvalid(string text) => Assert.False(Hotkey.TryParse(text, out _));

    [Fact]
    public void RoundTripsInCanonicalOrder()
    {
        var hotkey = Hotkey.Parse("shift+alt+win+ctrl+q");
        Assert.Equal("Win+Ctrl+Alt+Shift+Q", hotkey.ToString());
        Assert.Equal(hotkey, Hotkey.Parse(hotkey.ToString()));
    }

    [Fact]
    public void GlobalUsability()
    {
        Assert.False(Hotkey.Parse("Shift+A").IsUsableGlobally);
        Assert.True(Hotkey.Parse("F9").IsUsableGlobally);
        Assert.True(Hotkey.Parse("Win+Alt+V").IsUsableGlobally);
    }

    [Fact]
    public void EveryDefaultHotkeyParses()
    {
        foreach (var action in ActionCatalog.All.Where(a => a.DefaultHotkey.Length > 0))
            Assert.True(Hotkey.TryParse(action.DefaultHotkey, out var h) && h.IsUsableGlobally, action.Id);
    }
}

public class LocalizationTests
{
    [Fact]
    public void EveryEntryHasBothLanguages()
    {
        foreach (var (key, entry) in StringTable.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Ru), key);
            Assert.False(string.IsNullOrWhiteSpace(entry.En), key);
        }
    }

    [Fact]
    public void FormatPlaceholdersMatch()
    {
        foreach (var (key, entry) in StringTable.All)
            Assert.True(Placeholders(entry.Ru).SetEquals(Placeholders(entry.En)), key);
    }

    [Fact]
    public void PluralEntriesHaveEnoughForms()
    {
        foreach (var (key, entry) in StringTable.All.Where(e => e.Value.Ru.Contains('|') && !e.Key.EndsWith("fileFilter")))
        {
            Assert.Equal(3, entry.Ru.Split('|').Length);
            Assert.Equal(2, entry.En.Split('|').Length);
        }
    }

    [Fact]
    public void EveryFeatureAndActionHasText()
    {
        foreach (var f in FeatureCatalog.All)
        {
            Assert.True(StringTable.All.ContainsKey(f.TitleKey), f.TitleKey);
            Assert.True(StringTable.All.ContainsKey(f.DescriptionKey), f.DescriptionKey);
            Assert.True(StringTable.All.ContainsKey("group." + f.Group), f.Group.ToString());
            Assert.True(StringTable.All.ContainsKey("energy." + f.Energy), f.Energy.ToString());
        }
        foreach (var p in FeaturePreset.All)
        {
            Assert.True(StringTable.All.ContainsKey(p.TitleKey), p.TitleKey);
            Assert.True(StringTable.All.ContainsKey(p.DescriptionKey), p.DescriptionKey);
            Assert.All(p.Features, id => Assert.True(FeatureCatalog.Get(id).IsReady, id));
        }
        foreach (var a in ActionCatalog.All)
            Assert.True(StringTable.All.ContainsKey(ActionCatalog.TitleKey(a.Id)), ActionCatalog.TitleKey(a.Id));
    }

    [Fact]
    public void RadialDefaultLabelsResolve()
    {
        var loc = new Localizer();
        foreach (var item in Flatten(RadialProfile.CreateDefault().Items))
        {
            if (item.Label.StartsWith('@')) Assert.True(loc.Has(item.Label[1..]), item.Label);
            if (item.Kind == RadialItemKind.Action) Assert.True(ActionCatalog.TryGet(item.Target, out _), item.Target);
        }
        Assert.True(loc.Has(RadialProfile.CreateDefault().Name[1..]));
    }

    [Theory]
    [InlineData(1, "запись")]
    [InlineData(2, "записи")]
    [InlineData(5, "записей")]
    [InlineData(11, "записей")]
    [InlineData(21, "запись")]
    [InlineData(22, "записи")]
    [InlineData(112, "записей")]
    public void RussianPlurals(long n, string expected)
    {
        var loc = new Localizer();
        loc.SetLanguage(UiLanguage.Ru);
        Assert.Equal(expected, loc.Plural("clipboard.items", n));
    }

    [Fact]
    public void EnglishPlurals()
    {
        var loc = new Localizer();
        loc.SetLanguage(UiLanguage.En);
        Assert.Equal("item", loc.Plural("clipboard.items", 1));
        Assert.Equal("items", loc.Plural("clipboard.items", 3));
    }

    [Fact]
    public void ResolvesSystemLanguage()
    {
        Assert.Equal(UiLanguage.Ru, Localizer.Resolve(AppLanguage.System, CultureInfo.GetCultureInfo("ru-RU")));
        Assert.Equal(UiLanguage.En, Localizer.Resolve(AppLanguage.System, CultureInfo.GetCultureInfo("de-DE")));
        Assert.Equal(UiLanguage.En, Localizer.Resolve(AppLanguage.English, CultureInfo.GetCultureInfo("ru-RU")));
    }

    [Fact]
    public void DurationsReadNaturally()
    {
        var loc = new Localizer();
        loc.SetLanguage(UiLanguage.Ru);
        Assert.Equal("1\u00A0ч 24\u00A0мин", loc.Duration(TimeSpan.FromMinutes(84)));
        Assert.Equal("45\u00A0с", loc.Duration(TimeSpan.FromSeconds(45)));
    }

    private static HashSet<string> Placeholders(string s) =>
        System.Text.RegularExpressions.Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).ToHashSet();

    private static IEnumerable<RadialItem> Flatten(IEnumerable<RadialItem> items) =>
        items.SelectMany(i => new[] { i }.Concat(Flatten(i.Children)));
}

public class SettingsTests
{
    [Fact]
    public void NormalizeFillsDefaults()
    {
        var s = SettingsStore.Normalize(new AppSettings());
        Assert.Equal("Win+Alt+V", s.Hotkeys[ActionIds.ClipboardHistory]);
        Assert.True(s.IsInstalled(FeatureIds.ClipboardHistory));
        Assert.False(s.IsInstalled(FeatureIds.Mixer)); // planned features never count as installed
        Assert.NotEmpty(s.RadialMenu.Profiles);
    }

    [Fact]
    public void RoundTripsThroughJson()
    {
        var s = SettingsStore.Normalize(new AppSettings());
        s.General.Theme = AppTheme.Light;
        s.Clipboard.MaxItems = 42;
        s.SetInstalled(FeatureIds.Shelf, false);
        var json = SettingsStore.Serialize(s);
        Assert.Contains("\"light\"", json);
        var back = SettingsStore.Deserialize(json);
        Assert.Equal(AppTheme.Light, back.General.Theme);
        Assert.Equal(42, back.Clipboard.MaxItems);
        Assert.False(back.IsInstalled(FeatureIds.Shelf));
    }

    [Fact]
    public void ClampsHandEdits()
    {
        var s = SettingsStore.Deserialize("{ \"clipboard\": { \"maxItems\": -5, \"autoClearSeconds\": 1 }, }");
        Assert.Equal(10, s.Clipboard.MaxItems);
        Assert.Equal(5, s.Clipboard.AutoClearSeconds);
    }

    [Fact]
    public void BrokenFileIsSetAsideAndDefaultsLoad()
    {
        var dir = Directory.CreateTempSubdirectory();
        var path = Path.Combine(dir.FullName, "settings.json");
        File.WriteAllText(path, "{ not json");
        var store = new SettingsStore(path);
        var s = store.Load(out var problem);
        Assert.NotNull(problem);
        Assert.NotNull(s);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(dir.FullName, "settings.broken-*.json"));
        store.Save(s);
        Assert.True(File.Exists(path));
        dir.Delete(true);
    }
}

public class ClipboardHistoryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DuplicatesArePromoted()
    {
        var h = new ClipboardHistory();
        h.AddOrPromote(ClipEntry.FromText("one", null, T0), T0);
        h.AddOrPromote(ClipEntry.FromText("two", null, T0), T0.AddSeconds(1));
        var again = h.AddOrPromote(ClipEntry.FromText("one", null, T0), T0.AddSeconds(2));
        Assert.Equal(2, h.Count);
        Assert.Equal("one", h.Items[0].Text);
        Assert.Equal(1, again.UseCount);
    }

    [Fact]
    public void LinksAreDetected()
    {
        Assert.Equal(ClipKind.Link, ClipEntry.FromText("https://example.com/a?b=1", null, T0).Kind);
        Assert.Equal(ClipKind.Text, ClipEntry.FromText("see https://example.com", null, T0).Kind);
    }

    [Fact]
    public void PinnedSurviveLimits()
    {
        var h = new ClipboardHistory();
        h.Options.MaxItems = 2;
        var pinned = h.AddOrPromote(ClipEntry.FromText("keep me", null, T0), T0);
        h.SetPinned(pinned.Id, true);
        for (int i = 0; i < 5; i++) h.AddOrPromote(ClipEntry.FromText("item " + i, null, T0), T0.AddSeconds(i + 1));
        Assert.Equal(3, h.Count);
        Assert.Contains(h.Items, e => e.Id == pinned.Id);
        Assert.Equal("item 4", h.Items[0].Text);
    }

    [Fact]
    public void RetentionDropsOldEntries()
    {
        var h = new ClipboardHistory();
        h.Options.RetentionDays = 7;
        h.AddOrPromote(ClipEntry.FromText("old", null, T0), T0);
        h.AddOrPromote(ClipEntry.FromText("new", null, T0), T0.AddDays(10));
        Assert.Single(h.Items);
        Assert.Equal("new", h.Items[0].Text);
    }

    [Fact]
    public void SearchIgnoresCaseAndYo()
    {
        var h = new ClipboardHistory();
        h.AddOrPromote(ClipEntry.FromText("Ёлка на площади", null, T0), T0);
        h.AddOrPromote(ClipEntry.FromText("something else", null, T0), T0.AddSeconds(1));
        var found = h.Query("елка", ClipFilter.All).ToList();
        Assert.Single(found);
        Assert.Empty(h.Query("елка", ClipFilter.Links));
    }

    [Fact]
    public void ClearKeepsPinned()
    {
        var h = new ClipboardHistory();
        var a = h.AddOrPromote(ClipEntry.FromText("a", null, T0), T0);
        h.AddOrPromote(ClipEntry.FromText("b", null, T0), T0);
        h.SetPinned(a.Id, true);
        h.Clear();
        Assert.Single(h.Items);
    }

    [Fact]
    public void PersistsAndReloads()
    {
        var h = new ClipboardHistory();
        h.AddOrPromote(ClipEntry.FromFiles(new[] { @"C:\a.txt", @"C:\b.txt" }, "explorer.exe", T0), T0);
        var json = h.Serialize();
        var back = new ClipboardHistory();
        back.Load(json);
        Assert.Equal(ClipKind.Files, back.Items[0].Kind);
        Assert.Equal(2, back.Items[0].Files!.Count);
    }

    [Theory]
    [InlineData("#fa0", "#FFAA00")]
    [InlineData("#FFB020", "#FFB020")]
    [InlineData("rgb(255, 176, 32)", "#FFB020")]
    [InlineData("#12345", null)]
    [InlineData("hello", null)]
    public void DetectsColors(string text, string? expected) => Assert.Equal(expected, ClipText.TryParseColor(text));

    [Fact]
    public void PreviewCollapsesWhitespace() =>
        Assert.Equal("a b c", ClipText.Preview("  a\n\n b\t c  "));
}

public class UrlCleanerTests
{
    [Theory]
    [InlineData("https://example.com/page?utm_source=x&utm_medium=y&id=5", "https://example.com/page?id=5", 2)]
    [InlineData("https://example.com/?fbclid=abc", "https://example.com/", 1)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=AbCdEf", "https://youtu.be/dQw4w9WgXcQ", 1)]
    [InlineData("https://www.youtube.com/watch?v=abc&pp=ygU&t=42", "https://www.youtube.com/watch?v=abc&t=42", 1)]
    [InlineData("https://open.spotify.com/track/1?si=xyz", "https://open.spotify.com/track/1", 1)]
    [InlineData("https://x.com/user/status/1?s=20&t=abc", "https://x.com/user/status/1", 2)]
    [InlineData("https://ya.ru/?ysclid=l0k1&text=grip", "https://ya.ru/?text=grip", 1)]
    [InlineData("https://example.com/a?id=1#section", "https://example.com/a?id=1#section", 0)]
    [InlineData("https://shop.example/item?UTM_Campaign=Fall", "https://shop.example/item", 1)]
    public void RemovesTrackers(string input, string expected, int removed)
    {
        var result = UrlCleaner.Clean(input);
        Assert.Equal(expected, result.Url);
        Assert.Equal(removed, result.RemovedCount);
    }

    [Fact]
    public void KeepsYouTubeFeatureWhenMeaningful()
    {
        var r = UrlCleaner.Clean("https://www.youtube.com/watch?v=abc&feature=youtu.be");
        Assert.Equal("https://www.youtube.com/watch?v=abc", r.Url);
        var keep = UrlCleaner.Clean("https://www.youtube.com/results?feature=other&search_query=x");
        Assert.Contains("feature=other", keep.Url);
    }

    [Fact]
    public void UnwrapsRedirects()
    {
        var r = UrlCleaner.Clean("https://www.google.com/url?sa=t&q=https%3A%2F%2Fexample.com%2Fa%3Futm_source%3Dg%26x%3D1&ved=abc");
        Assert.True(r.Unwrapped);
        Assert.Equal("https://example.com/a?x=1", r.Url);

        var vk = UrlCleaner.Clean("https://vk.com/away.php?to=https%3A%2F%2Fgithub.com%2F&cc_key=");
        Assert.Equal("https://github.com/", vk.Url);
    }

    [Fact]
    public void SimplifiesGoogleSearch()
    {
        var r = UrlCleaner.Clean("https://www.google.com/search?q=grip&sca_esv=1&ei=abc&oq=grip&gs_lp=xyz&sclient=gws-wiz");
        Assert.Equal("https://www.google.com/search?q=grip", r.Url);
        var off = UrlCleaner.Clean("https://www.google.com/search?q=grip&ei=abc", new UrlCleanerOptions { CleanSearchLinks = false });
        Assert.Contains("ei=abc", off.Url);
    }

    [Fact]
    public void StripsAmazonRefPath()
    {
        var r = UrlCleaner.Clean("https://www.amazon.de/dp/B000123/ref=sr_1_1?keywords=x&qid=123&sr=8-1");
        Assert.Equal("https://www.amazon.de/dp/B000123?keywords=x", r.Url);
    }

    [Fact]
    public void RespectsSkipDomainsAndExtras()
    {
        var skip = UrlCleaner.Clean("https://my.site.com/?utm_source=a", new UrlCleanerOptions { SkipDomains = new[] { "site.com" } });
        Assert.False(skip.Changed);
        var extra = UrlCleaner.Clean("https://a.com/?session=1&k=2", new UrlCleanerOptions { ExtraParameters = new[] { "session" } });
        Assert.Equal("https://a.com/?k=2", extra.Url);
    }

    [Theory]
    [InlineData("not a link")]
    [InlineData("ftp://example.com/?utm_source=x")]
    [InlineData("https://exa mple.com")]
    public void IgnoresNonHttp(string input) => Assert.False(UrlCleaner.Clean(input).Changed);
}

public class CalculatorTests
{
    [Theory]
    [InlineData("2+2", 4)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("-2^2", -4)]
    [InlineData("2^3^2", 512)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("3,5 * 2", 7)]
    [InlineData("200 + 15%", 230)]
    [InlineData("200 - 10%", 180)]
    [InlineData("50%", 0.5)]
    [InlineData("15% от 200", 30)]
    [InlineData("15% of 200", 30)]
    [InlineData("15% от 2400 + 120", 480)]
    [InlineData("sqrt(16)", 4)]
    [InlineData("√16 + 1", 5)]
    [InlineData("5!", 120)]
    [InlineData("2π", 6.283185307179586)]
    [InlineData("3(4+1)", 15)]
    [InlineData("1 000 000 / 4", 250000)]
    [InlineData("3x4", 12)]
    [InlineData("3 х 4", 12)]
    [InlineData("12 × 3 ÷ 4", 9)]
    [InlineData("2**10", 1024)]
    [InlineData("log(1000)", 3)]
    [InlineData("1e3 + 1", 1001)]
    [InlineData("(1+2", 3)]
    public void Evaluates(string input, double expected)
    {
        Assert.True(Calculator.TryEvaluate(input, out var value), input);
        Assert.Equal(expected, value, 9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("2 3")]
    [InlineData("1/0")]
    [InlineData("2 +")]
    [InlineData("sqrt(-1)")]
    public void RejectsNonsense(string input) => Assert.False(Calculator.TryEvaluate(input, out _));

    [Theory]
    [InlineData("2+2", true)]
    [InlineData("42", false)]
    [InlineData("sqrt(2)", true)]
    [InlineData("-5", false)]
    [InlineData("5-3", true)]
    [InlineData("chrome", false)]
    [InlineData("3x4", true)]
    public void DetectsMath(string input, bool expected) => Assert.Equal(expected, Calculator.LooksLikeMath(input));

    [Fact]
    public void FormatsForCulture()
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        // The group separator comes from ICU data (U+00A0 today), which differs between ICU versions.
        Assert.Equal("1" + ru.NumberFormat.NumberGroupSeparator + "234,5", Calculator.Format(1234.5, ru));
        Assert.Equal("1,234.5", Calculator.Format(1234.5, CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("0.3333333333", Calculator.Format(1.0 / 3, CultureInfo.GetCultureInfo("en-US")));
    }
}

public class UnitConverterTests
{
    [Theory]
    [InlineData("10 km to mi", 6.21371192, "mi")]
    [InlineData("10 км в милях", 6.21371192, "mi")]
    [InlineData("100f to c", 37.7777778, "c")]
    [InlineData("0 c", 32, "f")]
    [InlineData("1,5 гб в мб", 1536, "mb")]
    [InlineData("100 mbit to mb", 11.920929, "mb")]
    [InlineData("2 h to min", 120, "min")]
    [InlineData("5 kg in lb", 11.0231131, "lb")]
    [InlineData("1 inch to cm", 2.54, "cm")]
    [InlineData("60 kmh to mph", 37.2822715, "mph")]
    [InlineData("30 с в мин", 0.5, "min")]
    public void Converts(string input, double expected, string target)
    {
        Assert.True(UnitConverter.TryConvert(input, out var c), input);
        Assert.Equal(target, c.To.Id);
        Assert.Equal(expected, c.Result, 5);
    }

    [Theory]
    [InlineData("10 km to kg")]
    [InlineData("hello world")]
    [InlineData("2 apples")]
    [InlineData("5")]
    public void RejectsNonsense(string input) => Assert.False(UnitConverter.TryConvert(input, out _));

    [Fact]
    public void Describes()
    {
        Assert.True(UnitConverter.TryConvert("10 km to mi", out var c));
        Assert.Equal("10 км = 6,2137 миль", UnitConverter.Describe(c, UiLanguage.Ru));
        Assert.Equal("10 km = 6.2137 mi", UnitConverter.Describe(c, UiLanguage.En));
    }
}

public class SearchTests
{
    [Fact]
    public void RanksLikeALauncher()
    {
        int exact = FuzzyMatcher.Score("chrome", "Chrome");
        int prefix = FuzzyMatcher.Score("chr", "Chrome");
        int word = FuzzyMatcher.Score("code", "Visual Studio Code");
        int acronym = FuzzyMatcher.Score("vsc", "Visual Studio Code");
        int loose = FuzzyMatcher.Score("vscd", "Visual Studio Code");
        Assert.True(exact > prefix && prefix > word && word > acronym && acronym > loose && loose > 0);
        Assert.Equal(0, FuzzyMatcher.Score("xyz", "Chrome"));
    }

    [Fact]
    public void MatchesRussianAcronyms() =>
        Assert.True(FuzzyMatcher.Score("дз", "Диспетчер задач") > 0);

    [Theory]
    [InlineData("ыуеештпы", "settings")]
    [InlineData("ntktuhfv", "телеграм")]
    [InlineData("ghbdtn", "привет")]
    public void SwapsKeyboardLayout(string typed, string meant) => Assert.Equal(meant, KeyboardLayout.Swap(typed));

    [Fact]
    public void SwapReturnsNullForDigits() => Assert.Null(KeyboardLayout.Swap("12345"));

    [Fact]
    public void FindsEmojiInBothLanguages()
    {
        Assert.Contains(EmojiCatalog.Search("heart"), x => x.Entry.Emoji == "❤️");
        Assert.Contains(EmojiCatalog.Search("сердце"), x => x.Entry.Emoji == "❤️");
        Assert.Contains(EmojiCatalog.Search("хлопушка"), x => x.Entry.Emoji == "🎬");
    }

    [Fact]
    public void SettingsCatalogHasUniqueTargets() =>
        Assert.Equal(WindowsSettingsCatalog.All.Count, WindowsSettingsCatalog.All.Select(s => s.Target).Distinct().Count());
}

public class RadialGeometryTests
{
    [Theory]
    [InlineData(0, -100, 8, 0)]    // straight up
    [InlineData(100, 0, 8, 2)]     // right
    [InlineData(0, 100, 8, 4)]     // down
    [InlineData(-100, 0, 8, 6)]    // left
    [InlineData(70, -70, 8, 1)]    // up-right
    [InlineData(-10, -100, 4, 0)]
    [InlineData(100, 0, 4, 1)]
    public void HitsTheRightSlice(double dx, double dy, int count, int expected) =>
        Assert.Equal(expected, RadialGeometry.HitTest(dx, dy, count, 20));

    [Fact]
    public void DeadZoneReturnsNothing() => Assert.Equal(-1, RadialGeometry.HitTest(5, 5, 8, 20));

    [Fact]
    public void PointAtIsClockwiseFromTop()
    {
        var (x, y) = RadialGeometry.PointAt(0, 0, 10, 90);
        Assert.Equal(10, x, 6);
        Assert.Equal(0, y, 6);
    }
}

public class KeepAwakeTests
{
    [Fact]
    public void TimedSessionsExpire()
    {
        var now = DateTimeOffset.Now;
        var s = KeepAwakeSession.ForMinutes(30, now);
        Assert.False(s.IsExpired(now.AddMinutes(29)));
        Assert.True(s.IsExpired(now.AddMinutes(30)));
        Assert.True(KeepAwakeSession.ForMinutes(0, now).IsIndefinite);
    }

    [Fact]
    public void UntilTimeRollsToTomorrow()
    {
        var now = new DateTimeOffset(2026, 9, 24, 20, 0, 0, TimeSpan.FromHours(3));
        var s = KeepAwakeSession.UntilTime(new TimeOnly(8, 0), now);
        Assert.Equal(TimeSpan.FromHours(12), s.Until!.Value - now);
    }

    [Theory]
    [InlineData("18:30", 18, 30)]
    [InlineData("1830", 18, 30)]
    [InlineData("7.05", 7, 5)]
    [InlineData("9", 9, 0)]
    public void ParsesClock(string text, int h, int m)
    {
        Assert.True(KeepAwakeText.TryParseClock(text, out var t));
        Assert.Equal(new TimeOnly(h, m), t);
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("12:61")]
    [InlineData("soon")]
    public void RejectsBadClock(string text) => Assert.False(KeepAwakeText.TryParseClock(text, out _));
}

public class ShakeDetectorTests
{
    [Fact]
    public void DetectsAWiggle()
    {
        var d = new ShakeDetector();
        long t = 0;
        bool fired = false;
        double x = 500;
        foreach (var target in new[] { 580.0, 500, 580, 500, 580 })
        {
            while (Math.Abs(x - target) > 0.5)
            {
                x += Math.Sign(target - x) * 10;
                t += 10;
                fired |= d.Add(t, x);
            }
        }
        Assert.True(fired);
    }

    [Fact]
    public void IgnoresASlowDrag()
    {
        var d = new ShakeDetector();
        bool fired = false;
        for (int i = 0; i < 200; i++) fired |= d.Add(i * 16, 100 + i * 3);
        Assert.False(fired);
    }
}

public class SampleHistoryTests
{
    [Fact]
    public void ReadsOldestToNewest()
    {
        var h = new SampleHistory(3);
        h.Add(1);
        h.Add(2);
        h.Add(3);
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, h.Values);
        Assert.Equal(3, h.Latest);
    }

    [Fact]
    public void WrapsAroundPastCapacity()
    {
        var h = new SampleHistory(3);
        h.Add(1);
        h.Add(2);
        h.Add(3);
        h.Add(4); // pushes out 1
        Assert.Equal(new[] { 2.0, 3.0, 4.0 }, h.Values);
        Assert.Equal(3, h.Count);
        Assert.Equal(4, h.Latest);
        Assert.Equal(4, h.Max);
    }

    [Fact]
    public void EmptyHistoryIsZero()
    {
        var h = new SampleHistory(5);
        Assert.Equal(0, h.Latest);
        Assert.Equal(0, h.Max);
        Assert.Empty(h.Values);
    }
}

public class CpuUsageTests
{
    [Fact]
    public void FullyIdleIsZero()
    {
        // 1 second passes, all of it idle.
        long ticks = TimeSpan.FromSeconds(1).Ticks;
        Assert.Equal(0, CpuUsage.PercentBetween(0, 0, 0, ticks, ticks, 0), 3);
    }

    [Fact]
    public void FullyBusyIsHundred()
    {
        long ticks = TimeSpan.FromSeconds(1).Ticks;
        // No idle time at all; all of it user time (kernel time still includes idle, here zero).
        Assert.Equal(100, CpuUsage.PercentBetween(0, 0, 0, 0, 0, ticks), 3);
    }

    [Fact]
    public void HalfBusyIsFifty()
    {
        long ticks = TimeSpan.FromSeconds(1).Ticks;
        // Half a second idle, half a second of kernel+user work on top of that idle baseline.
        Assert.Equal(50, CpuUsage.PercentBetween(0, 0, 0, ticks / 2, ticks / 2, ticks / 2), 3);
    }

    [Fact]
    public void NoElapsedTimeIsZeroNotNaN()
    {
        Assert.Equal(0, CpuUsage.PercentBetween(10, 20, 5, 10, 20, 5));
    }
}

public class MonitorWarningsTests
{
    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    [InlineData(100, true)]
    public void CpuHighAtNinety(double percent, bool expected) => Assert.Equal(expected, MonitorWarnings.IsCpuHigh(percent));

    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    public void MemoryHighAtNinety(double percent, bool expected) => Assert.Equal(expected, MonitorWarnings.IsMemoryHigh(percent));

    [Theory]
    [InlineData(6, false)]
    [InlineData(5, true)]
    [InlineData(0, true)]
    public void DiskLowAtFivePercentFree(double freePercent, bool expected) => Assert.Equal(expected, MonitorWarnings.IsDiskLow(freePercent));

    [Fact]
    public void BatteryLowOnlyWhenNotCharging()
    {
        Assert.True(MonitorWarnings.IsBatteryLow(5, charging: false));
        Assert.False(MonitorWarnings.IsBatteryLow(5, charging: true));
        Assert.False(MonitorWarnings.IsBatteryLow(50, charging: false));
    }
}

public class WindowZonesTests
{
    // A 1921x1081 work area (odd on purpose) at a non-zero origin, to catch off-by-one
    // and origin-offset mistakes that a clean 1920x1080-at-(0,0) area would hide.
    private const int X = 100, Y = 50, W = 1921, H = 1081;

    [Fact]
    public void HalvesMeetExactlyInTheMiddle()
    {
        var left = WindowZones.Compute(WindowZone.LeftHalf, X, Y, W, H);
        var right = WindowZones.Compute(WindowZone.RightHalf, X, Y, W, H);
        Assert.Equal(left.X + left.Width, right.X); // no gap, no overlap
        Assert.Equal(W, left.Width + right.Width); // together they cover the whole width
        Assert.Equal(H, left.Height);
        Assert.Equal(H, right.Height);
    }

    [Fact]
    public void TopAndBottomHalvesMeetExactly()
    {
        var top = WindowZones.Compute(WindowZone.TopHalf, X, Y, W, H);
        var bottom = WindowZones.Compute(WindowZone.BottomHalf, X, Y, W, H);
        Assert.Equal(top.Y + top.Height, bottom.Y);
        Assert.Equal(H, top.Height + bottom.Height);
    }

    [Fact]
    public void FourQuartersExactlyTileTheArea()
    {
        var tl = WindowZones.Compute(WindowZone.TopLeftQuarter, X, Y, W, H);
        var tr = WindowZones.Compute(WindowZone.TopRightQuarter, X, Y, W, H);
        var bl = WindowZones.Compute(WindowZone.BottomLeftQuarter, X, Y, W, H);
        var br = WindowZones.Compute(WindowZone.BottomRightQuarter, X, Y, W, H);

        Assert.Equal(tl.X, bl.X);
        Assert.Equal(tr.X, br.X);
        Assert.Equal(tl.Y, tr.Y);
        Assert.Equal(bl.Y, br.Y);
        Assert.Equal(tl.X + tl.Width, tr.X);
        Assert.Equal(tl.Y + tl.Height, bl.Y);
        Assert.Equal(W, tl.Width + tr.Width);
        Assert.Equal(H, tl.Height + bl.Height);
    }

    [Fact]
    public void ThreeThirdsExactlyTileTheArea()
    {
        var left = WindowZones.Compute(WindowZone.LeftThird, X, Y, W, H);
        var center = WindowZones.Compute(WindowZone.CenterThird, X, Y, W, H);
        var right = WindowZones.Compute(WindowZone.RightThird, X, Y, W, H);
        Assert.Equal(left.X + left.Width, center.X);
        Assert.Equal(center.X + center.Width, right.X);
        Assert.Equal(W, left.Width + center.Width + right.Width);
        Assert.Equal(left.Width, center.Width); // only the last third absorbs the rounding remainder
    }

    [Fact]
    public void TwoThirdsPairsMatchTheThirds()
    {
        var leftThird = WindowZones.Compute(WindowZone.LeftThird, X, Y, W, H);
        var leftTwoThirds = WindowZones.Compute(WindowZone.LeftTwoThirds, X, Y, W, H);
        var rightTwoThirds = WindowZones.Compute(WindowZone.RightTwoThirds, X, Y, W, H);
        Assert.Equal(leftTwoThirds.Width, 2 * leftThird.Width);
        Assert.Equal(X, leftTwoThirds.X);
        Assert.Equal(leftThird.X + leftThird.Width, rightTwoThirds.X);
        Assert.Equal(X + W, rightTwoThirds.X + rightTwoThirds.Width);
    }

    [Fact]
    public void MaximizeFillsTheWholeArea()
    {
        var rect = WindowZones.Compute(WindowZone.Maximize, X, Y, W, H);
        Assert.Equal(new ZoneRect(X, Y, W, H), rect);
    }

    [Fact]
    public void EveryZoneStaysInsideTheArea()
    {
        foreach (var zone in Enum.GetValues<WindowZone>())
        {
            var r = WindowZones.Compute(zone, X, Y, W, H);
            Assert.True(r.X >= X && r.Y >= Y, zone.ToString());
            Assert.True(r.X + r.Width <= X + W, zone.ToString());
            Assert.True(r.Y + r.Height <= Y + H, zone.ToString());
        }
    }
}

public class GpuUsageTests
{
    private const string TypicalOutput =
        "\"(PDH-CSV 4.0) (Coordinated Universal Time)(0)\",\"\\\\HOST\\GPU Engine(pid_1000_luid_0x00000000_0x0000ABCD_phys_0_eng_0_engtype_3D)\\Utilization Percentage\",\"\\\\HOST\\GPU Engine(pid_2000_luid_0x00000000_0x0000ABCD_phys_0_eng_1_engtype_VideoDecode)\\Utilization Percentage\"\r\n" +
        "\"09/24/2026 14:00:00.000\",\"0.000000\",\"0.000000\"\r\n" +
        "\"09/24/2026 14:00:01.000\",\"12.500000\",\"3.750000\"\r\n";

    [Fact]
    public void TakesTheBusiestEngineFromTheLastRow()
    {
        Assert.Equal(12.5, GpuUsage.ParsePercent(TypicalOutput));
    }

    [Fact]
    public void NoInstancesIsNull()
    {
        // No GPU Engine category on this machine: typeperf still prints a lone header/no data.
        Assert.Null(GpuUsage.ParsePercent("\"(PDH-CSV 4.0) (Coordinated Universal Time)(0)\"\r\n"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage, not csv at all")]
    [InlineData("one line only, no header even")]
    public void UnrecognizedOutputIsNullNotAThrow(string bogus)
    {
        Assert.Null(GpuUsage.ParsePercent(bogus));
    }

    [Fact]
    public void ClampsAnImplausiblyHighReading()
    {
        const string csv = "\"ts\",\"col\"\r\n\"09/24/2026 14:00:00.000\",\"0\"\r\n\"09/24/2026 14:00:01.000\",\"140\"\r\n";
        Assert.Equal(100, GpuUsage.ParsePercent(csv));
    }

    [Fact]
    public void ByProcessGroupsOneRowPerPid()
    {
        var byProcess = GpuUsage.ParseByProcess(TypicalOutput);
        Assert.Equal(2, byProcess.Count);
        Assert.Equal(1000, byProcess[0].ProcessId); // busiest first
        Assert.Equal(12.5, byProcess[0].Percent);
        Assert.Equal(2000, byProcess[1].ProcessId);
        Assert.Equal(3.75, byProcess[1].Percent);
    }

    [Fact]
    public void ByProcessSumsMultipleEnginesOfTheSameProcess()
    {
        const string csv =
            "\"ts\",\"\\\\H\\GPU Engine(pid_500_luid_0x0_0x1_phys_0_eng_0_engtype_3D)\\Utilization Percentage\"," +
            "\"\\\\H\\GPU Engine(pid_500_luid_0x0_0x1_phys_0_eng_1_engtype_Copy)\\Utilization Percentage\"\r\n" +
            "\"t0\",\"0\",\"0\"\r\n\"t1\",\"20\",\"5\"\r\n";
        var byProcess = GpuUsage.ParseByProcess(csv);
        Assert.Equal(new GpuProcessUsage(500, 25), Assert.Single(byProcess));
    }

    [Fact]
    public void ByProcessIgnoresInstancesWithoutAPid()
    {
        // A total/_Total-style instance, or anything else the pid_ pattern doesn't match.
        const string csv = "\"ts\",\"\\\\H\\GPU Engine(_Total)\\Utilization Percentage\"\r\n\"t0\",\"0\"\r\n\"t1\",\"50\"\r\n";
        Assert.Empty(GpuUsage.ParseByProcess(csv));
    }

    [Fact]
    public void ByProcessOnUnrecognizedOutputIsEmptyNotAThrow()
    {
        Assert.Empty(GpuUsage.ParseByProcess(""));
    }
}

public class ByteFormatTests
{
    [Theory]
    [InlineData(0, "0 Б")]
    [InlineData(512, "512 Б")]
    [InlineData(1024, "1 КБ")]
    [InlineData(1536, "1,5 КБ")]
    [InlineData(1024L * 1024, "1 МБ")]
    [InlineData(1024L * 1024 * 1024 * 3 + 1024L * 1024 * 471, "3,46 ГБ")]
    public void FormatsRussianSizes(double bytes, string expected) =>
        Assert.Equal(expected, ByteFormat.Size(bytes, CultureInfo.GetCultureInfo("ru-RU"), russian: true));

    [Fact]
    public void FormatsEnglishRate()
    {
        Assert.Equal("1.4 MB/s", ByteFormat.Rate(1024 * 1024 * 1.4, CultureInfo.GetCultureInfo("en-US"), russian: false));
    }
}

public class NoteTitleTests
{
    [Fact]
    public void UsesFirstNonBlankLine()
    {
        Assert.Equal("Идеи для монтажа", NoteTitle.From("\n\n  Идеи для монтажа  \nостальной текст", "Untitled"));
    }

    [Fact]
    public void StripsLeadingHeadingMarks()
    {
        Assert.Equal("Список дел", NoteTitle.From("## Список дел\n- пункт", "Untitled"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n   ")]
    [InlineData("#")]
    public void FallsBackToPlaceholderWhenBlank(string? content) =>
        Assert.Equal("Untitled", NoteTitle.From(content, "Untitled"));

    [Fact]
    public void TruncatesLongLines()
    {
        var title = NoteTitle.From("This first line is definitely longer than the tab can comfortably show", "Untitled");
        Assert.EndsWith("…", title);
        Assert.True(title.Length <= NoteTitle.MaxLength + 1);
    }
}

public class NoteStoreTests
{
    [Fact]
    public void AddCreatesANoteWithAUniqueId()
    {
        var store = new NoteStore();
        var a = store.Add();
        var b = store.Add();
        Assert.Equal(2, store.Notes.Count);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void RemoveDropsTheNote()
    {
        var store = new NoteStore();
        var a = store.Add();
        store.Add();
        Assert.True(store.Remove(a.Id));
        Assert.Single(store.Notes);
        Assert.False(store.Remove(a.Id));
    }

    [Fact]
    public void SetContentUpdatesAndFiresChanged()
    {
        var store = new NoteStore();
        var note = store.Add();
        int changes = 0;
        store.Changed += (_, _) => changes++;
        store.SetContent(note.Id, "hello", DateTimeOffset.UtcNow);
        Assert.Equal("hello", store.Find(note.Id)!.Content);
        Assert.Equal(1, changes);
        // Setting the same content again is a no-op, not a second change.
        store.SetContent(note.Id, "hello", DateTimeOffset.UtcNow);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void PersistsAndReloads()
    {
        var store = new NoteStore();
        var note = store.Add();
        store.SetContent(note.Id, "line one\nline two", DateTimeOffset.UtcNow);
        var json = store.Serialize();

        var back = new NoteStore();
        back.Load(json);
        Assert.Single(back.Notes);
        Assert.Equal("line one\nline two", back.Notes[0].Content);
        Assert.Equal(note.Id, back.Notes[0].Id);
    }
}
