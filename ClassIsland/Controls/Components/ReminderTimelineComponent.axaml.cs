using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.Components;

[ComponentInfo("3ce6d794-1687-4845-842a-27fcdaaa7823", "日程时间线", "\ue8bd",
    "以横向时间线显示当天 Reminder 日程，直观查看各日程的时间位置与当前进度。")]
public partial class ReminderTimelineComponent : ComponentBase, INotifyPropertyChanged
{
    private readonly IProfileService _profileService;
    private readonly IExactTimeService _exactTimeService;
    private DispatcherTimer? _refreshTimer;
    private EventHandler? _timerHandler;

    // 颜色常量（不再使用共享静态画刷，避免透明度污染）
    private static readonly Color ClrOnce = Colors.DodgerBlue;
    private static readonly Color ClrDaily = Colors.MediumSeaGreen;
    private static readonly Color ClrWeekly = Colors.DarkOrange;
    private static readonly Color ClrYearly = Colors.MediumPurple;
    private static readonly Color ClrDisable = Colors.LightGray;
    private static readonly Color ClrTime = Colors.White;

    private const double PillH = 22;
    private const double Top = 5;
    private const double Gap = 6;
    private const double GrpPad = 4;
    private const double PadL = 12;
    private const double PadR = 12;
    private const int MaxDisplay = 20;

    private double _canvasW;
    private bool _hasItems;
    private int _lastMin = -1;
    private double _tFirst, _tLast;
    private readonly List<(double Time, double Left, double Right)> _visualGroups = new();
    private readonly List<Border> _pills = new();

    // 跟踪已订阅的提醒，确保正确取消
    private readonly HashSet<Reminder> _subscribedReminders = new();

    public ReminderTimelineComponent(IProfileService profileService, IExactTimeService exactTimeService)
    {
        _profileService = profileService;
        _exactTimeService = exactTimeService;
        InitializeComponent();
        AttachedToVisualTree += OnAtt;
        DetachedFromVisualTree += OnDet;
    }

    private void OnAtt(object? s, VisualTreeAttachmentEventArgs e)
    {
        Subscribe();
        StartTimer();
        Rebuild();
    }

    private void OnDet(object? s, VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        StopTimer();
    }

    private void Subscribe()
    {
        // 订阅服务属性变更（用于检测 Profile 替换）
        if (_profileService is INotifyPropertyChanged npc)
            npc.PropertyChanged += OnSvcChanged;

        // 订阅集合变更
        _profileService.Profile.Reminders.CollectionChanged += OnChanged;

        // 订阅当前所有提醒
        foreach (var r in _profileService.Profile.Reminders)
        {
            if (_subscribedReminders.Add(r))
                r.PropertyChanged += OnPropChanged;
        }
    }

    private void Unsubscribe()
    {
        // 取消服务事件
        if (_profileService is INotifyPropertyChanged npc)
            npc.PropertyChanged -= OnSvcChanged;

        // 取消集合事件
        _profileService.Profile.Reminders.CollectionChanged -= OnChanged;

        // 取消所有已订阅提醒的属性变更
        foreach (var r in _subscribedReminders)
            r.PropertyChanged -= OnPropChanged;
        _subscribedReminders.Clear();
    }

