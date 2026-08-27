using Avalonia.Threading;

namespace ClassIsland.Core.Extensions.UI;

/// <summary>
/// <see cref="Dispatcher"/> 的扩展方法。
/// </summary>
public static class DispatcherExtensions
{
    /// <summary>
    /// 切换到 UI 线程
    /// 如果当前已在 UI 线程上则返回
    /// </summary>
    public static async Task SwitchToUIThread()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => { });
        }
    }

    /// <summary>
    /// 在 UI 线程上执行操作
    /// 如已在 UI 线程, 则同步执行, 否则调度到 UI 线程执行
    /// </summary>
    /// <param name="dispatcher">目标调度器</param>
    /// <param name="action">执行操作</param>
    public static async Task InvokeIfNeededAsync(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await dispatcher.InvokeAsync(action);
    }

    /// <summary>
    /// 在 UI 线程上执行操作, 返回结果
    /// 如已在 UI 线程, 则同步执行, 否则调度到 UI 线程执行
    /// </summary>
    /// <param name="dispatcher">目标调度器</param>
    /// <param name="func">执行操作</param>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <returns>结果</returns>
    public static async Task<T> InvokeIfNeededAsync<T>(this Dispatcher dispatcher, Func<T> func)
    {
        if (dispatcher.CheckAccess())
        {
            return func();
        }
        return await dispatcher.InvokeAsync(func);
    }

    /// <summary>
    /// 在 UI 线程上执行操作
    /// 如已在 UI 线程, 则同步执行, 否则投递到 UI 线程执行
    /// 与 <see cref="InvokeIfNeededAsync(Dispatcher, Action)"/> 不同, 本方法不等待操作就返回
    /// </summary>
    /// <param name="dispatcher">目标调度器</param>
    /// <param name="action">执行操作</param>
    public static void PostIfNeeded(this Dispatcher dispatcher, Action action)
    {
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }
        dispatcher.Post(action);
    }
}
