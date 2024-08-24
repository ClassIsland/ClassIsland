using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class CollectionToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not ObservableCollection<string> v ? "" : string.Join("\r\n", v);
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not string v ? Binding.DoNothing : new ObservableCollection<string>(v.Split("\r\n"));
    }
}