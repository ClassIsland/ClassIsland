using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Management;
using ClassIsland.ViewModels;

namespace ClassIsland.Controls;

/// <summary>
/// JoinManagementDialog.xaml 的交互逻辑
/// </summary>
public partial class JoinManagementDialog : MyWindow
{
    public static List<FilePickerFileType> ManagementConfigFileTypes { get; } =
    [
        new FilePickerFileType("配置文件")
        {
            Patterns =["*.json"]
        }
    ];
    
    public JoinManagementViewModel ViewModel { get; } = new();

    public IManagementService ManagementService { get; } = IAppHost.GetService<IManagementService>();

    public JoinManagementDialog()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        if (File.Exists(Services.Management.ManagementService.ManagementPresetPath))
        {
            ViewModel.ConfigFilePath = Services.Management.ManagementService.ManagementPresetPath;
            LoadManagementSettings();
        }
        base.OnInitialized();
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