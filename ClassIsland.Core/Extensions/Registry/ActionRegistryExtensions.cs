using System.Reflection;
using System.Text.Json;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Automation;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册行动提供方的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class ActionRegistryExtensions
{
    /// <summary>
    /// 注册一个行动提供方。
    /// </summary>
    /// <typeparam name="TAction">行动提供方，继承自<see cref="ActionBase"/>。</typeparam>
    public static IServiceCollection AddAction<TAction>(this IServiceCollection services)
        where TAction : ActionBase
    {
        var info = RegisterActionInfo(typeof(TAction));
        services.AddKeyedTransient<ActionBase, TAction>(info.Id);
        return services;
    }

    /// <summary>
    /// 注册一个行动提供方。
    /// </summary>
    /// <typeparam name="TAction">行动提供方，继承自<see cref="ActionBase"/>。</typeparam>
    /// <typeparam name="TSettingsControl">行动设置界面，继承自<see cref="ActionSettingsControlBase"/>。</typeparam>
    public static IServiceCollection AddAction<TAction, TSettingsControl>(this IServiceCollection services)
        where TAction : ActionBase
        where TSettingsControl : ActionSettingsControlBase
    {
        var info = RegisterActionInfo(typeof(TAction));
        services.AddKeyedTransient<ActionBase, TAction>(info.Id);
        services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(info.Id);
        return services;
    }

    static ActionInfo RegisterActionInfo(Type actionType)
    {
        if (actionType.GetCustomAttributes(false).FirstOrDefault(x => x is ActionInfo) is not ActionInfo info)
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: 未标注 ActionInfo 特性。");

        info.IsRevertable = HasOverriddenOnRevert(actionType);

        if (!IActionService.ActionInfos.TryAdd(info.Id, info))
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: ID {info.Id} 已被占用。");

        ProcessAddToGroup(info);

        return info;
    }

    static bool HasOverriddenOnRevert(Type type)
    {
        if (!typeof(ActionBase).IsAssignableFrom(type))
            throw new ArgumentException("行动提供方须继承自 ActionBase。", nameof(type));

        return type.GetMethod(
            "OnRevert",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null
        )?.DeclaringType == type;
    }

    static void ProcessAddToGroup(ActionInfo info)
    {
        if (info.AddDefaultToMenu)
        {
            var group = IActionService.ActionMenuTree;

            if (!string.IsNullOrEmpty(info.DefaultGroupToMenu))
            {
                if (IActionService.ActionMenuTree.TryGetValue(info.DefaultGroupToMenu, out var node) &&
                    node is ActionMenuTreeGroup g)
                    group = g.Children;
                else
                {
                    g = new ActionMenuTreeGroup(info.DefaultGroupToMenu);
                    group.Add(g);
                    group = g.Children;
                }
            }
            group.Add(new ActionMenuTreeItem(info.Id, info.Name, info.IconGlyph));
        }
    }





    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public static IServiceCollection AddAction
        (this IServiceCollection services,
         string id,
         string name = "",
         string iconGlyph = "\ue01f",
         Action<object, string>? onHandle = null)
    {
        var info = new ActionInfo(id, name, iconGlyph);
        info.IsRevertable = true;
        if (IActionService.ActionInfos.TryAdd(id, info))
        {
            IActionService.ObsoleteActionHandlers[id] = (typeof(void), onHandle, null);
            ProcessAddToGroup(info);
            services.AddKeyedTransient<ActionBase, ObsoleteV2ActionAdapter>(id);
        }

        return services;
    }

    [Obsolete("注意！行动 v2 注册方法已过时，请参阅 ClassIsland 开发文档进行迁移。")]
    public static IServiceCollection AddAction<TSettings, TSettingsControl>
        (this IServiceCollection services,
         string id,
         string name = "",
         string iconGlyph = "\ue01f",
         string defaultGroupToMenu = "",
         Action<object, string>? onHandle = null)
         where TSettingsControl : ActionSettingsControlBase
    {
        var info = new ActionInfo(id, name, iconGlyph, defaultGroupToMenu: defaultGroupToMenu);
        info.IsRevertable = true;
        if (IActionService.ActionInfos.TryAdd(id, info))
        {
            IActionService.ObsoleteActionHandlers[id] = (typeof(TSettings), onHandle, null);
            ProcessAddToGroup(info);
            services.AddKeyedTransient<ActionBase, ObsoleteV2ActionAdapter>(id);
            services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(id);
        }

        return services;
    }

    [Obsolete("行动 v2 兼容器。")]
    class ObsoleteV2ActionAdapter : ActionBase
    {
        protected override async Task OnInvoke()
        {
            var (ty, ac, _) = IActionService.ObsoleteActionHandlers[ActionItem.Id];
            if (ac == null) return;
            await base.OnInvoke();

            if (SettingsInternal is JsonElement json)
                SettingsInternal = json.Deserialize(ty);
            if (SettingsInternal?.GetType() != ty)
                SettingsInternal = Activator.CreateInstance(ty);

            ac(SettingsInternal, ActionSet.Guid.ToString());
        }

        protected override async Task OnRevert()
        {
            var (ty, _, ac) = IActionService.ObsoleteActionHandlers[ActionItem.Id];
            if (ac == null) return;
            await base.OnInvoke();

            if (SettingsInternal is JsonElement json)
                SettingsInternal = json.Deserialize(ty);
            if (SettingsInternal?.GetType() != ty)
                SettingsInternal = Activator.CreateInstance(ty);

            ac(SettingsInternal, ActionSet.Guid.ToString());
        }
    }
}