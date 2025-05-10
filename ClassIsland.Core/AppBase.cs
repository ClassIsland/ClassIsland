using System.Reflection;
using System.Windows;
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
    /// 停止当前应用程序。
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// 获取应用是否已裁剪资源。
    /// </summary>
    /// <returns></returns>
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
    public string PackagingType => IsMsix ? "msix" : "singleFile";

    /// <summary>
    /// 应用二进制文件的平台架构
    /// </summary>
    public abstract string Platform { get; }

    /// <summary>
    /// 应用二进制文件面向的操作系统
    /// </summary>
    public abstract string OperatingSystem { get; }

    /// <summary>
    /// 应用分发频道
    /// </summary>
    public string AppSubChannel => $"{OperatingSystem}_{Platform}_{(IsAssetsTrimmed() ? "trimmed" : "full")}_{PackagingType}";

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
    public static string AppCodeName => "RyouYamada";
    // ReSharper restore StringLiteralTypo

    /// <summary>
    /// 应用长版本号
    /// </summary>
    public static string AppVersionLong =>
        $"{AppVersion}-{AppCodeName}-{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch}) (Core {IAppHost.CoreVersion})";
    
    /// <summary>
    /// 应用当前生命周期状态
    /// </summary>
    public static ApplicationLifetime CurrentLifetime { get; internal set; } = ApplicationLifetime.None;
}