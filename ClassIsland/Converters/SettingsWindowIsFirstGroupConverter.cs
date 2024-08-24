using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClassIsland.Converters;

public class SettingsWindowIsFirstGroupConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not CollectionViewGroup cvg)
            return Visibility.Collapsed;
        var view = parameter as CollectionViewSource;

        var groupIndex = view?.View.Groups.IndexOf(cvg);
        return groupIndex == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}