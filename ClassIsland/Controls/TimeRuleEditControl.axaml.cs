using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Core.Extensions;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
namespace ClassIsland.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl : UserControl
{
    /// <inheritdoc />
    public TimeRuleEditControl()
    {
        this.GetObservable(TimeRuleProperty).Subscribe(TimeRulePropertyOnNext);
        InitializeComponent();
    }

    private void TimeRulePropertyOnNext(TimeRule? newValue)
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        if (newValue != null)
        {
            UpdateWeekCountDivTotalOptions();
            UpdateWeekCountDivOptions();

            ViewModel.WeekCountDivIndex = newValue.WeekCountDiv;
            ViewModel.WeekCountDivTotalIndex = newValue.WeekCountDivTotal - 2;
            
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_updating || TimeRule == null) return;
        if (e.PropertyName == nameof(ViewModel.WeekCountDivIndex))
        {
            TimeRule.WeekCountDiv = ViewModel.WeekCountDivIndex;
        }
        else if (e.PropertyName == nameof(ViewModel.WeekCountDivTotalIndex))
        {
            TimeRule.WeekCountDivTotal = ViewModel.WeekCountDivTotalIndex + 2;
            if (TimeRule.WeekCountDivTotal < TimeRule.WeekCountDiv)
            {
                _updating = true;
                ViewModel.WeekCountDivIndex = -1;
                Dispatcher.UIThread.Post(() =>
                    ViewModel.WeekCountDivIndex = 0,
                    DispatcherPriority.Background);
                _updating = false;
            }
            UpdateWeekCountDivTotalOptions();
            UpdateWeekCountDivOptions();
        }
    }
    
    private void UpdateWeekCountDivOptions()
    {
        if (_updating || TimeRule == null) return;      
        if (ViewModel.WeekCountDivOptions.Count == TimeRule.WeekCountDivTotal + 1) return;
        _updating = true;
        
        if (TimeRule.WeekCountDivTotal == 2)
        {
            ViewModel.WeekCountDivOptions = ["不限", "单周", "双周"];
        }
        else
        {
            ViewModel.WeekCountDivOptions = ["不限"];
            for (var i = 1; i <= TimeRule.WeekCountDivTotal; i++)
            {
                ViewModel.WeekCountDivOptions.Add($"第{i.ToChinese()}周");
            }
        }

        var w = ViewModel.WeekCountDivIndex;
        WeekCountDivListBox.ItemsSource = ViewModel.WeekCountDivOptions;
        ViewModel.WeekCountDivIndex = Math.Min(w, ViewModel.WeekCountDivOptions.Count - 1); // 在单双周和多周间切换时，索引会掉为 -1

        _updating = false;
    }
    
    private void UpdateWeekCountDivTotalOptions()
    {
        if (_updating) return;
        if (ViewModel.WeekCountDivTotalOptions.Count == MaxCycle - 1) return;
        _updating = true;
        
        ViewModel.WeekCountDivTotalOptions = ["两周"];
        for (var i = 3; i <= MaxCycle; i++)
        {
            ViewModel.WeekCountDivTotalOptions.Add($"{i.ToChinese()}周");
        }

        var w = ViewModel.WeekCountDivTotalIndex;
        ViewModel.WeekCountDivTotalIndex = -1;
        
        Dispatcher.UIThread.Post(() =>
        {
            WeekCountDivTotalListBox.ItemsSource = ViewModel.WeekCountDivTotalOptions;
            ViewModel.WeekCountDivTotalIndex = Math.Min(w, ViewModel.WeekCountDivTotalOptions.Count - 1);
            
            _updating = false;
        });
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings.MultiWeekRotationMaxCycle))
        {
            UpdateWeekCountDivTotalOptions();
        }
    }

    public TimeRuleEditViewModel ViewModel { get; } = new();
    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    private bool _updating;
    private int MaxCycle => Math.Max(SettingsService.Settings.MultiWeekRotationMaxCycle, TimeRule?.WeekCountDivTotal ?? 0);
    public TimeRule? TimeRule
    {
        get => GetValue(TimeRuleProperty);
        set => SetValue(TimeRuleProperty, value);
    }
    public static readonly StyledProperty<TimeRule?> TimeRuleProperty = 
        AvaloniaProperty.Register<TimeRuleEditControl, TimeRule?>(nameof(TimeRule));
}
