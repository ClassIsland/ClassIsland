using System.Reflection;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册插件 ContributorInfo 信息的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class PluginContributorRegistryExtensions
{
    /// <summary>
    /// 为当前插件注册全局 ContributorInfo 信息。
    /// </summary>
    /// <param name="assembly">插件程序集（通常传 <c>typeof(本插件任一类).Assembly</c>）。</param>
    /// <param name="message">插件帮助文本，将填入 <see cref="ContributorInfo.PluginMessage"/>，可选。</param>
    /// <param name="name">插件的显示名称，将填入 <see cref="ContributorInfo.PluginName"/>。自动获取，无需填写。</param>
    public static IServiceCollection SetPluginContributorInfo(this IServiceCollection services, Assembly assembly, string? message = null, string? name = null)
    {
        ContributorInfoHelper.RegisterPluginContributorInfo(assembly, message, name);
        return services;
    }

    /// <summary>
    /// 注册一个贡献者 ID 对应的名称。
    /// </summary>
    /// <param name="id">贡献者 ID。</param>
    /// <param name="name">贡献者名称。</param>
    public static IServiceCollection AddContributorDisplayName(this IServiceCollection services, string id, string name)
    {
        IPluginService.ContributorDisplayNames.TryAdd(id, name);
        return services;
    }
}
