using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 将 <see cref="TimeLayoutItem.TimeType"/> 转换为对应的 FluentIcon 图标的值转换器。
/// </summary>
public class TimeTypeToIconGlyphConverter : IValueConverter
{
    /// <summary>
    /// 获取 <see cref="TimeTypeToIconGlyphConverter"/> 的实例。
    /// </summary>
    public static readonly TimeTypeToIconGlyphConverter Instance = new();

    private TimeTypeToIconGlyphConverter() {}
    
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int type)
        {
            return "";
        }
        return type switch
        {
            0 => "\ue47a",
            1 => "\ue4c4",
            2 => "\uf021",
            3 => "\ue01f",
            _ => ""
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return 0;
    }
}