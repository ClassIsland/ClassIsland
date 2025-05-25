using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class RippleEffectTranslationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double d)
        {
            return 0.0;
        }
        //Console.WriteLine(-d / 2);
        return -d / 2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}