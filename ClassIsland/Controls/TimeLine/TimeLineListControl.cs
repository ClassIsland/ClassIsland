using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Models.EventArgs;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.TimeLine;

public class TimeLineListControl : ListBox
{

    public static readonly RoutedEvent<TimeLineInsertTimePointEventArgs> RequestInsertTimePointEvent =
        RoutedEvent.Register<TimeLineListControl, TimeLineInsertTimePointEventArgs>(
            nameof(RequestInsertTimePoint), RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

    public event EventHandler<TimeLineInsertTimePointEventArgs>? RequestInsertTimePoint
    {
        add => AddHandler(RequestInsertTimePointEvent, value);
        remove => RemoveHandler(RequestInsertTimePointEvent, value);
    }

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<TimeLineListControl, double>(
        nameof(Scale));

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    private static double BaseTicks { get; } = 1000000000.0;

    static TimeLineListControl()
    {
        
    }

    public static readonly StyledProperty<bool> IsReadonlyProperty = AvaloniaProperty.Register<TimeLineListControl, bool>(
        nameof(IsReadonly));

    public bool IsReadonly
    {
        get => GetValue(IsReadonlyProperty);
        set => SetValue(IsReadonlyProperty, value);
    }

    public static readonly StyledProperty<bool> IsPanningModeEnabledProperty = AvaloniaProperty.Register<TimeLineListControl, bool>(
        nameof(IsPanningModeEnabled));

    public bool IsPanningModeEnabled
    {
        get => GetValue(IsPanningModeEnabledProperty);
        set => SetValue(IsPanningModeEnabledProperty, value);
    }

    public static readonly StyledProperty<bool> IsStickyProperty = AvaloniaProperty.Register<TimeLineListControl, bool>(
        nameof(IsSticky));

    public bool IsSticky
    {
        get => GetValue(IsStickyProperty);
        set => SetValue(IsStickyProperty, value);
    }

    public TimeLineListControl()
    {
        this.GetObservable(ItemsSourceProperty).Skip(1).Subscribe(OnItemsSourceChanged);
        AddHandler(TimeLineListItemSeparatorAdornerControl.SeparatorLikeTimePointMovedEvent, SeparatorLikeTimePointMovedEventHandler);
    }

    private void SeparatorLikeTimePointMovedEventHandler(object sender, RoutedEventArgs e)
    {
        if (e is not SeparatorLikeTimePointMovedEventArgs args)
        {
            return;
        }
        if (ItemsSource is not ObservableCollection<TimeLayoutItem> layout)
        {
            return;
        }

        var rawIndex = layout.IndexOf(args.Item);
        if (rawIndex == -1)
        {
            return;
        }

        var isSorted = true;
        var timeLikeTimePoints = layout.Where(x => x.TimeType is 0 or 1 or 2).ToList();
        for (var index = 0; index < timeLikeTimePoints.Count - 1; index++)
        {
            var i = timeLikeTimePoints[index + 1];
            if (timeLikeTimePoints[index].StartTime < timeLikeTimePoints[index + 1].StartTime) continue;
            isSorted = false;
            break;
        }

        if (isSorted)
        {
            return;
        }

        var validTimePoints = layout.Where(x => x.TimeType is 0 or 1).ToList();
        for (var index = 0; index < validTimePoints.Count; index++)
        {
            var i = validTimePoints[index];
            if (i.StartTime <= args.Item.StartTime) continue;
            Console.WriteLine($"{rawIndex} -> {layout.IndexOf(i)}");
            layout.Move(rawIndex, layout.IndexOf(i));
            SelectedItem = args.Item;
            return;
        }
        layout.Move(rawIndex, layout.Count - 1);
        SelectedItem = args.Item;
    }

    private void OnItemsSourceChanged(IEnumerable? newValue)
    {
        var timeLayoutItems = (ObservableCollection<TimeLayoutItem>?)newValue;
        if (timeLayoutItems == null || timeLayoutItems.Count <= 0)
            return;
        ScrollIntoView(timeLayoutItems[0]);
        SelectedIndex = 0;
    }

    /// <summary>
    /// Scrolls the specified item into view, centering it vertically in the viewport.
    /// </summary>
    public void ScrollIntoViewCentered(object? item)
    {
        if (item == null)
            return;

        // First, ensure the item container is realized
        ScrollIntoView(item);

        // Find the ScrollViewer in the visual tree
        var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer == null)
            return;

        // Find the container for the item
        var index = Items.Cast<object>().ToList().IndexOf(item);
        if (index < 0)
            return;

        var container = ContainerFromIndex(index);
        if (container == null)
            return;

        // Get the item's position and size from Canvas.Top and its height
        var itemTop = Canvas.GetTop(container);
        var itemHeight = container.Bounds.Height;

        // Calculate the offset to center the item
        var viewportHeight = scrollViewer.Viewport.Height;
        var targetOffset = itemTop + itemHeight / 2 - viewportHeight / 2;

        // Clamp to valid scroll range
        targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.Extent.Height - viewportHeight));

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, targetOffset);
    }
}
