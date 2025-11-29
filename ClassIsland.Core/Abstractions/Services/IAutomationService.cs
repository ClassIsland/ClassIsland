using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using Workflow = ClassIsland.Core.Models.Automation.Workflow;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 自动化服务。负责管理自动化配置及触发器、自动执行自动化工作流和时间点行动。
/// </summary>
public interface IAutomationService
{
    /// <summary>
    /// 所有触发器提供方信息。
    /// </summary>
    static List<TriggerInfo> RegisteredTriggers { get; } = [];

    /// <summary>
    /// 当前配置文件的所有自动化工作流。
    /// </summary>
    ObservableCollection<Workflow> Workflows { get; set; }

    /// <summary>
    /// 自动化配置文件列表。
    /// </summary>
    IReadOnlyList<string> Configs { get; set; }

    /// <summary>
    /// 保存自动化配置。
    /// </summary>
    /// <param name="note">保存备注。</param>
    void SaveConfig(string note = "");

    /// <summary>
    /// 重新加载自动化配置文件列表。
    /// </summary>
    void RefreshConfigs();
}