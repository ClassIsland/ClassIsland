using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class WeekIndexToWeekStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case int index:
                return ToWeek(index);
            case ObservableCollection<int> coll:
                return string.Join(" ", coll.ToList().ConvertAll(ToWeek));
            default:
                return "";
        }

        static string ToWeek(int index) => index switch
        {
            0 => "周日",
            1 => "周一",
            2 => "周二",
            3 => "周三",
            4 => "周四",
            5 => "周五",
            6 => "周六",
            _ => "???"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}