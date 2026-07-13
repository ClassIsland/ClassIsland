namespace ClassIsland.Platforms.Abstraction.Services;

/// <summary>
/// 提供应用生命周期操作的服务。
/// </summary>
public interface IAppLifetimeService
{
    /// <summary>
    /// 停止当前应用程序。
    /// </summary>
    internal void Shutdown();

    /// <summary>
    /// 重启当前应用程序。
    /// </summary>
    /// <param name="parameters">重启时使用的参数</param>
    /// <param name="restartToLauncher">是否重启至启动器</param>
    internal void Restart(string[] parameters, bool restartToLauncher);

    /// <summary>
    /// 在需要用户手动结束进程的平台上，等待平台资源进入可终止状态。
    /// </summary>
    internal Task PrepareForManualTerminationAsync(
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
