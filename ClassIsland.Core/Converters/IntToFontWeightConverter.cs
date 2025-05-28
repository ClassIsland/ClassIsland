using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ClassIsland.Core.Converters;

/// <summary>
/// <see cref="int"/> 到 <see cref="FontWeight"/> 的转换器。
/// </summary>
public class IntToFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue switch
            {
                100 => FontWeight.Thin,
                200 => FontWeight.ExtraLight,
                300 => FontWeight.Light,
                400 => FontWeight.Normal,
                500 => FontWeight.Medium,
                600 => FontWeight.SemiBold,
                700 => FontWeight.Bold,
                800 => FontWeight.ExtraBold,
                900 => FontWeight.Black,
                _ => FontWeight.Normal
            };
        }
        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FontWeight weight)
        {
            return (int)weight;
        }

        return 400;
    }
}