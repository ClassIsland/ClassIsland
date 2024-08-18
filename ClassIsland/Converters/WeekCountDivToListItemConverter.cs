using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class WeekCountDivToListItemConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var num = (int)values[0];
        var total = (int)values[1] - 1;
        Console.WriteLine(total);
        if (num == 0) return "不限";
        if (total <= 2)
        {
            if (num == 1) return "单周";
            if (num == 2) return "双周";
        }
        var num_ = num switch
        {
            1 => "一",
            2 => "二",
            3 => "三",
            4 => "四",
            _ => num.ToString()
        };
        return $"第{num_}周";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}