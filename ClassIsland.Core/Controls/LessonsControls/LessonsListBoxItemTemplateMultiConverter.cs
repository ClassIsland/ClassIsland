﻿using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

internal class LessonsListBoxItemTemplateMultiConverter : DependencyObject, IMultiValueConverter
{
    public static readonly DependencyProperty MinimizedDataTemplateProperty = DependencyProperty.Register(
        nameof(MinimizedDataTemplate), typeof(DataTemplate), typeof(LessonsListBoxItemTemplateMultiConverter), new PropertyMetadata(default(DataTemplate)));

    public DataTemplate MinimizedDataTemplate
    {
        get { return (DataTemplate)GetValue(MinimizedDataTemplateProperty); }
        set { SetValue(MinimizedDataTemplateProperty, value); }
    }

    public static readonly DependencyProperty ExpandedDataTemplateProperty = DependencyProperty.Register(
        nameof(ExpandedDataTemplate), typeof(DataTemplate), typeof(LessonsListBoxItemTemplateMultiConverter), new PropertyMetadata(default(DataTemplate)));

    public DataTemplate ExpandedDataTemplate
    {
        get { return (DataTemplate)GetValue(ExpandedDataTemplateProperty); }
        set { SetValue(ExpandedDataTemplateProperty, value); }
    }

    public static readonly DependencyProperty SeparatorDataTemplateProperty = DependencyProperty.Register(
        nameof(SeparatorDataTemplate), typeof(DataTemplate), typeof(LessonsListBoxItemTemplateMultiConverter), new PropertyMetadata(default(DataTemplate)));

    public DataTemplate SeparatorDataTemplate
    {
        get { return (DataTemplate)GetValue(SeparatorDataTemplateProperty); }
        set { SetValue(SeparatorDataTemplateProperty, value); }
    }

    public DataTemplate BlankDataTemplate { get; } = new();


    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // 传入参数：
        // [0]: int            TimeType
        // [1]: bool           IsHideDefault
        // [2]: TimeLayoutItem SelectedItem
        // [3]: TimeLayoutItem CurrentItem
        // [4]: bool           DiscardHidingDefault (reserved)
        // [5]: bool           ShowCurrentTimeLayoutItemOnlyOnClass
        // [6]: bool           HideFinishedClass
        if (values.Length < 7)
            return BlankDataTemplate;
        if (values[0] is not int timeType ||
            values[1] is not bool isHideDefault ||
            values[4] is not bool discardHidingDefault ||
            values[5] is not bool showCurrentTimeLayoutItemOnlyOnClass ||
            values[6] is not bool hideFinishedClass)
        {
            return BlankDataTemplate;
        }

        if (timeType == 3)
        {
            return BlankDataTemplate;
        }

        var selectedItem = values[2] as TimeLayoutItem;
        var currentItem = values[3] as TimeLayoutItem;
        if (currentItem != selectedItem && selectedItem?.TimeType == 0 && showCurrentTimeLayoutItemOnlyOnClass)
            return BlankDataTemplate;

        var itemDateTime = (currentItem?.TimeType == 2) ? currentItem?.StartSecond : currentItem?.EndSecond;
        if (itemDateTime.HasValue && TimeOnly.FromDateTime(itemDateTime.Value) < TimeOnly.FromDateTime(IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime()) && hideFinishedClass)
        {
            return BlankDataTemplate;
        }

        if (timeType == 2)
        {
            return SeparatorDataTemplate;
        }

        var hide = (timeType == 1 || (isHideDefault && !discardHidingDefault)) && selectedItem != currentItem;
        if (hide)
        {
            return BlankDataTemplate;
        }


        return selectedItem == currentItem ? ExpandedDataTemplate : MinimizedDataTemplate;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}