using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 插件元数据
/// </summary>
public class PluginManifest : ObservableRecipient
{
    /// <summary>
    /// 入口程序集。加载插件时，将在此入口程序集中搜索插件类。
    /// </summary>
    /// <example>MyPlugin.dll</example>
    public string EntranceAssembly { get; set; } = "";

    /// <summary>
    /// 插件显示名称。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 插件ID。
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 插件描述。
    /// </summary>
    public string Description { get; set; } = "";
}