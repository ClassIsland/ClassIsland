using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ClassIsland.Shared.Models.Action;

/// <summary>
/// 代表一个行动项。
/// </summary>
//TODO: 迁移 Action 至 ActionItem。
public partial class ActionItem : ObservableRecipient
{
    /// <summary>
    /// 将行动项设定为准备运行。
    /// </summary>
    /// <param name="cancellationTokenSource">用于取消行动项运行的 <see cref="CancellationTokenSource"/>，通常与所在的行动组共享。</param>
    public void SetStartRunning(CancellationTokenSource? cancellationTokenSource = null)
    {
        CancellationTokenSource = cancellationTokenSource;
        Exception = null;
        IsWorking = true;
        Progress = null;
    }

    /// <summary>
    /// 将行动项设定为结束运行。
    /// </summary>
    public void SetEndRunning()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
        IsWorking = false;
        Progress = null;
    }



    /// <summary>
    /// 行动项 ID。
    /// </summary>
    [ObservableProperty] string _id = "";

    /// <summary>
    /// 行动项的设置。
    /// </summary>
    [ObservableProperty, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? _settings;

    /// <summary>
    /// 行动项的恢复行动项是否启用。
    /// </summary>
    [ObservableProperty, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool _isRevertEnabled = true;

    /// <summary>
    /// 行动项是否为恢复行动项。恢复行动项只会在"恢复行动"界面中展示。
    /// </summary>
    [ObservableProperty, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool _isRevertActionItem = false;



    /// <summary>
    /// 行动项错误。无则为 null。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] [field: JsonIgnore]
    Exception? _exception;

    /// <summary>
    /// 行动项是否正在运行。
    /// </summary>
    [ObservableProperty, JsonIgnore] bool _isWorking = false;

    /// <summary>
    /// 行动项运行进度。范围 0~100。未报告则为 null。
    /// </summary>
    [ObservableProperty, JsonIgnore] double? _progress = null;

    /// <summary>
    /// 用于取消行动项运行的 <see cref="CancellationTokenSource"/>，通常与所在的行动组共享。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] [field: JsonIgnore]
    CancellationTokenSource? _cancellationTokenSource = null;
}