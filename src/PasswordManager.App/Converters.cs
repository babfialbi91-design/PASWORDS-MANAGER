using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PasswordManager.Services;

namespace PasswordManager.App;

public sealed class StrengthLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Localization.Strength(PasswordQuality.Strength(value as string ?? string.Empty));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StrengthColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var brush = PasswordQuality.Strength(value as string ?? string.Empty) switch
        {
            PasswordStrength.VeryStrong or PasswordStrength.Strong => "#35D07F",
            PasswordStrength.Medium => "#FFB020",
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
