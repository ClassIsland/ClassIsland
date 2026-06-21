namespace ClassIsland.Core.Attributes;

/// <summary>
/// 功能贡献者信息。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ContributorInfo(string text) : Attribute
{
    /// <summary>
    /// 该功能的贡献者描述。
    /// </summary>
    public string? Text { get; } = text;

    /// <summary>
    /// 插件名称。
    /// </summary>
    public string? Plugin { get; set; }

    /// <summary>
    /// 插件
    /// </summary>
    public string? Message { get; set; }
}
