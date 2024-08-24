namespace ClassIsland.Core.Models.Plugin;


/// <summary>
/// 插件仓库清单，用于构建插件索引（<see cref="PluginIndexItem"/>）。
/// </summary>
public class PluginRepoManifest : PluginManifest
{
    /// <summary>
    /// 插件仓库所有者
    /// </summary>
    /// <example>HelloWRC</example>
    public string RepoOwner { get; set; } = "";

    /// <summary>
    /// 插件仓库名称
    /// </summary>
    /// <example>MyPlugin</example>
    public string RepoName { get; set; } = "";

    /// <summary>
    /// 资产文件根目录
    /// </summary>
    public string AssetsRoot { get; set; } = "master";
}