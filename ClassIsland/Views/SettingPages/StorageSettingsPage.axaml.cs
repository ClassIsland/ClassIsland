using System;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// StorageSettingsPage.xaml 的交互逻辑
/// </summary>
[Group("classisland.general")]
[SettingsPageInfo("storage", "存储", "\ue6b7", "\ue6b6", SettingsPageCategory.Internal)]
public partial class StorageSettingsPage : SettingsPageBase
{
    public StorageSettingsViewModel ViewModel { get; } = IAppHost.GetService<StorageSettingsViewModel>();

    public ILogger<StorageSettingsPage> Logger => ViewModel.Logger;

    public StorageSettingsPage()
    {
        ViewModel.SettingsService.Settings.BackupFilesSize = Helpers.StorageSizeHelper.FormatSize(Helpers.StorageSizeHelper.GetFolderStorageSize(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups/")));
        DataContext = this;
        InitializeComponent();
        IosImportedFilesSettings.IsVisible = PlatformHelper.IsAppleMobile;
    }

    private async void ButtonCreateBackup_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsBackupFinished = false;
        ViewModel.IsBackingUp = true;
        try
        {
            await FileFolderService.CreateBackupAsync();
            ViewModel.IsBackupFinished = true;
            this.ShowSuccessToast("备份成功。");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法创建备份。");
            this.ShowErrorToast("无法创建备份", exception);
        }
        ViewModel.IsBackingUp = false;
    }

    private async void ButtonViewBackupFiles_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await PlatformServices.LauncherService.LaunchPath(
                Path.GetFullPath(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups")));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法浏览备份文件。");
            this.ShowErrorToast($"无法浏览备份文件", exception);
        }
    }

    private async void ButtonRecoverBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }
        AppBase.Current.Restart(["-m", "-r"]);
    }

    private async void ButtonViewImportedFiles_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(CommonDirectories.AppImportedFilesFolderPath);
            await PlatformServices.LauncherService.LaunchPath(
                CommonDirectories.AppImportedFilesFolderPath);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法浏览 iOS 导入文件副本。");
            this.ShowErrorToast("无法浏览导入文件副本", exception);
        }
    }

    private async void ButtonClearImportedFiles_OnClick(object sender, RoutedEventArgs e)
    {
        var confirmed = await ContentDialogHelper.ShowConfirmationDialog(
            "清空导入文件副本",
            "清理后，仍引用这些副本的自定义图片、音频和尚未完成的导入操作将无法继续使用。请确认当前没有相关操作或设置后再清理。",
            root: TopLevel.GetTopLevel(this));
        if (!confirmed)
        {
            return;
        }

        try
        {
            if (Directory.Exists(CommonDirectories.AppImportedFilesFolderPath))
            {
                Directory.Delete(CommonDirectories.AppImportedFilesFolderPath, true);
            }

            Directory.CreateDirectory(CommonDirectories.AppImportedFilesFolderPath);
            this.ShowSuccessToast("已清空导入文件副本。");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "无法清空 iOS 导入文件副本。");
            this.ShowErrorToast("无法清空导入文件副本", exception);
        }
    }
}
