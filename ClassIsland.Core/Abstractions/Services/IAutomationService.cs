using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models;
namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 自动化服务。负责管理自动化工作流、时间点行动、触发器。
/// </summary>
//TODO: 更新接口。
public interface IAutomationService
{
    /// <summary>
    /// 当前配置文件的所有自动化
    /// </summary>
    ObservableCollection<Workflow> Workflows { get; set; }

    /// <summary>
    /// 自动化配置文件列表
    /// </summary>
    IReadOnlyList<string> Configs { get; set; }

    /// <summary>
    /// 保存自动化配置
    /// </summary>
    void SaveConfig(string note = "");

    /// <summary>
    /// 重新加载自动化配置文件列表。
    /// </summary>
    void RefreshConfigs();

    public static List<TriggerInfo> RegisteredTriggers { get; } = [];
}