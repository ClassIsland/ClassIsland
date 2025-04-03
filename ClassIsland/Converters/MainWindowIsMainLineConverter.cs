using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Converters;

public class MainWindowIsMainLineConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not CollectionViewGroup cvg)
            return false;
        if (cvg.Items.FirstOrDefault() is ComponentSettings component)
        {
            return component.RelativeLineNumber == 0;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}