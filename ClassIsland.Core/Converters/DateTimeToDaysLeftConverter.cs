using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

[Obsolete]
public class DateTimeToDaysLeftConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ret = ((DateTime)value - DateTime.Today).Days;
        return ret > 0 ? ret.ToString() : "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DateTime.Today.AddDays((int)value);
    }
}
