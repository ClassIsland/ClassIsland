using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

internal class LessonsListBoxFadingStateConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // 传入参数：
        // [0]: bool              IsFadingEnabled
        // [2]: TimeLayoutItem    CurrentItem
        // [2]: TimeLayoutItem?   SelectedItem
        // [2]: IList<...>        ValidTimePoints
        if (values.Count < 4)
        {
            return false;
        }

        if (values[0] is not bool isFadingEnabled ||
            values[1] is not TimeLayoutItem currentItem ||
            values[2] is not TimeLayoutItem selectedItem ||
            values[3] is not IList<TimeLayoutItem> validTimePoints)
        {
            return false;
        }

        if (!isFadingEnabled)
        {
            return false;
        }

        var currentItemIndex = validTimePoints.IndexOf(currentItem);
        var selectedItemIndex = validTimePoints.IndexOf(selectedItem);

        return selectedItemIndex > currentItemIndex;
    }
}