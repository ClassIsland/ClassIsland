namespace ClassIsland.Platforms.Abstraction.Models;

/// <summary>
/// 代表前台窗口变化事件参数。
/// </summary>
public class ForegroundWindowChangedEventArgs
{
    /// <summary>
    /// 平台句柄
    /// </summary>
    public nint Handle { get; }

    internal ForegroundWindowChangedEventArgs(nint handle)
    {
        Handle = handle;
    }
}