using ClassIsland.Shared;
using ClassIsland.Core.Models.Action;
namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 行动服务。
/// </summary>
public interface IActionService
{
    /// <summary>
    /// 已经注册的行动列表。
    /// </summary>
    static ObservableDictionary<string, ActionRegistryInfo> Actions { get; } = [];

    /// <summary>
    /// 注册指定行动的处理方法。
    /// </summary>
    void RegisterActionHandler(string id, ActionRegistryInfo.HandleDelegate handler);

    /// <summary>
    /// 注册指定行动的恢复方法。
    /// </summary>
    void RegisterActionBackHandler(string id, ActionRegistryInfo.HandleDelegate handler);

    /// <summary>
    /// 触发行动列表。
    /// </summary>
    void InvokeActionList(ActionList actionList);

    /// <summary>
    /// 触发恢复行动列表。
    /// </summary>
    void InvokeBackActionList(ActionList actionList);

    internal void DebugInvokeActionListSync(ActionList actionList);
}