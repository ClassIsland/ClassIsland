using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.JsonConverters;
using CommunityToolkit.Mvvm.ComponentModel;
#pragma warning disable CS0657
namespace ClassIsland.Shared.Models.Automation;

/// <summary>
/// 代表一个行动组。
/// </summary>
/// <seealso cref="ActionItem"/>
public partial class ActionSet : ObservableRecipient
{
    /// <summary>
    /// 将行动组设定为开始运行。
    /// </summary>
    /// <param name="isInvoke">开始触发为 true，开始恢复为 false。</param>
    public void SetStartRunning(bool isInvoke)
    {
        InterruptCts?.Dispose();
        RunningTcs?.Task?.Dispose();
        InterruptCts = new();
        RunningTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Status = isInvoke ? ActionSetStatus.Invoking : ActionSetStatus.Reverting;
    }

    /// <summary>
    /// 将行动组设定为结束运行。
    /// </summary>
    /// <param name="isInvoke">结束触发为 true，结束恢复为 false。</param>
    public void SetEndRunning(bool isInvoke)
    {
        var isCancellationRequested = InterruptCts?.Token.IsCancellationRequested != false;
        InterruptCts?.Dispose();
        InterruptCts = null;
        Status = isCancellationRequested switch
        {
            false => isInvoke && IsRevertEnabled ? ActionSetStatus.IsOn : ActionSetStatus.Normal,
            true when Status is ActionSetStatus.Invoking => ActionSetStatus.Normal,
            true when Status is ActionSetStatus.Reverting => ActionSetStatus.IsOn,
            _ => Status
        };
        RunningTcs?.SetResult(null);
        RunningTcs?.Task?.Dispose();
        RunningTcs = null;
    }



    /// <summary>
    /// 行动组的名称或备注。
    /// </summary>
    [ObservableProperty] string _name = "新行动组";

    /// <summary>
    /// 行动组中的所有行动项。
    /// </summary>
    [property: JsonPropertyName("Actions")] // v2 名称。
    [ObservableProperty] ObservableCollection<ActionItem> _actionItems = [];

    /// <summary>
    /// 行动组是否启用。
    /// </summary>
    [ObservableProperty] bool _isEnabled = true;

    /// <summary>
    /// 行动组是否启用恢复。
    /// </summary>
    [ObservableProperty] bool _isRevertEnabled = false;
    partial void OnIsRevertEnabledChanged(bool value)
    {
        if (!value && Status == ActionSetStatus.IsOn)
            Status = ActionSetStatus.Normal;
    }

    /// <summary>
    /// 行动组状态。
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsWorking))]
    [property: JsonConverter(typeof(ActionSetStatusJsonConverter))]
    ActionSetStatus _status = ActionSetStatus.Normal;

    /// <summary>
    /// 获取行动组是否正在运行。
    /// </summary>
    public bool IsWorking => Status is ActionSetStatus.Invoking or ActionSetStatus.Reverting;

    /// <summary>
    /// 获取行动组 Guid。可用于标识设置叠层。
    /// </summary>
    public Guid Guid { get; set; } = Guid.NewGuid();



    /// <summary>
    /// 用于中断行动组运行的 <see cref="CancellationTokenSource"/>。
    /// </summary>
    internal CancellationTokenSource? InterruptCts = null;

    /// <summary>
    /// 用于通知行动组运行生命周期结束的 <see cref="TaskCompletionSource"/>。
    /// </summary>
    internal TaskCompletionSource<object?>? RunningTcs = null;
}