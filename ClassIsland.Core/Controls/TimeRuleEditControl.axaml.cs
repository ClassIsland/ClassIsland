using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl : UserControl
{
    private TimeRule? _oldTimeRule;
    
    public static readonly StyledProperty<TimeRule?> TimeRuleProperty = AvaloniaProperty.Register<TimeRuleEditControl, TimeRule?>(
        nameof(TimeRule));

    public TimeRule? TimeRule
    {
        get => GetValue(TimeRuleProperty);
        set => SetValue(TimeRuleProperty, value);
    }

    private void RuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeRule.WeekCountDivTotal) && TimeRule != null)
        {
            if (TimeRule.WeekCountDiv > TimeRule.WeekCountDivTotal)
                TimeRule.WeekCountDiv = 0;
            UpdateWeekCountDivs();
        }
    }

    /// <inheritdoc />
    public TimeRuleEditControl()
    {
        this.GetObservable(TimeRuleProperty).Subscribe(newRule =>
        {
            if (newRule != null)
            {
                newRule.PropertyChanged += RuleOnPropertyChanged;
            }

            if (_oldTimeRule != null)
            {
                _oldTimeRule.PropertyChanged -= RuleOnPropertyChanged;
            }

            _oldTimeRule = newRule;
            UpdateWeekCountDivs();
        });
        InitializeComponent();
    }

    private void TimeRuleEditControl_OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    private void UpdateWeekCountDivs()
    {
        if (TimeRule == null)
        {
            return;
        }
        var w = TimeRule.WeekCountDiv;
        TimeRule.WeekCountDivs = [];
        foreach (var i in Enumerable.Range(0, TimeRule.WeekCountDivTotal + 1).ToList())
            TimeRule.WeekCountDivs.Add(((Func<int, int, string>) delegate (int num, int total)
            {
                if (num == 0) return "不限";
                if (total <= 2)
                {
                    if (num == 1) return "单周";
                    if (num == 2) return "双周";
                }
                return $"第{num switch
                {
                    1 => "一",
                    2 => "二",
                    3 => "三",
                    4 => "四",
                    _ => num.ToString()
                }}周";
            })(i, TimeRule.WeekCountDivTotal));
        TimeRule.WeekCountDiv = w;
    }
}
