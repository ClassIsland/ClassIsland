using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class StringPreventEmptyConverter : IValueConverter
{
    public static StringPreventEmptyConverter Instance { get; } = new();
    
    private StringPreventEmptyConverter()
    {
        
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
        {
            return BindingOperations.DoNothing;
        }

        return value;
    }
}