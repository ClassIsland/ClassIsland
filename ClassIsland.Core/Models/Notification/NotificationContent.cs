using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Notification;

/// <summary>
/// 代表一个提醒中的一部分内容。
/// </summary>
public class NotificationContent : ObservableRecipient
{
    private object? _content;
    private DataTemplate? _contentTemplate;
    private bool _isSpeechEnabled = true;
    private string _speechContent = "";
    private TimeSpan _duration = TimeSpan.FromSeconds(5);
    private DateTime? _endTime;

    /// <summary>
    /// 提醒内容。
    /// </summary>
    public object? Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    /// <summary>
    /// 提醒内容模板。如果此值不为 null，将在呈现提醒内容 <see cref="Content"/> 时使用。
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => _contentTemplate;
        set => SetProperty(ref _contentTemplate, value);
    }

    /// <summary>
    /// 显示此部分的提醒时是否启用语音。
    /// </summary>
    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set => SetProperty(ref _isSpeechEnabled, value);
    }

    /// <summary>
    /// 显示此部分提醒时的语音内容。
    /// </summary>
    public string SpeechContent
    {
        get => _speechContent;
        set => SetProperty(ref _speechContent, value);
    }

    /// <summary>
    /// 此部分的显示时长。当 <see cref="EndTime"/> 不为 null 时，此项不起作用。
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    /// <summary>
    /// 此部分显示的结束时间。
    /// </summary>
    public DateTime? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }
}