using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class PreventNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value ?? Binding.DoNothing;
    }
}