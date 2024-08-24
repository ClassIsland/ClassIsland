using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl : UserControl
{
    public static readonly DependencyProperty TimeRuleProperty = DependencyProperty.Register(
        nameof(TimeRule), typeof(TimeRule), typeof(TimeRuleEditControl), new PropertyMetadata(default(TimeRule),
            (o, args) =>
            {

                if (o is not TimeRuleEditControl control) 
                    return;
                if (args.NewValue is TimeRule newRule)
                {
                    newRule.PropertyChanged += control.RuleOnPropertyChanged;
                }

                if (args.OldValue is TimeRule oldRule)
                {
                    oldRule.PropertyChanged -= control.RuleOnPropertyChanged;
                }
                control.UpdateWeekCountDivs();
            }));

    private void RuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeRule.WeekCountDivTotal) && TimeRule != null)
        {
            if (TimeRule.WeekCountDiv > TimeRule.WeekCountDivTotal)
                TimeRule.WeekCountDiv = 0;
            UpdateWeekCountDivs();
        }
    }

    public TimeRule? TimeRule
    {
        get { return (TimeRule)GetValue(TimeRuleProperty); }
        set { SetValue(TimeRuleProperty, value); }
    }

    /// <inheritdoc />
    public TimeRuleEditControl()
    {
        InitializeComponent();
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
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