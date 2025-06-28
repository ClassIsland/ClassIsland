using Avalonia.Controls;
using ClassIsland.Core.Models.UI;

namespace ClassIsland.Core.Helpers.UI;

/// <summary>
/// Toast 显示辅助类
/// </summary>
public static class ToastsHelper
{
    /// <summary>
    /// 显示一条 Toast 消息\
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    public static void ShowToast(this Control control, ToastMessage message)
    {
        control.RaiseEvent(new ShowToastEventArgs(message));
    }
    
    /// <summary>
    /// 显示一条 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    public static void ShowToast(this Control control, string message)
    {
        control.RaiseEvent(new ShowToastEventArgs(new ToastMessage(message)));
    }
}