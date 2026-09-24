using System.Globalization;
using System.Text.RegularExpressions;
using Grip.Core.Localization;

namespace Grip.Core.Calc;

public enum UnitCategory { Length, Mass, Temperature, Volume, Speed, Data, Time, Area }

public sealed record UnitDefinition(
    string Id,
    UnitCategory Category,
    double Factor,
    string SymbolRu,
    string SymbolEn,
    string[] Aliases)
{
    public string Symbol(UiLanguage language) => language == UiLanguage.Ru ? SymbolRu : SymbolEn;
}

public sealed record UnitConversion(double Value, UnitDefinition From, double Result, UnitDefinition To);

/// <summary>
/// "10 km to mi", "10 км в милях", "100f to c", "1,5 гб в мб". Without a
/// target unit it converts to the usual counterpart (km → mi, °C → °F, …).
/// Data sizes follow Windows: 1 KB = 1024 bytes; network bits are decimal.
/// </summary>
public static partial class UnitConverter
{
    private static readonly List<UnitDefinition> Units = new()
    {
        // Length, base: metre
        new("mm", UnitCategory.Length, 0.001, "мм", "mm", new[] { "mm", "мм", "миллиметр", "миллиметра", "миллиметров", "millimeter", "millimeters", "millimetre" }),
        new("cm", UnitCategory.Length, 0.01, "см", "cm", new[] { "cm", "см", "сантиметр", "сантиметра", "сантиметров", "centimeter", "centimeters", "centimetre" }),
        new("m", UnitCategory.Length, 1, "м", "m", new[] { "m", "м", "метр", "метра", "метров", "meter", "meters", "metre", "metres" }),
        new("km", UnitCategory.Length, 1000, "км", "km", new[] { "km", "км", "километр", "километра", "километров", "kilometer", "kilometers", "kilometre" }),
        new("in", UnitCategory.Length, 0.0254, "дюйм", "in", new[] { "in", "inch", "inches", "\"", "дюйм", "дюйма", "дюймов", "дюймах" }),
        new("ft", UnitCategory.Length, 0.3048, "фут", "ft", new[] { "ft", "foot", "feet", "'", "фут", "фута", "футов", "футах" }),
        new("yd", UnitCategory.Length, 0.9144, "ярд", "yd", new[] { "yd", "yard", "yards", "ярд", "ярда", "ярдов" }),
        new("mi", UnitCategory.Length, 1609.344, "миль", "mi", new[] { "mi", "mile", "miles", "миля", "мили", "миль", "милях", "милю" }),
        new("nmi", UnitCategory.Length, 1852, "мор. миль", "nmi", new[] { "nmi", "морская", "морских", "nautical" }),

        // Mass, base: kilogram
        new("mg", UnitCategory.Mass, 1e-6, "мг", "mg", new[] { "mg", "мг", "миллиграмм", "миллиграмма", "миллиграммов" }),
        new("g", UnitCategory.Mass, 0.001, "г", "g", new[] { "g", "г", "гр", "грамм", "грамма", "граммов", "gram", "grams" }),
        new("kg", UnitCategory.Mass, 1, "кг", "kg", new[] { "kg", "кг", "килограмм", "килограмма", "килограммов", "kilogram", "kilograms", "kilo", "kilos" }),
        new("t", UnitCategory.Mass, 1000, "т", "t", new[] { "t", "т", "тонна", "тонны", "тонн", "ton", "tons", "tonne", "tonnes" }),
        new("oz", UnitCategory.Mass, 0.028349523125, "унц.", "oz", new[] { "oz", "ounce", "ounces", "унция", "унции", "унций" }),
        new("lb", UnitCategory.Mass, 0.45359237, "фунт.", "lb", new[] { "lb", "lbs", "pound", "pounds", "фунт", "фунта", "фунтов", "фунтах" }),

        // Temperature: handled by formula, factor unused
        new("c", UnitCategory.Temperature, 1, "°C", "°C", new[] { "c", "°c", "°с", "celsius", "цельсий", "цельсия", "цельсию" }),
        new("f", UnitCategory.Temperature, 1, "°F", "°F", new[] { "f", "°f", "fahrenheit", "фаренгейт", "фаренгейта", "фаренгейту" }),
        new("k", UnitCategory.Temperature, 1, "K", "K", new[] { "k", "kelvin", "кельвин", "кельвина", "кельвинов" }),

        // Volume, base: litre
        new("ml", UnitCategory.Volume, 0.001, "мл", "ml", new[] { "ml", "мл", "миллилитр", "миллилитра", "миллилитров", "milliliter", "milliliters" }),
        new("l", UnitCategory.Volume, 1, "л", "l", new[] { "l", "л", "литр", "литра", "литров", "литрах", "liter", "liters", "litre", "litres" }),
        new("m3", UnitCategory.Volume, 1000, "м³", "m³", new[] { "m3", "м3", "м³", "m³", "куб", "кубометр", "кубометра", "кубометров" }),
        new("gal", UnitCategory.Volume, 3.785411784, "галл.", "gal", new[] { "gal", "gallon", "gallons", "галлон", "галлона", "галлонов" }),
        new("qt", UnitCategory.Volume, 0.946352946, "кварт", "qt", new[] { "qt", "quart", "quarts", "кварта", "кварты", "кварт" }),
        new("pt", UnitCategory.Volume, 0.473176473, "пинт", "pt", new[] { "pt", "pint", "pints", "пинта", "пинты", "пинт" }),
        new("floz", UnitCategory.Volume, 0.0295735295625, "жидк. унц.", "fl oz", new[] { "floz", "fl oz", "fluid ounce", "fluid ounces" }),
        new("cup", UnitCategory.Volume, 0.2365882365, "чаш.", "cup", new[] { "cup", "cups", "чашка", "чашки", "чашек" }),

        // Speed, base: metre per second
        new("mps", UnitCategory.Speed, 1, "м/с", "m/s", new[] { "m/s", "м/с", "mps" }),
        new("kmh", UnitCategory.Speed, 1000.0 / 3600.0, "км/ч", "km/h", new[] { "km/h", "км/ч", "kmh", "kph", "кмч" }),
        new("mph", UnitCategory.Speed, 1609.344 / 3600.0, "миль/ч", "mph", new[] { "mph", "mi/h", "миль/ч" }),
        new("kn", UnitCategory.Speed, 1852.0 / 3600.0, "уз.", "kn", new[] { "kn", "knot", "knots", "узел", "узла", "узлов" }),

        // Data, base: byte
        new("bit", UnitCategory.Data, 1.0 / 8, "бит", "bit", new[] { "bit", "bits", "бит", "бита", "битов" }),
        new("kbit", UnitCategory.Data, 1000.0 / 8, "Кбит", "Kbit", new[] { "kbit", "kbits", "kb/s", "кбит", "kbps" }),
        new("mbit", UnitCategory.Data, 1e6 / 8, "Мбит", "Mbit", new[] { "mbit", "mbits", "мбит", "mbps", "мбит/с" }),
        new("gbit", UnitCategory.Data, 1e9 / 8, "Гбит", "Gbit", new[] { "gbit", "gbits", "гбит", "gbps", "гбит/с" }),
        new("b", UnitCategory.Data, 1, "Б", "B", new[] { "byte", "bytes", "байт", "байта", "байтов", "б" }),
        new("kb", UnitCategory.Data, 1024, "КБ", "KB", new[] { "kb", "kib", "кб", "килобайт", "килобайта", "килобайтов", "kilobyte", "kilobytes" }),
        new("mb", UnitCategory.Data, 1024.0 * 1024, "МБ", "MB", new[] { "mb", "mib", "мб", "мегабайт", "мегабайта", "мегабайтов", "megabyte", "megabytes" }),
        new("gb", UnitCategory.Data, 1024.0 * 1024 * 1024, "ГБ", "GB", new[] { "gb", "gib", "гб", "гигабайт", "гигабайта", "гигабайтов", "gigabyte", "gigabytes", "гиг" }),
        new("tb", UnitCategory.Data, 1024.0 * 1024 * 1024 * 1024, "ТБ", "TB", new[] { "tb", "tib", "тб", "терабайт", "терабайта", "терабайтов", "terabyte", "terabytes" }),

        // Time, base: second
        new("ms", UnitCategory.Time, 0.001, "мс", "ms", new[] { "ms", "мс", "миллисекунда", "миллисекунды", "миллисекунд", "millisecond", "milliseconds" }),
        new("s", UnitCategory.Time, 1, "с", "s", new[] { "s", "sec", "secs", "second", "seconds", "с", "сек", "секунда", "секунды", "секунд", "секундах" }),
        new("min", UnitCategory.Time, 60, "мин", "min", new[] { "min", "mins", "minute", "minutes", "мин", "минута", "минуты", "минут", "минутах" }),
        new("h", UnitCategory.Time, 3600, "ч", "h", new[] { "h", "hr", "hrs", "hour", "hours", "ч", "час", "часа", "часов", "часах" }),
        new("d", UnitCategory.Time, 86400, "сут", "d", new[] { "d", "day", "days", "д", "день", "дня", "дней", "сут", "сутки", "суток", "днях" }),
        new("wk", UnitCategory.Time, 604800, "нед", "wk", new[] { "wk", "week", "weeks", "нед", "неделя", "недели", "недель", "неделях" }),
        new("yr", UnitCategory.Time, 31557600, "г.", "yr", new[] { "yr", "year", "years", "год", "года", "лет", "годах" }),

        // Area, base: square metre
        new("cm2", UnitCategory.Area, 1e-4, "см²", "cm²", new[] { "cm2", "см2", "см²", "cm²" }),
        new("m2", UnitCategory.Area, 1, "м²", "m²", new[] { "m2", "м2", "м²", "m²", "кв.м", "квм" }),
        new("km2", UnitCategory.Area, 1e6, "км²", "km²", new[] { "km2", "км2", "км²", "km²" }),
        new("ha", UnitCategory.Area, 1e4, "га", "ha", new[] { "ha", "га", "гектар", "гектара", "гектаров", "hectare", "hectares" }),
        new("ac", UnitCategory.Area, 4046.8564224, "акр", "ac", new[] { "ac", "acre", "acres", "акр", "акра", "акров" }),
        new("ft2", UnitCategory.Area, 0.09290304, "фут²", "ft²", new[] { "ft2", "ft²", "sqft", "sq ft" }),
    };

