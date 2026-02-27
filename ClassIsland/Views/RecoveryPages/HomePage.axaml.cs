using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Services;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Path = System.IO.Path;

namespace ClassIsland.Views.RecoveryPages;

/// <summary>
/// HomePage.xaml 的交互逻辑
/// </summary>
public partial class HomePage : UserControl
{
    public Frame? MainFrame { get; init; }

    public HomePage()
    {
        InitializeComponent();
        if (AppBase.Current.PackagingType is "folder" or "folderClassic")
        {
            List<string> validInstallations;
            try
            {
                validInstallations = Directory.GetDirectories(CommonDirectories.AppPackageRoot)
                    .Where(dir =>
                        Path.GetFileName(dir).StartsWith("app", StringComparison.OrdinalIgnoreCase) &&
                        !File.Exists(Path.Combine(dir, ".destroy")) &&
                        !File.Exists(Path.Combine(dir, ".partial")) &&
                        File.Exists(Path.Combine(dir, "ClassIsland.Desktop" + AppBase.PlatformExecutableExtension)))
                    .ToList();
            }
            catch (Exception ex)
            {
                this.ShowErrorToast($"无法检测其他安装实例的状态:{ex.Message}" + Environment.NewLine + "\"回滚\"选项将不可用。");
                validInstallations = new List<string>();
            }

            ButtonRollBack.IsEnabled = validInstallations.Count > 1;
        }
    }

    private void ButtonContinue_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart(["-m"]);
    }

    private void ButtonContinueSafe_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart(["-m", "--safe"]);
    }

    private void ButtonContinueDiagnostic_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart(["-m", "--diagnostic", "--verbose"]);
    }

    private void ButtonOpenLogFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(CommonDirectories.AppLogFolderPath) ?? "",
            UseShellExecute = true
        });
    }

    private async void ButtonCleanTempFiles_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await ContentDialogHelper.ShowConfirmationDialog("清除临时文件", 
            "此操作将删除所有缓存和临时文件，并且无法恢复。您确定要清除临时文件吗？",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        try
        {
            if (Directory.Exists(CommonDirectories.AppTempFolderPath))
            {
                Directory.Delete(CommonDirectories.AppTempFolderPath, true);
            }
            if (Directory.Exists(CommonDirectories.AppCacheFolderPath))
            {
                Directory.Delete(CommonDirectories.AppCacheFolderPath, true);
            }
            this.ShowSuccessToast($"操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法清除临时文件", exception);
        }
    }

    private async void ButtonResetSettings_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await ContentDialogHelper.ShowConfirmationDialog("重置应用设置",
            "此操作将重置应用的设置信息，并且无法恢复，不影响档案、组件、自动化、插件等的配置。您确定要重置应用设置吗？"+Environment.NewLine+Environment.NewLine+"如果您确实希望重置应用设置，请在下方文本框输入 ⌈我确认重置应用设置⌋。",
            "我确认重置应用设置",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        try
        {
            if (File.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json")))
            {
                File.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"));
            }
            if (File.Exists(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json.bak")))
            {
                File.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json.bak"));
            }
            this.ShowSuccessToast($"操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法重置应用设置", exception);
        }
    }

    private async void ButtonResetConfigs_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await ContentDialogHelper.ShowConfirmationDialog("重置全部配置",
            "此操作将重置应用的除档案外的全部设置，包括组件、自动化等配置。您确定要重置全部配置吗？"+Environment.NewLine+Environment.NewLine+"如果您确实希望重置全部配置，请在下方文本框输入 ⌈我确认重置除课表信息外全部配置⌋。",
            "我确认重置除课表信息外全部配置",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        try
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
            
            this.ShowSuccessToast($"操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法重置应用设置", exception);
        }
    }

    private async void ButtonResetAll_OnClick(object sender, RoutedEventArgs e)
    {
        var result = await ContentDialogHelper.ShowConfirmationDialog("重置全部信息",
            "此操作将重置应用的所有信息，恢复到安装前的状态，包括档案配置、应用设置、组件配置、自动化配置、插件设置、已安装的插件等，并且无法恢复。您确定要重置全部信息吗？"+Environment.NewLine+Environment.NewLine+"如果您确实希望重置全部信息，请在下方文本框输入 ⌈我确认重置包括课表配置在内的全部信息⌋。",
            "我确认重置包括课表配置在内的全部信息",
            root: TopLevel.GetTopLevel(this));
        if (!result)
        {
            return;
        }

        try
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
            if (Directory.Exists(PluginService.PluginsRootPath))
            {
                Directory.Delete(PluginService.PluginsRootPath, true);
            }

            this.ShowSuccessToast($"操作成功完成。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法重置应用设置", exception);
        }
    }

    private void ButtonRecoverBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (MainFrame != null)
        {
            MainFrame.Content = new RecoverBackupPage()
            {
                MainFrame = MainFrame,
                LastPage = this
            };
        }
    }

    private void ButtonRollBack_Onclick(object sender, RoutedEventArgs e)
    {
        //TODO:实现回滚功能(版本管理器)
        this.ShowToast("此功能仍在开发中");
    }
}