using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class DateTimeToDaysLeftConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var ret = ((DateTime)value - DateTime.Today).Days.ToString();
        return int.Parse(ret) < 0 ? "0" : ret;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DateTime.Today.AddDays((int)value);
    }
}
