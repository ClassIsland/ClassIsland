using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using ClassIsland.Core.Models.Plugin;
using ClassIsland.Shared;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 插件市场服务。
/// </summary>
public interface IPluginMarketService : INotifyPropertyChanged
{
    /// <summary>
    /// 已将插件仓库与本地插件合并的全部插件
    /// </summary>
    public ObservableDictionary<string, PluginInfo> MergedPlugins { get; set; }

    /// <summary>
    /// 是否正在加载插件源
    /// </summary>
    public bool IsLoadingPluginSource { get; set; }

    /// <summary>
    /// 插件源加载进度
    /// </summary>
    public double PluginSourceDownloadProgress { get; set; }

    /// <summary>
    /// 插件源加载异常
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 刷新插件源。
    /// </summary>
    public Task RefreshPluginSourceAsync();

    /// <summary>
    /// 请求下载插件
    /// </summary>
    /// <param name="id">要下载的插件id</param>
    public void RequestDownloadPlugin(string id);

    /// <summary>
    /// 请求重启事件
    /// </summary>
    public event EventHandler? RestartRequested;

    /// <summary>
    /// 重载本地插件源
    /// </summary>
    public void LoadPluginSource();

    /// <summary>
    /// 已加载的插件源
    /// </summary>
    public ObservableDictionary<string, PluginIndex> Indexes { get; }
}