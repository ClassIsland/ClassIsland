namespace ClassIsland.Core.Attributes;

/// <summary>
/// 代表类别信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GroupAttribute(string id) : Attribute
{
    /// <summary>
    /// 类别 Id
    /// </summary>
    public string Id { get; } = id;
}