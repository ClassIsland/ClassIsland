using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 插件服务。用于管理应用各插件的加载和设置。
/// </summary>
public interface IPluginService
{
    internal static ObservableCollection<PluginInfo> LoadedPluginsInternal { get; } = new();

    /// <summary>
    /// 已加载的插件信息列表。
    /// </summary>
    public static IReadOnlyList<PluginInfo> LoadedPlugins => LoadedPluginsInternal;
}