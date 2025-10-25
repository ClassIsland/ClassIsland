using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class CollectionToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not ObservableCollection<string> v ? "" : string.Join("\r\n", v);
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not string v ? BindingOperations.DoNothing : new ObservableCollection<string>(v.Split("\r\n"));
    }
}