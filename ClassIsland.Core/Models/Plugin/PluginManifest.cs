using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using YamlDotNet.Serialization;

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

    /// <summary>
    /// 插件图标路径。默认为icon.png。
    /// </summary>
    public string Icon { get; set; } = "icon.png";

    /// <summary>
    /// 插件自述文件路径。默认为README.md。
    /// </summary>
    public string Readme { get; set; } = "README.md";

    /// <summary>
    /// 项目 Url
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 插件版本
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// 插件目标 ClassIsland 版本
    /// </summary>
    public string ApiVersion { get; set; } = "";
}