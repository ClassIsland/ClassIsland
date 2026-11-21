namespace ClassIsland.Core.Helpers;

/// <summary>
/// 当前发行版的外部插件能力；由构建配置决定，不能通过用户设置开启。
/// </summary>
public static class PluginSupport
{
    /// <summary>
    /// 是否提供外部插件的安装、市场和执行功能。
    /// </summary>
    public static bool IsEnabled =>
#if CLASSISLAND_APP_STORE
        false;
#else
        true;
#endif
}
