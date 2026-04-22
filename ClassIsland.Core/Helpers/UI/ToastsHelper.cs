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
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用，可用于后续更新或关闭。</returns>
    public static ToastMessage ShowToast(this Control control, ToastMessage message)
    {
        control.RaiseEvent(new ShowToastEventArgs(message));
        return message;
    }
    
    /// <summary>
    /// 显示一条 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用。</returns>
    public static ToastMessage ShowToast(this Control control, string message)
    {
        return ShowToast(control, new ToastMessage(message));
    }

    /// <summary>
    /// 显示一条警告等级的 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用。</returns>
    public static ToastMessage ShowWarningToast(this Control control, string message)
    {
        return ShowToast(control, new ToastMessage(message)
        {
            Severity = InfoBarSeverity.Warning
        });
    }
    
    /// <summary>
    /// 显示一条错误等级的 Toast 消息
    /// </summary>
    /// <param name="control">包含在要显示消息的容器中的控件</param>
    /// <param name="message">要显示的消息</param>
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用。</returns>
    public static ToastMessage ShowErrorToast(this Control control, string message)
    {
        return ShowToast(control, new ToastMessage(message)
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
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用。</returns>
    public static ToastMessage ShowSuccessToast(this Control control, string message)
    {
        return ShowToast(control, new ToastMessage(message)
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
    /// <returns>返回 <see cref="ToastMessage"/> 实例引用。</returns>
    public static ToastMessage ShowErrorToast(this Control control, string title, Exception exception)
    {
        var message = new ToastMessage()
        {
            Title = title,
            Message = exception.Message,
            Severity = InfoBarSeverity.Error,
            AutoClose = false
        };
        control.RaiseEvent(new ShowToastEventArgs(message));
        return message;
    }
}