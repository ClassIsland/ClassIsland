using System.Runtime.InteropServices;
using System.Windows;
using ClassIsland.Core.Controls.NotificationTemplates;
using ClassIsland.Core.Models.Notification.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Protobuf.WellKnownTypes;
using MaterialDesignThemes.Wpf;

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
    private object? _contentTemplateResourceKey;

    /// <summary>
    /// 提醒内容。
    /// </summary>
    public object? Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    /// <summary>
    /// 提醒内容模板，可选。如果此值不为 null，将在呈现提醒内容 <see cref="Content"/> 时使用。
    /// </summary>
    /// <remarks>
    /// 即使不设置此属性，ContentPresenter 也会根据设置的数据类型选择资源中对应的数据模板进行呈现。
    /// </remarks>
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

    /// <summary>
    /// 代表空内容
    /// </summary>
    public static readonly NotificationContent Empty = new ();

    /// <summary>
    /// 初始化一个 <see cref="NotificationContent"/> 对象
    /// </summary>
    public NotificationContent()
    {
        
    }

    /// <summary>
    /// 初始化一个 <see cref="NotificationContent"/> 对象
    /// </summary>
    /// <param name="content">提醒内容对象</param>
    public NotificationContent(object? content) : this()
    {
        Content = content;
    }

    #region Templates

    /// <summary>
    /// 从模板创建双图标提醒遮罩内容。
    /// </summary>
    /// <param name="text">遮罩文本</param>
    /// <param name="leftIcon">左侧图标</param>
    /// <param name="rightIcon">右侧图标</param>
    /// <param name="hasRightIcon">是否拥有右侧图标</param>
    /// <param name="factory">提醒内容处理工厂</param>
    /// <returns>提醒内容 <see cref="NotificationContent"/> 对象</returns>
    public static NotificationContent CreateTwoIconsMask(string text,
        PackIconKind leftIcon = PackIconKind.AlertCircleOutline, PackIconKind rightIcon = PackIconKind.BellRing, bool hasRightIcon=true,
        Action<NotificationContent>? factory = null)
    {
        var content = new NotificationContent
        {
            Content = new TwoIconsMaskTemplateData()
            {
                LeftIconKind = leftIcon,
                RightIconKind = rightIcon,
                HasRightIcon = hasRightIcon,
                Text = text
            },
            SpeechContent = text,
        };
        factory?.Invoke(content);
        return content;
    }

    /// <summary>
    /// 从模板创建简单文本内容。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="factory">提醒内容处理工厂</param>
    /// <returns>提醒内容 <see cref="NotificationContent"/> 对象</returns>
    public static NotificationContent CreateSimpleTextContent(string text,
        Action<NotificationContent>? factory = null)
    {
        var content = new NotificationContent
        {
            Content = new SimpleTextTemplateData()
            {
                Text = text
            },
            SpeechContent = text,
        };
        factory?.Invoke(content);
        return content;
    }

    /// <summary>
    /// 从模板创建滚动文本内容。
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="repeatCount">滚动重复次数</param>
    /// <param name="factory">提醒内容处理工厂</param>
    /// <param name="duration">提醒显示时长</param>
    /// <returns>提醒内容 <see cref="NotificationContent"/> 对象</returns>
    public static NotificationContent CreateRollingTextContent(string text, TimeSpan? duration=null, int repeatCount=2,
        Action<NotificationContent>? factory = null)
    {
        duration ??= TimeSpan.FromSeconds(20);
        var content = new NotificationContent
        {
            Content = new RollingTextTemplate(new RollingTextTemplateData
            {
                Text = text,
                Duration = duration.Value,
                RepeatCount = repeatCount
            }),
            SpeechContent = text,
            Duration = duration.Value
        };
        factory?.Invoke(content);
        return content;
    }

    #endregion
}