using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var r = System.Convert.ToString(value);
        return r ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType == typeof(int))
        {
            if (int.TryParse((string)value, out var re))
            {
                return re;
            }
        }

        if (targetType == typeof(double))
        {
            if (double.TryParse((string)value, out var re))
            {
                return re;
            }
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse((string)value, out var re))
            {
                return re;
            }
        }

        return 0;
    }
}