using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared;

using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Shared.Models.Action;
using CommunityToolkit.Mvvm.Input;

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

    public static readonly StyledProperty<ActionSet> ActionSetProperty = AvaloniaProperty.Register<ActionControl, ActionSet>(
        nameof(ActionSet));

    public ActionSet ActionSet
    {
        get => GetValue(ActionSetProperty);
        set => SetValue(ActionSetProperty, value);
    }

    private void ButtonAddAction_OnClick(object sender, RoutedEventArgs e)
    {
        ActionSet.Actions.Add(new());
    }

    [RelayCommand]
    private void RemoveAction(ClassIsland.Shared.Models.Action.Action action)
    {
        ActionSet.Actions.Remove(action);
    }
}
