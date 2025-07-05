using Avalonia.Controls;
using ClassIsland.Core.Models.UI;
using FluentAvalonia.UI.Controls;

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
        ShowToast(control, new ToastMessage(message));
    }

    /// <summary>
    /// 显示一条警告等级的 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    public static void ShowWarningToast(this Control control, string message)
    {
        ShowToast(control, new ToastMessage(message)
        {
            Severity = InfoBarSeverity.Warning
        });
    }
    
    /// <summary>
    /// 显示一条错误等级的 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    public static void ShowErrorToast(this Control control, string message)
    {
        ShowToast(control, new ToastMessage(message)
        {
            Severity = InfoBarSeverity.Error,
            Duration = TimeSpan.FromSeconds(10)
        });
    }
    
    /// <summary>
    /// 显示一条成功等级的 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    public static void ShowSuccessToast(this Control control, string message)
    {
        ShowToast(control, new ToastMessage(message)
        {
            Severity = InfoBarSeverity.Success
        });
    }
    
    /// <summary>
    /// 显示一条错误等级的 Toast 消息，并将异常信息作为正文内容。
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="title">要显示的错误标题</param>
    /// <param name="exception">异常内容</param>
    public static void ShowErrorToast(this Control control, string title, Exception exception)
    {
        control.RaiseEvent(new ShowToastEventArgs(new ToastMessage()
        {
            Title = title,
            Message = exception.Message,
            Severity = InfoBarSeverity.Error,
            AutoClose = false
        }));
    }
}