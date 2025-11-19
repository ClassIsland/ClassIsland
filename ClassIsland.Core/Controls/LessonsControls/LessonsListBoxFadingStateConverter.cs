using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

internal class LessonsListBoxFadingStateConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // 传入参数：
        // [0]: bool              IsFadingEnabled
        // [1]: TimeLayoutItem    CurrentItem
        // [2]: TimeLayoutItem?   SelectedItem
        // [2]: IList<...>        ValidTimePoints
        if (values.Count < 4)
        {
            return false;
        }

        if (values[0] is not bool isFadingEnabled ||
            values[3] is not IList<TimeLayoutItem> validTimePoints)
        {
            return false;
        }

        if (!isFadingEnabled)
        {
            return false;
        }
        
        var currentItem = values[1] as TimeLayoutItem;
        var selectedItem = values[2] as TimeLayoutItem;
        // var currentItemIndex = validTimePoints.IndexOf(currentItem);
        // var selectedItemIndex = validTimePoints.IndexOf(selectedItem);
        var itemDateTime = (currentItem?.TimeType == 2) ? currentItem?.StartTime : currentItem?.EndTime;
        if ((itemDateTime < selectedItem?.StartTime 
             || itemDateTime.HasValue && itemDateTime.Value <
             IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().TimeOfDay))
        {
            return true;
        }

        return false;
    }
}