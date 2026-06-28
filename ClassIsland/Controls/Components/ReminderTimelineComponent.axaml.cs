using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.Components;

[ComponentInfo("3ce6d794-1687-4845-842a-27fcdaaa7823", "日程时间线", "\ue8bd",
    "以横向时间线显示当天 Reminder 日程，直观查看各日程的时间位置与当前进度。")]
public partial class ReminderTimelineComponent : ComponentBase<ReminderTimelineComponentSettings>, INotifyPropertyChanged
{
    private readonly IProfileService _profileService;
    private readonly IExactTimeService _exactTimeService;
    private DispatcherTimer? _refreshTimer;
    private EventHandler? _timerHandler;
    private bool _isAttached;

    // 颜色常量
    private static readonly Color ClrOnce = Colors.DodgerBlue;
    private static readonly Color ClrDaily = Colors.MediumSeaGreen;
    private static readonly Color ClrWeekly = Colors.DarkOrange;
    private static readonly Color ClrYearly = Colors.MediumPurple;
    private static readonly Color ClrDisable = Colors.LightGray;

    private const double PillH = 22;
    private const double Top = 5;
    private const double Gap = 6;
    private const double GrpPad = 4;
    private const double PadL = 12;
    private const double PadR = 12;
    // MaxDisplay 已被替换为 Settings.MaxRemindersBefore / MaxRemindersAfter
    private const double TimeLabelWidth = 45;

    private double _canvasW;
    private bool _hasItems;
    private int _lastMin = -1;
    private double _tFirst, _tLast;
    private readonly List<(double Time, double Left, double Right)> _visualGroups = new();
    private readonly List<Control> _pills = new();

    private readonly HashSet<Reminder> _subscribedReminders = new();

