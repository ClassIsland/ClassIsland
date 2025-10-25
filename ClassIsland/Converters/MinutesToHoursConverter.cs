using System;
using System.Globalization;
using System.Windows.Data;
namespace ClassIsland.Converters;
public class MinutesToHoursConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
            // 处理负数：取绝对值进行计算
            var absMinutes = Math.Abs(minutes);
            
            if (absMinutes > 60)
            {
                // 如果有负数，在结果前添加负号
                string sign = minutes < 0 ? "-" : "";
                return $"{sign}{absMinutes / 60}";
            }
            
            return minutes.ToString(); // 返回原始值（包含可能的负号）
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
