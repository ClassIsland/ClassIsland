using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Core.Assists;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using ReactiveUI;

namespace ClassIsland.Controls.Components;

/// <summary>
/// CountDownComponent.xaml 的交互逻辑
/// </summary>
[PseudoClasses(":connector-colored", ":compact", ":progress-colored", ":progress-visible")]
[ComponentInfo("7C645D35-8151-48BA-B4AC-15017460D994", "倒计时", "\uf361", "显示距离某一天的倒计时。")]
public partial class CountDownComponent : ComponentBase<CountDownComponentSettings>, INotifyPropertyChanged
{
    public static readonly FuncValueConverter<double, Geometry?> PercentToPathGeometryConverter = new(percentage =>
    {
        // 控件尺寸
        const double width = 22;
        const double height = 22;

        // 描边厚度，我们需要它来计算半径，以防止圆角超出边界
        const double strokeThickness = 3;

        const double radius = (width / 2) - (strokeThickness / 2);
        var center = new Point(width / 2, height / 2);

        if (percentage >= 100) percentage = 99.9999; // 防止闭合时出现渲染问题
        if (percentage <= 0) return null;

        double angle = (percentage / 100) * 360;
            
        // 计算起点和终点
        var startPoint = new Point(
            center.X,
            center.Y - radius
        );

        double angleRad = (Math.PI / 180.0) * (angle - 90);
        var endPoint = new Point(
            center.X + radius * Math.Cos(angleRad),
            center.Y + radius * Math.Sin(angleRad)
        );

        // 创建路径数据
        var segments = new PathSegments
        {
            new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180,
                SweepDirection = SweepDirection.Clockwise,
                IsStroked = true
            }
        };
            
        var figure = new PathFigure
        {
            StartPoint = startPoint,
            Segments = segments,
            IsClosed = false
        };

        var geometry = new PathGeometry();
        geometry.Figures?.Add(figure);
            
        return geometry;
    });
    
    private string _daysLeft = "";
    private double _percent = 0.0;
    private ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimerService { get; }
    public SettingsService SettingsService { get; }

    public string DaysLeft
    {
        get => _daysLeft;
        set
        {
            if (value == _daysLeft) return;
            _daysLeft = value;
            OnPropertyChanged();
        }
    }

    public double Percent
    {
        get => _percent;
        set
        {
            if (value.Equals(_percent)) return;
            _percent = value;
            OnPropertyChanged();
        }
    }

    public CountDownComponent(ILessonsService lessonsService, IExactTimeService exactTimeService, SettingsService settingsService)
    {
        InitializeComponent();
        LessonsService = lessonsService;
        ExactTimerService = exactTimeService;
        SettingsService = settingsService;
        IDisposable? observer1 = null;
        IDisposable? observer2 = null;
        IDisposable? observer3 = null;
        IDisposable? observer4 = null;
        AttachedToVisualTree += (_, _) =>
        {
            UpdateContent();
            observer1?.Dispose();
            observer2?.Dispose();
            observer3?.Dispose();
            observer4?.Dispose();
            observer1 = Settings.ObservableForProperty(x => x.IsConnectorColorEmphasized)
                .Subscribe(_ => UpdateStyleClasses());
            observer2 = Settings.ObservableForProperty(x => x.IsCompactModeEnabled)
                .Subscribe(_ => UpdateStyleClasses());
            observer3 = Settings.ObservableForProperty(x => x.UseAccentOnProgressBar)
                .Subscribe(_ => UpdateStyleClasses());
            observer4 = Settings.ObservableForProperty(x => x.ShowProgress)
                .Subscribe(_ => UpdateStyleClasses());
            UpdateStyleClasses();
            LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        };
        DetachedFromVisualTree += (_, _) => {
            observer1?.Dispose();
            observer2?.Dispose();
            observer3?.Dispose();
            observer4?.Dispose();
            LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        };
    }

    private void UpdateStyleClasses()
    {
        PseudoClasses.Set(":connector-colored", Settings.IsConnectorColorEmphasized);
        PseudoClasses.Set(":compact", Settings.IsCompactModeEnabled);
        PseudoClasses.Set(":progress-colored", Settings.UseAccentOnProgressBar);
        PseudoClasses.Set(":progress-visible", Settings.ShowProgress);
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private (DateTime start, DateTime end) GetTimeRangeForStaticTime()
    {
        return (Settings.StartTime, Settings.OverTime);
    }
    
    private (DateTime start, DateTime end) GetTimeRangeForCycle(DateTime now)
    {
        var start = Settings.CycleStartTime;
        var before = Settings.IsAdvancedCycleTimingEnabled ? Settings.CycleBeforeDuration : TimeSpan.Zero;
        var after = Settings.IsAdvancedCycleTimingEnabled ? Settings.CycleAfterDuration : TimeSpan.Zero;
        var duration = Settings.CycleDuration;
        var totalDuration = before + duration + after;
        if (totalDuration <= TimeSpan.Zero)
        {
            return (start, start);
        }
        // 学生再次踏上轮回
        var cycles = (int)Math.Min(Math.Floor((now - start) / totalDuration),
            Settings.IsCycleCountLimited ? Settings.CycleCountLimit : double.PositiveInfinity);
        var timingStart = start + (cycles * totalDuration) + before;
        return (timingStart, timingStart + duration);
    }
    
    private (DateTime start, DateTime end) GetTimeRangeForToday(DateTime now)
    {
        var today = now.Date;
        var tl = LessonsService.CurrentClassPlan?.TimeLayout;
        if (Settings.NatureTimeUseMode == 1)
        {
            return (today, today.AddDays(1));
        }

        var fallbackStart = Settings.NatureTimeUseMode == 2 ? DateTime.MinValue : today;
        var fallbackEnd = Settings.NatureTimeUseMode == 2 ? DateTime.MinValue : today.AddDays(1);
        var start = today + tl?.Layouts.FirstOrDefault(x => x.TimeType is 0 or 1)?.StartTime ?? fallbackStart;
        var end = today + tl?.Layouts.LastOrDefault(x => x.TimeType is 0 or 1)?.EndTime ?? fallbackEnd;
        return (start, end);
    }
    
    private (DateTime start, DateTime end) GetTimeRangeForWeek(DateTime now)
    {
        var today = now.Date;
        var startWeekDay = Settings.IsCustomWeekCountdownStartDayEnabled
            ? Settings.WeekCountdownStartDay
            : SettingsService.Settings.SingleWeekStartTime.DayOfWeek;
        var startWeekTime = today.AddDays(-((today.DayOfWeek - startWeekDay + 7) % 7));
        var endWeekTime = today.AddDays(7);
        if (Settings.NatureTimeUseMode == 1)
        {
            return (startWeekTime, endWeekTime.AddDays(1));
        }
        ClassPlan? firstCp = null;
        ClassPlan? lastCp = null;
        var firstCpDate = startWeekTime;
        var lastCpDate = endWeekTime;
        for (int i = 0; i < 7; i++)
        {
            var day = startWeekTime.AddDays(i);
            if (firstCp == null)
            {
                firstCp = LessonsService.GetClassPlanByDate(day);
                firstCpDate = day;
            }
            var l = LessonsService.GetClassPlanByDate(day);
            if (l != null)
            {
                lastCp = l;
                lastCpDate = day;
            }
        }

        var firstTl = firstCp?.TimeLayout?.Layouts.FirstOrDefault(x => x.TimeType is 0 or 1);
        var lastTl = lastCp?.TimeLayout?.Layouts.LastOrDefault(x => x.TimeType is 0 or 1);
        if (firstTl == null || lastTl == null)
        {
            return Settings.NatureTimeUseMode == 2
                ? (DateTime.MinValue, DateTime.MinValue)
                : (startWeekTime, endWeekTime.AddDays(1));
        }

        return (firstCpDate + firstTl.StartTime, lastCpDate + lastTl.EndTime);
    }

    private void UpdateContent()
    {
        var now = ExactTimerService.GetCurrentLocalDateTime();
        var (start, end) = Settings.CountdownSource switch
        {
            0 => GetTimeRangeForStaticTime(),
            1 => GetTimeRangeForCycle(now),
            2 => GetTimeRangeForToday(now),
            3 => GetTimeRangeForWeek(now),
            _ => (DateTime.MinValue, DateTime.MinValue)
        };
        var delta = end - now;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        var totalTime = end - start;
        var totalSeconds = totalTime.TotalSeconds;
        DaysLeft = Settings.CustomStringFormat
            .Replace("%D", Math.Ceiling(delta.TotalDays).ToString(CultureInfo.InvariantCulture))
            .Replace("%H", Math.Ceiling(delta.TotalHours).ToString(CultureInfo.InvariantCulture))
            .Replace("%M", Math.Ceiling(delta.TotalMinutes).ToString(CultureInfo.InvariantCulture))
            .Replace("%S", Math.Ceiling(delta.TotalSeconds).ToString(CultureInfo.InvariantCulture))
            .Replace("%X", Math.Ceiling(delta.TotalMilliseconds).ToString(CultureInfo.InvariantCulture))
            .Replace("%P",
                Math.Round(totalTime <= TimeSpan.Zero ? 0.0 : (totalTime - delta) / totalTime, 2)
                    .ToString("P0", CultureInfo.InvariantCulture))
            .Replace("%L",
                Math.Round(totalTime <= TimeSpan.Zero ? 0.0 : delta / totalTime, 2)
                    .ToString("P0", CultureInfo.InvariantCulture))
            .Replace("%d", delta.Days.ToString(CultureInfo.InvariantCulture))
            .Replace("%h", delta.Hours.ToString(CultureInfo.InvariantCulture))
            .Replace("%m", delta.Minutes.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%s", delta.Seconds.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%x", delta.Milliseconds.ToString("000", CultureInfo.InvariantCulture));
        var value = (Settings.IsProgressInverted
            ? end - now
            : now - start).TotalSeconds;
        var progressTick = MainWindowStylesAssist.GetIsProgressAccuracyReduced(this)
            ? Math.Max(10.0, totalSeconds / 500.0)
            : 1.0;
        var secondsTicked = Math.Round(value / progressTick) * progressTick;
        Percent = totalSeconds <= 0 ? 0 : secondsTicked / totalSeconds * 100.0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
