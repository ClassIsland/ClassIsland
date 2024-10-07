using ClassIsland.Shared;
using ClassIsland.Core.Models.Action;
using Action = ClassIsland.Core.Models.Action.Action;
namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 行动服务。
/// </summary>
public interface IActionService
{
    /// <summary>
    /// 已注册的行动。
    /// </summary>
    static ObservableDictionary<string, ActionRegistryInfo> Actions { get; } = [];

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
    void Invoke(Actionset actionset);

    /// <summary>
    /// 恢复行动组。
    /// </summary>
    void Revert(Actionset actionset);

    /// <summary>
    /// 行动是否有内建的恢复。
    /// </summary>
    bool ExistRevertHandler(Action action);
}