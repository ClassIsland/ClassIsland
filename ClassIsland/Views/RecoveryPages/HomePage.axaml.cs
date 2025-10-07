using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Services;
using FluentAvalonia.UI.Controls;
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
            "此操作将重置应用的设置信息，并且无法恢复，不影响档案、组件、自动化、插件等的配置。您确定要重置应用设置吗？\n\n如果您确实希望重置应用设置，请在下方文本框输入 ⌈我确认重置应用设置⌋。",
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
            "此操作将重置应用的除档案外的全部设置，包括组件、自动化等配置。您确定要重置全部配置吗？\n\n如果您确实希望重置全部配置，请在下方文本框输入 ⌈我确认重置除课表信息外全部配置⌋。",
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
            "此操作将重置应用的所有信息，恢复到安装前的状态，包括档案配置、应用设置、组件配置、自动化配置、插件设置、已安装的插件等，并且无法恢复。您确定要重置全部信息吗？\n\n如果您确实希望重置全部信息，请在下方文本框输入 ⌈我确认重置包括课表配置在内的全部信息⌋。",
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
}