    private static readonly Dictionary<string, UnitDefinition> ByAlias = BuildAliases();

    private static readonly Dictionary<string, string> DefaultTargets = new()
    {
        ["km"] = "mi", ["mi"] = "km", ["m"] = "ft", ["ft"] = "m", ["cm"] = "in", ["in"] = "cm", ["mm"] = "in",
        ["yd"] = "m", ["nmi"] = "km",
        ["kg"] = "lb", ["lb"] = "kg", ["g"] = "oz", ["oz"] = "g", ["t"] = "lb", ["mg"] = "g",
        ["c"] = "f", ["f"] = "c", ["k"] = "c",
        ["l"] = "gal", ["gal"] = "l", ["ml"] = "floz", ["floz"] = "ml", ["cup"] = "ml", ["qt"] = "l", ["pt"] = "l", ["m3"] = "l",
        ["kmh"] = "mph", ["mph"] = "kmh", ["mps"] = "kmh", ["kn"] = "kmh",
        ["mbit"] = "mb", ["gbit"] = "gb", ["kbit"] = "kb", ["mb"] = "gb", ["gb"] = "mb", ["kb"] = "mb", ["tb"] = "gb", ["b"] = "kb", ["bit"] = "b",
        ["h"] = "min", ["min"] = "s", ["s"] = "ms", ["d"] = "h", ["wk"] = "d", ["yr"] = "d", ["ms"] = "s",
        ["m2"] = "ft2", ["ft2"] = "m2", ["ha"] = "ac", ["ac"] = "ha", ["km2"] = "ha", ["cm2"] = "m2",
    };

