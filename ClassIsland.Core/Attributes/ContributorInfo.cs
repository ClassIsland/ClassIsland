namespace ClassIsland.Core.Attributes;

/// 贡献者信息。用于为应用内的功能标识其贡献者与插件来源。<br/>
/// 添加此信息需要遵守规范。<br/>
/// https://docs.classisland.tech/dev/contributor-attribution.html
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
    
    /// <see cref="ContributorInfo"/>
    public static implicit operator ContributorInfo(string details) => new(details);
}
