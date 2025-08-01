using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Action;
using ClassIsland.Shared.Models.Action;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 行动服务。负责管理行动提供方，并提供行动的运行方法。
/// </summary>
//TODO：更新接口
public interface IActionService
{
    /// <summary>
    /// 所有行动信息。
    /// </summary>
    static IReadOnlyDictionary<string, ActionInfo> ActionInfos => PrivateActionInfos;
    static Dictionary<string, ActionInfo> PrivateActionInfos { get; set; } = new();

    /// <summary>
    /// 注册行动信息。
    /// </summary>
    /// <param name="actionType">行动提供方类型。</param>
    /// <param name="settingsControlType">行动设置界面类型。</param>
    /// <returns>行动信息。</returns>
    static ActionInfo RegisterActionInfo(Type actionType, Type? settingsControlType = null)
    {
        if (actionType.GetCustomAttributes(false).FirstOrDefault(x => x is ActionInfo) is not ActionInfo actionInfo)
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: 未标注 ActionInfo 特性。");

        if (ActionInfos.ContainsKey(actionInfo.Id))
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: ID {actionInfo.Id} 已被占用。");

        PrivateActionInfos.Add(actionInfo.Id, actionInfo);
        return actionInfo;
    }

    /// <summary>
    /// 触发行动组。行动错误已被捕获。
    /// </summary>
    /// <param name="actionSet">要触发的行动组。</param>
    /// <param name="isRevertable">行动项是否将会被恢复。默认为 true。</param>
    Task InvokeActionSetAsync(ActionSet actionSet, bool isRevertable = true);

    /// <summary>
    /// 恢复行动组。行动错误已被捕获。
    /// </summary>
    /// <param name="actionSet">要恢复的行动组。</param>
    Task RevertActionSetAsync(ActionSet actionSet);

    /// <summary>
    /// 触发行动项。行动错误已被捕获。
    /// </summary>
    /// <param name="actionItem">要触发的行动项。</param>
    /// <param name="actionSet">行动项所在行动组。可留空。</param>
    /// <param name="isRevertable">行动项是否将会被恢复。默认为 true。</param>
    Task InvokeActionItemAsync(ActionItem actionItem, ActionSet? actionSet = null, bool isRevertable = true);

    /// <summary>
    /// 恢复行动项。行动错误已被捕获。设置为不能恢复的行动项会被忽略。
    /// </summary>
    /// <param name="actionItem">要恢复的行动项。</param>
    /// <param name="actionSet">行动项所在行动组。可留空。</param>
    Task RevertActionItemAsync(ActionItem actionItem, ActionSet? actionSet = null);
}