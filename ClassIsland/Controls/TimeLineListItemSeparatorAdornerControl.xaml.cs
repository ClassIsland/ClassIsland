using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ClassIsland.Models.EventArgs;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// TimeLineListItemSeparatorAdornerControl.xaml 的交互逻辑
/// </summary>
public partial class TimeLineListItemSeparatorAdornerControl : UserControl
{
    private static double BaseTicks { get; } = 1000000000.0;

    public static readonly DependencyProperty TimePointProperty = DependencyProperty.Register(
        nameof(TimePoint), typeof(TimeLayoutItem), typeof(TimeLineListItemSeparatorAdornerControl), new PropertyMetadata(default(TimeLayoutItem)));

    public TimeLayoutItem? TimePoint
    {
        get { return (TimeLayoutItem)GetValue(TimePointProperty); }
        set { SetValue(TimePointProperty, value); }
    }

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(double), typeof(TimeLineListItemSeparatorAdornerControl), new PropertyMetadata(default(double)));

    public double Scale
    {
        get { return (double)GetValue(ScaleProperty); }
        set { SetValue(ScaleProperty, value); }
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(TimeLineListItemSeparatorAdornerControl), new PropertyMetadata(default(bool)));

    public bool IsReadOnly
    {
        get { return (bool)GetValue(IsReadOnlyProperty); }
        set { SetValue(IsReadOnlyProperty, value); }
    }

    // Register a custom routed event using the Bubble routing strategy.
    public static readonly RoutedEvent SeparatorLikeTimePointMovedEvent = EventManager.RegisterRoutedEvent(
        name: "SeparatorLikeTimePointMoved",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(EventHandler<SeparatorLikeTimePointMovedEventArgs>),
        ownerType: typeof(TimeLineListItemSeparatorAdornerControl));

    // Provide CLR accessors for adding and removing an event handler.
    public event RoutedEventHandler SeparatorLikeTimePointMoved
    {
        add { AddHandler(SeparatorLikeTimePointMovedEvent, value); }
        remove { RemoveHandler(SeparatorLikeTimePointMovedEvent, value); }
    }

    public TimeLineListItemSeparatorAdornerControl()
    {
        InitializeComponent();
    }

    private TimeSpan RoundTime(TimeSpan time)
    {
        var b = TimeSpan.FromMinutes(5).Ticks;
        return TimeSpan.FromTicks((long)Math.Round((double)time.Ticks / b) * b);
    }

    private TimeSpan GetDelta(TimeSpan raw, double v)
    {
        var tsDelta = TimeSpan.FromTicks((long)(v / Scale * BaseTicks));
        var t = RoundTime(raw + tsDelta);
        return t - raw;
    }

    private void Thumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (TimePoint == null)
        {
            return;
        }
        var v = e.VerticalChange;
        var d = GetDelta(TimePoint.StartSecond.TimeOfDay, v);

        TimePoint.StartSecond += d;
        TimePoint.EndSecond += d;
    }

    private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (TimePoint == null)
        {
            return;
        }
        RaiseEvent(new SeparatorLikeTimePointMovedEventArgs(TimePoint));
    }

    void Thumb_OnMouseEnter(object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = false);
    void Thumb_OnMouseUp(object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = true);
    void Thumb_OnMouseLeave(object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = true);
}