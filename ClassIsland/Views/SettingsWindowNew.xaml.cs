using ClassIsland.Core.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : MyWindow
{
    public SettingsNewViewModel ViewModel { get; } = new();

    [NotNull]
    public NavigationService? NavigationService { get; set; }

    public SettingsWindowNew()
    {
        InitializeComponent();
        DataContext = this;
        NavigationService = NavigationFrame.NavigationService;
        NavigationService.LoadCompleted += NavigationServiceOnLoadCompleted;
    }

    private void NavigationServiceOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        NavigationService.RemoveBackEntry();
    }


    private void NavigationListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var page = IAppHost.Host?.Services.GetKeyedService<SettingsPageBase>(ViewModel.SelectedPageInfo?.Id);
        NavigationService.RemoveBackEntry();
        NavigationService.Navigate(page);
        //ViewModel.FrameContent;
        
    }
}