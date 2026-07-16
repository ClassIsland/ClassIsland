using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Loader;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Core.Models.Plugin;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 插件服务。用于管理应用各插件的加载和设置。
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// 插件包文件扩展名。
    /// </summary>
    public static readonly string PluginPackageExtension = ".cipx";

    internal static ObservableCollection<PluginInfo> LoadedPluginsInternal { get; } = new();

    internal static ObservableCollection<string> LoadedPluginsIds { get; set; } = new();

    /// <summary>
    /// 已加载的插件信息列表。
    /// </summary>
    public static IReadOnlyList<PluginInfo> LoadedPlugins => LoadedPluginsInternal;

    /// <summary>
    /// 插件包文件类型
    /// </summary>
    public static FilePickerFileType PluginPackageFileType { get; } = new("ClassIsland 插件包")
    {
        Patterns = ["*.cipx"]
    };

    /// <summary>
    /// 通过 <see cref="AssemblyLoadContext"/> 获取插件信息。
    /// </summary>
    internal static Func<Assembly, PluginInfo?> GetPluginInfo { get; set; }

    /// <summary>
    /// 贡献者 ID 对应的名称映射表。<br/>
    /// 插件可通过 <see cref="ContributorRegistryExtensions.AddContributorAliases"/> 注册。
    /// </summary>
    public static Dictionary<string, string> ContributorAliases { get; } = new() // 初始值由 ClassIsland 维护。
    {
        ["amiya"]  = "Amiya",
        ["baiyao"] = "白杳",
        ["clover"] = "Clover Yan",
        ["doctor"] = "Doctor-yoi",
        ["dryice"] = "干冰DryIce",
        ["laoshui"]= "LaoShui",
        ["lipoly"] = "LiPolymer",
        ["lrs"]    = "lrs2187",
        ["lyxwx"]  = "流焰xwx",
        ["ryo"]    = "DannyFeng",
        ["wrc"]    = "HelloWRC",
        ["xiaowuap"] = "吴恩泽",
    };
}