using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared;

using System.Windows;
using System.Windows.Input;
using A = ClassIsland.Core.Models.Action;
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

    public static readonly DependencyProperty ActionsetProperty = DependencyProperty.Register(nameof(Actionset), typeof(Actionset), typeof(ActionControl), new PropertyMetadata(default(Actionset)));
    public Actionset Actionset
    {
        get => (Actionset)GetValue(ActionsetProperty);
        set => SetValue(ActionsetProperty, value);
    }

    private void ButtonAddAction_OnClick(object sender, RoutedEventArgs e)
    {
        Actionset.Actions.Add(new());
    }

    private void CommandRemoveAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not A.Action action) return;

        Actionset.Actions.Remove(action);
    }
}