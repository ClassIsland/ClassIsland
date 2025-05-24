using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class LargeNumberToFriendlyNumberConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long number)
        {
            return "";
        }

        return number switch
        {
            < 1000 => number.ToString(),
            < 1000_000 => $"{Math.Round((double)number / 1000, 1)}K",
            < 1000_000_000 => $"{Math.Round((double)number / 1000_000, 1)}M",
            _ => $"{Math.Round((double)number / 1000_000_000, 1)}B"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}