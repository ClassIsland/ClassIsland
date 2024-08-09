using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ClassIsland.Core.Abstractions.Converters;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Converters;

public class
    AttachedSettingsControlInfoIdToAttachedSettingsControlInfoMultiConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
        {
            return null;
        }

        if (values[0] is not ICollection<AttachedSettingsControlInfo> c || values[1] is not string id)
        {
            return null;
        }

        return c.FirstOrDefault(x => x.Guid == id);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}