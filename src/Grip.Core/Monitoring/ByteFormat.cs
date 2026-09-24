using System.Globalization;
using Grip.Core.Calc;

namespace Grip.Core.Monitoring;

/// <summary>
/// Human-readable byte sizes and rates. Binary units (1024-based) to match
/// how Windows itself reports memory and disk sizes.
/// </summary>
public static class ByteFormat
{
    private static readonly string[] UnitsRu = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
    private static readonly string[] UnitsEn = { "B", "KB", "MB", "GB", "TB" };

    /// <summary>A size such as "3,46 ГБ". <paramref name="russian"/> picks the unit spelling.</summary>
    public static string Size(double bytes, CultureInfo culture, bool russian)
    {
        var units = russian ? UnitsRu : UnitsEn;
        double sign = bytes < 0 ? -1 : 1;
        double value = Math.Abs(bytes);
        int i = 0;
        while (value >= 1024 && i < units.Length - 1)
        {
            value /= 1024;
            i++;
        }
        int decimals = value >= 100 ? 0 : value >= 10 ? 1 : 2;
        string number = Calculator.Format(sign * Math.Round(value, decimals), culture, grouping: false);
        return $"{number} {units[i]}";
    }

    /// <summary>A rate such as "1,4 МБ/с" / "1.4 MB/s".</summary>
    public static string Rate(double bytesPerSecond, CultureInfo culture, bool russian) =>
        Size(bytesPerSecond, culture, russian) + (russian ? "/с" : "/s");
}
