using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.Controls;

/// <summary>
/// NavigationButtonsControl.xaml 的交互逻辑
/// </summary>
public partial class NavigationButtonsControl : UserControl
{
    public static readonly DependencyProperty NavigationNextCommandParameterProperty = DependencyProperty.Register(
        nameof(NavigationNextCommandParameter), typeof(object), typeof(NavigationButtonsControl), new PropertyMetadata(default(object)));

    public object NavigationNextCommandParameter
    {
        get { return (object)GetValue(NavigationNextCommandParameterProperty); }
        set { SetValue(NavigationNextCommandParameterProperty, value); }
    }

    public static readonly DependencyProperty IsNavigateBackButtonEnabledProperty = DependencyProperty.Register(
        nameof(IsNavigateBackButtonEnabled), typeof(bool), typeof(NavigationButtonsControl), new PropertyMetadata(true));

    public bool IsNavigateBackButtonEnabled
    {
        get { return (bool)GetValue(IsNavigateBackButtonEnabledProperty); }
        set { SetValue(IsNavigateBackButtonEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsNavigateNextButtonEnabledProperty = DependencyProperty.Register(
        nameof(IsNavigateNextButtonEnabled), typeof(bool), typeof(NavigationButtonsControl), new PropertyMetadata(true));

    public bool IsNavigateNextButtonEnabled
    {
        get { return (bool)GetValue(IsNavigateNextButtonEnabledProperty); }
        set { SetValue(IsNavigateNextButtonEnabledProperty, value); }
    }

    public NavigationButtonsControl()
    {
        InitializeComponent();
    }
}