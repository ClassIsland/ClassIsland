using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = System.Convert.ToString(value);
        return r ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return int.Parse((string)value);
    }
}