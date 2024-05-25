using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using ClassIsland.Core;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Management;
using ClassIsland.Services.Management;
using ClassIsland.ViewModels;

namespace ClassIsland.Controls;

/// <summary>
/// JoinManagementDialog.xaml 的交互逻辑
/// </summary>
public partial class JoinManagementDialog : UserControl
{
    public JoinManagementViewModel ViewModel { get; } = new();

    public ManagementService ManagementService { get; } = IAppHost.GetService<ManagementService>();

    public JoinManagementDialog()
    {
        InitializeComponent();
        
    }

    protected override void OnInitialized(EventArgs e)
    {
        if (File.Exists(ManagementService.ManagementPresetPath))
        {
            ViewModel.ConfigFilePath = ManagementService.ManagementPresetPath;
            LoadManagementSettings();
        }
        base.OnInitialized(e);
    }

    private void FileBrowserButton_OnFileSelected(object? sender, EventArgs e)
    {
        LoadManagementSettings();
    }

    private void LoadManagementSettings()
    {
        try
        {
            ViewModel.ManagementSettings = ConfigureFileHelper.LoadConfig<ManagementSettings>(ViewModel.ConfigFilePath);
            ViewModel.IsConfigLoaded = true;
        }
        catch (Exception exception)
        {
            ViewModel.ErrorMessage = exception.Message;
            ViewModel.IsErrorMessageOpen = true;
        }
    }

    private async void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        await JoinManagement();
    }

    private async Task JoinManagement()
    {
        ViewModel.IsWorking = true;
        try
        { 
            await ManagementService.JoinManagementAsync(ViewModel.ManagementSettings);
        }
        catch (Exception exception)
        {
            ViewModel.ErrorMessage = exception.Message;
            ViewModel.IsErrorMessageOpen = true;
        }
        ViewModel.IsWorking = false;
    }
}