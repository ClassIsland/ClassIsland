using System.Diagnostics.Contracts;
using System.Reflection;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
namespace ClassIsland.Core.Helpers;

/// ContributorInfo 注册信息提取工具。
public static class ContributorInfoHelper
{
    internal static HashSet<Assembly> ClassIslandAssemblies { get; } =
    [                              // ClassIsland: 在加载时添加
        typeof(AppBase).Assembly,  // ClassIsland.Core
        typeof(IAppHost).Assembly, // ClassIsland.Shared
    ];

    internal static bool IsBuiltInAssembly(Assembly assembly) => ClassIslandAssemblies.Contains(assembly);

    /// RegisterPluginContributorInfo 由插件使用 IServiceCollection 方法调用，为插件注册全局 ContributorInfo 信息。
    internal static void RegisterPluginContributorInfo(Assembly assembly, string? message = null)
    {
        var plugin = IPluginService.GetPluginInfo(assembly) 
                  ?? throw new InvalidOperationException("意外：插件程序集未在插件加载前被 PluginService 注册。");
        plugin.ContributorMessage = message;
    }

    /// Extract 从指定类型中提取 ContributorInfo 特性，并附加插件信息。
    [Pure]
    public static ContributorInfo Extract(Type type)
    {
        var assembly = type.Assembly;
        
        if (IsBuiltInAssembly(assembly))
        {
            var info = type.GetCustomAttributes(false).OfType<ContributorInfo>().FirstOrDefault()
                    ?? new ContributorInfo();
            info.PluginId = "ClassIsland";
            return info;
        }
        
        var plugin = IPluginService.GetPluginInfo(assembly);
        if (plugin != null)
        {
            var info = type.GetCustomAttributes(false).OfType<ContributorInfo>().FirstOrDefault()
                    ?? new ContributorInfo();
            info.PluginId = plugin.Manifest.Id;
            info.CustomizedPluginMessage = plugin.ContributorMessage;
            return info;
        }

        throw new InvalidOperationException($"意外：{type.FullName} 的程序集 {assembly.FullName} 未在插件加载前被 PluginService 注册。");
    }
}