    private static Dictionary<string, UnitDefinition> BuildAliases()
    {
        var map = new Dictionary<string, UnitDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var unit in Units)
        {
            map.TryAdd(unit.Id, unit);
            foreach (var alias in unit.Aliases) map.TryAdd(alias, unit);
        }
        return map;
    }

    // "<number> <unit> [to|in|в|во|->|=] [<unit>]"
    [GeneratedRegex(@"^\s*(?<num>[-+]?\d[\d\s ]*(?:[.,]\d+)?|[-+]?[.,]\d+)\s*(?<from>.+?)(?:\s+(?:to|in|into|as|в|во|на|->|=>|=)\s+(?<to>.+?))?\s*\??\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex QueryRegex();

    public static bool TryConvert(string input, out UnitConversion conversion)
    {
        conversion = null!;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 80) return false;
        var m = QueryRegex().Match(input);
        if (!m.Success) return false;

        var numberText = m.Groups["num"].Value.Replace(" ", "").Replace(" ", "").Replace(',', '.');
        if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return false;

        if (!TryFindUnit(m.Groups["from"].Value, out var from)) return false;

        UnitDefinition? to = null;
        if (m.Groups["to"].Success)
        {
            if (!TryFindUnit(m.Groups["to"].Value, out var target) || target.Category != from.Category) return false;
            to = target;
        }
        else if (DefaultTargets.TryGetValue(from.Id, out var targetId))
        {
            to = Units.First(u => u.Id == targetId);
        }
        if (to == null || to.Id == from.Id) return false;

        conversion = new UnitConversion(value, from, Convert(value, from, to), to);
        return true;
    }

    private static bool TryFindUnit(string text, out UnitDefinition unit)
    {
        var key = text.Trim().TrimEnd('.', '?').Trim();
        if (ByAlias.TryGetValue(key, out unit!)) return true;
        // "градусов цельсия" → "цельсия", "в милях" already stripped by the regex.
        var last = key.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return last != null && ByAlias.TryGetValue(last, out unit!);
    }

    public static double Convert(double value, UnitDefinition from, UnitDefinition to)
    {
        if (from.Category == UnitCategory.Temperature)
        {
            double celsius = from.Id switch
            {
                "f" => (value - 32) * 5 / 9,
                "k" => value - 273.15,
                _ => value,
            };
            return to.Id switch
            {
                "f" => celsius * 9 / 5 + 32,
                "k" => celsius + 273.15,
                _ => celsius,
            };
        }
        return value * from.Factor / to.Factor;
    }

    /// <summary>"10 км = 6,2137 миль".</summary>
    public static string Describe(UnitConversion c, UiLanguage language)
    {
        var culture = language == UiLanguage.Ru ? CultureInfo.GetCultureInfo("ru-RU") : CultureInfo.GetCultureInfo("en-US");
        return $"{FormatValue(c.Value, culture)} {c.From.Symbol(language)} = {FormatValue(c.Result, culture)} {c.To.Symbol(language)}";
    }

    public static string FormatValue(double value, CultureInfo culture)
    {
        double abs = Math.Abs(value);
        if (abs != 0 && (abs >= 1e12 || abs < 1e-6)) return value.ToString("0.####E+0", culture);
        int decimals = abs >= 100 ? 2 : abs >= 1 ? 4 : 6;
        return Math.Round(value, decimals).ToString("#,##0.######", culture);
    }
}
