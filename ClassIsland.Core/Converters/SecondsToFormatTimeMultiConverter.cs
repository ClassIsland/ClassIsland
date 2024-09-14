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
        var v0 = TimeSpan.FromSeconds(value[0] as long? ?? 0); // 已过时长或剩余时长
        var v1 = TimeSpan.FromSeconds(value[1] as long? ?? 0); // 总时长

        if (v1 < TimeSpan.FromHours(1))
            return $"{v0.Minutes}/{v1.Minutes}min"; // 传统样式
        if (v1 < TimeSpan.FromMinutes(61))
            return $"{v0.Minutes}/60min"; // 1小时整样式
        else {
            var t0 = v0.TotalHours >= 1 ? // 0h03m -> 3m
                $"{Math.Floor(v0.TotalHours)}h{v0.Minutes:00}m" :
                $"{v0.Minutes}m";
            return $"{t0}/{Math.Floor(v1.TotalHours)}h{v1.Minutes:00}m"; // 时分样式
        }

    }

    public object[] ConvertBack(object value, Type[] targetType, object? parameter, CultureInfo culture) => new object[] { };
}