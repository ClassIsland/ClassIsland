using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册提醒提供方的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class NotificationProviderRegistryExtensions
{
    /// <summary>
    /// 注册一个提醒提供方
    /// </summary>
    /// <typeparam name="TNotificationProvider">提醒提供方类型。在该类上标记 <see cref="ContributorInfo"/> 信息。</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 服务集合</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddNotificationProvider<TNotificationProvider>(this IServiceCollection services) where TNotificationProvider : NotificationProviderBase
    {
        Register(services, typeof(TNotificationProvider));
        services.AddHostedService<TNotificationProvider>();
        return services;
    }

    /// <summary>
    /// 注册一个提醒提供方
    /// </summary>
    /// <typeparam name="TNotificationProvider">提醒提供方类型。在该类上标记 <see cref="ContributorInfo"/> 信息。</typeparam>
    /// <typeparam name="TNotificationProviderSettingsControl">提醒提供方设置控件类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 服务集合</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddNotificationProvider<TNotificationProvider, TNotificationProviderSettingsControl>(this IServiceCollection services) 
        where TNotificationProvider : NotificationProviderBase 
        where TNotificationProviderSettingsControl : NotificationProviderControlBase
    {
        var info = Register(services, typeof(TNotificationProvider), typeof(TNotificationProviderSettingsControl));
        services.AddHostedService<TNotificationProvider>();
        services.AddKeyedTransient<NotificationProviderControlBase, TNotificationProviderSettingsControl>(info.Guid);
        return services;
    }

    private static NotificationProviderInfo Register(IServiceCollection services, Type notificationProvider, Type? settings = null)
    {
        var attributes = notificationProvider.GetCustomAttributes(false);
        if (attributes.FirstOrDefault(x => x is NotificationProviderInfo) is not NotificationProviderInfo info)
        {
            throw new ArgumentException($"无法注册提醒提供方，因为这个提醒提供方 {notificationProvider.FullName} 没有注册信息。");
        }

        if (NotificationProviderRegistryService.RegisteredProviders.FirstOrDefault(x => x.Guid == info.Guid) is { } info2)
        {
            throw new ArgumentException($"此提醒提供方id {info.Guid} 已经被 {info2.Name} 占用。");
        }

        
        info.ProviderType = notificationProvider;
        if (notificationProvider.BaseType?.GenericTypeArguments.Length > 0)
        {
            info.HasSettings = true;
        }

        foreach (var i in attributes.OfType<NotificationChannelInfo>().ToList())
        {
            i.AssociatedProviderGuid = info.Guid;
            info.RegisteredChannels.Add(i);
        }

        info.ContributorInfo = ContributorInfoHelper.Extract(notificationProvider);

        if (settings != null)
        {
            info.SettingsType = settings;
        }
        NotificationProviderRegistryService.RegisteredProviders.Add(info);
        
        return info;
    }
}