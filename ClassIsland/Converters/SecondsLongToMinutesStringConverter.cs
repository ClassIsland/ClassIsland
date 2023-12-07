using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class SecondsLongToMinutesStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (long)value;
        return $"{Math.Ceiling(v*1.0/60):F0}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}