using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.SettingsWindow;
using ClassIsland.Core.Services.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于向设置界面注册设置页面的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class SettingsWindowRegistryExtensions
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
            throw new ArgumentException($"无法注册设置页面 {type.FullName}，因为设置页面没有注册信息。");
        }

        if (SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == info.Id) != null)
        {
            throw new ArgumentException($"此设置页面id {info.Id} 已经被占用。");
        }

        if (type.GetCustomAttributes(false).OfType<FullWidthPageAttribute>().Any())
        {
            info.UseFullWidth = true;
        }
        if (type.GetCustomAttributes(false).OfType<HidePageTitleAttribute>().Any())
        {
            info.HidePageTitle = true;
        }
        if (type.GetCustomAttributes(false).OfType<GroupAttribute>().FirstOrDefault() is {} group)
        {
            info.GroupId = group.Id;
        }
        services.AddKeyedTransient<SettingsPageBase, T>(info.Id);
        SettingsWindowRegistryService.Registered.Add(info);
        return services;
    }

    /// <summary>
    /// 添加新的设置页面分组。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> 实例</param>
    /// <param name="id">分组 ID</param>
    /// <param name="icon">分组图标表达式</param>
    /// <param name="name">分组名称</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 实例</returns>
    public static IServiceCollection AddSettingsPageGroup(this IServiceCollection services, string id, string icon, string name)
    {
        return services.AddSettingsPageGroup(id, new SettingsPageGroupInfo()
        {
            IconExpression = icon,
            Name = name
        });
    }
    
    /// <summary>
    /// 添加新的设置页面分组。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> 实例</param>
    /// <param name="id">分组 ID</param>
    /// <param name="info">分组信息</param>
    /// <returns>原来的 <see cref="IServiceCollection"/> 实例</returns>
    public static IServiceCollection AddSettingsPageGroup(this IServiceCollection services, string id, SettingsPageGroupInfo info) 
    {
        SettingsWindowRegistryService.Groups.Add(id, info);
        return services;
    }
}
