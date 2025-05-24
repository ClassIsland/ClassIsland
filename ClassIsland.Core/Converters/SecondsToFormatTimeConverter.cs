using System.Globalization;
using System.Windows;
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
public class SecondsToFormatTimeConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty CeilingProperty = DependencyProperty.Register(
        nameof(Ceiling), typeof(bool), typeof(SecondsToFormatTimeConverter), new PropertyMetadata(default(bool)));

    public bool Ceiling
    {
        get { return (bool)GetValue(CeilingProperty); }
        set { SetValue(CeilingProperty, value); }
    }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var withSeconds = parameter as bool? ?? false;

        if (withSeconds) {
            var v = TimeSpan.FromSeconds(value as long? ?? value as int? ?? 0);
            return v.TotalSeconds switch // 显示秒数
            {
                >= 3600 => $"{Math.Floor(v.TotalHours)}:{v.Minutes:00}:{v.Seconds:00}",
                >= 60 => $"{v.Minutes}:{v.Seconds:00}",
                >= 0 => $"{v.Seconds}s",
                _ => ""
            };
        }

        var rounded = TimeSpan.FromMinutes(Round(TimeSpan.FromSeconds(value as long? ?? value as int? ?? 0).TotalMinutes));
        return rounded.TotalSeconds switch // 不显示秒数
        {
            >= 3600 => $"{Math.Floor(rounded.TotalHours)}h{rounded.Minutes:00}m",
            //>= 3600 => "60min", // 1小时整样式
            >= 0 => $"{rounded.Minutes}min",
            _ => ""
        };

        double Round(double x) => Ceiling ? Math.Ceiling(x) : Math.Floor(x);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}