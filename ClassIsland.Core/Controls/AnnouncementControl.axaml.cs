using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services.Metadata;
using ClassIsland.Core.Commands;
using ClassIsland.Core.Enums.Metadata.Announcement;
using ClassIsland.Core.Models.Metadata.Announcement;
using ClassIsland.Shared;
using FluentAvalonia.UI.Controls;

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

    public static FuncValueConverter<Severity, FAInfoBarSeverity> AnnouncementSeverityToInfoBarSeverityConverter =
        new (x => x switch
        {
            Severity.Announcement => FAInfoBarSeverity.Informational,
            Severity.Info => FAInfoBarSeverity.Informational,
            Severity.Important => FAInfoBarSeverity.Warning,
            Severity.Warning => FAInfoBarSeverity.Warning,
            Severity.Critical => FAInfoBarSeverity.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
        });

    private void ButtonRead_OnClick(FAInfoBar infoBar, EventArgs args)
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

    private void ButtonDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        UriNavigationCommands.UriNavigationCommand.Execute(Announcement.DetailsUri);
    }
}
