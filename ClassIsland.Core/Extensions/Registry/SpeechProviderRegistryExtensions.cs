using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Abstractions.Services.SpeechService;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared.Abstraction.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册语音提供方的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class SpeechProviderRegistryExtensions
{
    /// <summary>
    /// 注册一个语音提供方
    /// </summary>
    /// <typeparam name="TSpeechProvider">语音提供方类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 服务集合</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddSpeechProvider<TSpeechProvider>(this IServiceCollection services) where TSpeechProvider : class, ISpeechService
    {
        var info = Register(services, typeof(TSpeechProvider));
        services.AddKeyedSingleton<ISpeechService, TSpeechProvider>(info.Id);
        return services;
    }

    /// <summary>
    /// 注册一个语音提供方
    /// </summary>
    /// <typeparam name="TSpeechProvider">语音提供方类型</typeparam>
    /// <typeparam name="TSpeechProviderSettingsControl">语音提供方设置控件类型</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> 服务集合</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 对象</returns>
    public static IServiceCollection AddSpeechProvider<TSpeechProvider, TSpeechProviderSettingsControl>(this IServiceCollection services)
        where TSpeechProvider : class, ISpeechService
        where TSpeechProviderSettingsControl : SpeechProviderControlBase
    {
        var info = Register(services, typeof(TSpeechProvider), typeof(TSpeechProviderSettingsControl));
        services.AddKeyedSingleton<ISpeechService, TSpeechProvider>(info.Id);
        services.AddKeyedSingleton<SpeechProviderControlBase, TSpeechProviderSettingsControl>(info.Id);
        return services;
    }

    private static SpeechProviderInfo Register(IServiceCollection services, Type provider, Type? settings = null)
    {
        if (provider.GetCustomAttributes(false).FirstOrDefault(x => x is SpeechProviderInfo) is not SpeechProviderInfo info)
        {
            throw new ArgumentException($"无法注册语音提供方，因为这个语音提供方 {provider.FullName} 没有注册信息。");
        }

        if (SpeechProviderRegistryService.RegisteredProviders.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new ArgumentException($"此语音提供方id {info.Id} 已经被占用。");
        }


        info.SettingsControlType = provider;

        if (settings != null)
        {
            info.SettingsControlType = settings;
        }
        SpeechProviderRegistryService.RegisteredProviders.Add(info);

        return info;
    }
}