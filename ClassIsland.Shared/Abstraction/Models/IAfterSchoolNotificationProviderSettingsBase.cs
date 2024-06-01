namespace ClassIsland.Shared.Abstraction.Models;

public interface IAfterSchoolNotificationProviderSettingsBase
{
    public bool IsEnabled
    {
        get;
        set;
    }

    public string NotificationMsg
    {
        get;
        set;
    }
}