using System.Threading.Tasks;
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
    void InvokeAction() => _ = InvokeActionWithPreviewMarkerAsync();

    /// <summary>
    /// 手动触发行动组时给每个行动项打上预览标记，让提醒类行动把内容转发给悬浮主界面
    /// 宿主进程显示，而不是像自动触发那样在主 App 内显示。
    /// </summary>
    async Task InvokeActionWithPreviewMarkerAsync()
    {
        var actionSet = ActionSet;
        foreach (var item in actionSet.ActionItems)
        {
            item.IsPreview = true;
        }

        try
        {
            await ActionService.InvokeActionSetAsync(actionSet);
        }
        finally
        {
            foreach (var item in actionSet.ActionItems)
            {
                item.IsPreview = false;
            }
        }
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