using Avalonia.Controls;
using ClassIsland.Core.Controls;

namespace ClassIsland.Core.Extensions.UI;

/// <summary>
/// <see cref="Window"/> 的扩展方法。
/// </summary>
public static class WindowExtensions
{
    /// <summary>
    /// 为不继承 MyWindow 的类初始化 MyWindow 扩展特性。
    /// </summary>
    /// <param name="window">窗口</param>
    public static void UseMyWindowExt(this Window window)
    {
        MyWindow.SetupMyWindowExt(window);
    }
}