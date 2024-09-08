using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 将秒数转换为格式化时长
/// </summary>
/// <param name="withSeconds">
/// 转换后是否显示秒数
/// </param>
/// <returns>
/// 显示秒数形似：1:02:34，12:34，34s<br/>
/// 不显示秒数形似：1h02m，12min
/// </returns>
public class SecondsToFormatTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var withSeconds = parameter as bool? ?? false;
        var v = TimeSpan.FromSeconds(value as long? ?? 0);

        if (withSeconds) {
            return v.TotalSeconds switch // 显示秒数
            {
                >= 3600 => $"{Math.Floor(v.TotalHours)}:{v.Minutes:00}:{v.Seconds:00}",
                >= 60 => $"{v.Minutes}:{v.Seconds:00}",
                >= 0 => $"{v.Seconds}s",
                _ => ""
            };
        } else {
            return v.TotalSeconds switch // 不显示秒数
            {
                >= 3660 => $"{Math.Floor(v.TotalHours)}h{v.Minutes:00}m",
                >= 3600 => "60min", // 1小时整样式
                >= 0 => $"{v.Minutes}min",
                _ => ""
            };
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}