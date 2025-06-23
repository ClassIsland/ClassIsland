using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using System.Windows;
namespace ClassIsland.Controls;

/// <summary>
/// 时间规则编辑控件。
/// </summary>
public partial class TimeRuleEditControl
{
    public TimeRuleEditControl()
    {
        InitializeComponent();
        ViewModel.DivListBox = DivListBox;
    }

    public TimeRuleEditViewModel ViewModel { get; } = new();

    public static readonly DependencyProperty TimeRuleProperty =
        DependencyProperty.Register(nameof(TimeRule), typeof(TimeRule), typeof(TimeRuleEditControl),
            new PropertyMetadata(null, OnTimeRuleChanged));

    public TimeRule TimeRule
    {
        get => (TimeRule)GetValue(TimeRuleProperty);
        set => SetValue(TimeRuleProperty, value);
    }

    private static void OnTimeRuleChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
    {
        if (o is TimeRuleEditControl control)
        {
            control.ViewModel.TimeRule = (TimeRule)args.NewValue;
        }
    }
}