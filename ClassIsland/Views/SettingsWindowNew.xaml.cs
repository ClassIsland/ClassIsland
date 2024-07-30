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
using Microsoft.AppCenter.Analytics;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using ClassIsland.Services;
using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;
using Sentry;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : MyWindow
{
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
            await CoreNavigate();
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
        ViewModel.SelectedPageInfo =
            SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == LaunchSettingsPage);
        ViewModel.IsRendered = true;
        await CoreNavigate();
    }

    private async Task BeginStoryboardAsync(string key)
    {
        BeginStoryboard(key, out var complete);
        if (!complete.IsCancellationRequested)
            await Task.Run(() => complete.WaitHandle.WaitOne(), complete);
        await Dispatcher.Yield();
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

    private async void NavigationServiceOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        if (e.ExtraData as SettingsWindowNavigationExtraData? == SettingsWindowNavigationExtraData.NavigateFromNavigationView)  
        {
            // 如果是从设置导航栏导航的，那么就要清除掉返回项目
            NavigationService.RemoveBackEntry();
            await Dispatcher.Yield();
            ViewModel.IsNavigating = false;
            await BeginStoryboardAsync("NavigationEntering");
        }
        ViewModel.IsNavigating = false;
        ViewModel.CanGoBack = NavigationService.CanGoBack;
    }


    private async void NavigationListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        
    }

    private async Task CoreNavigate()
    {
        Logger.LogTrace("开始导航");
        switch (ViewModel.SelectedPageInfo?.Category)
        {
            // 判断是否可以导航
            case SettingsPageCategory.Internal or SettingsPageCategory.External when
                ManagementService.Policy.DisableSettingsEditing:
            case SettingsPageCategory.Debug when
                ManagementService.Policy.DisableDebugMenu:
                return;
        }

        ViewModel.IsNavigating = true;
        if (ViewModel.IsViewCompressed)
        {
            ViewModel.IsNavigationDrawerOpened = false;
        }
        await BeginStoryboardAsync("NavigationLeaving");
        HangService.AssumeHang();
        // 从ioc容器获取页面
        var page = IAppHost.Host?.Services.GetKeyedService<SettingsPageBase>(ViewModel.SelectedPageInfo?.Id);
        // 清空抽屉
        ViewModel.IsDrawerOpen = false;
        ViewModel.DrawerContent = null;
        // 进行导航
        NavigationService.RemoveBackEntry();
        NavigationService.Navigate(page, SettingsWindowNavigationExtraData.NavigateFromNavigationView);
        //ViewModel.FrameContent;
        NavigationService.RemoveBackEntry();
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

    public void Open()
    {
        if (!IsOpened)
        {
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

    public void Open(string key)
    {
        ViewModel.SelectedPageInfo = SettingsWindowRegistryService.Registered.FirstOrDefault(x => x.Id == key) ?? ViewModel.SelectedPageInfo;
        LaunchSettingsPage = key;
        Open();
    }

    public void OpenUri(Uri uri)
    {
        if (uri.Segments.Length > 2)
        {
            var uriSegment = uri.Segments[2].EndsWith('/') ? uri.Segments[2][..^1] : uri.Segments[2];
            Open(uriSegment);
        }
        else if (uri.Segments.Length == 2)
            Open();
    }

    private void SettingsWindowNew_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        IsOpened = false;
        Hide();
        SettingsService.SaveSettings();
        ComponentsService.SaveConfig();
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
            var r = CommonDialog.ShowDialog("ClassIsland", $"您正在导出应用的诊断数据。导出的诊断数据将包含应用当前运行的日志、系统及环境信息、应用设置、当前加载的档案和集控设置（如有），可能包含敏感信息，请在导出后注意检查。", new BitmapImage(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.Relative)),
                60, 60, [
                    new DialogAction()
                    {
                        PackIconKind = PackIconKind.Cancel,
                        Name = "取消"
                    },
                    new DialogAction()
                    {
                        PackIconKind = PackIconKind.Check,
                        Name = "继续",
                        IsPrimary = true
                    }
                ]);
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
        if (item.Category is SettingsPageCategory.Internal or SettingsPageCategory.External && ManagementService.Policy.DisableSettingsEditing)
        {e.Accepted = false;
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
}