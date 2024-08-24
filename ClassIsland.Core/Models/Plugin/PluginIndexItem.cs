using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 代表一个插件的索引信息。
/// </summary>
public class PluginIndexItem : PluginInfo
{
    /// <summary>
    /// 插件最新版本下载链接
    /// </summary>
    public string DownloadUrl { get; set; } = "";

    /// <summary>
    /// 插件最新版本下载MD5
    /// </summary>
    public string DownloadMd5 { get; set; } = "";

}