using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Services.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于在主界面注册组件的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class ComponentRegistryExtensions
{
    /// <summary>
    /// 注册主界面组件
    /// </summary>
    /// <typeparam name="TComponent">组件类型</typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddComponent<TComponent>(this IServiceCollection services) where TComponent : ComponentBase
    {
        Register(services, typeof(TComponent));
        return services;
    }

    /// <summary>
    /// 注册主界面组件
    /// </summary>
    /// <typeparam name="TComponent">组件类型</typeparam>
    /// <typeparam name="TSettings">组件设置控件类型</typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddComponent<TComponent, TSettings>(this IServiceCollection services) where TComponent : ComponentBase where TSettings : class
    {
        Register(services, typeof(TComponent), typeof(TSettings));
        return services;
    }

    private static ComponentInfo Register(IServiceCollection services, Type component, Type? settings = null) 
    {
        if (component.GetCustomAttributes(false).FirstOrDefault(x => x is ComponentInfo) is not ComponentInfo info)
        {
            throw new ArgumentException($"无法注册组件，因为这个组件 {component.FullName} 没有注册信息。");
        }

        if (ComponentRegistryService.Registered.FirstOrDefault(x => x.Guid == info.Guid) != null)
        {
            throw new ArgumentException($"此组件id {info.Guid} 已经被占用。");
        }
        services.AddTransient(component);
        info.ComponentType = component;
        if (settings != null)
        {
            services.AddTransient(settings);
            info.SettingsType = settings;
        }
        ComponentRegistryService.Registered.Add(info);
        ComponentRegistryService.RegisteredSettings.Add(new ComponentSettings()
        {
            Id = info.Guid.ToString()
        });
        return info;
    }
}