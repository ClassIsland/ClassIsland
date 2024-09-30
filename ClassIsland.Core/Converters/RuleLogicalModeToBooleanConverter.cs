﻿using ClassIsland.Core.Enums;

using System.Globalization;
using System.Windows.Data;

namespace ClassIsland.Core.Converters;

public class RuleLogicalModeToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (RulesetLogicalMode)value == RulesetLogicalMode.And;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? RulesetLogicalMode.And : RulesetLogicalMode.Or;
    }
}