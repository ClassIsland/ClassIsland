using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassIsland.Helpers;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.TimeLine;

public partial class TimeLineListItemExpandingAdornerControl : UserControl
{
    private static readonly TimeSpan MinTime = TimeSpan.Zero;
    private static readonly TimeSpan MaxTime = new TimeSpan(23, 59, 59);
    
    public TimeLineListItemExpandingAdornerControl() => InitializeComponent();

    public static readonly StyledProperty<TimeLayoutItem> TimePointProperty = AvaloniaProperty.Register<TimeLineListItemExpandingAdornerControl, TimeLayoutItem>(
        nameof(TimePoint));

    public TimeLayoutItem TimePoint
    {
        get => GetValue(TimePointProperty);
        set => SetValue(TimePointProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<TimeLayoutItem>> TimeLayoutProperty = AvaloniaProperty.Register<TimeLineListItemExpandingAdornerControl, ObservableCollection<TimeLayoutItem>>(
        nameof(TimeLayout));

    public ObservableCollection<TimeLayoutItem> TimeLayout
    {
        get => GetValue(TimeLayoutProperty);
        set => SetValue(TimeLayoutProperty, value);
    }

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<TimeLineListItemExpandingAdornerControl, double>(
        nameof(Scale), 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<bool> IsReadonlyProperty = AvaloniaProperty.Register<TimeLineListItemExpandingAdornerControl, bool>(
        nameof(IsReadonly));

    public bool IsReadonly
    {
        get => GetValue(IsReadonlyProperty);
        set => SetValue(IsReadonlyProperty, value);
    }

    public static readonly StyledProperty<bool> IsStickyProperty = AvaloniaProperty.Register<TimeLineListItemExpandingAdornerControl, bool>(
        nameof(IsSticky));

    public bool IsSticky
    {
        get => GetValue(IsStickyProperty);
        set => SetValue(IsStickyProperty, value);
    }

    private bool _isAddTimePointPopupVisible = true;

    public static readonly DirectProperty<TimeLineListItemExpandingAdornerControl, bool> IsAddTimePointPopupVisibleProperty = AvaloniaProperty.RegisterDirect<TimeLineListItemExpandingAdornerControl, bool>(
        nameof(IsAddTimePointPopupVisible), o => o.IsAddTimePointPopupVisible, (o, v) => o.IsAddTimePointPopupVisible = v);

    public bool IsAddTimePointPopupVisible
    {
        get => _isAddTimePointPopupVisible;
        set => SetAndRaise(IsAddTimePointPopupVisibleProperty, ref _isAddTimePointPopupVisible, value);
    }

    public static readonly RoutedEvent<TimeLineInsertTimePointEventArgs> RequestInsertTimePointEvent = RoutedEvent.Register<TimeLineListItemExpandingAdornerControl, TimeLineInsertTimePointEventArgs>(
        nameof(RequestInsertTimePoint), RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

    public event EventHandler<TimeLineInsertTimePointEventArgs>? RequestInsertTimePoint
    {
        add => AddHandler(RequestInsertTimePointEvent, value);
        remove => RemoveHandler(RequestInsertTimePointEvent, value);
    }
    
    private TimeSpan _initStartTime = TimeSpan.Zero;
    private TimeSpan _initEndTime = TimeSpan.Zero;

    /// <summary>
    /// 获取上一个时间点。
    /// </summary>
    /// <param name="type">限定时间点类型。默认为 [0,1]，即排除分割线。</param>
    /// <returns>如果不存在，将返回 null。</returns>
    TimeLayoutItem? PrevTimePoint(int[]? type = null)
    {
        type ??= [0, 1];
        var validTimePoints = TimeLayout.Where(x => type.Contains(x.TimeType)).ToList();
        var index = validTimePoints.IndexOf(TimePoint) - 1;
        if (index >= 0 && index < validTimePoints.Count)
        {
            return validTimePoints[index];
        }

        return null;
    }

    TimeLayoutItem? PrevTimePoint(int type) => PrevTimePoint([type]);

    /// <summary>
    /// 获取下一个时间点。
    /// </summary>
    /// <param name="type">限定时间点类型。默认为 [0,1]，即排除分割线。</param>
    /// <returns>如果不存在，将返回 null。</returns>
    TimeLayoutItem? NextTimePoint(int[]? type = null)
    {
        type ??= [0, 1];
        var validTimePoints = TimeLayout.Where(x => type.Contains(x.TimeType)).ToList();
        var index = validTimePoints.IndexOf(TimePoint) + 1;
        if (index >= 0 && index < validTimePoints.Count)
        {
            return validTimePoints[index];
        }

        return null;
    }

    TimeLayoutItem? NextTimePoint(int type) => NextTimePoint([type]);

    /// <summary>
    /// 调整周边的分割线。<br/>
    /// 此方法的存在是因为分割线在时间表中的位置不确定。
    /// </summary>
    /// <param name="oldTime">调整前的时间</param>
    /// <param name="newTime">调整后的时间</param>
    void DragAdjoiningSeparator(TimeSpan oldTime, TimeSpan newTime)
    {
        foreach (var s in (from s in TimeLayout where s.TimeType == 2 select s).Where(s =>
                     s.StartTime == oldTime))
            s.StartTime = s.StartTime = newTime;
    }

    static TimeSpan RoundTime(TimeSpan time) => TimeSpan.FromMinutes(Math.Round(time.TotalMinutes / 5) * 5);
    TimeSpan GetDelta(TimeSpan raw, double v) => RoundTime(TimeSpan.FromSeconds(v * 100 / Scale) + raw) - raw;

    void ThumbTop_OnDragDelta(object _, VectorEventArgs e)
    {
        var a = _initStartTime;
        var b = PrevTimePoint();
        var d = GetDelta(_initStartTime, e.Vector.Y);
        // Console.WriteLine($"{_initStartTime} {e.Vector.Y} {d}");
        if (TimePoint.EndTime > a + d && (!(a + d < PrevTimePoint()?.EndTime) || IsSticky))
        {
            var newStart = TimeSpanHelper.Clamp(a + d, MinTime, MaxTime);
            if (!IsSticky)
            {
                TimePoint.StartTime = newStart;
                return;
            }
            if (b != null && b.StartTime >= newStart)
            {
                return;
            }
            if (b != null && b.EndTime == TimePoint.StartTime)
                b.EndTime = newStart;
            TimePoint.StartTime = newStart;

            // else if (b?.TimeType == 0)
            //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, TimePoint.StartSecond, b.EndSecond);
            DragAdjoiningSeparator(a, TimePoint.StartTime);
        }
    }

    void ThumbBottom_OnDragDelta(object sender, VectorEventArgs e)
    {
        var a = TimePoint.EndTime;
        var b = NextTimePoint();
        var d = GetDelta(a, e.Vector.Y);
        Console.WriteLine(d);
        if (a + d > TimePoint.StartTime && (!(NextTimePoint()?.StartTime < a + d) || IsSticky))
        {
            var newEnd = TimeSpanHelper.Clamp(a + d, MinTime, MaxTime);
            if (!IsSticky)
            {
                TimePoint.EndTime = newEnd;
                return;
            }
            if (b != null && b.EndTime <= newEnd)
            {
                return;
            }
            if (b != null && b.StartTime == a)
                b.StartTime = newEnd;
            TimePoint.EndTime = newEnd;
            // else if (b?.TimeType == 0)
            //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, b.StartSecond, TimePoint.EndSecond);
            DragAdjoiningSeparator(a, TimePoint.EndTime);
        }
    }

    void Thumb_OnDragDelta(object sender, VectorEventArgs e)
    {
        var a1 = _initStartTime;
        var a2 = _initEndTime;
        var d = GetDelta(_initStartTime, e.Vector.Y);

        if (!IsSticky && (a1 + d < PrevTimePoint()?.EndTime || NextTimePoint()?.StartTime < a2 + d))
            return;
        if (!TimeSpanHelper.Within(a1 + d, MinTime, MaxTime) || !TimeSpanHelper.Within(a2 + d, MinTime, MaxTime))
        {
            return;
        }

        var newStart = TimeSpanHelper.Clamp(a1 + d, MinTime, MaxTime);
        var newEnd = TimeSpanHelper.Clamp(a2 + d, MinTime, MaxTime);
        if (!IsSticky)
        {
            TimePoint.StartTime = newStart;
            TimePoint.EndTime = newEnd;
            return;
        }
        var b1 = PrevTimePoint();
        var b2 = NextTimePoint();
        //Console.WriteLine($"{a1} {b1?.EndSecond.TimeOfDay} {newStart}");
        if (b1 != null && b1.StartTime >= newStart)
        {
            return;
        }
        if (b2 != null && b2.EndTime <= newEnd)
        {
            return;
        }
        if (b1 != null && b1.EndTime == TimePoint.StartTime)
        {
            b1.EndTime = newStart;
        }

        // else if (b1?.TimeType == 0)
        //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, b1.EndSecond, TimePoint.StartSecond);
        if (b2 != null && b2.StartTime == TimePoint.EndTime)
        {
            b2.StartTime = newEnd;
        }

        // else if (b2?.TimeType == 0)
        //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, TimePoint.EndSecond, b2.StartSecond);
        TimePoint.StartTime = newStart;
        TimePoint.EndTime = newEnd;
        DragAdjoiningSeparator(a1, TimePoint.StartTime);
        DragAdjoiningSeparator(a2, TimePoint.EndTime);
        // App.GetService<ProfileSettingsWindow>().UpdateTimeLayout();
    }

    private void ThumbTop_OnDragStarted(object? sender, VectorEventArgs e)
    {
        _initStartTime = TimePoint.StartTime;
    }

    private void Thumb_OnDragStarted(object? sender, VectorEventArgs e)
    {
        _initStartTime = TimePoint.StartTime;
        _initEndTime = TimePoint.EndTime;
    }

    private void ButtonAddOnClass_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new TimeLineInsertTimePointEventArgs(RequestInsertTimePointEvent)
        {
            Kind = 0,
            TimePoint = TimePoint,
            Location = TimeLineInsertTimePointEventArgs.InsertLocation.After
        });
    }

    private void ButtonAddOnBreaking_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new TimeLineInsertTimePointEventArgs(RequestInsertTimePointEvent)
        {
            Kind = 1,
            TimePoint = TimePoint,
            Location = TimeLineInsertTimePointEventArgs.InsertLocation.After
        });
    }

    private void ButtonDismiss_OnClick(object? sender, RoutedEventArgs e)
    {
        IsAddTimePointPopupVisible = false;
    }
}