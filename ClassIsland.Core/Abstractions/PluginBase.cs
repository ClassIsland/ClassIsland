using System.Diagnostics.CodeAnalysis;
using ClassIsland.Core.Models.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ClassIsland.Core.Abstractions;

/// <summary>
/// ClassIsland 插件抽象基类。
/// </summary>
public abstract class PluginBase
{
    /// <summary>
    /// 当前插件的设置目录。插件的各项设置应当存放在此目录中。
    /// </summary>
    public string PluginConfigFolder { get; internal set; } = "";

    /// <summary>
    /// 当前插件的信息
    /// </summary>
    public PluginInfo Info { get; internal set; } = null!;

    /// <summary>
    /// 初始化插件。一般在这个方法中完成插件的各项服务的注册。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="services"></param>
    public abstract void Initialize(HostBuilderContext context, IServiceCollection services);

    /// <summary>
    /// 当应用退出时触发。
    /// </summary>
    public abstract void OnShutdown();
}