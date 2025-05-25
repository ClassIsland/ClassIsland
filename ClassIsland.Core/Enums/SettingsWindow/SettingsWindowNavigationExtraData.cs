namespace ClassIsland.Core.Enums.SettingsWindow;

/// <summary>
/// 设置窗口导航附加信息
/// </summary>
[Obsolete("现在使用 SettingsWindowNavigationData 来获取设置页面导航信息。")]
public enum SettingsWindowNavigationExtraData
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 从导航栏进行导航
    /// </summary>
    NavigateFromNavigationView
}