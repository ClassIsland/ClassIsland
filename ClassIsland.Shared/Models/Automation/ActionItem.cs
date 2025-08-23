using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
#pragma warning disable CS0657
namespace ClassIsland.Shared.Models.Automation;

/// <summary>
/// 代表一个行动项。
/// </summary>
/// <seealso cref="ActionSet"/>
public partial class ActionItem : ObservableRecipient
{
    /// <summary>
    /// 将行动项设定为开始运行。
    /// 此方法由 ActionBase 自动调用。
    /// </summary>
    public void SetStartRunning()
    {
        Exception = null;
        IsWorking = true;
        Progress = null;
    }

    /// <summary>
    /// 将行动项设定为结束运行。
    /// 此方法由 ActionBase 自动调用。
    /// </summary>
    public void SetEndRunning()
    {
        IsWorking = false;
        Progress = null;
    }



    /// <summary>
    /// 行动项 ID。
    /// </summary>
    [ObservableProperty] string _id = "";

    /// <summary>
    /// 行动项设置。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    object? _settings;

    /// <summary>
    /// 行动项是否启用恢复。在为恢复行动项时此项无效。在行动提供方不支持恢复时此项无效。（未启用。）
    /// </summary>
    // [ObservableProperty] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool _isRevertEnabled = true;

    /// （未启用。始终为 true。）
    [JsonIgnore]
    public bool IsRevertEnabled
    {
        get => true;
        set { }
    }

    /// <summary>
    /// 行动项是否为恢复行动项。恢复行动项只会在"恢复行动"界面中展示。（未启用。）
    /// </summary>
    // [ObservableProperty] [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool _isRevertActionItem = false;

    /// （未启用。始终为 false。）
    [JsonIgnore]
    public bool IsRevertActionItem
    {
        get => false;
        set { }
    }


    /// <summary>
    /// 行动项错误。无则为 null。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] string? _exception;

    /// <summary>
    /// 行动项是否正在运行。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] bool _isWorking = false;

    /// <summary>
    /// 行动项运行进度。范围 0~100。未报告则为 null。
    /// </summary>
    [ObservableProperty] [property: JsonIgnore] double? _progress = null;

    /// （数据驱动用属性。）
    [JsonIgnore] internal bool IsNewAdded { get; set; } = false;
}