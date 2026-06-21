using System.Reflection;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册插件 Contributor 信息的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class PluginContributorRegistryExtensions
{
    /// <summary>
    /// 为当前插件注册全局 ContributorInfo 信息。
    /// </summary>
    /// <param name="assembly">插件程序集（通常传 <c>typeof(本插件任一类).Assembly</c>）。</param>
    /// <param name="plugin">插件的显示名称，将填入 <see cref="ContributorInfo.Plugin"/>。</param>
    /// <param name="message">插件帮助文本，可选。</param>
    public static IServiceCollection SetPluginContributorInfo(this IServiceCollection services, Assembly assembly, string? plugin = null, string? message = null)
    {
        ContributorInfoHelper.RegisterPluginContributorInfo(assembly, plugin, message);
        return services;
    }
}
