using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Converters;

public class SizeLongToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return Helpers.StorageSizeHelper.FormatSize((ulong)d);
        } else if (value is long d2)
        {
            return Helpers.StorageSizeHelper.FormatSize((ulong)d2);
        }
        else
        {
            return Helpers.StorageSizeHelper.FormatSize((ulong)value);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}