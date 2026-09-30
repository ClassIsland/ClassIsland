using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

public class SubjectLocationDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string location && !string.IsNullOrWhiteSpace(location)
            ? $" @{location}"
            : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
