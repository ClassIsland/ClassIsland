using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 将两个秒数转换为格式化时长
/// </summary>
/// <returns>
/// 形似 1h02m/1h12m，12m/1h12m，8/36min
/// </returns>
public class SecondsToFormatTimeMultiConverter : IMultiValueConverter
{
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value.Length < 2)
        {
            return "";
        }
        var ceiling = (string?)parameter == "1";
        var v0 = TimeSpan.FromMinutes(Round(TimeSpan.FromSeconds(value[0] as long? ?? 0).TotalMinutes)); // 已过时长或剩余时长
        var v1 = TimeSpan.FromSeconds(value[1] as long? ?? 0); // 总时长

        var t0 = v0.TotalHours >= 1 ? // 0h03m -> 3m
            $"{Math.Floor(v0.TotalHours)}h{v0.Minutes:00}m" :
            $"{v0.TotalMinutes}m";
        var t1 = v1.TotalHours >= 1 ? // 0h03m -> 3m
            $"{Math.Floor(v1.TotalHours)}h{v1.Minutes:00}m" :
            $"{v1.TotalMinutes}m";
        return $"{t0}/{t1}"; // 时分样式

        double Round(double x) => ceiling ? Math.Ceiling(x) : Math.Floor(x);
    }

    public object[] ConvertBack(object value, Type[] targetType, object? parameter, CultureInfo culture) => new object[] { };
}