using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// 日程可视化时间线控件，在24小时时间线上展示日程块，支持日期切换和过滤。
/// </summary>
public partial class ReminderTimelineControl : UserControl
{
    #region Scale 属性
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<ReminderTimelineControl, double>(nameof(Scale), 3.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }
    #endregion

    #region SelectedDate 属性
    public static readonly StyledProperty<DateTime> SelectedDateProperty =
        AvaloniaProperty.Register<ReminderTimelineControl, DateTime>(
            nameof(SelectedDate), DateTime.Today);

    public DateTime SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }
    #endregion

    #region Reminders 属性
    public static readonly DirectProperty<ReminderTimelineControl, ObservableCollection<Reminder>> RemindersProperty =
        AvaloniaProperty.RegisterDirect<ReminderTimelineControl, ObservableCollection<Reminder>>(
            nameof(Reminders),
            o => o.Reminders,
            (o, v) => o.Reminders = v);

    private ObservableCollection<Reminder> _reminders = [];

    public ObservableCollection<Reminder> Reminders
    {
        get => _reminders;
        set
        {
            if (_reminders != null)
            {
                _reminders.CollectionChanged -= OnRemindersCollectionChanged;
                foreach (var r in _reminders)
                    UnsubscribeFromReminderPropertyChanged(r);
            }
            SetAndRaise(RemindersProperty, ref _reminders, value);
            if (_reminders != null)
            {
                _reminders.CollectionChanged += OnRemindersCollectionChanged;
                foreach (var r in _reminders)
                    SubscribeToReminderPropertyChanged(r);
            }
            RefreshFilteredReminders();
        }
    }

    private void OnRemindersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Reminder>())
                SubscribeToReminderPropertyChanged(item);
        }
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<Reminder>())
                UnsubscribeFromReminderPropertyChanged(item);
        }
        RefreshFilteredReminders();
    }

    private void SubscribeToReminderPropertyChanged(Reminder? reminder)
    {
        if (reminder != null)
            reminder.PropertyChanged += OnReminderPropertyChanged;
    }

    private void UnsubscribeFromReminderPropertyChanged(Reminder? reminder)
    {
        if (reminder != null)
            reminder.PropertyChanged -= OnReminderPropertyChanged;
    }

    private void OnReminderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Reminder.Frequency):
            case nameof(Reminder.Time):
            case nameof(Reminder.TimeOfDay):
            case nameof(Reminder.WeekDays):
            case nameof(Reminder.StartDate):
            case nameof(Reminder.EndDate):
            case nameof(Reminder.YearMonth):
            case nameof(Reminder.YearDay):
            case nameof(Reminder.IsEnabled):
                // 防抖：属性变更只重新计算布局，不重建列表（避免闪烁）
                if (!_refreshQueued)
                {
                    _refreshQueued = true;
                    Dispatcher.UIThread.Post(() =>
                    {
                        _refreshQueued = false;
                        RecalculateItemLayout();
                    });
                }
                break;
        }
    }
    #endregion

    #region SelectedReminder 属性
    public static readonly StyledProperty<Reminder?> SelectedReminderProperty =
        AvaloniaProperty.Register<ReminderTimelineControl, Reminder?>(nameof(SelectedReminder));

    public Reminder? SelectedReminder
    {
        get => GetValue(SelectedReminderProperty);
        set => SetValue(SelectedReminderProperty, value);
    }
    #endregion

    #region DisplayedReminders 属性（过滤后）
    public static readonly DirectProperty<ReminderTimelineControl, ObservableCollection<Reminder>> DisplayedRemindersProperty =
        AvaloniaProperty.RegisterDirect<ReminderTimelineControl, ObservableCollection<Reminder>>(
            nameof(DisplayedReminders),
            o => o.DisplayedReminders);

    private bool _refreshQueued;
    private bool _needsLayoutRefresh;

    private readonly Dictionary<Guid, double> _itemLeftOffsets = new();

    private ObservableCollection<Reminder> _displayedReminders = [];

    public ObservableCollection<Reminder> DisplayedReminders => _displayedReminders;

    private void RefreshFilteredReminders()
    {
        _displayedReminders.Clear();
        if (_reminders == null || _reminders.Count == 0)
        {
            EmptyHint.IsVisible = false;
            _needsLayoutRefresh = false;
            return;
        }

        var date = SelectedDate.Date;
        foreach (var r in _reminders)
        {
            if (IsReminderOnDate(r, date))
                _displayedReminders.Add(r);
        }

        EmptyHint.IsVisible = _displayedReminders.Count == 0;

        // 标记需要延迟到容器生成后计算布局
        _needsLayoutRefresh = true;
        TimelineCanvas.LayoutUpdated += OnTimelineCanvasLayoutUpdated;
    }

    private void OnTimelineCanvasLayoutUpdated(object? sender, EventArgs e)
    {
        if (!_needsLayoutRefresh) return;

        // 检查所有容器是否已生成
        for (var i = 0; i < _displayedReminders.Count; i++)
            if (ReminderItemsControl.ItemContainerGenerator.ContainerFromIndex(i) == null)
                return; // 还没生成完，等下一次 LayoutUpdated

        _needsLayoutRefresh = false;
        TimelineCanvas.LayoutUpdated -= OnTimelineCanvasLayoutUpdated;
        RecalculateItemLayout();
    }

    private static double GetItemVisualHeight() => 46.0; // MinHeight 42 + Margin top/bottom 4

    // 属性变更时使用：不重建列表，仅重新计算偏移
    private void RecalculateItemLayout()
    {
        _itemLeftOffsets.Clear();

        // 彻底重置 MinWidth，从视口宽度重新计算，防止累积
        TimelineCanvas.MinWidth = 0;
        var viewWidth = TimelineScrollViewer.Viewport.Width;

        if (_displayedReminders.Count <= 1)
        {
            ResetItemPositions();
            TimelineCanvas.MinWidth = 0;
            return;
        }

        // 按时间排序
        var sorted = _displayedReminders
            .Select((r, i) => (Reminder: r, Index: i))
            .OrderBy(x => x.Reminder.TimeOfDay.TotalMinutes)
            .ToList();

        // 构建重叠组
        var groups = BuildOverlapGroups(sorted);

        // 为重叠组计算基于实际宽度的累积偏移
        const double gap = 8.0;
        foreach (var group in groups)
        {
            if (group.Count <= 1) continue;

            double currentLeft = 0;
            for (var j = 0; j < group.Count; j++)
            {
                var (reminder, index) = group[j];
                _itemLeftOffsets[reminder.Id] = currentLeft;

                // 测量容器实际宽度
                var container = ReminderItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
                var itemWidth = container is Control c && c.Bounds.Width > 0 ? c.Bounds.Width : 180.0;
                currentLeft += Math.Max(itemWidth, 100.0) + gap;
            }
        }

        ApplyItemPositions();

        // 用视口宽度 + 偏移量决定 Canvas 所需宽度
        var maxRight = viewWidth;
        foreach (var offset in _itemLeftOffsets.Values)
            if (offset > maxRight) maxRight = offset;
        TimelineCanvas.MinWidth = Math.Max(viewWidth, maxRight + 250);
    }

    private List<List<(Reminder, int)>> BuildOverlapGroups(List<(Reminder Reminder, int Index)> sorted)
    {
        var groups = new List<List<(Reminder, int)>>();
        if (sorted.Count == 0) return groups;

        var itemHeight = GetItemVisualHeight();
        const double allowedOverlap = 4.0;
        var currentGroup = new List<(Reminder, int)> { sorted[0] };

        for (var i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];

            var prevTop = prev.Reminder.TimeOfDay.Ticks / 1000000000.0 * Scale;
            var currTop = curr.Reminder.TimeOfDay.Ticks / 1000000000.0 * Scale;
            var prevBottom = prevTop + itemHeight;

            if (currTop < prevBottom - allowedOverlap)
                currentGroup.Add(curr);
            else
            {
                groups.Add(currentGroup);
                currentGroup = new List<(Reminder, int)> { curr };
            }
        }
        groups.Add(currentGroup);
        return groups;
    }

    private void ApplyItemPositions()
    {
        for (var i = 0; i < _displayedReminders.Count; i++)
        {
            var container = ReminderItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is not Control c) continue;
            var reminder = _displayedReminders[i];

            Canvas.SetLeft(c, _itemLeftOffsets.GetValueOrDefault(reminder.Id, 0.0));
            c.ClearValue(Control.WidthProperty);
            c.ClearValue(Canvas.TopProperty);
        }
    }

    private void ResetItemPositions()
    {
        for (var i = 0; i < _displayedReminders.Count; i++)
        {
            var container = ReminderItemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container is not Control c) continue;
            Canvas.SetLeft(c, 0.0);
            c.ClearValue(Control.WidthProperty);
            c.ClearValue(Canvas.TopProperty);
        }
    }

    private static bool IsReminderOnDate(Reminder r, DateTime date)
    {
        // 不禁用已关闭的日程，让其显示为灰色
        bool InRange(DateTime dt)
        {
            if (r.StartDate.HasValue && dt.Date < r.StartDate.Value.Date) return false;
            if (r.EndDate.HasValue && dt.Date > r.EndDate.Value.Date) return false;
            return true;
        }

        switch (r.Frequency)
        {
            case ReminderFrequency.Once:
                return r.Time.Date == date.Date && InRange(r.Time);
            case ReminderFrequency.Daily:
                return InRange(date);
            case ReminderFrequency.Weekly:
            {
                var flag = DayOfWeekToFlag(date.DayOfWeek);
                return r.WeekDays.HasFlag(flag) && InRange(date);
            }
            case ReminderFrequency.Yearly:
            {
                int month = r.YearMonth > 0 ? r.YearMonth : r.Time.Month;
                int day = r.YearDay > 0 ? r.YearDay : r.Time.Day;
                return date.Month == month && date.Day == day && InRange(date);
            }
            default:
                return false;
        }
    }

    private static ReminderWeekDays DayOfWeekToFlag(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Sunday => ReminderWeekDays.Sunday,
        DayOfWeek.Monday => ReminderWeekDays.Monday,
        DayOfWeek.Tuesday => ReminderWeekDays.Tuesday,
        DayOfWeek.Wednesday => ReminderWeekDays.Wednesday,
        DayOfWeek.Thursday => ReminderWeekDays.Thursday,
        DayOfWeek.Friday => ReminderWeekDays.Friday,
        DayOfWeek.Saturday => ReminderWeekDays.Saturday,
        _ => ReminderWeekDays.None
    };
    #endregion

    /// <summary>
    /// 请求在指定时间添加一个新日程。
    /// </summary>
    public event EventHandler<DateTime>? AddReminderRequested;

    /// <summary>
    /// 日程被选中时触发。
    /// </summary>
    public event EventHandler<Reminder?>? ReminderSelected;

    #region 频率→颜色转换器
    public static readonly IValueConverter FrequencyToBrushConverter =
        new FuncValueConverter<ReminderFrequency, IBrush>(f => new SolidColorBrush(f switch
        {
            ReminderFrequency.Once => Colors.DodgerBlue,
            ReminderFrequency.Daily => Colors.MediumSeaGreen,
            ReminderFrequency.Weekly => Colors.DarkOrange,
            ReminderFrequency.Yearly => Colors.MediumPurple,
            _ => Colors.Gray
        }));

    public static readonly IValueConverter FrequencyToLabelConverter =
        new FuncValueConverter<ReminderFrequency, string>(f => f switch
        {
            ReminderFrequency.Once => "单次",
            ReminderFrequency.Daily => "每天",
            ReminderFrequency.Weekly => "每周",
            ReminderFrequency.Yearly => "每年",
            _ => ""
        });

    public static readonly IValueConverter BoolToOpacityConverter =
        new FuncValueConverter<bool, double>(b => b ? 1.0 : 0.45);
    #endregion

    public ReminderTimelineControl()
    {
        InitializeComponent();

        // 鼠标滚轮缩放
        TimelineScrollViewer.AddHandler(PointerWheelChangedEvent, OnTimelineWheel, RoutingStrategies.Tunnel);

        // 日期变化时重新过滤
        this.GetObservable(SelectedDateProperty).Subscribe(_ => RefreshFilteredReminders());

        // 选中日程变化时自动滚动到对应位置
        this.GetObservable(SelectedReminderProperty).Subscribe(_ => OnSelectedReminderChanged());
    }

    private void OnTimelineWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Control)
            return;

        e.Handled = true;
        var delta = e.Delta.Y > 0 ? 0.25 : -0.25;
        Scale = Math.Max(0.5, Math.Min(10.0, Scale + delta));
    }

    private void ButtonPrevDay_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    private void ButtonNextDay_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    private void ReminderBlock_OnTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: Reminder reminder })
        {
            SelectedReminder = reminder;
            ReminderSelected?.Invoke(this, reminder);
            e.Handled = true;
        }
    }

    private void ClickToAddOverlay_OnTapped(object? sender, TappedEventArgs e)
    {
        var pos = e.GetPosition(TimelineCanvas);
        var adjustedY = pos.Y;

        var ticks = (long)(adjustedY / Scale * 1000000000.0);
        var timeOfDay = TimeSpan.FromTicks(Math.Max(0, Math.Min(ticks, TimeSpan.FromHours(24).Ticks)));

        // 取整到最近的5分钟
        var roundedMinutes = (int)Math.Round(timeOfDay.TotalMinutes / 5.0) * 5;
        var roundedTime = TimeSpan.FromMinutes(roundedMinutes);

        AddReminderRequested?.Invoke(this, SelectedDate.Date + roundedTime);
        e.Handled = true;
    }

    private void OnSelectedReminderChanged()
    {
        if (SelectedReminder == null) return;

        // 如果当前日期不显示该日程，切换到合适的日期
        if (!DisplayedReminders.Contains(SelectedReminder))
        {
            var targetDate = FindDisplayDateForReminder(SelectedReminder);
            if (targetDate.HasValue)
            {
                SelectedDate = targetDate.Value.Date;
            }
        }

        // 延迟到布局更新后滚动到对应位置
        Dispatcher.UIThread.Post(ScrollToSelectedReminder, DispatcherPriority.Render);
    }

    private void ScrollToSelectedReminder()
    {
        if (SelectedReminder == null) return;

        var timeOfDay = SelectedReminder.TimeOfDay;
        var y = timeOfDay.Ticks / 1000000000.0 * Scale;

        var viewportHeight = TimelineScrollViewer.Viewport.Height;
        if (viewportHeight <= 0) return;

        var scrollOffset = y - viewportHeight / 2.0;
        TimelineScrollViewer.Offset = new Vector(0, Math.Max(0, scrollOffset));
    }

    private DateTime? FindDisplayDateForReminder(Reminder r)
    {
        // 已可在当前日期显示
        if (IsReminderOnDate(r, SelectedDate.Date))
            return SelectedDate.Date;

        switch (r.Frequency)
        {
            case ReminderFrequency.Once:
                return r.Time.Date;
            case ReminderFrequency.Daily:
                return r.StartDate ?? DateTime.Today;
            case ReminderFrequency.Weekly:
            {
                var start = r.StartDate ?? DateTime.Today;
                for (var i = 0; i < 14; i++)
                {
                    var candidate = start.AddDays(i);
                    if (IsReminderOnDate(r, candidate))
                        return candidate.Date;
                }
                return DateTime.Today;
            }
            case ReminderFrequency.Yearly:
            {
                var month = r.YearMonth > 0 ? r.YearMonth : r.Time.Month;
                var day = r.YearDay > 0 ? r.YearDay : r.Time.Day;
                var year = DateTime.Today.Year;
                for (var y = 0; y <= 1; y++)
                {
                    var targetYear = year + y;
                    try
                    {
                        var candidate = new DateTime(targetYear, month,
                            Math.Min(day, DateTime.DaysInMonth(targetYear, month)));
                        if (IsReminderOnDate(r, candidate))
                            return candidate.Date;
                    }
                    catch
                    {
                        // ignore
                    }
                }
                return DateTime.Today;
            }
            default:
                return null;
        }
    }
}
