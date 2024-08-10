using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class SecondsLongToMinutesSecondsStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = (long)value;
        var seconds = v % 60;
        var minutes = v / 60;
        return $"{minutes:D2}\'{seconds:D2}\"";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
