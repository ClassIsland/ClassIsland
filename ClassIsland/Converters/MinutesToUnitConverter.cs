using System;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class MinutesToUnitConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            // 使用绝对值来判断，但保持负数的单位逻辑
            var absMinutes = Math.Abs(minutes);
            
            // 当分钟数的绝对值大于60时返回"h"，否则返回"min"
            return absMinutes >= 60 ? "h" : "min";
        }

        // 默认返回分钟单位
        return "min";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
