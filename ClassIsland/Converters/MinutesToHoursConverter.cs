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
            if (minutes > 60)
            {
                double hours = Math.Round(minutes / 60.0, 1); // 保留1位小数
                return hours % 1 == 0 ? $"{hours:0} h" : $"{hours:0.0} h";
            }
            return $"{minutes} min";    // 显示分钟
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}