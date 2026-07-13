using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;

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

    internal static readonly Dictionary<Assembly, ContributorInfo> PluginContributorInfos = new();

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
    /// 贡献者 ID 代号到名称的映射表。<br/>
    /// 插件可通过 <see cref="PluginContributorRegistryExtensions.AddContributorDisplayName"/> 注册。
    /// </summary>
    public static Dictionary<string, string> ContributorDisplayNames { get; } = new() // 初始值由 ClassIsland 维护。
    {
        ["baiyao"] = "白杳",
        ["doctor"] = "Doctor-yoi",
        ["dryice"] = "干冰DryIce",
        ["lipoly"] = "LiPolymer",
        ["lrs"]    = "lrs2187",
        ["lyxwx"]  = "流焰xwx",
        ["wrc"]    = "HelloWRC",
        ["xiaowuap"] = "吴恩泽",
    };
}