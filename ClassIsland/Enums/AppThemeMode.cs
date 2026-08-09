namespace ClassIsland.Enums;

/// <summary>
/// 应用明暗主题模式，数值需兼容已持久化的 Settings.Theme 值。
/// </summary>
public enum AppThemeMode
{
    FollowSystem = 0,
    Light = 1,
    Dark = 2,
    FollowSunriseSunset = 3
}
