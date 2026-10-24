using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassIsland.Core.Converters;

internal sealed class MultipleDateSelectorDayConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not DateTime date)
            return parameter as string == "Description" ? null : false;

        var day = DateOnly.FromDateTime(date);
        var isSelected = values[1] is IReadOnlySet<DateOnly> dates && dates.Contains(day);
        return parameter as string == "Description"
            ? $"{day:yyyy/M/d}，{(isSelected ? "已选择" : "未选择")}" : isSelected;
    }
}
