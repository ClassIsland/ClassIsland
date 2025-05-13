using ClassIsland.Core.Abstractions.Models.Marketplace;
using ClassIsland.Core.Models.XamlTheme;

namespace ClassIsland.Core.Models.XamlTheme;

/// <summary>
/// 主题仓库清单，用于构建主题索引（<see cref="ThemeIndexInfo"/>）。
/// </summary>
public class ThemeRepoManifest : ThemeManifest, IMarketplaceItemRepoManifest
{
    /// <summary>
    /// 主题仓库所有者
    /// </summary>
    /// <example>HelloWRC</example>
    public string RepoOwner { get; set; } = "";

    /// <summary>
    /// 主题仓库名称
    /// </summary>
    /// <example>MyPlugin</example>
    public string RepoName { get; set; } = "";

    /// <summary>
    /// 资产文件根目录
    /// </summary>
    public string AssetsRoot { get; set; } = "master";
    
    /// <summary>
    /// 主题发布工件名称。留空将匹配 *.cipx 的发布工件。
    /// </summary>
    public string? ArtifactName { get; set; }
}