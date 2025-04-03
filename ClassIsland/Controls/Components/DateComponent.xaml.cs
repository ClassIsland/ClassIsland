using ClassIsland.Core.Abstractions.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// DateComponent.xaml 的交互逻辑
/// </summary>
[ComponentInfo("DF3F8295-21F6-482E-BADA-FA0E5F14BB66", "日期", PackIconKind.CalendarOutline, "显示今天的日期和星期。")]
public partial class DateComponent : ComponentBase, INotifyPropertyChanged
{
    public ILessonsService LessonsService { get; }

    private IExactTimeService ExactTimeService { get; }

    private DateTime _today = DateTime.Now;

    public DateTime Today
    {
        get => _today;
        set
        {
            if (value == _today) return;
            _today = value;
            OnPropertyChanged();
        }
    }

    public DateComponent(IExactTimeService exactTimeService, ILessonsService lessonsService)
    {
        LessonsService = lessonsService;
        InitializeComponent();
        ExactTimeService = exactTimeService;
        Loaded += (_, _) => LessonsService.PostMainTimerTicked += UpdateDate;
        Unloaded += (_, _) => LessonsService.PostMainTimerTicked -= UpdateDate;
    }

    private void UpdateDate(object? sender, EventArgs e)
    {
        Today = ExactTimeService.GetCurrentLocalDateTime();
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