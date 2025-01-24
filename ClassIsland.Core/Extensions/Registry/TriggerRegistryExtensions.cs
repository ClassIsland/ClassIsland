using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册触发器的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class TriggerRegistryExtensions
{
    /// <summary>
    /// 注册一个自动化触发器。
    /// </summary>
    /// <typeparam name="TTrigger">自动化触发器类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 对象</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddTrigger<TTrigger>(this IServiceCollection services) where TTrigger : TriggerBase
    {
        var info = Register(typeof(TTrigger));
        services.AddKeyedTransient<TriggerBase, TTrigger>(info.Id);
        return services;
    }

    /// <summary>
    /// 注册一个自动化触发器。
    /// </summary>
    /// <typeparam name="TTrigger">自动化触发器类型</typeparam>
    /// <typeparam name="TSettings">自动化触发器设置界面类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 对象</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddTrigger<TTrigger, TSettings>(this IServiceCollection services) where TTrigger : TriggerBase where TSettings : TriggerSettingsControlBase
    {
        var info = Register(typeof(TTrigger), typeof(TSettings));
        services.AddKeyedTransient<TriggerBase, TTrigger>(info.Id);
        services.AddKeyedTransient<TriggerSettingsControlBase, TSettings>(info.Id);
        return services;
    }

    private static TriggerInfo Register(Type triggerType, Type? settingsType=null)
    {
        if (triggerType.GetCustomAttributes(false).FirstOrDefault(x => x is TriggerInfo) is not TriggerInfo info)
        {
            throw new InvalidOperationException($"无法注册自动化触发器 {triggerType.FullName}，因为此控件有注册信息。");
        }

        if (IAutomationService.RegisteredTriggers.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new InvalidOperationException($"此自动化触发器id {info.Id} 已经被占用。");
        }

        info.TriggerType = triggerType;
        info.SettingsControlType = settingsType;
        IAutomationService.RegisteredTriggers.Add(info);
        return info;
    }
}