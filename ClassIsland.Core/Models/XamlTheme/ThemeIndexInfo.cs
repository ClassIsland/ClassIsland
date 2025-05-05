using ClassIsland.Core.Models.Plugin;

namespace ClassIsland.Core.Models.XamlTheme;

/// <summary>
/// 代表一个主题的索引信息。
/// </summary>
public class ThemeIndexItem : ThemeInfo
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