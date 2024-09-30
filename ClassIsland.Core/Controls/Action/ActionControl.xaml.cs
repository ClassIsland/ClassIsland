using ClassIsland.Core.Models.Action;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using A = ClassIsland.Core.Models.Action;
namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// ActionControl.xaml 的交互逻辑
/// </summary>
public partial class ActionControl : UserControl
{
    public ActionControl()
    {
        InitializeComponent();
    }

    public static readonly ICommand RemoveActionCommand = new RoutedUICommand();

    public static readonly DependencyProperty ActionListProperty = DependencyProperty.Register(nameof(ActionList), typeof(ActionList), typeof(ActionControl), new PropertyMetadata(default(ActionList)));
    public ActionList ActionList
    {
        get { return (ActionList)GetValue(ActionListProperty); }
        set { SetValue(ActionListProperty, value); }
    }

    private void ButtonAddAction_OnClick(object sender, RoutedEventArgs e)
    {
        ActionList.Actions.Add(new());
    }

    private void CommandRemoveActionCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not A.Action action) return;
        ActionList.Actions.Remove(action);
    }
}