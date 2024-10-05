using MaterialDesignThemes.Wpf;
namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个行动的注册信息。
/// </summary>
/// <param name="id">行动ID，例如“classisland.example”。</param>
/// <param name="name">行动显示名称。</param>
/// <param name="iconKind">行动图标。</param>
public class ActionRegistryInfo(string id, string name = "", PackIconKind iconKind = PackIconKind.BacteriaOutline)
{
    /// <summary>
    /// 行动 ID。
    /// </summary>
    public string Id { get; internal set; } = id;

    /// <summary>
    /// 行动显示图标类型。
    /// </summary>
    public PackIconKind IconKind { get; internal set; } = iconKind;

    /// <summary>
    /// 行动显示名称。
    /// </summary>
    public string Name { get; internal set; } = string.IsNullOrEmpty(name) ? id : name;

    /// <summary>
    /// 设置控件类型。
    /// </summary>
    public Type? SettingsControlType { get; internal set; }

    /// <summary>
    /// 设置类型。
    /// </summary>
    public Type? SettingsType { get; internal set; }

    public delegate void HandleDelegate(object? settings, string guid);

    public HandleDelegate? Handle;

    public HandleDelegate? RevertHandle;
}