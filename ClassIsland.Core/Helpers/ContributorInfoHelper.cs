using System.Diagnostics.Contracts;
using System.Reflection;
using ClassIsland.Core.Attributes;
namespace ClassIsland.Core.Helpers;

/// <summary>
/// ContributorInfo 注册信息提取工具。
/// </summary>
public static class ContributorInfoHelper
{
    internal static readonly Dictionary<Assembly, PluginContributorInfo> PluginContributorInfos = new();

    /// 由 PluginService 在加载插件时自动调用，注册 Assembly 对应的插件名称。
    internal static void RegisterPlugin(Assembly assembly, string plugin) => PluginContributorInfos.Add(assembly, new()
    {
        Plugin = plugin
    });

    /// 由 IServiceCollection 方法调用，为插件注册全局 ContributorInfo 信息。
    internal static void RegisterPluginContributorInfo(Assembly assembly, string? plugin = null, string? message = null)
    {
        if (PluginContributorInfos.TryGetValue(assembly, out var info))
        {
            if (plugin != null) info.Plugin = plugin;
            if (message != null) info.Message = message;
        }
        else
        {
            throw new InvalidOperationException("插件程序集未在插件加载前被 PluginService 注册。");
        }
    }

    /// <summary>
    /// 从 <paramref name="type"/> 中提取 <see cref="ContributorInfo"/> 特性，并附加插件信息。
    /// </summary>
    [Pure] public static ContributorInfo? Extract(Type type)
    {
        var info = type.GetCustomAttributes(false).OfType<ContributorInfo>().FirstOrDefault();

        if (PluginContributorInfos.TryGetValue(type.Assembly, out var plugin))
        {
            info ??= new(null!);
            info.Plugin ??= plugin.Plugin;
            info.Message ??= plugin.Message;
        }

        return info;
    }

    internal class PluginContributorInfo
    {
        public string? Plugin { get; set; }
        public string? Message { get; set; }
    }
}
