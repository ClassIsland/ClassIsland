using System.Collections.ObjectModel;
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
    /// <summary>
    /// 删除行动命令。
    /// </summary>
    public static readonly ICommand RemoveActionCommand = new RoutedUICommand();

    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(nameof(Actions), typeof(ObservableCollection<A.Action>), typeof(ActionControl), new PropertyMetadata(default(ObservableCollection<A.Action>)));
    /// <summary>
    /// 该规则集绑定的行动集合。
    /// </summary>
    public ObservableCollection<A.Action> Actions
    {
        get { return (ObservableCollection<A.Action>)GetValue(ActionsProperty); }
        set { SetValue(ActionsProperty, value); }
    }

    public ActionControl()
    {
        InitializeComponent();
    }

    private void ButtonAddAction_OnClick(object sender, RoutedEventArgs e)
    {
        Actions.Add(new());
    }

    private void CommandRemoveActionCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not A.Action action) return;
        Actions.Remove(action);
    }
}