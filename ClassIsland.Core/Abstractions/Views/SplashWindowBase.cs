using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;

namespace ClassIsland.Core.Abstractions.Views;

/// <summary>
/// 启动屏幕基类
/// </summary>
public abstract class SplashWindowBase : Window, ISplashProvider
{
    /// <inheritdoc />
    public virtual async Task StartSplash()
    {
        var tcs = new TaskCompletionSource();
        Opened += async (s, e) =>
        {
            await TryWaitJobs();
            tcs.SetResult();
        };
        Show();
        await tcs.Task;
    }

    /// <inheritdoc />
    public virtual async Task EndSplash()
    {
        Close();
    }
    
    /// <summary>
    /// 尝试以阻塞的形式推动 Dispatcher 运行，当禁用动画等待时此方法不生效。
    /// </summary>
    protected static void TryRunJobs()
    {
        if (IThemeService.IsWaitForTransientDisabled)
        {
            return;
        }

        Dispatcher.UIThread.RunJobs();
    }
    
    /// <summary>
    /// 尝试以异步的形式推动 Dispatcher 运行，当禁用动画等待时此方法不生效。
    /// </summary>
    protected static async Task TryWaitJobs()
    {
        if (IThemeService.IsWaitForTransientDisabled)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}