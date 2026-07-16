using System.Reflection;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
namespace ClassIsland.Core.Helpers;

/// ContributorInfo 注册信息提取工具。
public static class ContributorInfoHelper
{
    internal static HashSet<Assembly> BuiltInAssemblies { get; } =
    [                              // ClassIsland: 在加载时添加
        typeof(AppBase).Assembly,  // ClassIsland.Core
        typeof(IAppHost).Assembly, // ClassIsland.Shared
    ];

    static bool IsBuiltInAssembly(Assembly assembly) => BuiltInAssemblies.Contains(assembly);

    /// RegisterPluginContributorInfo 由插件使用 IServiceCollection 方法调用，为插件注册全局 ContributorInfo 信息。
    internal static void RegisterPluginContributorInfo(Assembly assembly, string? message = null)
    {
        var plugin = IPluginService.GetPluginInfo(assembly) 
                  ?? throw new InvalidOperationException("意外：插件程序集未在插件加载前被 PluginService 注册。");
        plugin.ContributorMessage = message;
    }

    /// Setup 用于项目注册拓展，在注册项目时为其创建 ContributorInfo。
    public static ContributorInfo Setup(Assembly callingAssembly, Type? attributedType = null)
    {
        var info = attributedType?.GetCustomAttributes(false).OfType<ContributorInfo>().FirstOrDefault() ?? new();
        AttachPluginInfo(info, callingAssembly);
        RegistryContext.LastContributorInfo = info;
        return info;
    }

    /// AttachPluginInfo 为 ContributorInfo 附加上插件信息。
    public static void AttachPluginInfo(ContributorInfo info, Assembly assembly)
    {
        if (IsBuiltInAssembly(assembly))
            info.IsBuiltIn = true;
        else
        {
            var plugin = IPluginService.GetPluginInfo(assembly);
            if (plugin != null)
                info.PluginInfo = plugin;
            else throw new InvalidOperationException(
                $"意外：程序集 {assembly.FullName} 未在插件加载前被 PluginService 注册。");
        }
    }
}