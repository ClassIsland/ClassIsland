using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Templates;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls.LessonsControls;

internal class LessonsListBoxItemTemplateMultiConverter : AvaloniaObject, IMultiValueConverter
{
    public static readonly StyledProperty<DataTemplate> MinimizedDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(MinimizedDataTemplate));
    public DataTemplate MinimizedDataTemplate
    {
        get => GetValue(MinimizedDataTemplateProperty);
        set => SetValue(MinimizedDataTemplateProperty, value);
    
    }

    public static readonly StyledProperty<DataTemplate> ExpandedDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(ExpandedDataTemplate));
    public DataTemplate ExpandedDataTemplate
    {
        get => GetValue(ExpandedDataTemplateProperty);
        set => SetValue(ExpandedDataTemplateProperty, value);
    
    }

    public static readonly StyledProperty<DataTemplate> SeparatorDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(SeparatorDataTemplate));
    public DataTemplate SeparatorDataTemplate
    {
        get => GetValue(SeparatorDataTemplateProperty);
        set => SetValue(SeparatorDataTemplateProperty, value);
    
    }

    public DataTemplate BlankDataTemplate { get; } = new();


    public object? Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        // 传入参数：
        // [0]: int              TimeType
        // [1]: bool             IsHideDefault
        // [2]: TimeLayoutItem   SelectedItem
        // [3]: TimeLayoutItem   CurrentItem
        // [4]: bool             DiscardHidingDefault (reserved)
        // [5]: bool             ShowCurrentTimeLayoutItemOnlyOnClass
        // [6]: bool             HideFinishedClass
        // [7]: ICollection<...> ValidTimePoints
        // [8]: bool             IsLiveUpdatingEnabled
        if (values.Count < 8)
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

        if (values[7] is ICollection<TimeLayoutItem> validTimePoints &&
            (currentItem == null || !validTimePoints.Contains(currentItem)))
        {
            return BlankDataTemplate;
        }

        var itemDateTime = (currentItem?.TimeType == 2) ? currentItem?.StartSecond : currentItem?.EndSecond;
        if ((itemDateTime?.TimeOfDay < selectedItem?.StartSecond.TimeOfDay 
             || itemDateTime.HasValue && itemDateTime.Value.TimeOfDay <
                IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().TimeOfDay) && hideFinishedClass)
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
}