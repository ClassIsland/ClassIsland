using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Attributes;


/// <summary>
/// 代表认证提供方信息。
/// </summary>
/// <param name="id">此认证提供方的 ID</param>
/// <param name="name">此认证提供方的名称</param>
/// <param name="iconKind">此认证提供方的图标</param>
[AttributeUsage(AttributeTargets.Class)]
public class AuthorizeProviderInfo(string id, string name, PackIconKind iconKind) : Attribute
{
    /// <summary>
    /// 此认证提供方的 ID
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 此认证提供方的名称
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// 此认证提供方的图标
    /// </summary>
    public PackIconKind IconKind { get; } = iconKind;

    /// <summary>
    /// 认证提供方类型
    /// </summary>
    public Type? AuthorizeProviderType { get; internal set; }
}