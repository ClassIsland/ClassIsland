using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 将多个<see cref="double"/>转换为<see cref="Thickness"/>
/// </summary>
public class DoubleToThicknessMultiConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Length switch
        {
            1 => new Thickness(values[0] as double? ?? 0),
            2 => new Thickness(values[0] as double? ?? 0, values[1] as double? ?? 0,
                values[0] as double? ?? 0, values[1] as double? ?? 0
            ),
            4 => new Thickness(values[0] as double? ?? 0, values[1] as double? ?? 0,
                values[2] as double? ?? 0, values[3] as double? ?? 0
            ),
            _ => new Thickness()
        };
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}