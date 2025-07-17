using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Attributes;
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
}