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
        if (ManagementService.Connection is Services.Management.BashuPlatformConnection bashuConn &&
            !string.IsNullOrWhiteSpace(bashuConn.Settings.BashuDeviceToken))
        {
            ViewModel.IsAlreadyPaired = true;
            ViewModel.ConnectedClassName = !string.IsNullOrWhiteSpace(bashuConn.Settings.BashuClassName)
                ? bashuConn.Settings.BashuClassName
                : bashuConn.Settings.ClassIdentity;
            ViewModel.BashuServerUrl = string.IsNullOrWhiteSpace(bashuConn.Settings.BashuServerUrl)
                ? "https://bashu.cqaibase.cn"
                : bashuConn.Settings.BashuServerUrl;
        }
        else if (File.Exists(Services.Management.ManagementService.ManagementPresetPath))
        {
            ViewModel.ConfigFilePath = Services.Management.ManagementService.ManagementPresetPath;
            LoadManagementSettings();
        }
        base.OnInitialized();
    }

    private async void ButtonUnpair_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ManagementService.ExitManagementAsync();
        }
        catch (Exception ex) { PlatformStatus.Text = ex.Message; }
    }

    private async void ButtonSync_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWorking = true;
        try
        {
            var service = IAppHost.GetService<Services.Management.BashuPlatformService>();
            await service.PollOnceAsync(true);
            PlatformStatus.Text = $"{service.Status} · 课表同步：{service.LastSync}";
        }
        finally { ViewModel.IsWorking = false; }
    }

    private void ButtonAudioTest_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<ClassIsland.Core.Abstractions.Services.SpeechService.ISpeechService>()
            .EnqueueSpeechQueue("两江巴蜀平台，语音测试。请确认扬声器音量正常。");
        PlatformStatus.Text = "已发起语音测试；未听到声音时请检查系统输出设备与语音音量。";
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
            if (ViewModel.IsBashuMode)
            {
                ViewModel.ManagementSettings.ManagementServerKind = ClassIsland.Shared.Enums.ManagementServerKind.BashuPlatform;
                ViewModel.ManagementSettings.BashuServerUrl = ViewModel.BashuServerUrl;
                ViewModel.ManagementSettings.BashuPairingCode = ViewModel.BashuPairingCode;
                ViewModel.ManagementSettings.BashuDeviceName = ViewModel.BashuDeviceName;
            }
            await ManagementService.JoinManagementAsync(ViewModel.ManagementSettings);
            Close();
        }
        catch (Exception exception)
        {
            ViewModel.ErrorMessage = exception.Message;
            ViewModel.IsErrorMessageOpen = true;
        }
        ViewModel.IsWorking = false;
    }
}
