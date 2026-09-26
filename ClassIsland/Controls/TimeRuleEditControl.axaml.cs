using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Threading;
using ClassIsland.Core.Extensions;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;

namespace ClassIsland.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl : UserControl
{
    private readonly IDisposable _timeRulePropertyObserver;
    private bool _resourcesReleased;

    public static FuncValueConverter<DateOnly, DateTime> DateOnlyToDateTimeConverter { get; }
        = new(x => new DateTime(x, new TimeOnly(0, 0, 0)),
            DateOnly.FromDateTime);

    public static FuncValueConverter<int, int> CycleDaysToMaxOffsetDaysConverter { get; }
        = new(x => x - 1);
    
    /// <inheritdoc />
    public TimeRuleEditControl()
    {
        _timeRulePropertyObserver = this.GetObservable(TimeRuleProperty)
            .Skip(1)
            .Subscribe(_ => TimeRulePropertyOnNext());
        InitializeComponent();
    }

    private void TimeRulePropertyOnNext()
    {
        var newValue = TimeRule;
        ReleaseExternalSubscriptions();
        if (_resourcesReleased)
        {
            return;
        }

        if (newValue != null)
        {
            UpdateViewModel(newValue);

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
            _timeRuleObserver = newValue.WhenAnyPropertyChanged()
                .Subscribe(_ => UpdateViewModel(newValue));
        }
    }

    private void ReleaseExternalSubscriptions()
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _timeRuleObserver?.Dispose();
        _timeRuleObserver = null;
    }

    public void ReleaseResources()
    {
        if (_resourcesReleased)
        {
            return;
        }

        _resourcesReleased = true;
        _timeRulePropertyObserver.Dispose();
        ReleaseExternalSubscriptions();
        TimeRule = null;
    }

    private void UpdateViewModel(TimeRule timeRule)
    {
        ViewModel.WeekCountDivIndex = timeRule.WeekCountDiv;
        ViewModel.WeekCountDivTotalIndex = timeRule.WeekCountDivTotal - 2;
        
        UpdateWeekCountDivTotalOptions();
        UpdateWeekCountDivOptions();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_updatingDiv || TimeRule == null) return;
        if (e.PropertyName == nameof(ViewModel.WeekCountDivIndex))
        {
            TimeRule.WeekCountDiv = ViewModel.WeekCountDivIndex;
        }
        else if (e.PropertyName == nameof(ViewModel.WeekCountDivTotalIndex))
        {
            TimeRule.WeekCountDivTotal = ViewModel.WeekCountDivTotalIndex + 2;
            if (TimeRule.WeekCountDivTotal < TimeRule.WeekCountDiv)
            {
                _updatingDiv = true;
                ViewModel.WeekCountDivIndex = -1;
                Dispatcher.UIThread.Post(() =>
                    {
                        if (_resourcesReleased)
                        {
                            return;
                        }

                        if (TimeRule.WeekCountDivTotal < TimeRule.WeekCountDiv)
                        {
                            ViewModel.WeekCountDivIndex = 0;
                        }
                    },
                    DispatcherPriority.Background);
                _updatingDiv = false;
            }
            UpdateWeekCountDivTotalOptions();
            UpdateWeekCountDivOptions();
        }
    }
    
    private void UpdateWeekCountDivOptions()
    {
        if (_updatingDiv || TimeRule == null) return;      
        if (ViewModel.WeekCountDivOptions.Count == TimeRule.WeekCountDivTotal + 1) return;
        _updatingDiv = true;
        
        if (TimeRule.WeekCountDivTotal == 2)
        {
            ViewModel.WeekCountDivOptions = ["每周启用", "单周", "双周"];
        }
        else
        {
            ViewModel.WeekCountDivOptions = ["每周启用"];
            for (var i = 1; i <= TimeRule.WeekCountDivTotal; i++)
            {
                ViewModel.WeekCountDivOptions.Add($"第{i.ToChinese()}周");
            }
        }

        var w = ViewModel.WeekCountDivIndex;
        WeekCountDivListBox.ItemsSource = ViewModel.WeekCountDivOptions;
        ViewModel.WeekCountDivIndex = Math.Min(w, ViewModel.WeekCountDivOptions.Count - 1); // 在单双周和多周间切换时，索引会掉为 -1

        _updatingDiv = false;
    }
    
    private void UpdateWeekCountDivTotalOptions()
    {
        if (_updatingDivTotal) return;
        if (ViewModel.WeekCountDivTotalOptions.Count == MaxCycle - 1) return;
        _updatingDivTotal = true;
        
        ViewModel.WeekCountDivTotalOptions = ["两周"];
        for (var i = 3; i <= MaxCycle; i++)
        {
            ViewModel.WeekCountDivTotalOptions.Add($"{i.ToChinese()}周");
        }

        var w = ViewModel.WeekCountDivTotalIndex;
        ViewModel.WeekCountDivTotalIndex = -1;
        
        Dispatcher.UIThread.Post(() =>
        {
            if (_resourcesReleased)
            {
                return;
            }

            WeekCountDivTotalListBox.ItemsSource = ViewModel.WeekCountDivTotalOptions;
            ViewModel.WeekCountDivTotalIndex = Math.Min(w, ViewModel.WeekCountDivTotalOptions.Count - 1);
            
            _updatingDivTotal = false;
        });
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings.MultiWeekRotationMaxCycle))
        {
            UpdateWeekCountDivTotalOptions();
        }
    }

    [RelayCommand]
    private void PreEditSelectedDate(DateOnly date)
    {
        ViewModel.NewDateTime = new DateTime(date, new TimeOnly(0, 0, 0));
    }

    [RelayCommand]
    private void CommitEditSelectedDate(Control source)
    {
        if (TimeRule == null)
        {
            return;
        }

        var date = source.Tag is DateOnly value ? value : (DateOnly?)null;
        var newDateOnly = DateOnly.FromDateTime(ViewModel.NewDateTime);
        if (TimeRule.EnableDates.Contains(newDateOnly))
        {
            this.ShowWarningToast("日期已存在。");
            return;
        }
        if (date is {} d)
        {
            TimeRule.EnableDates.Replace(d, newDateOnly);
        }
        else
        {
            TimeRule.EnableDates.Add(newDateOnly);
        }

        FlyoutHelper.CloseAncestorFlyout(source);
    }

    [RelayCommand]
    private void RemoveDate(DateOnly date)
    {
        TimeRule?.EnableDates.Remove(date);
    }

    public TimeRuleEditViewModel ViewModel { get; } = new();
    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    
    private bool _updatingDiv;
    private bool _updatingDivTotal;
    private IDisposable? _timeRuleObserver;
    
    private int MaxCycle => Math.Max(SettingsService.Settings.MultiWeekRotationMaxCycle, TimeRule?.WeekCountDivTotal ?? 0);
    public TimeRule? TimeRule
    {
        get => GetValue(TimeRuleProperty);
        set => SetValue(TimeRuleProperty, value);
    }
    public static readonly StyledProperty<TimeRule?> TimeRuleProperty = 
        AvaloniaProperty.Register<TimeRuleEditControl, TimeRule?>(nameof(TimeRule));
}
