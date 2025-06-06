using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using System.Windows;

namespace ClassIsland.Controls.Components;

/// <summary>
/// CountDownComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("7C645D35-8151-48BA-B4AC-15017460D994", "倒计时日", PackIconKind.TimerOutline, "显示距离某一天的倒计时。")]
public partial class CountDownComponent : ComponentBase<CountDownComponentSettings>, INotifyPropertyChanged
{
    private string _daysLeft = "";
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

    public CountDownComponent(ILessonsService lessonsService, IExactTimeService exactTimeService)
    {
        InitializeComponent();
        LessonsService = lessonsService;
        ExactTimerService = exactTimeService;
        Loaded += (_, _) =>
        {
            UpdateContent();
            LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
        };
        Unloaded += (_, _) => {
            LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
        };
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        DaysLeft = $"{Math.Max((Settings.OverTime.Date - ExactTimerService.GetCurrentLocalDateTime().Date).Days, 0)}";
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