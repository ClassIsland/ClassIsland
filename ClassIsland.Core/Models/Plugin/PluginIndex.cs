using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 插件仓库索引
/// </summary>
public class PluginIndex : ObservableRecipient
{
    /// <summary>
    /// 插件仓库包含的插件列表
    /// </summary>
    public ObservableCollection<PluginIndexItem> Plugins { get; set; } = new();

    /// <summary>
    /// 插件下载镜像列表，键为镜像名，值为镜像根。镜像根在下载时会替换<see cref="PluginInfo"/>中下载链接的{root}模板。
    /// </summary>
    public Dictionary<string, string> DownloadMirrors { get; set; } = new();
}