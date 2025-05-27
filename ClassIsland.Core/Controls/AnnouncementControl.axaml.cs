using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Enums.Metadata.Announcement;
using ClassIsland.Core.Models.Metadata.Announcement;
using ClassIsland.Shared;

namespace ClassIsland.Core.Controls;

/// <summary>
/// AnnouncementControl.xaml 的交互逻辑
/// </summary>
public partial class AnnouncementControl : UserControl
{
    public static readonly StyledProperty<Announcement> AnnouncementProperty = AvaloniaProperty.Register<AnnouncementControl, Announcement>(
        nameof(Announcement));

    public AnnouncementControl()
    {
        InitializeComponent();
    }

    public Announcement Announcement
    {
        get => GetValue(AnnouncementProperty);
        set => SetValue(AnnouncementProperty, value);
    }

    private void ButtonRead_OnClick(object sender, RoutedEventArgs e)
    {
        var service = IAppHost.GetService<IAnnouncementService>();
        var readStateStorage = Announcement.ReadStateStorageScope switch
        {
            ReadStateStorageScope.Local => service.ReadAnnouncementsLocal,
            ReadStateStorageScope.Machine => service.ReadAnnouncementsMachine,
            _ => throw new ArgumentOutOfRangeException()
        };
        readStateStorage.Add(Announcement.Guid);
        
    }
}
