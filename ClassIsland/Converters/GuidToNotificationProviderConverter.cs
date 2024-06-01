using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;

namespace ClassIsland.Converters;

public class GuidToNotificationProviderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }
        var id = (string)value;
        var l = (from i in App.GetService<INotificationHostService>().NotificationProviders
            where i.ProviderGuid.ToString() == id
            select i)
            .ToList();
        return l.FirstOrDefault();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}