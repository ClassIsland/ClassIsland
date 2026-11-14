namespace ClassIsland.iOS.Services.Overlay;

/// <summary>
/// 悬浮主界面宿主进程的启动模式标记。由 <see cref="MainClass"/> 在进程启动最早期根据命令行或环境变量设置。
/// </summary>
internal static class IosHudMode
{
    /// <summary>
    /// 当前进程是否以悬浮主界面宿主模式运行。
    /// </summary>
    public static bool IsHudProcess { get; private set; }

    /// <summary>
    /// 识别宿主进程启动参数。环境变量用于在进程被原生层拉起时兜底。
    /// </summary>
    public static void Detect(string[] args)
    {
        IsHudProcess = args.Contains("--hud") ||
                       Environment.GetEnvironmentVariable("CLASSISLAND_HUD") == "1";
    }
}
