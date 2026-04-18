using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Avalonia.Data.Converters;
using ClassIsland.Core.Models.Components;

namespace ClassIsland.Converters;

public class DynamicIslandRootComponentVisibilityMultiConverter : IMultiValueConverter
{
    private const string ScheduleComponentId = "1DB2017D-E374-4BC6-9D57-0B4ADF03A6B8";

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3)
        {
            return true;
        }

        var isCompactModeActive = values[0] as bool? ?? false;
        var componentSettings = values[1] as ComponentSettings;
        var isEditMode = values[2] as bool? ?? false;

        if (isEditMode || !isCompactModeActive)
        {
            return true;
        }

        return ContainsScheduleComponent(componentSettings);
    }

    private static bool ContainsScheduleComponent(ComponentSettings? settings)
    {
        if (settings == null)
        {
            return false;
        }

        if (string.Equals(settings.Id, ScheduleComponentId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return settings.Children?.Any(ContainsScheduleComponent) == true;
    }
}
