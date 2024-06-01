namespace ClassIsland.Shared.Interfaces;

public interface INotificationProvider
{
    public string Name { get; set; }
    public string Description { get; set; }

    public Guid ProviderGuid { get; set; }

    public object? SettingsElement { get; set; }
    public object? IconElement { get; set; }

    public static readonly Uri DefaultNotificationSoundUri = new Uri("pack://application:,,,/ClassIsland;component/Assets/Media/Notification/1.wav");
}