using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Action;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册行动的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class ActionRegistryExtensions
{
    /// <summary>
    /// 注册无设置行动。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">行动ID，例如“classisland.example”。</param>
    /// <param name="name">行动名称。/</param>
    /// <param name="iconKind">行动图标。</param>
    /// <param name="onHandle">行动处理程序。</param>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddAction
        (this IServiceCollection services,
         string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
    {
        Register(id, name, iconKind, onHandle);
        return services;
    }

    /// <summary>
    /// 注册行动。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">行动ID，例如“classisland.example”。</param>
    /// <param name="name">行动名称。/</param>
    /// <param name="iconKind">行动图标。</param>
    /// <param name="onHandle">行动处理程序。</param>
    /// <typeparam name="TSettings">行动设置类型。</typeparam>
    /// <typeparam name="TSettingsControl">行动设置控件类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddAction<TSettings, TSettingsControl>
        (this IServiceCollection services,
         string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
         where TSettingsControl : ActionSettingsControlBase
    {
        var info = Register(id, name, iconKind, onHandle);
        services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(id);
        info.SettingsType = typeof(TSettings);
        info.SettingsControlType = typeof(TSettingsControl);
        return services;
    }


    private static ActionRegistryInfo Register
        (string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
    {
        if (IActionService.Actions.ContainsKey(id))
        {
            throw new InvalidOperationException($"已注册ID为 {id} 的行动。");
        }

        var info = new ActionRegistryInfo(id, name, iconKind);
        info.Handle += onHandle;
        IActionService.Actions.Add(id, info);

        return info;
    }
}