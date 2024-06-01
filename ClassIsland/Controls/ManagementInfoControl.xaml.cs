using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
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

    public string ManagementOrganization => App.GetService<IManagementService>().Manifest.OrganizationName;

    public IManagementService ManagementService { get; } = App.GetService<IManagementService>();
}