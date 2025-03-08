using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
    public static readonly DependencyProperty AnnouncementProperty = DependencyProperty.Register(
        nameof(Announcement), typeof(Announcement), typeof(AnnouncementControl), new PropertyMetadata(default(Announcement)));

    public Announcement Announcement
    {
        get { return (Announcement)GetValue(AnnouncementProperty); }
        set { SetValue(AnnouncementProperty, value); }
    }
    
    public AnnouncementControl()
    {
        InitializeComponent();
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