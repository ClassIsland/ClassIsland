using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class StringToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return double.TryParse(value as string, out var result) ? result : 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }
}