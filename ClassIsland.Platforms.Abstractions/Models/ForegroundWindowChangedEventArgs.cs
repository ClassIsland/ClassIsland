namespace ClassIsland.Platforms.Abstraction.Models;

/// <summary>
/// 代表前台窗口变化事件参数。
/// </summary>
public class ForegroundWindowChangedEventArgs
{
    /// <summary>
    /// 前台窗口句柄
    /// </summary>
    public nint Handle { get; }

    /// <summary>
    /// 初始化一个<see cref="ForegroundWindowChangedEventArgs"/>对象。
    /// </summary>
    /// <param name="handle">前台窗口句柄</param>
    public ForegroundWindowChangedEventArgs(nint handle)
    {
        Handle = handle;
    }
}