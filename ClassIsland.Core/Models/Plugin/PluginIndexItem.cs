using System.Text.Json.Serialization;

namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 代表一个插件的索引信息。
/// </summary>
public class PluginIndexItem : PluginInfo
{
    /// <summary>
    /// 下载链接列表，以版本号为键。
    /// </summary>
    public Dictionary<string, string> Downloads { get; set; } = new();

    /// <summary>
    /// 下载MD5校验列表，以版本号为键。
    /// </summary>
    public Dictionary<string, string> DownloadsMd5 { get; set; } = new();

}