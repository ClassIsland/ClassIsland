using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class MinutesToApproxTimeConverter : IValueConverter
{
    public static readonly MinutesToApproxTimeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return "";
        }

        var minutes = value switch
        {
            int i => i,
            double d => (int)Math.Round(d),
            string s when int.TryParse(s, out var i) => i,
            _ => 0
        };

        var isNegative = minutes < 0;
        var absMinutes = Math.Abs(minutes);

        if (absMinutes == 0)
        {
            return "0min";
        }

        var approxPrefix = isNegative ? "-~" : "~";

        if (absMinutes >= 60)
        {
            var hours = absMinutes / 60.0;
            var rounded = Math.Round(hours * 2.0) / 2.0;

            var text = Math.Abs(rounded % 1.0) < 1e-9
                ? ((int)rounded).ToString(culture)
                : rounded.ToString("0.0", culture);
            return approxPrefix + text + "h";
        }

        var roundedMinutes = (int)Math.Max(1, Math.Round(absMinutes / 5.0) * 5.0);
        return approxPrefix + roundedMinutes.ToString(culture) + "min";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}