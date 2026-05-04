using System.Globalization;

namespace KampusBag.MobileUI.Converters;

/// <summary>
/// bool → !bool  (IsVisible binding için)
/// </summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>
/// IsReadOnly bool → Placeholder metni
/// true  → "Bu kanal sadece okunabilir 🔒"
/// false → "Mesajınızı yazın..."
/// </summary>
public class ReadOnlyPlaceholderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => value is true
            ? "Bu kanal sadece okunabilir 🔒"
            : "Mesajınızı yazın...";

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
