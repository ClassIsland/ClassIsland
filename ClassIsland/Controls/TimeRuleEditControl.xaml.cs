using ClassIsland.Core.Extensions;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
namespace ClassIsland.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl
{
    public TimeRuleEditControl() => InitializeComponent();

    public static readonly DependencyProperty TimeRuleProperty =
        DependencyProperty.Register(nameof(TimeRule), typeof(TimeRule), typeof(TimeRuleEditControl),
            new PropertyMetadata(null, (o, args) =>
            {
                if (o is not TimeRuleEditControl control) return;
                if (args.OldValue is TimeRule oldRule) 
                    oldRule.PropertyChanged -= control.RuleOnPropertyChanged;
                if (args.NewValue is TimeRule newRule)
                {
                    newRule.PropertyChanged += control.RuleOnPropertyChanged;
                    control.UpdateWeekCountDivs();
                    control.UpdateWeekCountDivTotals();
                }
            }));

    public TimeRule TimeRule
    {
        get => (TimeRule)GetValue(TimeRuleProperty);
        set => SetValue(TimeRuleProperty, value);
    }

    private void RuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimeRule.WeekCountDivTotal))
            UpdateWeekCountDivs();
    }

    private void UpdateWeekCountDivs()
    {
        DivListBox.ClearValue(Selector.SelectedIndexProperty);

        var divList = new ObservableCollection<string>{"不限"};
        for (var i = 1; i <= TimeRule.WeekCountDivTotal; i++)
        {
            divList.Add(TimeRule.WeekCountDivTotal switch
            {
                <= 2 when i == 1 => "单周",
                <= 2 when i == 2 => "双周",
                _ => $"第{i.ToChinese()}周"
            });
        }
        DivListBox.ItemsSource = divList;

        DivListBox.SetBinding(Selector.SelectedIndexProperty, new Binding(nameof(TimeRule.WeekCountDiv)));
    }

    private void UpdateWeekCountDivTotals()
    {
        DivTotalListBox.ClearValue(Selector.SelectedIndexProperty);

        var divTotalList = new ObservableCollection<ListBoxItem> {
            new() { Visibility = Visibility.Collapsed },
            new() { Visibility = Visibility.Collapsed } };

        var maxCycle = App.GetService<SettingsService>().Settings.MultiWeekRotationMaxCycle;
        for (var i = 2; i <= maxCycle; i++)
        {
            divTotalList.Add(new ListBoxItem() { Content = $"{i.ToChinese()}周" });
        }
        DivTotalListBox.ItemsSource = divTotalList;

        DivTotalListBox.SetBinding(Selector.SelectedIndexProperty, new Binding(nameof(TimeRule.WeekCountDivTotal)));
    }
}