using System.IO;
using System.Text.Json.Serialization;
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
    public PluginManifest Manifest { get; internal set; } = new();

    /// <summary>
    /// 插件是否存在于本地
    /// </summary>
    [JsonIgnore]
    public bool IsLocal { get; internal set; } = false;

    /// <summary>
    /// 插件是否已启用
    /// </summary>

    [JsonIgnore]
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
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 插件是否将要卸载
    /// </summary>

    [JsonIgnore]
    public bool IsUninstalling
    {
        get
        {
            if (!IsLocal)
                return false;
            return File.Exists(Path.Combine(PluginFolderPath, ".uninstall"));
        }
        set
        {
            if (!IsLocal)
                throw new InvalidOperationException("无法为不存在本地的插件设置将要卸载状态。");
            var path = Path.Combine(PluginFolderPath, ".uninstall");
            if (value)
            {
                File.WriteAllText(path, "");
            }
            else
            {
                File.Delete(path);
            }   
            OnPropertyChanged();
        }
    }


    /// <summary>
    /// 插件文件路径。
    /// </summary>

    [JsonIgnore]
    public string PluginFolderPath { get; internal set; } = "";

    /// <summary>
    /// 图标真实路径
    /// </summary>
    public string RealIconPath { get; internal set; } = "";

    /// <summary>
    /// 插件加载时错误
    /// </summary>

    [JsonIgnore]
    public Exception? Exception { get; internal set; }

    /// <summary>
    /// 插件加载状态
    /// </summary>

    [JsonIgnore]
    public PluginLoadStatus LoadStatus { get; internal set; } = PluginLoadStatus.NotLoaded;
}