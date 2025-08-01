using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Shared.Models.Action;

/// <summary>
/// 代表一个行动组。
/// </summary>
public partial class ActionSet : ObservableRecipient
{
    /// <summary>
    /// 将行动组设定为准备运行。
    /// </summary>
    /// <param name="isInvoke">准备触发为 true，准备恢复为 false。</param>
    /// <param name="cancellationTokenSource">用于取消行动组运行的 <see cref="CancellationTokenSource"/>。</param>
    public void SetStartRunning(bool isInvoke, CancellationTokenSource? cancellationTokenSource = null)
    {
        CancellationTokenSource = cancellationTokenSource ?? new();
        Status = isInvoke ? ActionSetStatus.Invoking : ActionSetStatus.Reverting;
    }
    
    /// <summary>
    /// 将行动组设定为结束运行。
    /// </summary>
    /// <param name="isInvoke">结束触发为 true，结束恢复为 false。</param>
    public void SetEndRunning(bool isInvoke)
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
        Status = isInvoke && IsRevertEnabled ? ActionSetStatus.On : ActionSetStatus.Normal;
    }
    
    
    
    /// <summary>
    /// 行动组的名称或备注。
    /// </summary>
    [ObservableProperty] string _name = "新行动组";
    
    /// <summary>
    /// 行动组中的所有行动项。
    /// </summary>
    //TODO: 将 ActionSettings 迁移到 ActionItems
    [ObservableProperty] ObservableCollection<ActionItem> _actionItems = [];
    
    /// <summary>
    /// 行动组是否启用。
    /// </summary>
    [ObservableProperty] bool _isEnabled = true;
    
    /// <summary>
    /// 行动组是否启用恢复。
    /// </summary>
    public bool IsRevertEnabled
    {
        get => _isRevertEnabled;
        set
        {
            if (SetProperty(ref _isRevertEnabled, value))
                if (Status == ActionSetStatus.On) 
                    Status = ActionSetStatus.Normal;
        }
    }
    bool _isRevertEnabled = true;

    /// <summary>
    /// 行动组状态。
    /// </summary>
    [ObservableProperty] ActionSetStatus _status = ActionSetStatus.Normal;

    /// <summary>
    /// 行动组 Guid。可用于标识设置叠层。
    /// </summary>
    [ObservableProperty] Guid _guid = Guid.NewGuid();
    
    
    

    /// <summary>
    /// 用于取消行动组运行的 <see cref="CancellationTokenSource"/>。其通常与所有从属行动项共享。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] [field: JsonIgnore]
    CancellationTokenSource? _cancellationTokenSource = null;
}