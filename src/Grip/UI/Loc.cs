using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Grip.Core.Localization;

namespace Grip.UI;

/// <summary>
/// Observable face of the localizer for XAML bindings. Raising "Item[]"
/// refreshes every bound string when the language changes.
/// </summary>
public sealed class LocSource : INotifyPropertyChanged
{
    public static LocSource Instance { get; } = new();

    private LocSource()
    {
        Localizer.Instance.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] => Localizer.Instance.Get(key);

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// XAML shortcut for a localized string: <c>Text="{l:T panel.settings}"</c>.
/// Set <c>Upper=True</c> for section captions.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class T : MarkupExtension
{
    public T() { }

    public T(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = "";

    public bool Upper { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocSource.Instance,
            Mode = BindingMode.OneWay,
        };
        if (Upper) binding.Converter = UpperConverter.Instance;
        return binding.ProvideValue(serviceProvider);
    }
}

public sealed class UpperConverter : IValueConverter
{
    public static UpperConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value as string)?.ToUpper(Localizer.Instance.Culture) ?? value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}

/// <summary>Short-hand for code-behind: <c>L.S("key")</c>.</summary>
public static class L
{
    public static string S(string key) => Localizer.Instance.Get(key);

    public static string F(string key, params object?[] args) => Localizer.Instance.Format(key, args);

    public static string Count(string key, long count) => Localizer.Instance.Count(key, count);
}
