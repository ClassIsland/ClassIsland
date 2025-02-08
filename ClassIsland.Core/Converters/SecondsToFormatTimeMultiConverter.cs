using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 将两个秒数转换为格式化时长
/// </summary>
public class SecondsToFormatTimeMultiConverter : IMultiValueConverter
{
    public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value.Length < 2)
        {
            return "";
        }
        var ceiling = (string?)parameter == "1";
        var v0 = TimeSpan.FromMinutes(Round((value[0] as long? ?? 0) / 60)); // 已过时长或剩余时长
        var v1 = TimeSpan.FromMinutes(Math.Round((double)(value[1] as long? ?? 0) / 60)); // 总时长

        var t0 = v0.TotalHours >= 1 ?
            $"{Math.Floor(v0.TotalHours)}h{v0.Minutes:00}m" :
            $"{v0.Minutes}m";
        var t1 = v1.TotalHours >= 1 ?
            $"{Math.Floor(v1.TotalHours)}h{v1.Minutes:00}m" :
            $"{v1.Minutes}m";
        return $"{t0}/{t1}"; // 时分样式

        double Round(double x) => ceiling ? Math.Ceiling(x) : Math.Floor(x);
    }

    public object[] ConvertBack(object value, Type[] targetType, object? parameter, CultureInfo culture) => Array.Empty<object>();
}