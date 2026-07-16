using System.Reflection;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Ruleset;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册规则的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class RulesetRegistryExtensions
{
    /// <summary>
    /// 注册规则。使用 <see cref="ContributorRegistryExtensions.WithContributorInfo"/> 附加贡献者信息。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。</param>
    /// <param name="iconGlyph">规则图标。</param>
    /// <param name="onHandle">规则处理程序。</param>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddRule(this IServiceCollection services, string id, string name = "",
        string iconGlyph = "\uef27", RuleRegistryInfo.HandleDelegate? onHandle = null)
    {
        var info = Register(id, name, iconGlyph, onHandle);
        info.ContributorInfo = ContributorInfoHelper.Setup(Assembly.GetCallingAssembly());
        return services;
    }

    /// <summary>
    /// 注册规则。使用 <see cref="ContributorRegistryExtensions.WithContributorInfo"/> 附加贡献者信息。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。</param>
    /// <param name="iconGlyph">规则图标。</param>
    /// <param name="onHandle">规则处理程序。</param>
    /// <typeparam name="TSettings">规则设置类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddRule<TSettings>(this IServiceCollection services, string id, string name = "",
        string iconGlyph = "\uef27", RuleRegistryInfo.HandleDelegate? onHandle = null)
    {
        var info = Register(id, name, iconGlyph, onHandle);
        info.SettingsType = typeof(TSettings);

        info.ContributorInfo = ContributorInfoHelper.Setup(Assembly.GetCallingAssembly(), typeof(TSettings));
        return services;
    }

    /// <summary>
    /// 注册规则。使用 <see cref="ContributorRegistryExtensions.WithContributorInfo"/> 附加贡献者信息。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">规则ID，例如“classisland.example”。</param>
    /// <param name="name">规则名称。</param>
    /// <param name="iconGlyph">规则图标。</param>
    /// <param name="onHandle">规则处理程序。</param>
    /// <typeparam name="TSettings">规则设置类型。</typeparam>
    /// <typeparam name="TSettingsControl">规则设置控件类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddRule<TSettings, TSettingsControl>(this IServiceCollection services, string id, string name = "",
        string iconGlyph = "\uef27", RuleRegistryInfo.HandleDelegate? onHandle = null) where TSettingsControl : RuleSettingsControlBase
    {
        var info = Register(id, name, iconGlyph, onHandle);
        services.AddKeyedTransient<RuleSettingsControlBase, TSettingsControl>(id);
        info.SettingsType = typeof(TSettings);
        info.SettingsControlType = typeof(TSettingsControl);

        info.ContributorInfo = ContributorInfoHelper.Setup(Assembly.GetCallingAssembly(), typeof(TSettingsControl));
        return services;
    }


    private static RuleRegistryInfo Register(string id, string name,
        string iconGlyph, RuleRegistryInfo.HandleDelegate? onHandle)
    {
        if (IRulesetService.Rules.ContainsKey(id))
        {
            throw new InvalidOperationException($"已注册ID为 {id} 的规则。");
        }

        var info = new RuleRegistryInfo(id, name, iconGlyph);
        info.Handle += onHandle;
        IRulesetService.Rules.Add(id, info);
        
        return info;
    }
}