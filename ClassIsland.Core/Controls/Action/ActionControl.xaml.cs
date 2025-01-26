using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared;

using System.Windows;
using System.Windows.Input;
using ClassIsland.Shared.Models.Action;
namespace ClassIsland.Core.Controls.Action;

/// <summary>
/// ActionControl.xaml 的交互逻辑
/// </summary>
public partial class ActionControl
{
    public ActionControl()
    {
        InitializeComponent();
    }

    public static readonly ICommand RemoveActionCommand = new RoutedUICommand();

    public static readonly DependencyProperty ActionsetProperty = DependencyProperty.Register(nameof(ActionSet), typeof(ActionSet), typeof(ActionControl), new PropertyMetadata(default(ActionSet)));
    public ActionSet ActionSet
    {
        get => (ActionSet)GetValue(ActionsetProperty);
        set => SetValue(ActionsetProperty, value);
    }

    private void ButtonAddAction_OnClick(object sender, RoutedEventArgs e)
    {
        ActionSet.Actions.Add(new());
    }

    private void CommandRemoveAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Shared.Models.Action.Action action) return;

        ActionSet.Actions.Remove(action);
    }
}