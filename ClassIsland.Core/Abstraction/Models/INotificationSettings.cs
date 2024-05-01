namespace ClassIsland.Core.Abstraction.Models;

public interface INotificationSettings
{
    public bool IsNotificationEnabled
    {
        get;
        set;
    }

    public bool IsSpeechEnabled
    {
        get;
        set;
    }

    public bool IsNotificationEffectEnabled
    {
        get;
        set;
    }

    public bool IsNotificationSoundEnabled
    {
        get;
        set;
    }

    public string NotificationSoundPath
    {
        get;
        set;
    }

    public bool IsNotificationTopmostEnabled
    {
        get;
        set;
    }
}