using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Automation;
using CommunityToolkit.Mvvm.Input;
namespace ClassIsland.Core.Controls.Automation;

public partial class ActionSetRunningControl : UserControl
{
    private IActionService ActionService { get; } = IAppHost.GetService<IActionService>();

    public ActionSetRunningControl() => InitializeComponent();

    [RelayCommand]
    void InvokeAction()
    {
        ActionService.InvokeActionSetAsync(ActionSet);
    }

    [RelayCommand]
    void RevertAction()
    {
        ActionService.RevertActionSetAsync(ActionSet);
    }

    [RelayCommand]
    void InterruptAction()
    {
        ActionService.InterruptActionSetAsync(ActionSet);
    }

    public static readonly StyledProperty<ActionSet> ActionSetProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionSet>(nameof(ActionSet));

    public ActionSet ActionSet
    {
        get => GetValue(ActionSetProperty);
        set => SetValue(ActionSetProperty, value);
    }

    public static readonly StyledProperty<bool> IsRevertEnabledProperty =
        AvaloniaProperty.Register<ActionSetRunningControl, bool>(nameof(IsRevertEnabled), true);

    public bool IsRevertEnabled
    {
        get => GetValue(IsRevertEnabledProperty);
        set => SetValue(IsRevertEnabledProperty, value);
    }
}