using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Views;
namespace ClassIsland.Controls;

public partial class TimeLineListItemExpandingAdornerControl
{
    public TimeLineListItemExpandingAdornerControl() => InitializeComponent();

    static readonly DependencyProperty TimePointProperty  = DependencyProperty.Register(nameof(TimePoint),  typeof(TimeLayoutItem),                       typeof(TimeLineListItemExpandingAdornerControl));
    static readonly DependencyProperty TimeLayoutProperty = DependencyProperty.Register(nameof(TimeLayout), typeof(ObservableCollection<TimeLayoutItem>), typeof(TimeLineListItemExpandingAdornerControl));
    static readonly DependencyProperty ScaleProperty      = DependencyProperty.Register(nameof(Scale),      typeof(double),                               typeof(TimeLineListItemExpandingAdornerControl));
    static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool),                                 typeof(TimeLineListItemExpandingAdornerControl));

    public TimeLayoutItem                       TimePoint  { get => (TimeLayoutItem)GetValue(TimePointProperty);                        set => SetValue(TimePointProperty,  value); }
    public ObservableCollection<TimeLayoutItem> TimeLayout { get => (ObservableCollection<TimeLayoutItem>)GetValue(TimeLayoutProperty); set => SetValue(TimeLayoutProperty, value); }
    public double                               Scale      { get => (double)GetValue(ScaleProperty);                                    set => SetValue(ScaleProperty,      value); }
    public bool                                 IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty);                                 set => SetValue(IsReadOnlyProperty, value); }

    void Thumb_OnMouseEnter(object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = false);
    void Thumb_OnMouseUp   (object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = true);
    void Thumb_OnMouseLeave(object _, MouseEventArgs e) => VisualTreeUtils.FindParentVisuals<ScrollViewer>(this).ForEach(static i => i.IsManipulationEnabled = true);

/// <summary>
/// 获取上一个时间点。
/// </summary>
/// <param name="type">限定时间点类型。默认为 [0,1]，即排除分割线。</param>
/// <returns>如果不存在，将返回 null。</returns>
    TimeLayoutItem? PrevTimePoint(int[]? type = null)
    {
        for (var index = TimeLayout.IndexOf(TimePoint) - 1; index >= 0; index--)
            if (type?.Contains(TimeLayout[index].TimeType) ?? TimeLayout[index].TimeType != 2)
                return TimeLayout[index];
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
        for (var index = TimeLayout.IndexOf(TimePoint) + 1; index < TimeLayout.Count; index++)
            if (type?.Contains(TimeLayout[index].TimeType) ?? TimeLayout[index].TimeType != 2)
                return TimeLayout[index];
        return null;
    }
    TimeLayoutItem? NextTimePoint(int type) => NextTimePoint([type]);

/// <summary>
/// 调整周边的分割线。<br/>
/// 此方法的存在是因为分割线在时间表中的位置不确定。
/// </summary>
/// <param name="oldTime">调整前的时间</param>
/// <param name="newTime">调整后的时间</param>
    void DragAdjoiningSeparator(TimeSpan oldTime, DateTime newTime)
    {
        foreach (var s in (from s in TimeLayout where s.TimeType == 2 select s).Where(s => s.StartSecond.TimeOfDay == oldTime))
            s.StartSecond = s.EndSecond = newTime;
    }

    static TimeSpan RoundTime(TimeSpan time) => TimeSpan.FromMinutes(time.TotalMinutes - time.TotalMinutes % 5);
    TimeSpan GetDelta(TimeSpan raw, double v) => RoundTime(TimeSpan.FromSeconds(v * 100 / Scale) + raw) - raw;

    void ThumbTop_OnDragDelta(object _, DragDeltaEventArgs e)
    {
        var a = TimePoint.StartSecond.TimeOfDay;
        var b = PrevTimePoint();
        var d = GetDelta(a, e.VerticalChange);
        if (TimePoint.EndSecond.TimeOfDay > a + d && !(a + d < PrevTimePoint(0)?.EndSecond.TimeOfDay))
        {
            TimePoint.StartSecond += d;
            if (b?.TimeType == 1 && b.StartSecond < TimePoint.StartSecond)
                b.EndSecond = TimePoint.StartSecond;
         // else if (b?.TimeType == 0)
         //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, TimePoint.StartSecond, b.EndSecond);
            DragAdjoiningSeparator(a, TimePoint.StartSecond);
        }
    }

    void ThumbBottom_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var a = TimePoint.EndSecond.TimeOfDay;
        var b = NextTimePoint();
        var d = GetDelta(a, e.VerticalChange);
        if (a + d > TimePoint.StartSecond.TimeOfDay && !(NextTimePoint(0)?.StartSecond.TimeOfDay < a + d))
        {
            TimePoint.EndSecond += d;
            if (b?.TimeType == 1 && TimePoint.StartSecond < b.StartSecond)
                b.StartSecond = TimePoint.EndSecond;
         // else if (b?.TimeType == 0)
         //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, b.StartSecond, TimePoint.EndSecond);
            DragAdjoiningSeparator(a, TimePoint.EndSecond);
        }
    }

    void Thumb_OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        var a1 = TimePoint.StartSecond.TimeOfDay;
        var a2 = TimePoint.EndSecond.TimeOfDay;
        var d = GetDelta(a1, e.VerticalChange);
        if (a1 + d < PrevTimePoint(0)?.EndSecond.TimeOfDay || NextTimePoint(0)?.StartSecond.TimeOfDay < a2 + d)
            return;
        var b1 = PrevTimePoint();
        var b2 = NextTimePoint();
        TimePoint.StartSecond += d;
        TimePoint.EndSecond   += d;
        if (b1?.TimeType == 1 && b1.StartSecond < TimePoint.StartSecond)
            b1.EndSecond = TimePoint.StartSecond;
     // else if (b1?.TimeType == 0)
     //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, b1.EndSecond, TimePoint.StartSecond);
        if (b2?.TimeType == 1 && TimePoint.StartSecond < b2.StartSecond)
            b2.StartSecond = TimePoint.EndSecond;
     // else if (b2?.TimeType == 0)
     //     App.GetService<ProfileSettingsWindow>().AddTimeLayoutItem(1, TimePoint.EndSecond, b2.StartSecond);
        DragAdjoiningSeparator(a1, TimePoint.StartSecond);
        DragAdjoiningSeparator(a2, TimePoint.EndSecond);
     // App.GetService<ProfileSettingsWindow>().UpdateTimeLayout();
    }
}
