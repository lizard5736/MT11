using System.Globalization;
using Grip.Core.Settings;

namespace Grip.Core.Localization;

public enum UiLanguage { Ru, En }

/// <summary>
/// String lookup for the two shipped languages. Keys live in
/// <see cref="StringTable"/> with both translations side by side, so a
/// missing translation fails the test suite instead of reaching a user.
/// </summary>
public sealed class Localizer
{
    public static Localizer Instance { get; } = new();

    private UiLanguage _language = UiLanguage.Ru;

    public UiLanguage Language => _language;

    public CultureInfo Culture => _language == UiLanguage.Ru
        ? CultureInfo.GetCultureInfo("ru-RU")
        : CultureInfo.GetCultureInfo("en-US");

    public event EventHandler? LanguageChanged;

    public void SetLanguage(UiLanguage language)
    {
        if (_language == language) return;
        _language = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public static UiLanguage Resolve(AppLanguage setting, CultureInfo uiCulture) => setting switch
    {
        AppLanguage.Russian => UiLanguage.Ru,
        AppLanguage.English => UiLanguage.En,
        _ => uiCulture.TwoLetterISOLanguageName is "ru" or "uk" or "be" or "kk" or "ky" or "uz" or "tg" or "hy" or "az" or "ka"
            ? UiLanguage.Ru
            : UiLanguage.En,
    };

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (StringTable.All.TryGetValue(key, out var entry))
            return _language == UiLanguage.Ru ? entry.Ru : entry.En;
        return key;
    }

    public bool Has(string key) => StringTable.All.ContainsKey(key);

    public string Format(string key, params object?[] args) =>
        string.Format(Culture, Get(key), args);

    /// <summary>
    /// Picks the plural form for <paramref name="count"/>. Russian entries hold
    /// three forms ("запись|записи|записей"), English two ("item|items").
    /// </summary>
    public string Plural(string key, long count)
    {
        var forms = Get(key).Split('|');
        return PickPlural(forms, count, _language);
    }

    /// <summary>"5 записей", "1 item".</summary>
    public string Count(string key, long count) =>
        count.ToString("N0", Culture) + " " + Plural(key, count);

    public static string PickPlural(string[] forms, long count, UiLanguage language)
    {
        if (forms.Length == 0) return string.Empty;
        long n = Math.Abs(count);
        if (language == UiLanguage.Ru)
        {
            if (forms.Length < 3) return forms[n == 1 ? 0 : forms.Length - 1];
            long mod10 = n % 10, mod100 = n % 100;
            if (mod10 == 1 && mod100 != 11) return forms[0];
            if (mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14) return forms[1];
            return forms[2];
        }
        return n == 1 ? forms[0] : forms[Math.Min(1, forms.Length - 1)];
    }

    /// <summary>Text that may be a key reference ("@radial.item.media") or a literal the user typed.</summary>
    public string Resolve(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text[0] == '@' ? Get(text[1..]) : text;
    }

    /// <summary>"1 ч 24 мин", "1 h 24 min", "45 с".</summary>
    public string Duration(TimeSpan span)
    {
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;
        var parts = new List<string>(3);
        if (span.TotalDays >= 1) parts.Add(((int)span.TotalDays).ToString(Culture) + " " + Get("time.d"));
        if (span.Hours > 0) parts.Add(span.Hours.ToString(Culture) + " " + Get("time.h"));
        if (span.Minutes > 0 && span.TotalDays < 1) parts.Add(span.Minutes.ToString(Culture) + " " + Get("time.min"));
        if (parts.Count == 0) parts.Add(Math.Max(0, span.Seconds).ToString(Culture) + " " + Get("time.s"));
        return string.Join(" ", parts);
    }

    /// <summary>"только что", "5 мин назад", "вчера", "12 мар".</summary>
    public string Ago(DateTimeOffset moment, DateTimeOffset now)
    {
        var delta = now - moment;
        if (delta < TimeSpan.FromMinutes(1)) return Get("time.justNow");
        if (delta < TimeSpan.FromHours(1)) return Format("time.minutesAgo", (int)delta.TotalMinutes);
        if (delta < TimeSpan.FromHours(24) && moment.LocalDateTime.Date == now.LocalDateTime.Date)
            return Format("time.hoursAgo", (int)delta.TotalHours);
        if (moment.LocalDateTime.Date == now.LocalDateTime.Date.AddDays(-1)) return Get("time.yesterday");
        return moment.LocalDateTime.ToString(moment.Year == now.Year ? "d MMM" : "d MMM yyyy", Culture);
    }
}
