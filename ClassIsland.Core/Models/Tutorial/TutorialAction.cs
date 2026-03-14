using System.Text.Json.Serialization;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Shared.Models.Automation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Tutorial;

/// <summary>
/// 代表教程进行的动作
/// </summary>
public partial class TutorialAction : ObservableObject
{
    /// <summary>
    /// 执行的动作类型
    /// </summary>
    [ObservableProperty] private TutorialActionKind _kind = TutorialActionKind.None;

    /// <summary>
    /// 执行的动作的字符串类型的参数
    /// </summary>
    [ObservableProperty] private string _stringParameter = "";

    /// <summary>
    /// 当执行的动作为运行行动时，要触发的行动
    /// </summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [ObservableProperty] private ActionSet? _actionSet;
}