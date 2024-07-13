using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class SecondsLongToFormatHoursMinutesStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var useFloor = parameter as bool? ?? false;
        var v = TimeSpan.FromMinutes(Round(TimeSpan.FromSeconds(value as long? ?? 0).TotalMinutes));
        return v.TotalSeconds switch
        {
            >= 3600 => $@"{Math.Floor(v.TotalHours)}h{v.Minutes}m",
            >= 0 => $"{v.Minutes}m",
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