    private void OnSvcChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IProfileService.Profile))
        {
            Unsubscribe();
            Subscribe();
            Rebuild();
        }
    }

    private void OnChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        // 处理新增项
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Reminder>())
            {
                if (_subscribedReminders.Add(item))
                    item.PropertyChanged += OnPropChanged;
            }
        }

        // 处理移除项
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<Reminder>())
            {
                if (_subscribedReminders.Remove(item))
                    item.PropertyChanged -= OnPropChanged;
            }
        }

        // 对于 Reset 操作，需要重新订阅全部（理论上会触发 NewItems 包含全部）
        // 但为了安全，如果 e.Action == Reset，则完全重新订阅
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // 先取消所有，再重新订阅
            foreach (var r in _subscribedReminders)
                r.PropertyChanged -= OnPropChanged;
            _subscribedReminders.Clear();
            foreach (var r in _profileService.Profile.Reminders)
            {
                if (_subscribedReminders.Add(r))
                    r.PropertyChanged += OnPropChanged;
            }
        }

        _ = Dispatcher.UIThread.InvokeAsync(Rebuild);
    }

    private void OnPropChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Reminder.Title) or nameof(Reminder.Time) or nameof(Reminder.TimeOfDay)
            or nameof(Reminder.Frequency) or nameof(Reminder.WeekDays) or nameof(Reminder.StartDate)
            or nameof(Reminder.EndDate) or nameof(Reminder.YearMonth) or nameof(Reminder.YearDay)
            or nameof(Reminder.IsEnabled))
            _ = Dispatcher.UIThread.InvokeAsync(Rebuild);
    }

    private void StartTimer()
    {
        StopTimer();
        _timerHandler = (_, _) =>
        {
            var now = _exactTimeService.GetCurrentLocalDateTime();
            var m = (int)now.TimeOfDay.TotalMinutes;
            MoveNow(now.TimeOfDay);
            if (m != _lastMin)
            {
                _lastMin = m;
                Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Background);
            }
        };
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += _timerHandler;
        _refreshTimer.Start();
    }

    private void StopTimer()
    {
        if (_refreshTimer != null)
        {
            if (_timerHandler != null) _refreshTimer.Tick -= _timerHandler;
            _refreshTimer.Stop();
            _refreshTimer = null;
        }

        _timerHandler = null;
    }

    // ========================================================================

    private void Rebuild()
    {
        // 清理旧的胶囊
        foreach (var p in _pills)
            TimelineCanvas.Children.Remove(p);
        _pills.Clear();

        var now = _exactTimeService.GetCurrentLocalDateTime();
        var today = now.Date;
        var reminders = _profileService.Profile.Reminders
            .Where(r => IsOnDate(r, today))
            .OrderBy(r => r.TimeOfDay.TotalMinutes)
            .Take(MaxDisplay)
            .ToList();

        _hasItems = reminders.Count > 0;

        if (reminders.Count == 0)
        {
            EmptyHint.IsVisible = true;
            TrackLine.IsVisible = false;
            NowLine.IsVisible = false;
            NowDot.IsVisible = false;
            TimelineCanvas.Width = 200;
            NotifyPropertyChanged(nameof(HasItems));
            return;
        }

        EmptyHint.IsVisible = false;
        TrackLine.IsVisible = true;

        _tFirst = reminders.First().TimeOfDay.TotalMinutes;
        _tLast = reminders.Last().TimeOfDay.TotalMinutes;
        _visualGroups.Clear();

        // 按分钟分组
        var groups = new List<List<Reminder>>();
        foreach (var r in reminders)
        {
            if (groups.Count > 0 && (int)groups.Last().Last().TimeOfDay.TotalMinutes == (int)r.TimeOfDay.TotalMinutes)
                groups.Last().Add(r);
            else
                groups.Add([r]);
        }

        var curX = PadL;

        foreach (var g in groups)
        {
            var n = g.Count;
            var r0 = g.First();

            if (n == 1)
            {
                var r = r0;
                var w = CalcW(r);
                var past = r.TimeOfDay <= now.TimeOfDay;
                var op = (r.IsEnabled ? 1.0 : 0.4) * (past ? 0.45 : 1.0);
                MakePill(w, curX, r.Frequency, r.IsEnabled, op, r.TimeOfDay.ToString("hh\\:mm"), r.Title);
                _visualGroups.Add(((int)r.TimeOfDay.TotalMinutes, curX, curX + w));
                curX += w + Gap;
            }
            else
            {
                // 同一分钟：背景框 + 横向排列
                var iw = g.Max(r => CalcW(r));
                var bgW = iw * n + GrpPad * 2 + Gap * (n - 1);

                _visualGroups.Add(((int)r0.TimeOfDay.TotalMinutes, curX, curX + bgW));

                var bg = new Border
                {
                    Width = bgW,
                    Height = PillH + GrpPad * 2,
                    CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.25 },
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.White) { Opacity = 0.4 }
                };
                Canvas.SetLeft(bg, curX);
                Canvas.SetTop(bg, Top - GrpPad);
                TimelineCanvas.Children.Add(bg);
                _pills.Add(bg);

                var xo = curX + GrpPad;
                foreach (var r in g)
                {
                    var past = r.TimeOfDay <= now.TimeOfDay;
                    var op = (r.IsEnabled ? 0.9 : 0.4) * (past ? 0.5 : 1.0);
                    MakePill(iw, xo, r.Frequency, r.IsEnabled, op, r.TimeOfDay.ToString("hh\\:mm"), r.Title);
                    xo += iw + Gap;
                }

                curX += bgW + Gap;
            }
        }

        _canvasW = curX - Gap + PadR;
        TimelineCanvas.Width = _canvasW;
        TrackLine.Width = _canvasW;

        MoveNow(now.TimeOfDay);
        NotifyPropertyChanged(nameof(HasItems));
    }

    /// <summary>根据文本估算胶囊宽度</summary>
    private static double CalcW(Reminder r)
    {
        var titlePx = r.Title.Sum(c => c > 127 ? 12 : 7);
        var width = Math.Max(60, Math.Min(titlePx + 36 + 16, 140));
        return width;
    }

    private void MakePill(double w, double left, ReminderFrequency freq, bool isEnabled, double op, string time,
        string title)
    {
        // 获取颜色并新建画刷，避免共享实例透明度污染
        var color = PickColor(freq, isEnabled);
        var brush = new SolidColorBrush(color) { Opacity = op };

        var inner = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };
        inner.Children.Add(new TextBlock
        {
            Text = time,
            FontSize = 10,
            Foreground = new SolidColorBrush(ClrTime),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        inner.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            Foreground = Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(3, 0, 0, 0)
        });

        var pill = new Border
        {
            Width = w,
            Height = PillH,
            CornerRadius = new CornerRadius(5),
            Background = brush,
            ClipToBounds = true,
            Child = inner
        };
        Canvas.SetLeft(pill, left);
        Canvas.SetTop(pill, Top);
        TimelineCanvas.Children.Add(pill);
        _pills.Add(pill);
    }

    private void MoveNow(TimeSpan tod)
    {
        if (_pills.Count == 0 || _visualGroups.Count == 0)
        {
            NowLine.IsVisible = false;
            NowDot.IsVisible = false;
            return;
        }

        var nowMin = tod.TotalMinutes;
        var first = _visualGroups.First();
        var last = _visualGroups.Last();
        double x;

        if (nowMin <= first.Time)
        {
            x = first.Left;
        }
        else if (nowMin >= last.Time)
        {
            x = last.Right;
        }
        else
        {
            for (var i = 0; i < _visualGroups.Count - 1; i++)
            {
                var cur = _visualGroups[i];
                var next = _visualGroups[i + 1];
                if (nowMin >= cur.Time && nowMin < next.Time)
                {
                    var t = (nowMin - cur.Time) / (next.Time - cur.Time);
                    x = cur.Right + t * (next.Left - cur.Right);
                    goto draw;
                }
            }

            // 所有日程同一分钟
            x = first.Left + (last.Right - first.Left) / 2;
        }

        draw:
        if (x < 0) x = 0;
        if (x > _canvasW) x = _canvasW;

        Canvas.SetLeft(NowLine, x - 1);
        Canvas.SetLeft(NowDot, x - 3.5);
        NowLine.IsVisible = true;
        NowDot.IsVisible = true;
    }

    // ========================================================================

    private static Color PickColor(ReminderFrequency freq, bool isEnabled)
    {
        if (!isEnabled) return ClrDisable;
        return freq switch
        {
            ReminderFrequency.Once => ClrOnce,
            ReminderFrequency.Daily => ClrDaily,
            ReminderFrequency.Weekly => ClrWeekly,
            ReminderFrequency.Yearly => ClrYearly,
            _ => ClrDisable
        };
    }

    private static bool IsOnDate(Reminder r, DateTime date)
    {
        if ((r.StartDate.HasValue && date.Date < r.StartDate.Value.Date) ||
            (r.EndDate.HasValue && date.Date > r.EndDate.Value.Date))
            return false;

        return r.Frequency switch
        {
            ReminderFrequency.Once => r.Time.Date == date.Date,
            ReminderFrequency.Daily => true,
            ReminderFrequency.Weekly => (r.WeekDays & Flag(date.DayOfWeek)) != 0,
            ReminderFrequency.Yearly => date.Month == (r.YearMonth > 0 ? r.YearMonth : r.Time.Month) &&
                                        date.Day == (r.YearDay > 0 ? r.YearDay : r.Time.Day),
            _ => false
        };
    }

    private static ReminderWeekDays Flag(DayOfWeek d) => d switch
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

    public bool HasItems => _hasItems;
    public new event PropertyChangedEventHandler? PropertyChanged;
    private void NotifyPropertyChanged(string n = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}