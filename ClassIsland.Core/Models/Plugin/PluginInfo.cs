using System.IO;
using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo() : ObservableRecipient
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    public PluginManifest Manifest { get; set; } = new();

    /// <summary>
    /// 插件是否存在于本地
    /// </summary>
    public bool IsLocal { get; set; } = true;

    /// <summary>
    /// 插件是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get
        {
            if (!IsLocal)
                return false;
            return !File.Exists(Path.Combine(PluginFolderPath, ".disabled"));
        }
        set
        {
            if (!IsLocal)
                throw new InvalidOperationException("无法为不存在本地的插件设置启用状态。");
            var path = Path.Combine(PluginFolderPath, ".disabled");
            if (value)
            {
                File.Delete(path);
            }
            else
            {
                File.WriteAllText(path, "");
            }
        }
    }


    /// <summary>
    /// 插件文件路径。
    /// </summary>
    public string PluginFolderPath { get; internal set; } = "";

    /// <summary>
    /// 图标真实路径
    /// </summary>
    public string RealIconPath { get; set; } = "";

    /// <summary>
    /// 插件加载时错误
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 插件加载状态
    /// </summary>
    public PluginLoadStatus LoadStatus { get; set; } = PluginLoadStatus.NotLoaded;
}