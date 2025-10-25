using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.Models.EventArgs;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.TimeLine;

/// <summary>
/// TimeLineListItemSeparatorAdornerControl.xaml 的交互逻辑
/// </summary>
public partial class TimeLineListItemSeparatorAdornerControl : UserControl
{
    private static double BaseTicks { get; } = 1000000000.0;

    public static readonly StyledProperty<TimeLayoutItem> TimePointProperty = AvaloniaProperty.Register<TimeLineListItemSeparatorAdornerControl, TimeLayoutItem>(
        nameof(TimePoint));

    public TimeLayoutItem TimePoint
    {
        get => GetValue(TimePointProperty);
        set => SetValue(TimePointProperty, value);
    }

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<TimeLineListItemSeparatorAdornerControl, double>(
        nameof(Scale), 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<bool> IsReadonlyProperty = AvaloniaProperty.Register<TimeLineListItemSeparatorAdornerControl, bool>(
        nameof(IsReadonly));

    public bool IsReadonly
    {
        get => GetValue(IsReadonlyProperty);
        set => SetValue(IsReadonlyProperty, value);
    }

    // Register a custom routed event using the Bubble routing strategy.
    public static readonly RoutedEvent SeparatorLikeTimePointMovedEvent = RoutedEvent.Register<TimeLineListItemSeparatorAdornerControl, RoutedEventArgs>(
        nameof(SeparatorLikeTimePointMoved), RoutingStrategies.Bubble);

    // Provide CLR accessors for adding and removing an event handler.
    public event EventHandler<RoutedEventArgs> SeparatorLikeTimePointMoved
    {
        add => AddHandler(SeparatorLikeTimePointMovedEvent, value);
        remove => RemoveHandler(SeparatorLikeTimePointMovedEvent, value);
    }
    
    private TimeSpan _initStartTime = TimeSpan.Zero;

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

    private void Thumb_OnDragDelta(object sender, VectorEventArgs e)
    {
        if (TimePoint == null)
        {
            return;
        }
        var v = e.Vector.Y;
        var d = GetDelta(_initStartTime, v);

        TimePoint.StartTime = TimePoint.EndTime = _initStartTime + d;
    }

    private void Thumb_OnDragCompleted(object sender, VectorEventArgs e)
    {
        if (TimePoint == null)
        {
            return;
        }
        RaiseEvent(new SeparatorLikeTimePointMovedEventArgs(TimePoint));
    }

    private void Thumb_OnDragStarted(object? sender, VectorEventArgs e)
    {
        _initStartTime = TimePoint.StartTime;
    }
}
