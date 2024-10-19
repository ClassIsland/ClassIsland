using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using ClassIsland.Core.Services.Registry;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册认证提供方的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class AuthorizeProviderRegistryExtensions
{

    /// <summary>
    /// 注册认证提供方。
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TProvider">认证提供方类型</typeparam>
    /// <returns>原来的<see cref="IServiceCollection"/>对象</returns>
    public static IServiceCollection AddAuthorizeProvider<TProvider>(this IServiceCollection services) where TProvider : AuthorizeProviderControlBase
    {
        var provider = typeof(TProvider);
        if (provider.GetCustomAttributes(false).FirstOrDefault(x => x is AuthorizeProviderInfo) is not AuthorizeProviderInfo info)
        {
            throw new ArgumentException($"无法注册认证提供方，因为这个认证提供方 {provider.FullName} 没有注册信息。");
        }

        if (AuthorizeProviderRegistryService.RegisteredAuthorizeProviders.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new ArgumentException($"此认证提供方id {info.Id} 已经被占用。");
        }

        info.AuthorizeProviderType = provider;
        services.AddKeyedTransient<AuthorizeProviderControlBase, TProvider>(info.Id);
        AuthorizeProviderRegistryService.RegisteredAuthorizeProviders.Add(info);

        return services;
    }
}