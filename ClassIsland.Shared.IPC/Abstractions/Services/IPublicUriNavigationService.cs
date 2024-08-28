using dotnetCampus.Ipc.CompilerServices.Attributes;

namespace ClassIsland.Shared.IPC.Abstractions.Services;

/// <summary>
/// 向其它进程公开的 Uri 导航服务，用于在ClassIsland内部和外部通过uri进行导航。
/// </summary>
[IpcPublic(IgnoresIpcException = true)]
public interface IPublicUriNavigationService
{
    /// <summary>
    /// 导航到指定 Uri。
    /// </summary>
    /// <param name="uri">要导航的 Uri</param>
    void Navigate(Uri uri);

    /// <summary>
    /// 导航到指定 Uri，但在抛出异常时自动捕获，并显示错误提示。
    /// </summary>
    /// <param name="uri">要导航的 Uri</param>
    void NavigateWrapped(Uri uri);
}