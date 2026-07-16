using System.Reflection;
using System.Runtime.CompilerServices;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册 ContributorInfo 信息的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class ContributorRegistryExtensions
{
    /// <summary>
    /// 为当前插件注册全局 ContributorInfo 插件支持信息。用于 ContributorBadge 信息显示。
    /// </summary>
    /// <param name="message">插件支持信息，将覆盖默认插件支持信息。</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection SetPluginContributorInfo(this IServiceCollection services, string? message)
    {
        var assembly = Assembly.GetCallingAssembly();
        ContributorInfoHelper.RegisterPluginContributorInfo(assembly, message);
        return services;
    }

    /// 注册一个贡献者 ID 对应的名称。用于 ContributorBadge 贡献者名显示。
    /// 如果该 ID 已被注册，则不会更改原有名称。
    public static IServiceCollection AddContributorAliases(this IServiceCollection services, string id, string name)
    {
        IPluginService.ContributorAliases.TryAdd(id, name);
        return services;
    }

    /// 对刚刚注册的对象附加贡献者信息。
    public static IServiceCollection WithContributorInfo(this IServiceCollection services, ContributorInfo contributorInfo)
    {
        var refInfo = RegistryContext.LastContributorInfo;
        if (refInfo == null) throw new InvalidOperationException(
            "未找到最近注册的项。请确保跟在 AddRule/AddProfileTransferProvider 等特定注册方法之后调用。");

        if (contributorInfo.Details != null)
            refInfo.Details = contributorInfo.Details;

        if (contributorInfo.PluginId != null)
            refInfo.PluginId = contributorInfo.PluginId;

        RegistryContext.LastContributorInfo = null!;
        return services;
    }
}
