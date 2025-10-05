using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Shared.Models.Automation;
namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 行动服务。负责管理行动提供方、提供行动的运行方法。
/// </summary>
public interface IActionService
{
    /// <summary>
    /// 「添加行动」层叠菜单。
    /// 键为菜单元素中文名，值为菜单元素。
    /// </summary>
    static readonly ActionMenuTreeNodeCollection ActionMenuTree = [];

    /// <summary>
    /// 列表类型的只读「添加行动」层叠菜单。
    /// </summary>
    static IReadOnlyList<ActionMenuTreeNode> IListActionMenuTree => ActionMenuTree.ToList();

    /// <summary>
    /// 所有行动提供方信息。
    /// 键为行动提供方 ID，值为行动提供方信息。
    /// </summary>
    static readonly Dictionary<string, ActionInfo> ActionInfos = [];

    /// <summary>
    /// 触发行动组。行动错误已被捕获。
    /// </summary>
    /// <param name="isRevertable">行动是否将会被恢复。默认为 true。</param>
    Task InvokeActionSetAsync(ActionSet actionSet, bool isRevertable = true);

    /// <summary>
    /// 恢复行动组。行动错误已被捕获。
    /// </summary>
    Task RevertActionSetAsync(ActionSet actionSet);

    /// <summary>
    /// 中断行动组运行。此方法会等待行动组运行生命周期结束。
    /// </summary>
    Task InterruptActionSetAsync(ActionSet actionSet);



    /// <summary>
    /// 触发行动项。行动错误已被捕获。已中断运行的行动项会在此处拦截。
    /// </summary>
    /// <param name="isRevertable">行动是否将会被恢复。默认为 true。</param>
    internal Task InvokeActionItemAsync(ActionItem actionItem, ActionSet actionSet, bool isRevertable = true);

    /// <summary>
    /// 恢复行动项。行动错误已被捕获。设置为不能恢复的行动项会被忽略。
    /// </summary>
    internal Task RevertActionItemAsync(ActionItem actionItem, ActionSet actionSet);



    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    static readonly Dictionary<string, (Type, Action<object, string>?, Action<object, string>?)> ObsoleteActionHandlers = [];

    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public void RegisterActionHandler(string id, Action<object, string> i2);

    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public void RegisterRevertHandler(string id, Action<object, string> i3);
}
