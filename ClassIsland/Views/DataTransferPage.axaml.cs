using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class DataTransferPage : UserControl
{
    public DataTransferViewModel ViewModel { get; } = IAppHost.GetService<DataTransferViewModel>();
    
    public DataTransferPage()
    {
        InitializeComponent();
    }

    private async void SettingsExpanderImportFromClassIsland1_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.BrowseAction = BrowseClassIslandData;
        ViewModel.PerformImportAction = BeginPerformClassIslandImport;
        ViewModel.PageIndex = 1;
        ViewModel.ImportSourcePath = "";
        ViewModel.ImportDescription = "支持从 1.x 版本的 ClassIsland 导入课表、组件配置、自动化配置、应用设置、部分插件和主题等数据。";
    }
    
    private async void SettingsExpanderBrowse_OnClick(object? sender, RoutedEventArgs e)
    {
        
        if (ViewModel.BrowseAction == null || ViewModel.PerformImportAction == null)
        {
            return;
        }
        await ViewModel.BrowseAction();
        if (string.IsNullOrWhiteSpace(ViewModel.ImportSourcePath))
        {
            return;
        }

        await ViewModel.PerformImportAction();
    }
    
    private void ButtonTurnBackFromPage1_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.PageIndex = 0;
    }

    #region ClassIsland

    private async Task BrowseClassIslandData()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "选择先前版本的 ClassIsland 实例",
            FileTypeFilter =
            [
                new FilePickerFileType("ClassIsland 可执行文件")
                {
                    Patterns = ["ClassIsland.exe"]
                }
            ]
        });
        if (file.Count <= 0)
        {
            return;
        }

        ViewModel.ImportSourcePath = Path.GetDirectoryName(file[0].Path.AbsolutePath) ?? "";
    }

    private async Task BeginPerformClassIslandImport()
    {
        var r = await new ContentDialog()
        {
            Title = "重启以继续",
            Content = "应用需要重启以继续导入操作，要重启以继续导入吗？",
            PrimaryButtonText = "重启并继续",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync(TopLevel.GetTopLevel(this));
        if (r != ContentDialogResult.Primary)
        {
            return;
        }
        AppBase.Current.Restart(["--importV1", ViewModel.ImportSourcePath, "-m"]);
    }

    [Obsolete]
    public async Task PerformClassIslandImport(string root)
    {
        try
        {
            ViewModel.PageIndex = 2;
            await Task.Run(() =>
            {
                Directory.Delete(Path.Combine(CommonDirectories.AppRootFolderPath, "Plugins"), true);
                FileFolderService.CopyFolder(Path.Combine(root, "Profiles"),
                    Path.Combine(CommonDirectories.AppRootFolderPath, "Profiles"), true);
                FileFolderService.CopyFolder(Path.Combine(root, "Config"),
                    Path.Combine(CommonDirectories.AppRootFolderPath, "Config"), true);
                FileFolderService.CopyFolder(Path.Combine(root, "Plugins"),
                    Path.Combine(CommonDirectories.AppRootFolderPath, "Plugins"), true);

                var settings = ConfigureFileHelper.LoadConfigUnWrapped<Settings>(Path.Combine(root, "Settings.json"), false);
                settings.MainWindowFont = MainWindow.DefaultFontFamilyKey;
                settings.AutoInstallUpdateNextStartup = false;
                settings.ShowEchoCaveWhenSettingsPageLoading = false;
                if (settings.WeatherIconId == "classisland.weatherIcons.materialDesign")
                {
                    settings.WeatherIconId = "classisland.weatherIcons.fluentDesign";
                }
                ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppRootFolderPath, "Settings.json"), settings);

                if (!Directory.Exists(ComponentsService.ComponentSettingsPath))
                {
                    Directory.CreateDirectory(ComponentsService.ComponentSettingsPath);
                }
                foreach (var path in Directory.GetFiles(Path.Combine(CommonDirectories.AppConfigPath, "Islands")))
                {
                    var oldConfig =
                        ConfigureFileHelper.LoadConfigUnWrapped<ObservableCollection<ComponentSettings>>(path, false);
                    var newConfig = new ComponentProfile();
                    var e = oldConfig.OrderBy(x => x.RelativeLineNumber)
                        .GroupBy(x => x.RelativeLineNumber)
                        .Select(x => new MainWindowLineSettings()
                        {
                            IsMainLine = x.Key == 0,
                            Children = new ObservableCollection<ComponentSettings>(x)
                        });
                    newConfig.Lines = new ObservableCollection<MainWindowLineSettings>(e);
                    ConfigureFileHelper.SaveConfig(Path.Combine(ComponentsService.ComponentSettingsPath, Path.GetFileName(path)), newConfig);
                }
                foreach (var path in Directory.GetFiles(ProfileService.ProfilePath))
                {
                    var config = ConfigureFileHelper.LoadConfigUnWrapped<Profile>(path, false);
                    foreach (var tl in config.TimeLayouts)
                    {
                        foreach (var layoutItem in tl.Value.Layouts.Where(x =>
                                     !string.IsNullOrWhiteSpace(x.StartSecond) && !string.IsNullOrWhiteSpace(x.EndSecond)))
                        {
                            layoutItem.StartTime = DateTime.TryParse(layoutItem.StartSecond, out var r1)
                                ? r1.TimeOfDay
                                : TimeSpan.Zero;
                            layoutItem.EndTime = DateTime.TryParse(layoutItem.EndSecond, out var r2)
                                ? r2.TimeOfDay
                                : TimeSpan.Zero;
                        }
                    }
                    ConfigureFileHelper.SaveConfig(Path.Combine(ProfileService.ProfilePath, Path.GetFileName(path)), config);
                }
            });
            
            ViewModel.PageIndex = 3;
        }
        catch (Exception e)
        {
            this.ShowErrorToast("导入时发生意外错误", e);
        }

    }

    #endregion

    private void ButtonFinish_OnClick(object? sender, RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
}