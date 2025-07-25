using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class ClassChangeIsSwapModeOnToSwapModeIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool b)
        {
            return 0;
        }

        return b ? 1 : 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int i)
        {
            return false;
        }

        return i == 1;
    }
    
    private ClassChangeIsSwapModeOnToSwapModeIndexConverter() {}

    public static readonly ClassChangeIsSwapModeOnToSwapModeIndexConverter Instance = new();
}