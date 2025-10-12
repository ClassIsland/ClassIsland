using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.Profile;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Profile;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册档案数据迁移提供方的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class ProfileTransferProviderRegisterExtensions
{
    /// <summary>
    /// 注册一个档案迁移提供方
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">提供方 id</param>
    /// <param name="name">提供方名称</param>
    /// <param name="type">迁移类型</param>
    /// <param name="funcHandler">提供方处理器</param>
    /// <param name="icon">提供方图标表达式</param>
    /// <returns>原来的<see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddProfileTransferProvider(this IServiceCollection services, string id, string name, ProfileTransferProviderType type ,Action<TopLevel> funcHandler,
        string? icon = null)
    {
        RegisterCore(new ProfileTransferProviderInfo()
        {
            Id = id,
            Name = name,
            Type = type,
            FunctionHandler = funcHandler,
            Icon = IconExpressionHelper.TryParseOrNull(icon ?? "\ue68f") 
        });
        return services;
    }

    /// <summary>
    /// 注册一个档案迁移提供方
    /// </summary>
    /// <typeparam name="TProvider">要注册的提供方类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">提供方 id</param>
    /// <param name="type">迁移类型</param>
    /// <param name="name">提供方名称</param>
    /// <param name="icon">提供方图标表达式</param>
    /// <returns>原来的<see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddProfileTransferProvider<TProvider>(this IServiceCollection services, string id, string name, ProfileTransferProviderType type,
        string? icon = null) where TProvider : ProfileTransferProviderControlBase
    {
        var controlType = typeof(TProvider);
        RegisterCore(new ProfileTransferProviderInfo()
        {
            Id = id,
            Name = name,
            Type = type,
            HandlerControlType = controlType,
            Icon = IconExpressionHelper.TryParseOrNull(icon ?? "\ue68f"),
            UseFullWidth = controlType.GetCustomAttributes(false).OfType<FullWidthPageAttribute>().Any(),
            HidePageTitle = controlType.GetCustomAttributes(false).OfType<HidePageTitleAttribute>().Any()
        });
        return services;
    }

    private static void RegisterCore(ProfileTransferProviderInfo info)
    {
        if (IProfileTransferService.Providers.Any(x => x.Id == info.Id))
        {
            throw new InvalidOperationException($"已存在 id 为 {info.Id} 的迁移提供方");
        }
        
        
        IProfileTransferService.Providers.Add(info);
    }
}