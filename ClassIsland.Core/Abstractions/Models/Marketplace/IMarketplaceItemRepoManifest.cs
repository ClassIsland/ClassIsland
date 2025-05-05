namespace ClassIsland.Core.Abstractions.Models.Marketplace;

/// <summary>
/// 市场内容仓库清单
/// </summary>
public interface IMarketplaceItemRepoManifest : IMarketplaceItemManifest
{
    /// <summary>
    /// 仓库所有者
    /// </summary>
    /// <example>HelloWRC</example>
    public string RepoOwner { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    /// <example>MyPlugin</example>
    public string RepoName { get; set; }

    /// <summary>
    /// 资产文件根目录
    /// </summary>
    public string AssetsRoot { get; set; }
    
    /// <summary>
    /// 发布工件名称。
    /// </summary>
    public string? ArtifactName { get; set; }
}