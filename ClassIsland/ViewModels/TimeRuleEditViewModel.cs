using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TimeRuleEditViewModel : ObservableObject
{
    public TimeRuleEditViewModel()
    {
        Settings.PropertyChanged += SettingsService_PropertyChanged;
    }

    public Settings Settings { get; } = App.GetService<SettingsService>().Settings;
    private void SettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsService.Settings.MultiWeekRotationMaxCycle))
        {
            UpdateWeekCountDivTotals();
        }
    }

    partial void OnTimeRuleChanged(TimeRule? oldValue, TimeRule? newValue)
    {
        if (newValue == null) return;

        if (EditingTimeRule != null)
            EditingTimeRule.PropertyChanged -= OnEditingTimeRuleModified;
        EditingTimeRule = new TimeRule
        {
            WeekDay = newValue.WeekDay,
            WeekCountDiv = newValue.WeekCountDiv,
            WeekCountDivTotal = newValue.WeekCountDivTotal - 2
        };
        EditingTimeRule.PropertyChanged += OnEditingTimeRuleModified;
        UpdateWeekCountDivTotals();
        UpdateWeekCountDivs();
    }

    private void UpdateWeekCountDivs()
    {
        if (EditingTimeRule!.WeekCountDivTotal == WeekCountDivOptions.Count - 3) return;
        var w = EditingTimeRule!.WeekCountDiv;

        WeekCountDivOptions.Clear();
        WeekCountDivOptions.Add("不限");
        for (var i = 1; i <= EditingTimeRule.WeekCountDivTotal + 2; i++)
        {
            var name = EditingTimeRule.WeekCountDivTotal switch
            {
                <= 0 when i == 1 => "单周",
                <= 0 when i == 2 => "双周",
                _ => $"第{i.ToChinese()}周"
            };
            WeekCountDivOptions.Add(name);
        }

        if (w >= WeekCountDivOptions.Count)
        {
            EditingTimeRule.WeekCountDiv = -1;
            DivListBox.UpdateLayout();
            w = 0;
        }
        EditingTimeRule.WeekCountDiv = w;
    }

    private void UpdateWeekCountDivTotals()
    {
        var w = EditingTimeRule!.WeekCountDivTotal;
        var max = Math.Max(Settings.MultiWeekRotationMaxCycle, w + 2);
        if (max == WeekCountDivTotalOptions.Count + 1) return;

        _updating = true;

        WeekCountDivTotalOptions.Clear();
        WeekCountDivTotalOptions.Add("两周");
        for (var i = 3; i <= max; i++)
        {
            WeekCountDivTotalOptions.Add($"{i.ToChinese()}周");
        }

        EditingTimeRule.WeekCountDivTotal = w;
        _updating = false;
    }

    private void OnEditingTimeRuleModified(object? sender, PropertyChangedEventArgs e)
    {
        if (_updating) return;
        if (e.PropertyName == nameof(EditingTimeRule.WeekDay))
        {
            TimeRule!.WeekDay = EditingTimeRule!.WeekDay;
        }
        else if (e.PropertyName == nameof(EditingTimeRule.WeekCountDiv))
        {
            TimeRule!.WeekCountDiv = EditingTimeRule!.WeekCountDiv;
        }
        else if (e.PropertyName == nameof(EditingTimeRule.WeekCountDivTotal))
        {
            TimeRule!.WeekCountDivTotal = EditingTimeRule!.WeekCountDivTotal + 2;
            UpdateWeekCountDivTotals();
            UpdateWeekCountDivs();
        }
    }

    /// <summary>
    /// 实际 TimeRule。
    /// </summary>
    [ObservableProperty]
    private TimeRule? _timeRule;

    /// <summary>
    /// 控件编辑时所用的 EditingTimeRule。WeekCountDivTotal 较实际 TimeRule 小 2。
    /// </summary>
    [ObservableProperty]
    private TimeRule _editingTimeRule;

    [ObservableProperty]
    private ObservableCollection<string> _weekCountDivOptions = [];

    [ObservableProperty]
    private ObservableCollection<string> _weekCountDivTotalOptions = [];

    bool _updating;

    public NonScrollingListBox DivListBox { get; set; }
}