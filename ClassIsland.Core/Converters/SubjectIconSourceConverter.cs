using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Core.Helpers.UI;

namespace ClassIsland.Core.Converters;

public class SubjectIconSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string expression && !string.IsNullOrWhiteSpace(expression)
            ? IconExpressionHelper.TryParseOrNull(expression)
            : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
