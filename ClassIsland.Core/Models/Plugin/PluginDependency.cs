namespace ClassIsland.Core.Models.Plugin;

/// <summary>
/// 插件依赖信息
/// </summary>
public class PluginDependency
{
    /// <summary>
    /// 依赖插件 ID
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 是否是必选依赖
    /// </summary>
    public bool IsRequired { get; set; } = true;
}