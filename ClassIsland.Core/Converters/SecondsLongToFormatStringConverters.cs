using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class SecondsLongToFormatHoursMinutesStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = TimeSpan.FromSeconds(value as long? ?? 0);
        var useFloor = parameter as bool? ?? false;
        return v.TotalSeconds switch
        {
            >= 3600 => $@"{Round(v.TotalHours)}h{Round(v.TotalMinutes % 60.0)}m",
            >= 0 => $"{Round(v.TotalMinutes)}m",
            _ => ""
        };

        double Round(double x) => useFloor ? Math.Floor(x) : Math.Ceiling(x);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
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