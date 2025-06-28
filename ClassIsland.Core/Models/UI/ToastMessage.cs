using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Models.UI;

/// <summary>
/// 代表一条 Toast 消息的内容
/// </summary>
public class ToastMessage : ObservableRecipient
{
    private bool _isOpen = true;

    /// <summary>
    /// 初始化一个 <see cref="ToastMessage"/> 实例。
    /// </summary>
    public ToastMessage() : this("")
    {
        
    }

    /// <summary>
    /// 初始化一个 <see cref="ToastMessage"/> 实例。
    /// </summary>
    public ToastMessage(string message) : this("", message)
    {
        
    }
    
    /// <summary>
    /// 初始化一个 <see cref="ToastMessage"/> 实例。
    /// </summary>
    public ToastMessage(string title, string message)
    {
        Message = message;
        Title = title;
    }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Message { get; init; } = "";
    
    /// <summary>
    /// 消息标题
    /// </summary>
    public string Title { get; init; } = "";
    
    /// <summary>
    /// 消息重要程度
    /// </summary>
    public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
    
    /// <summary>
    /// 操作按钮内容
    /// </summary>
    public object? ActionContent { get; init; }
    
    /// <summary>
    /// 消息持续时间。只有在<see cref="AutoClose"/>属性为 true 时，这个属性才会生效。
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 消息是否会自动关闭。只有在这个属性为 true 时，<see cref="Duration"/>属性才会生效。
    /// </summary>
    public bool AutoClose { get; init; } = true;

    /// <summary>
    /// 用户是否可以自行关闭此消息
    /// </summary>
    public bool CanUserClose { get; init; } = true;

    /// <summary>
    /// 消息是否被显示
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        private set
        {
            if (value == _isOpen) return;
            _isOpen = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 结束消息显示。
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        ClosedCancellationTokenSource.Cancel();
    }

    internal CancellationTokenSource ClosedCancellationTokenSource { get; } = new();
}