    // 设置变更防抖
    private CancellationTokenSource? _settingsDebounceCts;

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
        _isAttached = true;
        Subscribe();
        StartTimer();
        Rebuild();
    }

    private void OnDet(object? s, VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        Unsubscribe();
        StopTimer();
        foreach (var c in _pills)
            TimelineCanvas.Children.Remove(c);
        _pills.Clear();
        _visualGroups.Clear();
    }

    private void Subscribe()
    {
        if (_profileService is INotifyPropertyChanged npc)
            npc.PropertyChanged += OnSvcChanged;

        _profileService.Profile.Reminders.CollectionChanged += OnChanged;

        foreach (var r in _profileService.Profile.Reminders)
        {
            if (_subscribedReminders.Add(r))
                r.PropertyChanged += OnPropChanged;
        }

        // 监听设置变更
        if (Settings is INotifyPropertyChanged settingsNpc)
            settingsNpc.PropertyChanged += OnSettingsChanged;
    }

    private void Unsubscribe()
    {
        if (_profileService is INotifyPropertyChanged npc)
            npc.PropertyChanged -= OnSvcChanged;

        _profileService.Profile.Reminders.CollectionChanged -= OnChanged;

        foreach (var r in _subscribedReminders)
            r.PropertyChanged -= OnPropChanged;
        _subscribedReminders.Clear();

        if (Settings is INotifyPropertyChanged settingsNpc)
            settingsNpc.PropertyChanged -= OnSettingsChanged;
        _settingsDebounceCts?.Cancel();
        _settingsDebounceCts?.Dispose();
        _settingsDebounceCts = null;
    }

    private void OnSvcChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IProfileService.Profile))
        {
            Unsubscribe();
            Subscribe();
            if (_isAttached) Rebuild();
        }
    }

    private void OnChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Reminder>())
            {
                if (_subscribedReminders.Add(item))
                    item.PropertyChanged += OnPropChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<Reminder>())
            {
                if (_subscribedReminders.Remove(item))
                    item.PropertyChanged -= OnPropChanged;
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var r in _subscribedReminders)
                r.PropertyChanged -= OnPropChanged;
            _subscribedReminders.Clear();
            foreach (var r in _profileService.Profile.Reminders)
            {
                if (_subscribedReminders.Add(r))
                    r.PropertyChanged += OnPropChanged;
            }
        }

        if (_isAttached)
            _ = Dispatcher.UIThread.InvokeAsync(Rebuild);
    }

    private void OnPropChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Reminder.Title) or nameof(Reminder.Time) or nameof(Reminder.TimeOfDay)
            or nameof(Reminder.Frequency) or nameof(Reminder.WeekDays) or nameof(Reminder.StartDate)
            or nameof(Reminder.EndDate) or nameof(Reminder.YearMonth) or nameof(Reminder.YearDay)
            or nameof(Reminder.IsEnabled))
        {
            if (_isAttached)
                _ = Dispatcher.UIThread.InvokeAsync(Rebuild);
        }
    }

    private void OnSettingsChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReminderTimelineComponentSettings.GroupsBefore)
            or nameof(ReminderTimelineComponentSettings.GroupsAfter)
            or nameof(ReminderTimelineComponentSettings.MaxPerGroup))
        {
            if (!_isAttached) return;
            _settingsDebounceCts?.Cancel();
            _settingsDebounceCts?.Dispose();
            _settingsDebounceCts = new CancellationTokenSource();
            var token = _settingsDebounceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(200, token).ConfigureAwait(false);
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (_isAttached) Rebuild();
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    // 被新的设置变更取消，正常流程
                }
            }, token);
        }
    }

    private void StartTimer()
    {
        StopTimer();
        _timerHandler = (_, _) =>
        {
            if (!_isAttached) return;
            var now = _exactTimeService.GetCurrentLocalDateTime();
            var m = (int)now.TimeOfDay.TotalMinutes;
            MoveNow(now.TimeOfDay);
            if (m != _lastMin)
            {
                _lastMin = m;
                Dispatcher.UIThread.Post(() =>
                {
                    if (_isAttached) Rebuild();
                }, DispatcherPriority.Background);
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
        if (!_isAttached) return;

        foreach (var c in _pills)
            TimelineCanvas.Children.Remove(c);
        _pills.Clear();

        var now = _exactTimeService.GetCurrentLocalDateTime();
        var today = now.Date;
        var allReminders = _profileService.Profile.Reminders
            .Where(r => IsOnDate(r, today))
            .OrderBy(r => r.TimeOfDay.TotalMinutes)
            .ToList();

        // 先按分钟分组
        var allGroups = new List<List<Reminder>>();
        foreach (var r in allReminders)
        {
            if (allGroups.Count > 0 &&
                (int)allGroups.Last().Last().TimeOfDay.TotalMinutes == (int)r.TimeOfDay.TotalMinutes)
                allGroups.Last().Add(r);
            else
                allGroups.Add([r]);
        }

        // 以分组为单位，向前/向后分别限数
        var pastGroups = allGroups
            .Where(g => g[0].TimeOfDay <= now.TimeOfDay)
            .TakeLast(Settings.GroupsBefore)
            .ToList();
        var futureGroups = allGroups
            .Where(g => g[0].TimeOfDay > now.TimeOfDay)
            .Take(Settings.GroupsAfter)
            .ToList();

        var groups = pastGroups.Concat(futureGroups).ToList();

        _hasItems = groups.Count > 0;

        if (groups.Count == 0)
        {
            EmptyHint.IsVisible = true;
            TrackLine.IsVisible = false;
            NowLine.IsVisible = false;
            NowDot.IsVisible = false;
            NotifyPropertyChanged(nameof(HasItems));
            return;
        }

        EmptyHint.IsVisible = false;
        TrackLine.IsVisible = true;

        _tFirst = groups.First().First().TimeOfDay.TotalMinutes;
        _tLast = groups.Last().Last().TimeOfDay.TotalMinutes;
        _visualGroups.Clear();

        var curX = PadL;

        foreach (var g in groups)
        {
            var n = g.Count;
            var r0 = g.First();
            var timeStr = r0.TimeOfDay.ToString("hh\\:mm");
            var color = PickColor(r0.Frequency, r0.IsEnabled);

            if (n == 1)   // 单个日程：时间标签（彩色） + 胶囊（仅标题）
            {
                var r = r0;
                var w = CalcW(r);
                var past = r.TimeOfDay <= now.TimeOfDay;
                var op = (r.IsEnabled ? 1.0 : 0.4) * (past ? 0.45 : 1.0);
                var pillW = w;
                var totalW = TimeLabelWidth + pillW;

                // 时间标签（彩色背景，透明度与胶囊一致）
                var timeLabel = CreateTimeLabel(timeStr, color, op);
                Canvas.SetLeft(timeLabel, curX);
                Canvas.SetTop(timeLabel, Top);
                TimelineCanvas.Children.Add(timeLabel);
                _pills.Add(timeLabel);

                // 胶囊（无时间）
                var pill = CreatePill(pillW, r.Frequency, r.IsEnabled, op, r.Title);
                Canvas.SetLeft(pill, curX + TimeLabelWidth);
                Canvas.SetTop(pill, Top);
                TimelineCanvas.Children.Add(pill);
                _pills.Add(pill);

                _visualGroups.Add(((int)r.TimeOfDay.TotalMinutes, curX, curX + totalW));
                curX += totalW + Gap;
            }
            else          // 多个日程：时间标签在外，分组框只含胶囊
            {
                var maxShown = Settings.MaxPerGroup;
                var remaining = n - maxShown;
                var shownItems = remaining > 0 ? g.Take(maxShown).ToList() : g;
                var hasEtc = remaining > 0;
                var etcWidth = hasEtc ? 30.0 : 0.0;
                var totalItems = shownItems.Count + (hasEtc ? 1 : 0); // 显示胶囊 + "等"胶囊
                var pillsWidth = shownItems.Sum(r => CalcW(r)) + Gap * (totalItems - 1) + etcWidth;
                // 分组框宽度 = 胶囊总宽 + 左右内边距
                var bgW = pillsWidth + GrpPad * 2;
                // 整个组占用的总宽度 = 时间标签宽度 + 分组框宽度
                var groupTotalW = TimeLabelWidth + bgW;

                _visualGroups.Add(((int)r0.TimeOfDay.TotalMinutes, curX, curX + groupTotalW));

                // 时间标签放在分组框外侧左侧
                double firstOp = (r0.IsEnabled ? 0.9 : 0.4) * (r0.TimeOfDay <= now.TimeOfDay ? 0.5 : 1.0);
                var timeLabel = CreateTimeLabel(timeStr, color, firstOp);
                Canvas.SetLeft(timeLabel, curX);
                Canvas.SetTop(timeLabel, Top);
                TimelineCanvas.Children.Add(timeLabel);
                _pills.Add(timeLabel);

                // 分组背景框（只含胶囊）
                var pillsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = Gap
                };
                foreach (var r in shownItems)
                {
                    var past = r.TimeOfDay <= now.TimeOfDay;
                    var op = (r.IsEnabled ? 0.9 : 0.4) * (past ? 0.5 : 1.0);
                    var pill = CreatePill(CalcW(r), r.Frequency, r.IsEnabled, op, r.Title);
                    pillsPanel.Children.Add(pill);
                }
                if (remaining > 0)
                {
                    // 超出部分显示"等"胶囊
                    var etcPill = CreatePill(etcWidth, g[0].Frequency, g[0].IsEnabled, 0.6, $"等{remaining}");
                    pillsPanel.Children.Add(etcPill);
                }

                var bg = new Border
                {
                    Width = bgW,
                    Height = PillH + GrpPad * 2,
                    CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.25 },
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.White) { Opacity = 0.4 },
                    Child = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(GrpPad, 0, GrpPad, 0),
                        Children = { pillsPanel }
                    }
                };
                Canvas.SetLeft(bg, curX + TimeLabelWidth);
                Canvas.SetTop(bg, Top - GrpPad);
                TimelineCanvas.Children.Add(bg);
                _pills.Add(bg);

                curX += groupTotalW + Gap;
            }
        }

        _canvasW = curX - Gap + PadR;
        TimelineCanvas.Width = _canvasW;
        TrackLine.Width = _canvasW;

        MoveNow(now.TimeOfDay);
        NotifyPropertyChanged(nameof(HasItems));
    }

    /// <summary>
    /// 创建带背景色的时间标签（背景色为日程颜色，文字白色）
    /// </summary>
    private Border CreateTimeLabel(string time, Color color, double opacity = 1.0)
    {
        var brush = new SolidColorBrush(color) { Opacity = opacity };
        return new Border
        {
            Width = TimeLabelWidth,
            Height = PillH,
            CornerRadius = new CornerRadius(4),
            Background = brush,
            Child = new TextBlock
            {
                Text = time,
                FontSize = 11,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            }
        };
    }

    /// <summary>
    /// 创建胶囊（仅标题，无时间）
    /// </summary>
    private Border CreatePill(double w, ReminderFrequency freq, bool isEnabled, double op, string title)
    {
        var color = PickColor(freq, isEnabled);
        var brush = new SolidColorBrush(color) { Opacity = op };

        var inner = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };

        var maxTextWidth = Math.Max(0, w - 8); // 减去 StackPanel 左右 Margin (4+4)
        inner.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = maxTextWidth
        });

        return new Border
        {
            Width = w,
            Height = PillH,
            CornerRadius = new CornerRadius(5),
            Background = brush,
            ClipToBounds = true,
            Child = inner
        };
    }

    private static double CalcW(Reminder r)
    {
        var titlePx = r.Title.Sum(c => c > 127 ? 12 : 7);
        var width = Math.Max(50, Math.Min(titlePx + 24 + 16, 130));
        return width;
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