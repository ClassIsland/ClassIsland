using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class TimeSpanToStringConverter : IValueConverter
{
    private TimeSpanToStringConverter() { }

    private const string DefaultTimeSpanFormat = @"d\:hh\:mm\:ss";

    public static readonly TimeSpanToStringConverter Instance = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not TimeSpan timeSpan
            ? TimeSpan.Zero.ToString(DefaultTimeSpanFormat)
            : timeSpan.ToString(DefaultTimeSpanFormat);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s)
        {
            return BindingOperations.DoNothing;
        }

        return TimeSpan.TryParse(s, out var timeSpan) ? timeSpan : BindingOperations.DoNothing;
    }
}