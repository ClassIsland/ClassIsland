using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.ViewModels;

namespace ClassIsland.Converters;

public sealed class HomeCalendarDayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not DateTime date) return null;
        var result = values[1] is IReadOnlyDictionary<DateTime, HomeCalendarDay> days && days.TryGetValue(date.Date, out var day)
            ? day : new HomeCalendarDay(date.Date, 0, false, false);
        return parameter as string == "Description" ? result.Description : result;
    }
}
