using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Ruleset;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册规则的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class RulesetRegistryExtensions
{
    /// <summary>
    /// 注册规则。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。/</param>
    /// <param name="iconKind">规则图标。</param>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddRule(this IServiceCollection services, string id, string name = "",
        PackIconKind iconKind = PackIconKind.CogOutline)
    {
        Register(id, name, iconKind);
        return services;
    }

    /// <summary>
    /// 注册规则。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。/</param>
    /// <param name="iconKind">规则图标。</param>
    /// <typeparam name="TSettings">规则设置类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddRule<TSettings>(this IServiceCollection services, string id, string name = "",
        PackIconKind iconKind = PackIconKind.CogOutline)
    {
        var info = Register(id, name, iconKind);
        info.SettingsType = typeof(TSettings);
        return services;
    }

    /// <summary>
    /// 注册规则。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。/</param>
    /// <param name="iconKind">规则图标。</param>
    /// <typeparam name="TSettings">规则设置类型。</typeparam>
    /// <typeparam name="TSettingsControl">规则设置控件类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddRule<TSettings, TSettingsControl>(this IServiceCollection services, string id, string name = "",
        PackIconKind iconKind = PackIconKind.CogOutline) where TSettingsControl : RuleSettingsControlBase
    {
        var info = Register(id, name, iconKind);
        services.AddKeyedTransient<RuleSettingsControlBase, TSettingsControl>(id);
        info.SettingsType = typeof(TSettings);
        info.SettingsControlType = typeof(TSettingsControl);
        return services;
    }


    private static RuleRegistryInfo Register(string id, string name = "",
        PackIconKind iconKind = PackIconKind.CogOutline)
    {
        if (IRulesetService.Rules.ContainsKey(id))
        {
            throw new InvalidOperationException($"已注册ID为 {id} 的规则。");
        }

        var info = new RuleRegistryInfo(id, name, iconKind);
        IRulesetService.Rules.Add(id, info);
        
        return info;
    }
}