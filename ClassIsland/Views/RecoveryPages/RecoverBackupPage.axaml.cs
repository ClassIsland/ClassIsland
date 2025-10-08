using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Services;
using ClassIsland.ViewModels.RecoveryPages;
using FluentAvalonia.UI.Controls;
using Path = System.IO.Path;

namespace ClassIsland.Views.RecoveryPages;

/// <summary>
/// RecoverBackupPage.xaml 的交互逻辑
/// </summary>
public partial class RecoverBackupPage : UserControl
{
    public Frame? MainFrame { get; init; }
    
    public UserControl? LastPage { get; init; }
    
    public RecoverBackupViewModel ViewModel { get; } = new();
    public RecoverBackupPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void RecoverBackupPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups")))
        {
            ViewModel.Backups =
                new ObservableCollection<string>(
                    Directory.GetFiles(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups")).OrderByDescending(Directory.GetLastWriteTime).Select(Path.GetFileName).Concat(
                        Directory.GetDirectories(Path.Combine(CommonDirectories.AppRootFolderPath, "Backups")).OrderByDescending(Directory.GetLastWriteTime).Select(Path.GetFileName))!); 
        }
    }

    private async Task RecoverBackupAsync(string backupPath)
    {
        if (ViewModel.RecoverMode == 1)
        {
            if (File.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json")))
            {
                File.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"));
            }
            if (File.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json.bak")))
            {
                File.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json.bak"));
            }
            if (Directory.Exists(CommonDirectories.AppConfigPath))
            {
                Directory.Delete(CommonDirectories.AppConfigPath, true);
            }
            if (Directory.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles")))
            {
                Directory.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles"), true);
            }
        }

        await Task.Run(() =>
        {
            if(Path.GetExtension(backupPath)==".zip"){
                ZipFile.ExtractToDirectory(backupPath, CommonDirectories.AppRootFolderPath, true);
            }
            if(Directory.Exists(backupPath)){
                FileFolderService.CopyFolder(backupPath, CommonDirectories.AppRootFolderPath, true);
            }
        });
    }

    private async void ButtonRecover_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackupName == null)
        {
            return;
        }

        var result = await ContentDialogHelper.ShowConfirmationDialog("恢复备份", 
            $"您确定要把应用配置恢复到备份 {ViewModel.SelectedBackupName} 的状态吗？此操作无法撤销。",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        var backupPath = Path.Combine(CommonDirectories.AppRootFolderPath, "Backups", ViewModel.SelectedBackupName);

        try
        {
            ViewModel.IsWorking = true;
            await RecoverBackupAsync(backupPath);
            ViewModel.IsWorking = false;
            this.ShowSuccessToast($"操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法恢复备份", exception);
        }
    }

    private void ButtonGoBack_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MainFrame != null)
        {
            MainFrame.Content = LastPage;
        }
    }
}