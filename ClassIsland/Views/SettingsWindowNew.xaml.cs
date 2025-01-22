using ClassIsland.Core.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using ClassIsland.Services;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
using Sentry;
using System.IO;
using ClassIsland.Controls;
using Path = System.IO.Path;
using System.Collections.ObjectModel;
using System.Web;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.SettingsWindow;
using Application = System.Windows.Application;
using YamlDotNet.Core.Tokens;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : MyWindow
{
    private const string KeepHistoryParameterName = "ci_keepHistory";

    public SettingsNewViewModel ViewModel { get; } = new();

    [NotNull]
    public NavigationService? NavigationService { get; set; }

    private bool IsOpened { get; set; } = false;

    public IManagementService ManagementService { get; }

    private IHangService HangService { get; }

    private ILogger<SettingsWindowNew> Logger;

    private DiagnosticService DiagnosticService { get; }

    public SettingsService SettingsService { get; }

    public static readonly string StartupSettingsPage = "general";

    private IComponentsService ComponentsService { get; }

    private string LaunchSettingsPage { get; set; } = StartupSettingsPage;

    private readonly Dictionary<string, SettingsPageBase?> _cachedPages = new();


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
        NavigationService = NavigationFrame.NavigationService;
        NavigationService.LoadCompleted += NavigationServiceOnLoadCompleted;
        NavigationService.Navigating += NavigationServiceOnNavigating;
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;

        if (ManagementService.Policy.DisableSettingsEditing)
        {
            LaunchSettingsPage = "about";
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
        if (e.PropertyName == nameof(SettingsService.Settings.IsDebugOptionsEnabled))
        {
            if (FindResource("NavigationCollectionViewSource") is CollectionViewSource source)
            {
                source.View.Refresh();
            }
        }
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        var page = SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == LaunchSettingsPage);
        ViewModel.IsRendered = true;
        await CoreNavigate(page);
        //await CoreNavigate(ViewModel.SelectedPageInfo);
    }

    private async Task BeginStoryboardAsync(string key)
    {
        BeginStoryboard(key, out var complete);
        if (!complete.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() => complete.WaitHandle.WaitOne(), complete);
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }
        if (!IThemeService.IsWaitForTransientDisabled)
        {
            await Dispatcher.Yield();
        }
    }

    private void BeginStoryboard(string key, out CancellationToken cancellationToken)
    {
        var complete = new CancellationTokenSource();
        cancellationToken = complete.Token;
        if (!IsInitialized)
            return;
        if (FindResource(key) is not Storyboard sb)
            return;
        sb.Completed += (sender, args) =>
        {
            complete.Cancel();
        };
        sb.Begin();
    }

    private async void NavigationServiceOnNavigating(object sender, NavigatingCancelEventArgs e)
    {
        ViewModel.IsNavigating = true;
    }

    private async Task UpdateEchoCaveAsync()
    {
        if (ViewModel.EchoCaveTextsAll.Count <= 0)
        {
            var stream = Application.GetResourceStream(new Uri("/Assets/Tellings.txt", UriKind.Relative))?.Stream;
            if (stream == null)
            {
                return;
            }

            var sayings = await new StreamReader(stream).ReadToEndAsync();
            ViewModel.EchoCaveTextsAll = [..sayings.Split("\r\n")];
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
        if (e.ExtraData is SettingsWindowNavigationData { IsNavigateFromSettingsWindow: true } data)  
        {
            // 如果是从设置导航栏导航的，并且没有要求保留历史记录，那么就要清除掉返回项目
            if (!data.KeepHistory)
            {
                NavigationService.RemoveBackEntry();
            }
            if (!IThemeService.IsWaitForTransientDisabled)
            {
                await Dispatcher.Yield();
            }
            ViewModel.IsNavigating = false;
            var child = LoadingAsyncBox.LoadingView as LoadingMask;
            child?.FinishFakeLoading();
            if (!IThemeService.IsTransientDisabled)
            {
                await BeginStoryboardAsync("NavigationEntering");
            }
        }
        ViewModel.IsNavigating = false;
        ViewModel.CanGoBack = NavigationService.CanGoBack;
    }


    private async void NavigationListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        
    }

    private async Task CoreNavigate(SettingsPageInfo? info, Uri? uri = null)
    {
        Logger.LogTrace("pre-开始导航");
        if (info == null)
        {
            return;
        }
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
        if (ViewModel.IsViewCompressed)
        {
            ViewModel.IsNavigationDrawerOpened = false;
        }
        ViewModel.SelectedPageInfo = info;

        var uriQuery = HttpUtility.ParseQueryString(uri?.Query ?? "");
        var keepHistory = uriQuery[KeepHistoryParameterName] == "true";
        var child = LoadingAsyncBox.LoadingView as LoadingMask;
        child?.StartFakeLoading();
        if (SettingsService.Settings.ShowEchoCaveWhenSettingsPageLoading)
        {
            await UpdateEchoCaveAsync();
        }
        if (!IThemeService.IsTransientDisabled)
        {
            await BeginStoryboardAsync("NavigationLeaving");
        }
        HangService.AssumeHang();
        // 从ioc容器获取页面
        var page = GetPage(info.Id);
        // 清空抽屉
        ViewModel.IsDrawerOpen = false;
        ViewModel.DrawerContent = null;
        // 进行导航
        if (!keepHistory)
        {
            NavigationService.RemoveBackEntry();
        }
        NavigationService.Navigate(page, new SettingsWindowNavigationData(true, uri != null, uri, keepHistory));
        //ViewModel.FrameContent;
        if (!keepHistory)
        {
            NavigationService.RemoveBackEntry();
        }
    }

    private SettingsPageBase? GetPage(string? id)
    {
        if (_cachedPages.TryGetValue(id ?? "", out var page))
        {
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
        NavigationService.GoBack();
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

    private void SettingsWindowNew_OnClosing(object? sender, CancelEventArgs e)
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
        if (DialogHost.IsDialogOpen(SettingsPageBase.DialogHostIdentifier))
            return;
        var r = await DialogHost.Show(FindResource("RestartDialog"), SettingsPageBase.DialogHostIdentifier);
        if (r as bool? != true)
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
        ViewModel.DrawerContent = FindResource(key);
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
            CommonDialog.ShowError($"无法退出管理：{ex.Message}");
        }
    }

    private void MenuItemAppLogs_OnClick(object sender, RoutedEventArgs e)
    {
        App.GetService<AppLogsWindow>().Open();
    }

    private async void MenuItemExportDiagnosticInfo_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = new CommonDialogBuilder()
                .SetContent("您正在导出应用的诊断数据。导出的诊断数据将包含应用 30 天内产生的日志、系统及环境信息、应用设置、当前加载的档案和集控设置（如有），可能包含敏感信息，请在导出后注意检查。")
                .SetIconKind(CommonDialogIconKind.Hint)
                .AddCancelAction()
                .AddAction("继续", PackIconKind.Check, true)
                .ShowDialog();
            
            if (r != 1)
                return;
            var dialog = new SaveFileDialog()
            {
                Title = "导出诊断数据",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "压缩文件(*.zip)|*.zip"
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            await DiagnosticService.ExportDiagnosticData(dialog.FileName);
        }
        catch (Exception exception)
        {
            CommonDialog.ShowError($"导出失败：{exception.Message}");
        }
    }

    private void NavigationCollectionViewSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not SettingsPageInfo item)
            return;
        if (item.HideDefault)
        {
            e.Accepted = false;
            return;
        }
        if (item.Category is SettingsPageCategory.Internal or SettingsPageCategory.External && ManagementService.Policy.DisableSettingsEditing)
        {
            e.Accepted = false;
            return;
        }
        if (item.Category == SettingsPageCategory.Debug && ManagementService.Policy.DisableDebugMenu)
        {
            e.Accepted = false;
            return;
        }

        if (item.Category == SettingsPageCategory.Debug && !SettingsService.Settings.IsDebugOptionsEnabled)
        {
            e.Accepted = false;
            return;
        }
    }

    private void MenuItemOpenLogFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(App.AppLogFolderPath) ?? "",
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
}