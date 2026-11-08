using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ClassIsland.Core.Helpers.UI;

namespace ClassIsland.Converters;

public class SubjectDetailsConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var teacher = values[0] as string;
        var location = values[1] as string;
        return string.Join("  ", new[] { teacher, string.IsNullOrWhiteSpace(location) ? "" : $"@{location}" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public class SubjectIconSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string expression ? IconExpressionHelper.TryParseOrNull(expression) : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}

public class SubjectColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && Color.TryParse(text, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
