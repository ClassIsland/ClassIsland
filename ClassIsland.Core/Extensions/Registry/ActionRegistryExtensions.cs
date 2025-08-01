using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
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
    public static IServiceCollection AddAction<TAction>(this IServiceCollection services) where TAction : ActionBase
    {
        var info = IActionService.RegisterActionInfo(typeof(TAction));
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
        var info = IActionService.RegisterActionInfo(typeof(TAction), typeof(TSettingsControl));
        services.AddKeyedTransient<ActionBase, TAction>(info.Id);
        services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(info.Id);
        return services;
    }
}