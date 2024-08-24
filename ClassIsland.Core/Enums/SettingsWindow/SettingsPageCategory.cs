namespace ClassIsland.Core.Enums.SettingsWindow;

/// <summary>
/// 设置页面类别
/// </summary>
public enum SettingsPageCategory
{
    /// <summary>
    /// 内部设置页面（不建议插件将自己的设置页面注册为这个类型）
    /// </summary>
    Internal,
    /// <summary>
    /// 扩展设置页面
    /// </summary>
    External,
    /// <summary>
    /// 关于页面
    /// </summary>
    About,
    /// <summary>
    /// 调试页面
    /// </summary>
    Debug,
}