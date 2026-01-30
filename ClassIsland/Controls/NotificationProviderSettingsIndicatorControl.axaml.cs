using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Models;
using ClassIsland.Shared.Abstraction.Models;
using ClassIsland.Shared.Models.Notification;

namespace ClassIsland.Controls;

public partial class NotificationProviderSettingsIndicatorControl : UserControl
{
    public static readonly StyledProperty<INotificationSettings> SettingsProperty = AvaloniaProperty.Register<NotificationProviderSettingsIndicatorControl, INotificationSettings>(
        nameof(Settings));

    public INotificationSettings Settings
    {
        get => GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    public static readonly StyledProperty<Settings> GlobalSettingsProperty = AvaloniaProperty.Register<NotificationProviderSettingsIndicatorControl, Settings>(
        nameof(GlobalSettings));

    public Settings GlobalSettings
    {
        get => GetValue(GlobalSettingsProperty);
        set => SetValue(GlobalSettingsProperty, value);
    }

    public static readonly StyledProperty<bool> IsSettingsEnabledProperty = AvaloniaProperty.Register<NotificationProviderSettingsIndicatorControl, bool>(
        nameof(IsSettingsEnabled));

    public bool IsSettingsEnabled
    {
        get => GetValue(IsSettingsEnabledProperty);
        set => SetValue(IsSettingsEnabledProperty, value);
    }
    
    public NotificationProviderSettingsIndicatorControl()
    {
        InitializeComponent();
    }
}