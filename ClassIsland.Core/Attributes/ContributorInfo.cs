namespace ClassIsland.Core.Attributes;

/// 功能贡献者信息。
[AttributeUsage(AttributeTargets.Class)]
public class ContributorInfo(string details) : Attribute
{
    /// 插件名称。
    public string? PluginName { get; set; }
    /// 插件支持信息。
    public string? PluginMessage { get; set; }
    /// 该功能的贡献者详情。
    public string? Details { get; set; } = details;
    
    internal ContributorInfo() : this(null!) { }
}
