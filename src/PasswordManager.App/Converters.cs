using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PasswordManager.Services;

namespace PasswordManager.App;

public sealed class StrengthLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => PasswordQuality.StrengthLabel(value as string ?? string.Empty);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StrengthColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var label = PasswordQuality.StrengthLabel(value as string ?? string.Empty);
        var brush = label switch
        {
            "قوية جداً" => "#35D07F",
            "قوية" => "#35D07F",
            "متوسطة" => "#FFB020",
            _ => "#FF5C5C"
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(brush));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class EmptyToDashConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? "—" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
