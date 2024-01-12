using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class SizeLongToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return NetworkSpeedFormater.FormatFileSize((long)d);
        }
        else
        {
            return NetworkSpeedFormater.FormatFileSize((long)value);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}