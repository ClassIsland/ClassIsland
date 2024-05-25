using System.Windows.Controls;

using ClassIsland.Services.Management;

namespace ClassIsland.Controls;

/// <summary>
/// ManagementInfoControl.xaml 的交互逻辑
/// </summary>
public partial class ManagementInfoControl : UserControl
{
    public ManagementInfoControl()
    {
        InitializeComponent();
    }

    public string ManagementOrganization => App.GetService<ManagementService>().Manifest.OrganizationName;

    public ManagementService ManagementService { get; } = App.GetService<ManagementService>();
}