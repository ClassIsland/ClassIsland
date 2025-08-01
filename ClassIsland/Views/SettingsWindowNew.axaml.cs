using ClassIsland.Core.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClassIsland.Services;
using Sentry;
using System.IO;
using ClassIsland.Controls;
using Path = System.IO.Path;
using System.Web;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.SettingsWindow;
using ClassIsland.Helpers;
using System.Transactions;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Platforms.Abstraction;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Data;
using FluentAvalonia.UI.Navigation;
using FluentAvalonia.UI.Windowing;
using ReactiveUI;
using Control = Avalonia.Controls.Control;
using SaveFileDialog = Avalonia.Controls.SaveFileDialog;
using TaskDialog = FluentAvalonia.UI.Controls.TaskDialog;
using TaskDialogButton = FluentAvalonia.UI.Controls.TaskDialogButton;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : MyWindow, INavigationPageFactory
{
    private const string KeepHistoryParameterName = "ci_keepHistory";
    private const string ErrorPageId = "_error";

    public SettingsNewViewModel ViewModel { get; } = new();

    private bool IsOpened { get; set; } = false;

    public IManagementService ManagementService { get; }

    private IHangService HangService { get; }

    private ILogger<SettingsWindowNew> Logger;

    private DiagnosticService DiagnosticService { get; }

    public SettingsService SettingsService { get; }

    public static readonly string StartupSettingsPage = "automation";

    private IComponentsService ComponentsService { get; }

    private string LaunchSettingsPage { get; set; } = StartupSettingsPage;

    private bool _isFirstNavigated = false;

    private readonly Dictionary<string, SettingsPageBase?> _cachedPages = new();
    
    public static readonly FuncValueConverter<object?, double> ControlToWidthConverter = new(x =>
    {
        if (x is Control control)
        {
            return control.Margin.Left + control.Width + control.Margin.Right;
        }

        return 0;
    });

    public SettingsWindowNew(IManagementService managementService, IHangService hangService,
        ILogger<SettingsWindowNew> logger, DiagnosticService diagnosticService, SettingsService settingsService,
        IComponentsService componentsService, IUriNavigationService uriNavigationService)
    {
        Logger = logger;
        DataContext = this;
        ManagementService = managementService;
        ComponentsService = componentsService;
        DiagnosticService = diagnosticService;
        HangService = hangService;
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        InitializeComponent();
        // SplashScreen = new EmptySplashScreen();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        NavigationFrame.NavigationPageFactory = this;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;

        if (ManagementService.Policy.DisableSettingsEditing)
        {
            LaunchSettingsPage = "about";
        }

        BuildNavigationMenuItems();
        SettingsService.Settings
            .ObservableForProperty(x => x.IsDebugOptionsEnabled)
            .Subscribe(_ => BuildNavigationMenuItems());
    }

    private void BuildNavigationMenuItems()
    {
        NavigationView.MenuItems.Clear();

        var infos = SettingsWindowRegistryService.Registered
            .Where(x => !x.HideDefault)
            .Where(x => !(ManagementService.Policy.DisableSettingsEditing && x.Category == SettingsPageCategory.Internal))
            .Where(x => !(ManagementService.Policy.DisableSettingsEditing && x.Category == SettingsPageCategory.External))
            .Where(x => !(ManagementService.Policy.DisableSettingsEditing && x.Category == SettingsPageCategory.External))
            .Where(x => !(ManagementService.Policy.DisableDebugMenu && x.Category == SettingsPageCategory.Debug))
            .Where(x => !(!SettingsService.Settings.IsDebugOptionsEnabled && x.Category == SettingsPageCategory.Debug))
            .OrderBy(x => x.Category)
            .GroupBy(x => x.Category)
            .ToList();
        foreach (var info in infos)
        {
            NavigationView.MenuItems.AddRange(info.Select(x => new NavigationViewItem()
            {
                IconSource = new FluentIconSource(x.UnSelectedIconGlyph),
                Content = x.Name,
                Tag = x
            }));
            if (info == infos.Last())
            {
                continue;
            }
            
            NavigationView.MenuItems.Add(new NavigationViewItemSeparator());
        }
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedPageInfo))
        {
            if (!IsLoaded || !ViewModel.IsRendered)
                return;
            await CoreNavigate(ViewModel.SelectedPageInfo);
        }
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_isFirstNavigated)
        {
            return;
        }
        var page = SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == LaunchSettingsPage);
        ViewModel.IsRendered = true;
        await CoreNavigate(page);
        ViewModel.IsCoverVisible = false;
        _isFirstNavigated = true;
        // await CoreNavigate(ViewModel.SelectedPageInfo);
    }

    private async void NavigationServiceOnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        ViewModel.IsNavigating = true;
    }

    private async Task UpdateEchoCaveAsync()
    {
        if (ViewModel.EchoCaveTextsAll.Count <= 0)
        {
            var stream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/Tellings.txt"));

            var sayings = await new StreamReader(stream).ReadToEndAsync();
            if (sayings.Contains("\r\n"))
            {
                ViewModel.EchoCaveTextsAll = [..sayings.Split("\r\n")];
            }
            else
            {
                ViewModel.EchoCaveTextsAll = [..sayings.Split("\n")];
            }
            
        }
        if (ViewModel.EchoCaveTexts.Count <= 0)
        {
            var collection = ViewModel.EchoCaveTextsAll.ToList();
            var countRaw = collection.Count;
            for (var i = 0; i < countRaw; i++)
            {
                var randomIndex = Random.Shared.Next(0, collection.Count - 1);
                ViewModel.EchoCaveTexts.Add(collection[randomIndex]);
                collection.RemoveAt(randomIndex);
            }
        }
        //Console.WriteLine(ViewModel.SayingsCollection.Count);
        if (ViewModel.EchoCaveTexts.Count > 0)
        {
            ViewModel.CurrentEchoCaveText = ViewModel.EchoCaveTexts[0];
            ViewModel.EchoCaveTexts.RemoveAt(0);
        }
    }

    private async void NavigationServiceOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        if (e.Parameter is SettingsWindowNavigationData { IsNavigateFromSettingsWindow: true } data)
        {
            var transaction = data.Transaction as ITransactionTracer;
            var span = data.Span as ISpan;
            try
            {
                // 如果是从设置导航栏导航的，并且没有要求保留历史记录，那么就要清除掉返回项目
                if (!data.KeepHistory)
                {
                    NavigationFrame.BackStack.Clear();
                }

                ViewModel.IsNavigating = false;
                span?.Finish(SpanStatus.Ok);
                transaction?.Finish(SpanStatus.Ok);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "无法完成设置页面导航 {}", ViewModel.SelectedPageInfo?.Id);
                span?.Finish(ex, SpanStatus.InternalError);
                transaction?.Finish(SpanStatus.InternalError);
            }
        }
        ViewModel.IsNavigating = false;
        ViewModel.CanGoBack = NavigationFrame.CanGoBack;
    }


    private async void NavigationListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private async Task CoreNavigate(SettingsPageInfo? info, Uri? uri = null)
    {
        Logger.LogTrace("pre-开始导航");
        if (info == null || Equals(info, ViewModel.SelectedPageInfo))
        {
            return;
        }

        var transaction = SentrySdk.StartTransaction("Navigate SettingsPage", "settings.navigate");
        transaction.SetTag("navigationPage", info.Name);
        transaction.SetTag("navigationPage.id", info.Id);
        var spanLoadPhase1 = transaction.StartChild("setupPage");
        switch (info.Category)
        {
            // 判断是否可以导航
            case SettingsPageCategory.Internal or SettingsPageCategory.External when
                ManagementService.Policy.DisableSettingsEditing:
            case SettingsPageCategory.Debug when
                ManagementService.Policy.DisableDebugMenu:
                return;
        }

        if (ViewModel.IsNavigating)
        {
            return;
        }
        Logger.LogTrace("开始导航");
        ViewModel.IsPopupOpen = false;
        ViewModel.IsNavigating = true;
        try
        {
            NavigationView.SelectedItem = NavigationView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x => Equals(x.Tag, info));

            ViewModel.SelectedPageInfo = info;

            var uriQuery = HttpUtility.ParseQueryString(uri?.Query ?? "");
            var keepHistory = uriQuery[KeepHistoryParameterName] == "true";
            if (SettingsService.Settings.ShowEchoCaveWhenSettingsPageLoading)
            {
                await UpdateEchoCaveAsync();
            }
            
            HangService.AssumeHang();
            // 清空抽屉
            ViewModel.IsDrawerOpen = false;
            ViewModel.DrawerContent = null;
            // 进行导航
            if (!keepHistory)
            {
                NavigationFrame.BackStack.Clear();
            }
            var spanLoadPhase2 = transaction.StartChild("frameNavigate");
            var data = new SettingsWindowNavigationData(true, uri != null, uri, keepHistory, transaction, spanLoadPhase2, info);
            NavigationFrame.NavigateFromObject(data);
            //ViewModel.FrameContent;
            if (!keepHistory)
            {
                NavigationFrame.BackStack.Clear();
            }
            spanLoadPhase1.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法完成设置页面导航 {}", info.Id);
            spanLoadPhase1.Finish(ex, SpanStatus.InternalError);
            transaction.Finish(SpanStatus.InternalError);
            ViewModel.IsNavigating = false;
            if (info.Id != ErrorPageId)
            {
                await CoreNavigate(SettingsWindowRegistryService.Registered.First(x => x.Id == ErrorPageId),
                    new Uri("classisland://app/settings/_error?error=true"));
            }
        }
        //finally
        //{
        //    ViewModel.IsNavigating = false;
        //}
    }

    private SettingsPageBase? GetPage(string? id, out bool cached)
    {
        cached = false;
        if (_cachedPages.TryGetValue(id ?? "", out var page))
        {
            cached = true;
            return page;
        }
        var pageNew = IAppHost.Host?.Services.GetKeyedService<SettingsPageBase>(id);
        if (SettingsService.Settings.SettingsPagesCachePolicy >= 1)
        {
            _cachedPages[id ?? ""] = pageNew;
        }

        return pageNew;
    }

    private void SettingsWindowNew_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.IsViewCompressed = Width < 800;
        if (WindowState == WindowState.Maximized)
            ViewModel.IsViewCompressed = false;
        if (!ViewModel.IsViewCompressed)
        {
            ViewModel.IsNavigationDrawerOpened = true;
        }
    }

    private void ButtonBaseToggleNavigationDrawer_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsNavigationDrawerOpened = !ViewModel.IsNavigationDrawerOpened;
    }

    private void ButtonGoBack_OnClick(object sender, RoutedEventArgs e)
    {
        NavigationFrame.GoBack();
    }

    public async void Open()
    {
        if (!IsOpened)
        {
            if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.EditSettingsAuthorizeLevel))
            {
                return;
            }
            SentrySdk.Metrics.Increment("views.SettingsWindow.open");
            IsOpened = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }
    }

    public async void Open(string key, Uri? uri = null)
    {
        var page = SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == key) ?? ViewModel.SelectedPageInfo;
        LaunchSettingsPage = key;
        await CoreNavigate(page, uri);
        Open();
    }

    public void OpenUri(Uri uri)
    {
        if (uri.Segments.Length > 2)
        {
            var uriSegment = uri.Segments[2].EndsWith('/') ? uri.Segments[2][..^1] : uri.Segments[2];
            Open(uriSegment, uri);
        }
        else if (uri.Segments.Length == 2)
            Open();
    }

    private void SettingsWindowNew_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        IsOpened = false;
        Hide();
        SettingsService.SaveSettings("关闭应用设置窗口");
        ComponentsService.SaveConfig();
        App.GetService<IAutomationService>().SaveConfig("关闭应用设置窗口");
        if (SettingsService.Settings.SettingsPagesCachePolicy <= 1)
        {
            _cachedPages.Clear();
        }
        GC.Collect();
    }

    private void CommandBindingOpenDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.DrawerContent = e.Parameter;
        ViewModel.IsDrawerOpen = true;
    }

    private void CommandBindingCloseDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.IsDrawerOpen = false;
    }

    private void ButtonRestartApp_OnClick(object sender, RoutedEventArgs e)
    {
        ShowRestartDialog();
    }

    private async void ShowRestartDialog()
    {
        var r = await new ContentDialog()
        {
            
            Title = "需要重启",
            Content = "部分设置需要重启以应用",
            PrimaryButtonText = "重启",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
        }.ShowAsync();
        if (r != ContentDialogResult.Primary)
            return;
        AppBase.Current.Restart();
    }

    private void CommandBindingRestartApp_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.IsRequestedRestart = true;
        ShowRestartDialog();
    }

    private void PopupButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupOpen = false;
    }

    private void OpenDrawer(string key)
    {
        ViewModel.DrawerContent = this.FindResource(key);
        ViewModel.IsDrawerOpen = true;
    }

    private void MenuItemExperimentalSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupOpen = false;
        OpenDrawer("ExperimentalSettings");
    }

    private async void MenuItemExitManagement_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await ManagementService.ExitManagementAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法退出管理。");
            _ = CommonTaskDialogs.ShowDialog("无法退出管理", $"无法退出管理：{ex.Message}", this);
        }
    }

    private void MenuItemAppLogs_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<AppLogsWindow>().Open();
    }

    private async void MenuItemExportDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        var message = new ToastMessage()
        {
            Message = "正在导出诊断信息…",
            CanUserClose = false,
            AutoClose = false,
        };
        try
        {
            var r = await new TaskDialog()
            {
                Header = "导出诊断信息",
                Content = "您正在导出应用的诊断数据。导出的诊断数据将包含应用 30 天内产生的日志、系统及环境信息、应用设置、当前加载的档案和集控设置（如有），可能包含敏感信息，请在导出后注意检查。",
                XamlRoot = this,
                Buttons =
                {
                    new TaskDialogButton("取消", false),
                    new TaskDialogButton("继续", true)
                    {
                        IsDefault = true
                    }
                }
            }.ShowAsync();

            if (!Equals(r, true))
                return;

            this.ShowToast(message);
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "导出诊断数据",
                SuggestedStartLocation =
                    await StorageProvider.TryGetFolderFromPathAsync(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                FileTypeChoices =
                [
                    new FilePickerFileType("压缩文件")
                    {
                        Patterns = ["*.zip"]
                    }
                ]
            });
            if (file == null)
            {
                return;
            }

            await DiagnosticService.ExportDiagnosticData(file.Path.LocalPath);
            this.ShowSuccessToast($"已导出诊断信息到 {file.Path.LocalPath}。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法导出诊断信息", exception);
        }
        finally
        {
            message.Close();
        }
    }

    private void MenuItemOpenLogFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(CommonDirectories.AppLogFolderPath) ?? "",
            UseShellExecute = true
        });
    }

    private void MenuItemOpenAppFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(".") ?? "",
            UseShellExecute = true
        });
    }

    private void MenuItemDebugWindowRule_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<WindowRuleDebugWindow>().Show();
    }

    private void MenuItemOpenDataFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(CommonDirectories.AppRootFolderPath) ?? "",
            UseShellExecute = true
        });
    }

    private async void MenuItemAddClassSwapShortcut_OnClick(object sender, RoutedEventArgs e)
    {
        if (!PlatformServices.DesktopService.IsUrlSchemeRegistered)
        {
            var urlDialogResult = await new TaskDialog()
            {
                Header = "创建快捷换课快捷方式",
                Content = "快捷换课快捷方式需要启用【注册 Url 协议】选项才能工作。您要启用它吗？",
                XamlRoot = this,
                Buttons =
                {
                    new TaskDialogButton("取消", false),
                    new TaskDialogButton("启用", true)
                    {
                        IsDefault = true
                    }
                }
            }.ShowAsync();
            if (!Equals(urlDialogResult, true))
            {
                return;
            }

            PlatformServices.DesktopService.IsUrlSchemeRegistered = true;
        }
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            SuggestedFileName = "快捷换课.url",
            SuggestedStartLocation =
                await StorageProvider.TryGetFolderFromPathAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
            FileTypeChoices = [
                new FilePickerFileType("快捷方式")
                {
                    Patterns = ["*.url"]
                }
            ]
        });
        if (file == null)
        {
            return;
        }

        try
        {
            await ShortcutHelpers.CreateClassSwapShortcutAsync(file.Path.LocalPath);
            this.ShowSuccessToast($"已创建快捷换课图标到 {file.Path.LocalPath}。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法创建快捷换课图标", exception);
        }
        
    }

    private async void MenuItemRestartToRecovery_OnClick(object sender, RoutedEventArgs e)
    {
        if (!await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.ExitApplicationAuthorizeLevel))
        {
            return;
        }
        AppBase.Current.Restart(["-m", "-r"]);
    }

    public Control? GetPage(Type srcType)
    {
        return Activator.CreateInstance(srcType) as Control;
    }

    public Control? GetPageFromObject(object target)
    {
        if (target is not SettingsWindowNavigationData data)
        {
            return null;
        }

        var page = GetPage(data.Info.Id, out var cached);
        if (data.Transaction is ITransactionTracer transaction)
        {
            transaction.SetTag("cache.hit", cached.ToString());
            transaction.SetTag("cache.policy", SettingsService.Settings.SettingsPagesCachePolicy.ToString());
        }

        if (page != null)
        {
            page.NavigationUri = data.NavigateUri;
        }
        return page;
    }

    private async void NavigationView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is NavigationViewItem navItem && navItem.Tag is SettingsPageInfo info)
        {
            await CoreNavigate(info);
        }
    }

    private void NavigationView_OnBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        NavigationFrame.GoBack();
    }

    private void TogglePaneButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;
    }

    private void MenuItemDataTransfer_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<DataTransferWindow>().ShowDialog(this);
    }
}
