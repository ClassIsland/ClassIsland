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
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Extensions;
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
using ClassIsland.Views.SettingPages;
using Sentry;
using System.IO;
using ClassIsland.Controls;
using Path = System.IO.Path;
using System.Web;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Models.SettingsWindow;
using ClassIsland.Helpers;
using System.Transactions;
using Avalonia;
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
using Markdown.Avalonia.Full;
using ReactiveUI;
using WindowsShortcutFactory;
using Control = Avalonia.Controls.Control;
using FATaskDialog = FluentAvalonia.UI.Controls.FATaskDialog;
using FATaskDialogButton = FluentAvalonia.UI.Controls.FATaskDialogButton;

namespace ClassIsland.Views;

/// <summary>
/// SettingsWindowNew.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindowNew : ViewBase, IFANavigationPageFactory
{
    private const string KeepHistoryParameterName = "ci_keepHistory";
    private const string ErrorPageId = "_error";

    public SettingsNewViewModel ViewModel { get; } = new();

    public IManagementService ManagementService { get; }

    private IHangService HangService { get; }

    private ILogger<SettingsWindowNew> Logger;

    private DiagnosticService DiagnosticService { get; }

    public SettingsService SettingsService { get; }

    public static readonly string StartupSettingsPage = "general";

    private IComponentsService ComponentsService { get; }
    private ITutorialService TutorialService { get; }

    private string LaunchSettingsPage { get; set; } = StartupSettingsPage;

    private bool _isFirstNavigated = false;
    private IReadOnlyList<SettingsNavigationSection> _navigationSections = [];

    private static bool UseNavigationDirectoryByDefault =>
        OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || App.ApplicationCommand.Mobile;

    private readonly Dictionary<string, SettingsPageBase?> _cachedPages = new();
    private IDisposable? _debugOptionsSubscription;
    private bool _resourcesReleased;
    
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
        IComponentsService componentsService, IUriNavigationService uriNavigationService,
        ITutorialService tutorialService)
    {
        Logger = logger;
        DataContext = this;
        ManagementService = managementService;
        ComponentsService = componentsService;
        TutorialService = tutorialService;
        DiagnosticService = diagnosticService;
        HangService = hangService;
        SettingsService = settingsService;
        SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        InitializeComponent();
        FaFrameLifetimeHelper.Attach(NavigationFrame);
        // SplashScreen = new EmptySplashScreen();
        
        // TitleBar.TitleBarHitTestType = FATitleBarHitTestType.Complex;
        NavigationFrame.NavigationPageFactory = this;

        if (ManagementService.Policy.DisableSettingsEditing)
        {
            LaunchSettingsPage = "about";
        }

        BuildNavigationMenuItems();
        _debugOptionsSubscription = SettingsService.Settings
            .ObservableForProperty(x => x.IsDebugOptionsEnabled)
            .Subscribe(_ => BuildNavigationMenuItems());
    }

    private void BuildNavigationMenuItems()
    {
        if (_resourcesReleased)
        {
            return;
        }

        NavigationView.MenuItems.Clear();
        ViewModel.FlattenNavigationItemsCache.Clear();

        _navigationSections = BuildNavigationSections();
        foreach (var section in _navigationSections)
        {
            if (section.ShowSeparator)
            {
                NavigationView.MenuItems.Add(new FANavigationViewItemSeparator());
            }

            foreach (var node in section.Nodes)
            {
                var item = new FANavigationViewItem
                {
                    IconSource = node.IconSource,
                    Content = node.Name,
                    Tag = node.Page
                };

                if (node.Children == null)
                {
                    ViewModel.FlattenNavigationItemsCache.Add(item);
                }
                else
                {
                    var children = node.Children.Select(page => new FANavigationViewItem
                    {
                        IconSource = page.UnSelectedIconSource,
                        Content = page.Name,
                        Tag = page
                    }).ToList();
                    ViewModel.FlattenNavigationItemsCache.AddRange(children);
                    item.MenuItems.AddRange(children);
                }

                NavigationView.MenuItems.Add(item);
            }
        }

        SelectNavigationItem(ViewModel.SelectedPageInfo);
        if (NavigationFrame.Content is SettingsNavigationPage directory)
        {
            ViewModel.PageTitle = directory.UpdateSections(_navigationSections);
        }
    }

    private IReadOnlyList<SettingsNavigationSection> BuildNavigationSections()
    {
        var pages = SettingsWindowRegistryService.Registered
            .Where(page => !page.HideDefault && CanNavigate(page))
            .Where(page => page.Category != SettingsPageCategory.Debug || SettingsService.Settings.IsDebugOptionsEnabled)
            .OrderBy(page => page.Category)
            .ToList();
        var pagesByGroup = pages.ToLookup(page => page.GroupId);
        var addedGroups = new HashSet<string>();
        var sections = new List<SettingsNavigationSection>();

        foreach (var category in pages.GroupBy(page => page.Category))
        {
            var nodes = new List<SettingsNavigationNode>();
            foreach (var page in category)
            {
                if (page.GroupId is { } groupId && SettingsWindowRegistryService.Groups.TryGetValue(groupId, out var group))
                {
                    if (addedGroups.Add(groupId))
                    {
                        nodes.Add(new SettingsNavigationNode(page, group, pagesByGroup[groupId].ToList()));
                    }
                }
                else
                {
                    nodes.Add(new SettingsNavigationNode(page));
                }
            }

            if (nodes.Count > 0)
            {
                sections.Add(new SettingsNavigationSection(nodes, sections.Count > 0));
            }
        }

        return sections;
    }

    private bool CanNavigate(SettingsPageInfo page)
    {
        return page.Category switch
        {
            SettingsPageCategory.Internal or SettingsPageCategory.External => !ManagementService.Policy.DisableSettingsEditing,
            SettingsPageCategory.Debug => !ManagementService.Policy.DisableDebugMenu,
            _ => true
        };
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }

    protected async override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (_resourcesReleased || _isFirstNavigated)
        {
            return;
        }
        ViewModel.IsRendered = true;
        if (NavigationFrame.Content == null)
        {
            var pageId = UseNavigationDirectoryByDefault ? SettingsNavigationPage.PageId : LaunchSettingsPage;
            await CoreNavigate(FindPageInfo(pageId));
        }

        if (_resourcesReleased)
        {
            return;
        }

        ViewModel.IsCoverVisible = false;
        _isFirstNavigated = true;
    }

    private async Task UpdateEchoCaveAsync()
    {
        if (_resourcesReleased)
        {
            return;
        }

        if (ViewModel.EchoCaveTextsAll.Count <= 0)
        {
            var sayings = await AssetLoader.ReadAllTextAsync(new Uri("avares://ClassIsland/Assets/Tellings.txt"));
            if (_resourcesReleased)
            {
                return;
            }

            ViewModel.EchoCaveTextsAll = [..sayings.Split(Environment.NewLine)];
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

    private void NavigationServiceOnLoadCompleted(object sender, FANavigationEventArgs e)
    {
        if (_resourcesReleased)
        {
            return;
        }

        var info = e.Content is SettingsPageBase { Tag: string pageId } ? FindPageInfo(pageId) : null;
        ViewModel.SelectedPageInfo = info;
        SelectNavigationItem(info);
        ViewModel.CanGoBack = NavigationFrame.CanGoBack;

        ViewModel.PageTitle = e.Content is SettingsNavigationPage directory
            ? directory.UpdateSections(_navigationSections)
            : info is { HidePageTitle: false } ? info.Name : null;
    }

    private async Task CoreNavigate(SettingsPageInfo? info, Uri? uri = null, bool preserveDirectoryHistory = false)
    {
        if (_resourcesReleased || ViewModel.IsNavigating || info == null || !CanNavigate(info))
        {
            return;
        }

        var query = HttpUtility.ParseQueryString(uri?.Query ?? "");
        var keepHistory = preserveDirectoryHistory || query[KeepHistoryParameterName] == "true";
        var isDirectory = info.Id == SettingsNavigationPage.PageId;
        var groupId = isDirectory ? ResolveDirectoryGroup(query[SettingsNavigationPage.PathParameterName]) : null;
        var isRoot = isDirectory && groupId == null;

        if (Equals(info, ViewModel.SelectedPageInfo) &&
            (!isDirectory || NavigationFrame.Content is SettingsNavigationPage directory && directory.GroupId == groupId))
        {
            if (UseNavigationDirectoryByDefault)
            {
                TrimNavigationHistory(keepHistory, isRoot);
            }
            return;
        }

        var transaction = SentrySdk.StartTransaction("Navigate SettingsPage", "settings.navigate");
        transaction.SetTag("navigationPage", info.Name);
        transaction.SetTag("navigationPage.id", info.Id);
        var setupSpan = transaction.StartChild("setupPage");
        ISpan? frameSpan = null;
        ViewModel.IsPopupOpen = false;
        ViewModel.IsNavigating = true;

        try
        {
            if (SettingsService.Settings.ShowEchoCaveWhenSettingsPageLoading)
            {
                await UpdateEchoCaveAsync();
            }

            if (_resourcesReleased || (!isRoot && !EnsureNavigationRoot()))
            {
                setupSpan.Finish(SpanStatus.Cancelled);
                transaction.Finish(SpanStatus.Cancelled);
                return;
            }

            HangService.AssumeHang();
            ViewModel.IsDrawerOpen = false;
            ViewModel.DrawerContent = null;
            setupSpan.Finish(SpanStatus.Ok);

            frameSpan = transaction.StartChild("frameNavigate");
            SettingsWindowNavigationData data = isDirectory
                ? new DirectoryNavigationData(info, groupId, uri, keepHistory, transaction, frameSpan)
                : new SettingsWindowNavigationData(true, uri != null, uri, keepHistory, transaction, frameSpan, info);
            var navigated = NavigationFrame.NavigateFromObject(data);
            if (navigated)
            {
                TrimNavigationHistory(keepHistory, isRoot);
            }

            var status = navigated ? SpanStatus.Ok : SpanStatus.Cancelled;
            frameSpan.Finish(status);
            transaction.Finish(status);
            return;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "无法完成设置页面导航 {}", info.Id);
            (frameSpan ?? setupSpan).Finish(ex, SpanStatus.InternalError);
            transaction.Finish(SpanStatus.InternalError);
        }
        finally
        {
            ViewModel.IsNavigating = false;
        }

        if (info.Id != ErrorPageId && !isRoot)
        {
            await CoreNavigate(FindPageInfo(ErrorPageId), new Uri("classisland://app/settings/_error?error=true"));
        }
    }

    private static SettingsPageInfo? FindPageInfo(string? pageId) =>
        SettingsWindowRegistryService.Registered.FirstOrDefault(page => page.Id == pageId);

    private string? ResolveDirectoryGroup(string? path)
    {
        if (path is not { Length: > 1 } || path[0] != '/')
        {
            return null;
        }

        var groupId = path[1..];
        return _navigationSections.SelectMany(section => section.Nodes).Any(node => node.GroupId == groupId)
            ? groupId
            : null;
    }

    private bool HasNavigationRootInHistory =>
        NavigationFrame.BackStack.FirstOrDefault()?.Context is DirectoryNavigationData { GroupId: null };

    private bool EnsureNavigationRoot()
    {
        if (!UseNavigationDirectoryByDefault || HasNavigationRootInHistory ||
            NavigationFrame.Content is SettingsNavigationPage { GroupId: null })
        {
            return true;
        }

        var directoryInfo = FindPageInfo(SettingsNavigationPage.PageId)!;
        if (!NavigationFrame.NavigateFromObject(new DirectoryNavigationData(directoryInfo, null, null, false)))
        {
            return false;
        }

        TrimNavigationHistory(keepHistory: false, isRoot: true);
        return true;
    }

    private void TrimNavigationHistory(bool keepHistory, bool isRoot)
    {
        if (keepHistory && !(UseNavigationDirectoryByDefault && isRoot))
        {
            return;
        }

        if (UseNavigationDirectoryByDefault && !isRoot && HasNavigationRootInHistory)
        {
            while (NavigationFrame.BackStack.Count > 1)
            {
                NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStack.Count - 1);
            }
        }
        else
        {
            NavigationFrame.BackStack.Clear();
        }

        NavigationFrame.ForwardStack.Clear();
        ViewModel.CanGoBack = NavigationFrame.CanGoBack;
    }

    private sealed class DirectoryNavigationData(SettingsPageInfo info, string? groupId, Uri? uri, bool keepHistory,
        object? transaction = null, object? span = null)
        : SettingsWindowNavigationData(true, uri != null, uri, keepHistory, transaction, span, info)
    {
        public string? GroupId { get; } = groupId;
    }

    private SettingsPageBase? GetPage(string? id, out bool cached)
    {
        cached = false;
        if (_resourcesReleased)
        {
            return null;
        }

        if (_cachedPages.TryGetValue(id ?? "", out var page))
        {
            cached = true;
            return page;
        }
        var pageNew = IAppHost.Host?.Services.GetKeyedService<SettingsPageBase>(id);
        if (id != SettingsNavigationPage.PageId && SettingsService.Settings.SettingsPagesCachePolicy >= 1)
        {
            _cachedPages[id ?? ""] = pageNew;
        }

        if (pageNew != null)
        {
            pageNew.Tag = id;
        }
        return pageNew;
    }

    private void SettingsWindowNew_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.IsViewCompressed = Bounds.Width < 800;
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

    public override async void Open(ViewBase? owner = null) => await OpenCore(owner, null, null);

    public async void Open(string key, Uri? uri = null) => await OpenCore(null, key, uri);

    private async Task OpenCore(ViewBase? owner, string? pageId, Uri? uri)
    {
        if (_resourcesReleased ||
            !await ManagementService.AuthorizeByLevel(ManagementService.CredentialConfig.EditSettingsAuthorizeLevel) ||
            _resourcesReleased)
        {
            return;
        }

        if (pageId != null)
        {
            await CoreNavigate(FindPageInfo(pageId), uri);
        }
        else if (UseNavigationDirectoryByDefault)
        {
            await CoreNavigate(FindPageInfo(SettingsNavigationPage.PageId));
        }

        if (_resourcesReleased)
        {
            return;
        }

        TutorialService.PushToNextSentenceByTag("classisland.settingsWindow.open");
        base.Open(owner);
    }

    public void OpenUri(Uri uri)
    {
        if (_resourcesReleased)
        {
            return;
        }

        if (uri.Segments.Length > 2)
        {
            var uriSegment = uri.Segments[2].EndsWith('/') ? uri.Segments[2][..^1] : uri.Segments[2];
            Open(uriSegment, uri);
        }
        else if (uri.Segments.Length == 2)
            Open();
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
    bool IsShowingRestartDialog = false;
    private async void ShowRestartDialog()
    {
        if (!IsShowingRestartDialog)
        {
            IsShowingRestartDialog = true;
            var r = await new FAContentDialog()
            {
                Title = "需要重启",
                Content = "部分设置需要重启以应用更改。",
                PrimaryButtonText = "重启",
                CloseButtonText = "取消",
                DefaultButton = FAContentDialogButton.Primary,
            }.ShowAsyncAuto(TopLevel.GetTopLevel(this));
            IsShowingRestartDialog = false;
            if (r != FAContentDialogResult.Primary)
                return;
            AppBase.Current.Restart();
        }
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
            var r = await new FATaskDialog()
            {
                Header = "导出诊断信息",
                Content = "您正在导出应用的诊断数据。导出的诊断数据将包含应用 30 天内产生的日志、系统及环境信息、应用设置、当前加载的档案、所使用的插件（如有）和集控设置（如有），可能包含敏感信息，请在导出后注意检查。",
                XamlRoot = this,
                Buttons =
                {
                    new FATaskDialogButton("取消", false),
                    new FATaskDialogButton("继续", true)
                    {
                        IsDefault = true
                    }
                }
            }.ShowAsync();

            if (!Equals(r, true))
                return;

            this.ShowToast(message);
            PopupHelper.DisableAllPopups();
            var file = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "导出诊断数据",
                SuggestedStartLocation =
                    await TopLevel.GetTopLevel(this)!.StorageProvider.TryGetFolderFromPathAsync(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
                FileTypeChoices =
                [
                    new FilePickerFileType("压缩文件")
                    {
                        Patterns = ["*.zip"]
                    }
                ]
            }, TopLevel.GetTopLevel(this)!);
            PopupHelper.RestoreAllPopups();
            if (file == null)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this)!;
            using var storageFile = await PlatformServices.FilePickerService.GetFileAsync(file, topLevel)
                                    ?? throw new FileNotFoundException("无法打开所选诊断数据文件。", file);
            await using var outputStream = await storageFile.OpenWriteAsync();
            if (outputStream.CanSeek)
            {
                outputStream.SetLength(0);
                outputStream.Position = 0;
            }
            await DiagnosticService.ExportDiagnosticData(outputStream);
            this.ShowSuccessToast($"已导出诊断信息到 {file}。");
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
        PlatformServices.LauncherService.LaunchPath(CommonDirectories.AppLogFolderPath);
    }

    private void MenuItemOpenAppFolder_OnClick(object sender, RoutedEventArgs e)
    {
        PlatformServices.LauncherService.LaunchPath(".");
    }

    private void MenuItemDebugWindowRule_OnClick(object sender, RoutedEventArgs e)
    {
        IAppHost.GetService<WindowRuleDebugWindow>().Show();
    }

    private void MenuItemOpenDataFolder_OnClick(object sender, RoutedEventArgs e)
    {
        PlatformServices.LauncherService.LaunchPath(CommonDirectories.AppRootFolderPath);
    }

    private async void MenuItemAddDesktopShortcut_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ShortcutHelpers.CreateDesktopShortcutAsync();
            this.ShowSuccessToast("已创建桌面快捷方式。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法创建桌面快捷方式", exception);
        }
    }

    private async void MenuItemAddStartMenuShortcut_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await ShortcutHelpers.CreateStartMenuShortcutAsync();
            this.ShowSuccessToast("已创建开始菜单快捷方式。");
        }
        catch (Exception exception)
        {
            this.ShowErrorToast("无法创建开始菜单快捷方式", exception);
        }
    }

    private async void MenuItemAddClassSwapShortcut_OnClick(object sender, RoutedEventArgs e)
    {
        if (!PlatformServices.DesktopService.IsUrlSchemeRegistered)
        {
            var urlDialogResult = await new FATaskDialog()
            {
                Header = "创建快捷换课快捷方式",
                Content = "快捷换课快捷方式需要启用【注册 Url 协议】选项才能工作。您要启用它吗？",
                XamlRoot = this,
                Buttons =
                {
                    new FATaskDialogButton("取消", false),
                    new FATaskDialogButton("启用", true)
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
        PopupHelper.DisableAllPopups();
        var file = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            SuggestedFileName = "快捷换课.url",
            SuggestedStartLocation =
                await TopLevel.GetTopLevel(this)!.StorageProvider.TryGetFolderFromPathAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)),
            FileTypeChoices = [
                new FilePickerFileType("快捷方式")
                {
                    Patterns = ["*.url"]
                }
            ]
        }, TopLevel.GetTopLevel(this)!);
        PopupHelper.RestoreAllPopups();
        if (file == null)
        {
            return;
        }

        try
        {
            var topLevel = TopLevel.GetTopLevel(this)!;
            using var storageFile = await PlatformServices.FilePickerService.GetFileAsync(file, topLevel)
                                    ?? throw new FileNotFoundException("无法打开所选快捷方式文件。", file);
            await using var outputStream = await storageFile.OpenWriteAsync();
            if (outputStream.CanSeek)
            {
                outputStream.SetLength(0);
                outputStream.Position = 0;
            }
            await ShortcutHelpers.CreateClassSwapShortcutAsync(outputStream);
            this.ShowSuccessToast($"已创建快捷换课图标到 {file}。");
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
        if (_resourcesReleased)
        {
            return null;
        }

        return Activator.CreateInstance(srcType) as Control;
    }

    public Control? GetPageFromObject(object target)
    {
        if (_resourcesReleased || target is not SettingsWindowNavigationData data)
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

        if (page is SettingsNavigationPage directory && data is DirectoryNavigationData directoryData)
        {
            directory.GroupId = directoryData.GroupId;
            directory.NodeInvoked += DirectoryNode_OnInvoked;
        }

        return page;
    }

    private async void DirectoryNode_OnInvoked(object? sender, SettingsNavigationNode node)
    {
        if (node.GroupId is { } groupId)
        {
            var uri = new Uri($"classisland://app/settings/{SettingsNavigationPage.PageId}?" +
                              $"{SettingsNavigationPage.PathParameterName}={Uri.EscapeDataString("/" + groupId)}");
            await CoreNavigate(FindPageInfo(SettingsNavigationPage.PageId), uri, preserveDirectoryHistory: true);
        }
        else
        {
            await CoreNavigate(node.Page, preserveDirectoryHistory: true);
        }
    }

    private async void NavigationView_OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer is FANavigationViewItem navItem && navItem.Tag is SettingsPageInfo info)
        {
            await CoreNavigate(info);
        }
    }

    private void NavigationView_OnBackRequested(object? sender, FANavigationViewBackRequestedEventArgs e) => TryGoBack();

    private void TogglePaneButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;
    }

    private async void MenuItemDataTransfer_OnClick(object? sender, RoutedEventArgs e)
    {
        await IAppHost.GetService<DataTransferWindow>().ShowModal(this);
    }

    private void MenuItemOpenManagementSettingsPage_OnClick(object? sender, RoutedEventArgs e)
    {
        IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/settings/management?ci_keepHistory=true"));
    }

    private void MenuItemJoinManagement_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new JoinManagementDialog();
        dialog.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e) => TryGoBack();

    protected override bool TryGoBackCore()
    {
        if (_resourcesReleased)
        {
            return false;
        }

        if (ViewModel.IsNavigating)
        {
            return true;
        }

        if (!NavigationFrame.CanGoBack)
        {
            return false;
        }

        NavigationFrame.GoBack();
        return true;
    }

    private void SelectNavigationItem(SettingsPageInfo? info)
    {
        var item = ViewModel.FlattenNavigationItemsCache
            .FirstOrDefault(x => Equals(x.Tag, info));
        if (item == null)
        {
            NavigationView.SelectedItem = null;
        }
        else if (NavigationView.MenuItems.Contains(item))
        {
            NavigationView.SelectedItem = item;
        } else if (NavigationView.MenuItems
                   .OfType<FANavigationViewItem>()
                   .FirstOrDefault(x => x.MenuItems.Contains(item))
                   is {} parent)
        {
            parent.IsChildSelected = true;
            var isFirstNavigated = _isFirstNavigated;
            Dispatcher.UIThread.Post(() =>
            {
                if (_resourcesReleased || ViewModel.SelectedPageInfo != info ||
                    !ViewModel.FlattenNavigationItemsCache.Contains(item))
                {
                    return;
                }

                if (!isFirstNavigated || UseNavigationDirectoryByDefault)
                {
                    parent.IsExpanded = true;
                }
                NavigationView.SelectedItem = item;
            });
        }
        
        foreach (var i in ViewModel.FlattenNavigationItemsCache.Where(x => !Equals(x.Tag, info)))
        {
            i.IsSelected = false;
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {   
        
    }

    private void ViewBase_OnClosed(object? sender, RoutedEventArgs e)
    {
        if (_resourcesReleased)
        {
            return;
        }

        try
        {
            SettingsService.SaveSettings("关闭应用设置窗口");
            ComponentsService.SaveConfig();
            App.GetService<IAutomationService>().SaveConfig("关闭应用设置窗口");
        }
        finally
        {
            ReleaseResources();
            GC.Collect();
        }
    }

    private void ReleaseResources()
    {
        if (_resourcesReleased)
        {
            return;
        }

        _resourcesReleased = true;

        SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _debugOptionsSubscription?.Dispose();
        _debugOptionsSubscription = null;
        Closed -= ViewBase_OnClosed;

        NavigationFrame.Navigated -= NavigationServiceOnLoadCompleted;
        NavigationFrame.NavigationPageFactory = null;
        NavigationFrame.IsNavigationStackEnabled = false;
        NavigationFrame.BackStack.Clear();
        NavigationFrame.ForwardStack.Clear();
        NavigationFrame.Content = null;
        NavigationView.SelectedItem = null;
        NavigationView.MenuItems.Clear();

        ViewModel.FrameContent = null;
        ViewModel.SelectedPageInfo = null;
        ViewModel.PageTitle = null;
        ViewModel.DrawerContent = null;
        ViewModel.IsDrawerOpen = false;
        ViewModel.IsNavigating = false;
        ViewModel.IsRendered = false;
        ViewModel.FlattenNavigationItemsCache.Clear();
        ViewModel.NavigationViewItems.Clear();
        _cachedPages.Clear();

        DataContext = null;
        Content = null;
        Resources.Clear();
    }
}
