using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;

namespace ClassIsland.Controls;

/// <summary>
/// WeekOffsetSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class WeekOffsetSettingsControl : UserControl, INotifyPropertyChanged
{
    private ObservableCollection<int> _currentWeeks = [-1, -1, 0, 0, 0];

    private IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();

    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    public ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();

    public ObservableCollection<int> CurrentWeeks
    {
        get => _currentWeeks;
        set
        {
            if (Equals(value, _currentWeeks)) return;
            _currentWeeks = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 初始化一个 <see cref="WeekOffsetSettingsControl"/> 对象。
    /// </summary>
    public WeekOffsetSettingsControl()
    {
        InitializeComponent();
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

    private void WeekOffsetSettingsControl_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Init();
    }

    private void Init()
    {
        CurrentWeeks = ConfigureFileHelper.CopyObject(LessonsService.MultiWeekRotation);
    }

    private void ButtonFinish_OnClick(object sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Settings;

        var dd = (ExactTimeService.GetCurrentLocalDateTime().Date - settings.SingleWeekStartTime).TotalDays;
        for (int i = 2; i < 5; i++)
        {
            int dw = (int)Math.Floor(dd / 7);
            int w = (dw - (CurrentWeeks[i] - 1) + i) % i;
            settings.MultiWeekRotationOffset[i] = w;
        }
    }

    private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.MultiWeekRotationOffset = [-1, -1, 0, 0, 0];
    }
}