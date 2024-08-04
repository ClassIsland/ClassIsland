using System.Windows;
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
    /// 停止当前应用程序。
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// 获取应用是否已裁剪资源。
    /// </summary>
    /// <returns></returns>
    public abstract bool IsAssetsTrimmed();

    /// <summary>
    /// 当应用启动时触发。
    /// </summary>
    public abstract event EventHandler? AppStarted;

    /// <summary>
    /// 当应用正在停止时触发。
    /// </summary>
    public abstract event EventHandler? AppStopping;

    internal AppBase()
    {
    }
}