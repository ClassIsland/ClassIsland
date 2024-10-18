namespace ClassIsland.Core.Attributes;

/// <summary>
/// 代表此元素可以由先前某个 ID 的元素迁移而来。
/// </summary>
/// <param name="id">源元素 ID</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MigrateFromAttribute(string id) : Attribute
{
    /// <summary>
    /// 源元素 ID
    /// </summary>
    public string Id { get; } = id;
}