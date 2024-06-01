namespace ClassIsland.Shared.Interfaces;

/// <summary>
/// 附加设置接口
/// </summary>
public interface IAttachedSettings
{
    /// <summary>
    /// 此附加设置是否启用
    /// </summary>
    public bool IsAttachSettingsEnabled { get; set; }
}