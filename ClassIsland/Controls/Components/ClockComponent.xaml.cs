using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Services;

using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// ClockComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("9E1AF71D-8F77-4B21-A342-448787104DD9", "时钟", PackIconKind.ClockDigital, "显示现在的时间，支持精确到秒。")]
public partial class ClockComponent : ComponentBase<ClockComponentSettings>, INotifyPropertyChanged
{
    private DateTime _currentTime = DateTime.Now;

    public DateTime CurrentTime
    {
        get => _currentTime;
        set
        {
            if (value == _currentTime) return;
            _currentTime = value;
            OnPropertyChanged();
        }
    }

    private bool _isTimeSeparatorShowing = true;

    public bool IsTimeSeparatorShowing
    {
        get => _isTimeSeparatorShowing;
        set
        {
            if (value == _isTimeSeparatorShowing) return;
            _isTimeSeparatorShowing = value;
            OnPropertyChanged();
        }
    }

    public ILessonsService LessonsService { get; }

    public IExactTimeService ExactTimeService { get; }

    public SettingsService SettingsService { get; }

    public ClockComponent(ILessonsService lessonsService, IExactTimeService exactTimeService, SettingsService settingsService)
    {
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        SettingsService = settingsService;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdateContent();
            LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        };
        Unloaded += (_, _) => LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        CurrentTime = Settings.ShowRealTime ? DateTime.Now : ExactTimeService.GetCurrentLocalDateTime();

        IsTimeSeparatorShowing = !Settings.FlashTimeSeparator || Settings.ShowSeconds || CurrentTime.Second % 2 == 1;
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