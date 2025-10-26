using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;

namespace ClassIsland.Core;

/// <summary>
/// 应用对象基类
/// </summary>
public abstract class AppBase : Application, IAppHost
{
    /// <summary>
    /// 获取当前应用程序实例。
    /// </summary>
    public new static AppBase Current => (Application.Current as AppBase)!;

    /// <summary>
    /// 重启应用程序。
    /// </summary>
    /// <param name="quiet">是否静默重启</param>
    public abstract void Restart(bool quiet=false);

    /// <summary>
    /// 重启应用程序。
    /// </summary>
    /// <param name="parameters">重启应用时使用的参数</param>
    public abstract void Restart(string[] parameters);

    /// <summary>
    /// 重启应用程序。
    /// </summary>
    /// <param name="parameters">重启应用时使用的参数</param>
    /// <param name="restartToLauncher">以启动器可执行程序作为重启目标</param>
    public abstract void Restart(string[] parameters, bool restartToLauncher);

    /// <summary>
    /// 停止当前应用程序。
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// 获取应用是否已裁剪资源。
    /// </summary>
    /// <returns></returns>
    [Obsolete("2.0 已弃用资源裁剪发布，此方法将恒返回 false.")]
    public abstract bool IsAssetsTrimmed();

    /// <summary>
    /// 应用是否属于开发构建
    /// </summary>
    public abstract bool IsDevelopmentBuild { get; }

    /// <summary>
    /// 应用是否处于 MSIX 打包
    /// </summary>
    public abstract bool IsMsix { get; }

    /// <summary>
    /// 当应用启动时触发。
    /// </summary>
    public abstract event EventHandler? AppStarted;

    /// <summary>
    /// 当应用正在停止时触发。
    /// </summary>
    public abstract event EventHandler? AppStopping;

    /// <summary>
    /// 应用打包类型
    /// </summary>
    public abstract string PackagingType { get; internal set; }

    /// <summary>
    /// 应用二进制文件的平台架构
    /// </summary>
    public abstract string Platform { get; }

    /// <summary>
    /// 应用二进制文件面向的操作系统
    /// </summary>
    public abstract string OperatingSystem { get; internal set; }
    
    /// <summary>
    /// 应用二进制文件的构建类型
    /// </summary>
    public abstract string BuildType { get; }

    /// <summary>
    /// 应用分发频道
    /// </summary>
    public string AppSubChannel => $"{OperatingSystem}_{Platform}_{BuildType}_{PackagingType}";

    internal AppBase()
    {
    }

    /// <summary>
    /// 应用版本
    /// </summary>
    public static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    /// <summary>
    /// 应用版本代号
    /// </summary>
    // ReSharper disable StringLiteralTypo
    public static string AppCodeName => "Khaslana";
    // ReSharper restore StringLiteralTypo

    /// <summary>
    /// 应用长版本号
    /// </summary>
    public static string AppVersionLong =>
        #if NIX
        $"{AppVersion}-{AppCodeName}-NIXBUILD_COMMIT(NIXBUILD_BRANCH) (Core {IAppHost.CoreVersion})";
        #else
        $"{AppVersion}-{AppCodeName}-{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch}) (Core {IAppHost.CoreVersion})";
        #endif
    
    /// <summary>
    /// 应用当前生命周期状态
    /// </summary>
    public static ApplicationLifetime CurrentLifetime { get; internal set; } = Enums.ApplicationLifetime.None;
    
    /// <summary>
    /// 应用当前的主窗口
    /// </summary>
    public Window? MainWindow { get; internal set; }

    /// <summary>
    /// 桌面生命周期对象
    /// </summary>
    public IClassicDesktopStyleApplicationLifetime? DesktopLifetime =>
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);

    /// <summary>
    /// Fluent Icons 字体
    /// </summary>
    public static FontFamily FluentIconsFontFamily { get; } = new FontFamily("avares://ClassIsland.Core/Assets/Fonts/#FluentSystemIcons-Resizable");

    /// <summary>
    /// Lucide Icons 字体
    /// </summary>
    public static FontFamily LucideIconsFontFamily { get; } = new FontFamily("avares://ClassIsland.Core/Assets/Fonts/#lucide");
    
    /// <summary>
    /// 虚根窗口
    /// </summary>
    public Window PhonyRootWindow = null!;

    /// <summary>
    /// 获得一个根窗口。
    /// </summary>
    /// <returns>优先返回当前激活的窗口。如果没有激活的窗口，则返回虚窗口。</returns>
    public Window GetRootWindow()
    {
        return Current.DesktopLifetime?.Windows.FirstOrDefault(x => x is { IsActive: true, IsVisible: true }) ??
               Current.PhonyRootWindow;
    }

    /// <summary>
    /// 应用入口二进制文件路径。
    /// </summary>
    public static string ExecutingEntrance { get; internal set; } = "";

    /// <summary>
    /// 当前平台可执行文件后缀
    /// </summary>
    public static string PlatformExecutableExtension => System.OperatingSystem.IsWindows() ? ".exe" : "";
}