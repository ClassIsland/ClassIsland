using ClassIsland.Shared;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Action;
using Action = ClassIsland.Shared.Models.Action.Action;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 行动服务。
/// </summary>
public interface IActionService
{
    /// <summary>
    /// 已注册的行动。
    /// </summary>
    static Dictionary<string, ActionRegistryInfo> Actions { get; } = [];

    /// <summary>
    /// 注册指定行动的处理方法。
    /// </summary>
    void RegisterActionHandler(string id, ActionRegistryInfo.HandleDelegate handler);

    /// <summary>
    /// 注册指定行动的恢复方法。
    /// </summary>
    void RegisterRevertHandler(string id, ActionRegistryInfo.HandleDelegate handler);

    /// <summary>
    /// 触发行动组。
    /// </summary>
    void Invoke(ActionSet actionSet);

    /// <summary>
    /// 恢复行动组。
    /// </summary>
    void Revert(ActionSet actionSet);

    /// <summary>
    /// 行动是否有内建的恢复。
    /// </summary>
    bool ExistRevertHandler(Action action);
}