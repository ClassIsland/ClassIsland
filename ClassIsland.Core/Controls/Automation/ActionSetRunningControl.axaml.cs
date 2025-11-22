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

    ActionSet _actionSet = new();

    public static readonly DirectProperty<ActionSetRunningControl, ActionSet> ActionSetProperty =
        AvaloniaProperty.RegisterDirect<ActionSetRunningControl, ActionSet>(
            nameof(ActionSet), o => o.ActionSet, (o, v) =>
            {
                if (v != null) o.ActionSet = v;
            });

    public ActionSet ActionSet
    {
        get => _actionSet;
        set => SetAndRaise(ActionSetProperty, ref _actionSet, value);
    }

    public static readonly StyledProperty<bool> IsRevertButtonVisibleProperty =
        AvaloniaProperty.Register<ActionSetRunningControl, bool>(nameof(IsRevertButtonVisible), true);

    public bool IsRevertButtonVisible
    {
        get => GetValue(IsRevertButtonVisibleProperty);
        set => SetValue(IsRevertButtonVisibleProperty, value);
    }
}