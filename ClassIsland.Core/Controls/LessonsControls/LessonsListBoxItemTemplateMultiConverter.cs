using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Layout;
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

    public static readonly StyledProperty<DataTemplate> VerticalExpandedDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(VerticalExpandedDataTemplate));
    public DataTemplate VerticalExpandedDataTemplate
    {
        get => GetValue(VerticalExpandedDataTemplateProperty);
        set => SetValue(VerticalExpandedDataTemplateProperty, value);
    }

    public static readonly StyledProperty<DataTemplate> VerticalMinimizedDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(VerticalMinimizedDataTemplate));
    public DataTemplate VerticalMinimizedDataTemplate
    {
        get => GetValue(VerticalMinimizedDataTemplateProperty);
        set => SetValue(VerticalMinimizedDataTemplateProperty, value);
    }

    public static readonly StyledProperty<DataTemplate> VerticalSeparatorDataTemplateProperty = AvaloniaProperty.Register<LessonsListBoxItemTemplateMultiConverter, DataTemplate>(
        nameof(VerticalSeparatorDataTemplate));
    public DataTemplate VerticalSeparatorDataTemplate
    {
        get => GetValue(VerticalSeparatorDataTemplateProperty);
        set => SetValue(VerticalSeparatorDataTemplateProperty, value);
    }


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
        // [8]: Orientation     Orientation
        if (values.Count < 9)
            return BlankDataTemplate;
        if (values[0] is not int timeType ||
            values[1] is not bool isHideDefault ||
            values[4] is not bool discardHidingDefault ||
            values[5] is not bool showCurrentTimeLayoutItemOnlyOnClass ||
            values[6] is not bool hideFinishedClass ||
            values[8] is not Orientation orientation)
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

        var itemDateTime = (currentItem?.TimeType == 2) ? currentItem?.StartTime : currentItem?.EndTime;
        if ((itemDateTime < selectedItem?.StartTime 
             || itemDateTime.HasValue && itemDateTime.Value <
                IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime().TimeOfDay) && hideFinishedClass)
        {
            return BlankDataTemplate;
        }

        if (timeType == 2)
        {
            return orientation == Orientation.Vertical ? VerticalSeparatorDataTemplate : SeparatorDataTemplate;
        }

        var hide = (timeType == 1 || (isHideDefault && !discardHidingDefault)) && selectedItem != currentItem;
        if (hide)
        {
            return BlankDataTemplate;
        }

        if (orientation == Orientation.Vertical)
        {
            return selectedItem == currentItem ? VerticalExpandedDataTemplate : VerticalMinimizedDataTemplate;
        }

        return selectedItem == currentItem ? ExpandedDataTemplate : MinimizedDataTemplate;
    }
}
