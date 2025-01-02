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
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ManagementCredentialsSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("management.credentials", "集控凭据设置", true, SettingsPageCategory.About)]
public partial class ManagementCredentialsSettingsPage
{
    public ManagementCredentialsSettingsViewModel ViewModel { get; } = new();

    public IManagementService ManagementService { get; }

    public ManagementCredentialsSettingsPage(IManagementService managementService)
    {
        ManagementService = managementService;
        DataContext = this;
        InitializeComponent();
    }
}