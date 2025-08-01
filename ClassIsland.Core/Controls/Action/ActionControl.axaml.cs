using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
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



    [RelayCommand]
    void AddAction(string id)
    {
        ActionSet.ActionItems.Add(new ActionItem { Id = id });
    }

    [RelayCommand]
    void RemoveAction(ActionItem actionItem)
    {
        ActionSet.ActionItems.Remove(actionItem);
    }



    public IActionService ActionService { get; } = IAppHost.GetService<IActionService>();
    public IReadOnlyDictionary<string, ActionInfo> ActionInfos { get; } = IActionService.ActionInfos;

    public static readonly StyledProperty<ActionSet> ActionSetProperty =
        AvaloniaProperty.Register<ActionControl, ActionSet>(nameof(ActionSet));

    public ActionSet ActionSet
    {
        get => GetValue(ActionSetProperty);
        set => SetValue(ActionSetProperty, value);
    }
}
