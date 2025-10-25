using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;


namespace ClassIsland.Core.Converters;

public class PreventNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value ?? BindingOperations.DoNothing;
    }
}