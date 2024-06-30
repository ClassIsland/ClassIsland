using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class SecondsLongToFormatHoursMinutesStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = new TimeSpan(0, 0, System.Convert.ToInt32(value) + 60); // 分钟向上取整
        return v.TotalSeconds switch
        {
            >= 3600 => v.ToString(@"h\:m"),
            >= 0 => v.ToString(@"%m"),
            _ => (object)null
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}
public class SecondsLongToFormatHoursMinutesSecondsStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = new TimeSpan(0, 0, System.Convert.ToInt32(value));
        return v.TotalSeconds switch
        {
            >= 3600 => v.ToString(@"h\:mm\:ss"),
            >= 60 => v.ToString(@"m\:ss"),
            >= 0 => v.ToString(@"s\s"),
            _ => (object)null
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}