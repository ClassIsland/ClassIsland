using System.Reflection;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 主视图页面注册扩展。
/// </summary>
public static class MainViewRegistryExtensions
{
    /// <summary>
    /// 注册带有 <see cref="MainViewPageInfo"/> 的主视图页面。
    /// </summary>
    /// <typeparam name="T">页面类型。</typeparam>
    /// <param name="services">用于注册页面的服务集合。</param>
    /// <returns>原服务集合。</returns>
    /// <exception cref="ArgumentException">页面缺少注册信息或页面 ID 已被使用。</exception>
    public static IServiceCollection AddMainViewPage<T>(this IServiceCollection services) where T : MainViewPageBase
    {
        var type = typeof(T);
        var info = type.GetCustomAttribute<MainViewPageInfo>(inherit: false)
                   ?? throw new ArgumentException($"无法注册主视图页面 {type.FullName}，因为页面没有注册信息。");
        if (services.Any(service => service.ServiceType == typeof(MainViewPageBase) && Equals(service.ServiceKey, info.Id)))
        {
            throw new ArgumentException($"此主视图页面 ID {info.Id} 已经被占用。");
        }

        info.UseFullWidth = type.IsDefined(typeof(FullWidthPageAttribute), inherit: false);
        info.HidePageTitle = type.IsDefined(typeof(HidePageTitleAttribute), inherit: false);
        services.AddKeyedTransient<MainViewPageBase, T>(info.Id);
        return services.AddSingleton(info);
    }
}
