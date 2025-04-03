using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace ClassIsland.Views.RecoveryPages;

/// <summary>
/// HomePage.xaml 的交互逻辑
/// </summary>
public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void ContinueWithArguments(string[] args)
    {
        AppBase.Current.Stop();
        var path = Environment.ProcessPath;
        var replaced = path!.Replace(".dll", ".exe");
        var startInfo = new ProcessStartInfo(replaced);
        startInfo.ArgumentList.Add("-m");
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }
        Process.Start(startInfo);
    }

    private void ButtonContinue_OnClick(object sender, RoutedEventArgs e)
    {
        ContinueWithArguments([]);
    }

    private void ButtonContinueSafe_OnClick(object sender, RoutedEventArgs e)
    {
        ContinueWithArguments([ "--safe" ]);
    }

    private void ButtonContinueDiagnostic_OnClick(object sender, RoutedEventArgs e)
    {
        ContinueWithArguments(["--diagnostic", "--verbose"]);
    }

    private void ButtonOpenLogFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(App.AppLogFolderPath) ?? "",
            UseShellExecute = true
        });
    }

    private void ButtonCleanTempFiles_OnClick(object sender, RoutedEventArgs e)
    {
        var result = new CommonDialogBuilder()
            .SetCaption("清除临时文件")
            .SetPackIcon(PackIconKind.GarbageCanEmpty)
            .SetContent("此操作将删除所有缓存和临时文件，并且无法恢复。您确定要清除临时文件吗？")
            .AddCancelAction()
            .AddConfirmAction()
            .ShowDialog();
        if (result != 1)
        {
            return;
        }

        try
        {
            if (Directory.Exists(App.AppTempFolderPath))
            {
                Directory.Delete(App.AppTempFolderPath, true);
            }
            if (Directory.Exists(App.AppCacheFolderPath))
            {
                Directory.Delete(App.AppCacheFolderPath, true);
            }
            CommonDialog.ShowInfo($"操作成功完成。");
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法清除临时文件：{exception.Message}");
        }
    }

    private void ButtonResetSettings_OnClick(object sender, RoutedEventArgs e)
    {
        var result = new CommonDialogBuilder()
            .SetCaption("重置应用设置")
            .SetPackIcon(PackIconKind.SettingsRefreshOutline)
            .SetContent("此操作将重置应用的设置信息，并且无法恢复，不影响档案、组件、自动化、插件等的配置。您确定要重置应用设置吗？\n\n如果您确实希望重置应用设置，请在下方文本框输入 ⌈我确认重置应用设置⌋。")
            .AddCancelAction()
            .AddConfirmAction()
            .HasInput(true)
            .ShowDialog(out var confirmation);
        if (result != 1 || confirmation != "我确认重置应用设置")
        {
            return;
        }

        try
        {
            if (File.Exists(Path.Combine(App.AppRootFolderPath, "Settings.json")))
            {
                File.Delete(Path.Combine(App.AppRootFolderPath, "Settings.json"));
            }
            if (File.Exists(Path.Combine(App.AppRootFolderPath, "Settings.json.bak")))
            {
                File.Delete(Path.Combine(App.AppRootFolderPath, "Settings.json.bak"));
            }
            CommonDialog.ShowInfo($"操作成功完成。");
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法重置应用设置：{exception.Message}");
        }
    }

    private void ButtonResetConfigs_OnClick(object sender, RoutedEventArgs e)
    {
        var result = new CommonDialogBuilder()
            .SetCaption("重置全部配置")
            .SetPackIcon(PackIconKind.FileRemoveOutline)
            .SetContent("此操作将重置应用的所有配置，包括应用设置、组件配置、自动化配置、插件设置等，并且无法恢复，不影响档案配置。您确定要重置全部配置吗？\n\n如果您确实希望重置全部配置，请在下方文本框输入 ⌈我确认重置全部配置⌋。")
            .AddCancelAction()
            .AddConfirmAction()
            .HasInput(true)
            .ShowDialog(out var confirmation);
        if (result != 1 || confirmation != "我确认重置全部配置")
        {
            return;
        }

        try
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
            
            CommonDialog.ShowInfo($"操作成功完成。");
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法重置应用设置：{exception.Message}");
        }
    }

    private void ButtonResetAll_OnClick(object sender, RoutedEventArgs e)
    {
        var result = new CommonDialogBuilder()
            .SetCaption("重置全部信息")
            .SetPackIcon(PackIconKind.Remove)
            .SetContent("此操作将重置应用的所有信息，恢复到安装前的状态，包括档案配置、应用设置、组件配置、自动化配置、插件设置、已安装的插件等，并且无法恢复。您确定要重置全部信息吗？\n\n如果您确实希望重置全部信息，请在下方文本框输入 ⌈我确认重置包括课表配置在内的全部信息⌋。")
            .AddCancelAction()
            .AddConfirmAction()
            .HasInput(true)
            .ShowDialog(out var confirmation);
        if (result != 1 || confirmation != "我确认重置包括课表配置在内的全部信息")
        {
            return;
        }

        try
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
            if (Directory.Exists(PluginService.PluginsRootPath))
            {
                Directory.Delete(PluginService.PluginsRootPath, true);
            }

            CommonDialog.ShowInfo($"操作成功完成。");
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"无法重置应用设置：{exception.Message}");
        }
    }

    private void ButtonRecoverBackup_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationService?.Navigate(new RecoverBackupPage());
    }
}