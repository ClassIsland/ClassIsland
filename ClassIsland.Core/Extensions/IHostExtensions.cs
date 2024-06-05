using System.Runtime.CompilerServices;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Core.Extensions;

public static class HostExtensions
{
    /// <summary>
    /// 注册设置页面
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T">设置页面类型</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddSettingsPage<T>(this IServiceCollection services) where T : SettingsPageBase
    {
        var type = typeof(T);
        if (type.GetCustomAttributes(false).FirstOrDefault(x => x is SettingsPageInfo) is not SettingsPageInfo info)
        {
            throw new ArgumentException("无法注册设置页面，因为设置页面没有注册信息。");
        }

        if (SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new ArgumentException($"此设置页面id {info.Id} 已经被占用。");
        }
        services.AddKeyedTransient<SettingsPageBase, T>(info.Id);
        SettingsWindowRegistryService.Registered.Add(info);
        return services;
    }
}