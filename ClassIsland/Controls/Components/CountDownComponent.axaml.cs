using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using ReactiveUI;

namespace ClassIsland.Controls.Components;

/// <summary>
/// CountDownComponent.xaml 的交互逻辑
/// </summary>
[PseudoClasses(":connector-colored", ":compact", ":progress-colored", ":progress-visible")]
[ComponentInfo("7C645D35-8151-48BA-B4AC-15017460D994", "倒计时日", "\uf361", "显示距离某一天的倒计时。")]
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

    public CountDownComponent(ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        InitializeComponent();
        LessonsService = lessonsService;
        ExactTimerService = exactTimeService;
        IDisposable? observer1 = null;
        IDisposable? observer2 = null;
        IDisposable? observer3 = null;
        IDisposable? observer4 = null;
        Loaded += (_, _) =>
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
        Unloaded += (_, _) => {
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

    private void UpdateContent()
    {
        var delta = Settings.OverTime - ExactTimerService.GetCurrentLocalDateTime();
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        var totalTime = Settings.OverTime - Settings.StartTime;
        var totalSeconds = totalTime.TotalSeconds;
        DaysLeft = Settings.CustomStringFormat
            .Replace("%D", Math.Ceiling(delta.TotalDays).ToString(CultureInfo.InvariantCulture))
            .Replace("%H", Math.Ceiling(delta.TotalHours).ToString(CultureInfo.InvariantCulture))
            .Replace("%M", Math.Ceiling(delta.TotalMinutes).ToString("00", CultureInfo.InvariantCulture))
            .Replace("%S", Math.Ceiling(delta.TotalSeconds).ToString("00", CultureInfo.InvariantCulture))
            .Replace("%X", Math.Ceiling(delta.TotalMilliseconds).ToString("000", CultureInfo.InvariantCulture))
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
            ? Settings.OverTime - ExactTimerService.GetCurrentLocalDateTime()
            : ExactTimerService.GetCurrentLocalDateTime() - Settings.StartTime).TotalSeconds;
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
