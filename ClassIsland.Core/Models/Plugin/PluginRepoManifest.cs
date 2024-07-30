namespace ClassIsland.Core.Models.Plugin;


/// <summary>
/// 插件仓库清单，用于构建插件索引（<see cref="PluginIndexItem"/>）。
/// </summary>
public class PluginRepoManifest : PluginManifest
{
    /// <summary>
    /// 插件仓库路径
    /// </summary>
    /// <example>ClassIsland/ExamplePlugins</example>
    public string RepoPath { get; set; } = "";
}