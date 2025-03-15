using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Services;
using ClassIsland.ViewModels.RecoveryPages;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace ClassIsland.Views.RecoveryPages;

/// <summary>
/// RecoverBackupPage.xaml 的交互逻辑
/// </summary>
public partial class RecoverBackupPage : Page
{
    public RecoverBackupViewModel ViewModel { get; } = new();
    public RecoverBackupPage()
    {
        InitializeComponent();
    }

    private void RecoverBackupPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(Path.Combine(App.AppRootFolderPath, "Backups")))
        {
            ViewModel.Backups =
                new ObservableCollection<string>(
                    Directory.GetDirectories(Path.Combine(App.AppRootFolderPath, "Backups")).OrderByDescending(Directory.GetLastWriteTime).Select(Path.GetFileName)!);
        }
    }

    private async Task RecoverBackupAsync(string backupRoot)
    {
        if (ViewModel.RecoverMode == 1)
        {
            if (File.Exists(Path.Combine(App.AppRootFolderPath, "Settings.json")))
            {
                File.Delete(Path.Combine(App.AppRootFolderPath, "Settings.json"));
            }
            if (File.Exists(Path.Combine(App.AppRootFolderPath, "Settings.json.bak")))
            {
                File.Delete(Path.Combine(App.AppRootFolderPath, "Settings.json.bak"));
            }
            if (Directory.Exists(App.AppConfigPath))
            {
                Directory.Delete(App.AppConfigPath, true);
            }
            if (Directory.Exists(Path.Combine(App.AppRootFolderPath, "Profiles")))
            {
                Directory.Delete(Path.Combine(App.AppRootFolderPath, "Profiles"), true);
            }
        }

        await Task.Run(() =>
        {
            FileFolderService.CopyFolder(backupRoot, App.AppRootFolderPath, true);
        });
    }

    private async void ButtonRecover_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackupName == null)
        {
            return;
        }

        var result = new CommonDialogBuilder()
            .SetCaption("恢复备份")
            .SetPackIcon(PackIconKind.Restore)
            .SetContent($"您确定要把应用配置恢复到备份 {ViewModel.SelectedBackupName} 的状态吗？此操作无法撤销。")
            .AddCancelAction()
            .AddConfirmAction()
            .ShowDialog();
        if (result != 1)
        {
            return;
        }

        var backupRoot = Path.Combine(App.AppRootFolderPath, "Backups", ViewModel.SelectedBackupName);

        try
        {
            ViewModel.IsWorking = true;
            await RecoverBackupAsync(backupRoot);
            ViewModel.IsWorking = false;
            CommonDialog.ShowInfo($"操作成功完成。");
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法恢复备份：{exception.Message}");
        }
    }
}