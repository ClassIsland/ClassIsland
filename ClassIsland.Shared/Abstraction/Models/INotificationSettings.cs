namespace ClassIsland.Shared.Abstraction.Models;

/// <summary>
/// 提醒提供方设置接口
/// </summary>
public interface INotificationSettings
{
    /// <summary>
    /// 是否启用提醒
    /// </summary>
    public bool IsNotificationEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 是否启用语音
    /// </summary>
    public bool IsSpeechEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 是否启用提醒效果
    /// </summary>
    public bool IsNotificationEffectEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 是否启用提醒音效
    /// </summary>
    public bool IsNotificationSoundEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 提醒音效路径
    /// </summary>
    public string NotificationSoundPath
    {
        get;
        set;
    }

    /// <summary>
    /// 是否在提醒时置顶主界面
    /// </summary>
    public bool IsNotificationTopmostEnabled
    {
        get;
        set;
    }

    /// <summary>
    /// 是否允许语音在通知结束后继续完成播放而非直接截断
    /// </summary>
    public bool AllowSpeechContinueAfterEnd
    {
        get;
        set;
    }

    /// <summary>
    /// 是否允许提醒音效在通知结束后继续完成播放而非直接截断
    /// </summary>
    public bool AllowSoundContinueAfterEnd
    {
        get;
        set;
    }
}