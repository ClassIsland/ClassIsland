using System;
using System.Globalization;
using System.Windows.Data;
namespace ClassIsland.Converters;
public class MinutesToHoursConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            if (minutes > 60) return (minutes / 60).ToString(); // 返回小时数
            return minutes.ToString(); // 返回分钟数
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
