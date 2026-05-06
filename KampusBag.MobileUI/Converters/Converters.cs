using System.Globalization;

namespace KampusBag.MobileUI.Converters;

/// <summary>bool → !bool</summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is bool b && !b;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => value is bool b && !b;
}

/// <summary>IsReadOnly bool → Placeholder metni</summary>
public class ReadOnlyPlaceholderConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? "Bu kanal sadece okunabilir 🔒" : "Mesajınızı yazın...";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>IsSending bool → Gönder butonunun ikonu</summary>
public class BoolToSendIconConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? "⏳" : "➤";
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>CanSend bool → Gönder butonunun rengi</summary>
public class BoolToSendColorConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true
            ? Color.FromArgb("#1B305E")
            : Color.FromArgb("#D1D5DB");
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>Generic bool → renk converter (XAML'da TrueValue/FalseValue parametreli)</summary>
public class BoolToColorConverter : IValueConverter
{
    public string TrueValue { get; set; } = "#059669";
    public string FalseValue { get; set; } = "#1B305E";

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true
            ? Color.FromArgb(TrueValue)
            : Color.FromArgb(FalseValue);
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
