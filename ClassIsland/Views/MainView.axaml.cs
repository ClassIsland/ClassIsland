using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using ClassIsland.Views.MainViewPages;
using ClassIsland.Views.MainViewParts;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Views;

public partial class MainView : ViewBase
{
    public MainViewViewModel ViewModel { get; } = IAppHost.GetService<MainViewViewModel>();
    public ClassChangingWindow? ClassChangingWindow { get; set; }

    private readonly Dictionary<string, MainViewPageBase> _pages = [];
    private FANavigationViewItem? _currentNavigationItem;
    private FANavigationViewItem? _pendingNavigationItem;
    private bool _isNavigating;
    private bool _isSynchronizingSelection;
    private Window? _scheduleWeekHost;

    public MainView()
    {
        InitializeComponent();
        var pages = IAppHost.GetService<IEnumerable<MainViewPageInfo>>().OrderBy(info => info.Category);
        foreach (var info in pages)
        {
            var icon = info.IconSource;
            if (icon is FluentIconSource fluentIcon)
            {
                fluentIcon.FontSize = 22;
            }

            MainNavigation.MenuItems.Add(new FANavigationViewItem
            {
                Content = info.Name,
                IconSource = icon,
                Tag = info
            });
        }

        MainNavigation.SelectionChanged += MainNavigation_OnSelectionChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ViewModel.ProfileService.Profile.PropertyChanged += Profile_OnPropertyChanged;
        _scheduleWeekHost = TopLevel.GetTopLevel(this) as Window;
        if (_scheduleWeekHost != null)
        {
            _scheduleWeekHost.Activated += ScheduleWeekHost_OnActivated;
        }

        if (MainPageHost.CurrentPage == null)
        {
            _pendingNavigationItem ??= MainNavigation.MenuItems.OfType<FANavigationViewItem>().FirstOrDefault();
        }
        else
        {
            UpdateSchedulePageActivity();
        }

        _ = NavigatePendingPagesAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewModel.ProfileService.Profile.PropertyChanged -= Profile_OnPropertyChanged;
        ViewModel.DeactivateCalendar();
        if (_scheduleWeekHost != null)
        {
            _scheduleWeekHost.Activated -= ScheduleWeekHost_OnActivated;
            _scheduleWeekHost = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private async void MainNavigation_OnSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (_isSynchronizingSelection || e.SelectedItem is not FANavigationViewItem item)
        {
            return;
        }

        _pendingNavigationItem = item;
        await NavigatePendingPagesAsync();
    }

    private async Task NavigatePendingPagesAsync()
    {
        if (_isNavigating)
        {
            return;
        }

        _isNavigating = true;
        try
        {
            while (VisualRoot != null && _pendingNavigationItem is { Tag: MainViewPageInfo info } item)
            {
                _pendingNavigationItem = null;
                if (ReferenceEquals(item, _currentNavigationItem))
                {
                    SynchronizeSelection();
                    continue;
                }

                var previousPage = MainPageHost.CurrentPage;
                MainViewPageBase? page = null;
                try
                {
                    page = GetPage(info);
                    if (previousPage == null)
                    {
                        await MainPageHost.PushAsync(page, null);
                    }
                    else
                    {
                        await MainPageHost.ReplaceAsync(page);
                    }
                }
                catch (Exception exception)
                {
                    var logger = IAppHost.GetService<ILogger<MainView>>();
                    logger.LogError(exception, "无法打开主视图页面 {PageId}。", info.Id);
                    // Navigation callbacks can throw after Avalonia has already changed the current page.
                    if (page != null && ReferenceEquals(MainPageHost.CurrentPage, page))
                    {
                        try
                        {
                            if (previousPage == null)
                            {
                                MainPageHost.RemovePage(page);
                            }
                            else
                            {
                                await MainPageHost.ReplaceAsync(previousPage, null);
                            }
                        }
                        catch (Exception restoreException)
                        {
                            logger.LogError(restoreException, "无法还原主视图的上一个页面。");
                        }

                        if (!ReferenceEquals(MainPageHost.CurrentPage, page))
                        {
                            _pages.Remove(info.Id);
                        }
                    }

                    if (VisualRoot != null)
                    {
                        ToastsHelper.ShowErrorToast(this, "无法打开此页面，请重新选择页面后重试。");
                    }
                }

                if (page != null && ReferenceEquals(MainPageHost.CurrentPage, page))
                {
                    _currentNavigationItem = item;
                    MainNavigation.Header = info.HidePageTitle ? null : info.Name;
                    MainNavigation.Classes.Set("full-width-title", info.UseFullWidth);
                    if (VisualRoot != null)
                    {
                        UpdateSchedulePageActivity();
                    }
                }

                if (_pendingNavigationItem == null)
                {
                    SynchronizeSelection();
                }
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private MainViewPageBase GetPage(MainViewPageInfo info)
    {
        if (_pages.TryGetValue(info.Id, out var page))
        {
            return page;
        }

        page = IAppHost.Host!.Services.GetRequiredKeyedService<MainViewPageBase>(info.Id);
        if (page is TodayMainViewPage or WeekMainViewPage)
        {
            page.DataContext = ViewModel;
        }
        page.Template = (IControlTemplate)Resources["MainViewPageTemplate"]!;
        NavigationPage.SetHasNavigationBar(page, false);
        _pages.Add(info.Id, page);
        return page;
    }

    private void SynchronizeSelection()
    {
        _isSynchronizingSelection = true;
        try
        {
            MainNavigation.SelectedItem = _currentNavigationItem;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private void UpdateSchedulePageActivity()
    {
        if (MainPageHost.CurrentPage is TodayMainViewPage)
        {
            ViewModel.ActivateCalendar();
        }
        else
        {
            ViewModel.DeactivateCalendar();
        }

        if (MainPageHost.CurrentPage is WeekMainViewPage)
        {
            ViewModel.RefreshScheduleWeek();
        }
    }

    private void ScheduleWeekHost_OnActivated(object? sender, EventArgs e)
    {
        if (MainPageHost.CurrentPage is WeekMainViewPage)
        {
            ViewModel.RefreshScheduleWeek();
        }

        ViewModel.RefreshCalendar();
    }

    private void Profile_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(Profile.ScheduleType))
        {
            ViewModel.RefreshScheduleModeState();
        }
    }

    internal async Task<bool> AuthorizeCourseEditAsync()
    {
        if (!ViewModel.CanEditMobileSchedule)
        {
            ToastsHelper.ShowWarningToast(this, "当前无法编辑课程，请检查档案编辑权限。");
            return false;
        }
        return await ViewModel.ManagementService.AuthorizeByLevel(
            ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel);
    }

    private async Task<bool> AuthorizeProfileEditAsync()
    {
        if (!ViewModel.CanEditProfileFeatures)
        {
            ToastsHelper.ShowWarningToast(this, "当前无法编辑档案，请检查档案编辑权限。");
            return false;
        }
        return await ViewModel.ManagementService.AuthorizeByLevel(
            ViewModel.ManagementService.CredentialConfig.EditProfileAuthorizeLevel);
    }

    internal async Task ShowCourseInfoAsync(Guid scheduleItemId, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseInfoView(scheduleItemId, date));
    }

    internal async Task ShowClassicCourseInfoAsync(ClassInfo classInfo, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseInfoView(classInfo, date));
    }

    internal async Task ShowCourseEditorAsync(Guid scheduleItemId, DateOnly date)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.ShowDrawer(new CourseEditView(scheduleItemId, date));
    }

    internal MenuFlyout? ShowMainMoreMenu(Control anchor)
    {
        if (AssociatedViewHost == null)
        {
            return null;
        }

        var menu = new MenuFlyout();
        var profile = new MenuItem
        {
            Header = $"当前档案：{System.IO.Path.GetFileName(ViewModel.ProfileService.CurrentProfilePath)}",
            Icon = new FluentIcon(Core.Icons.FluentIcons.PersonRegular)
        };
        profile.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ProfileManagementView());
            }
        };
        var mode = new MenuItem
        {
            Header = $"课程模式：{(ViewModel.IsScheduleMode ? "日程" : "经典")}",
            Icon = new FluentIcon(Core.Icons.FluentIcons.OptionsRegular)
        };
        mode.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ScheduleModeView());
            }
        };
        var transfer = new MenuItem
        {
            Header = "导入/导出课表",
            Icon = new FluentIcon(Core.Icons.FluentIcons.ArrowImportRegular)
        };
        transfer.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowManagementDrawerAsync(new ImportExportView());
            }
        };
        var subjects = new MenuItem
        {
            Header = "管理科目…",
            Icon = new FluentIcon(Core.Icons.FluentIcons.BookOpenRegular)
        };
        subjects.Click += async (_, _) =>
        {
            if (await AuthorizeProfileEditAsync())
            {
                await ShowSubjectManagementAsync();
            }
        };
        menu.Items.Add(profile);
        menu.Items.Add(mode);
        menu.Items.Add(transfer);
        menu.Items.Add(subjects);
        menu.ShowAt(anchor);
        return menu;
    }

    private async Task ShowManagementDrawerAsync(ViewBase view)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }
        await AssociatedViewHost.ShowDrawer(view);
    }

    private async Task ShowSubjectManagementAsync()
    {
        if (AssociatedViewHost == null)
        {
            return;
        }
        var host = AssociatedViewHost;
        var view = new SubjectManagementView();
        if (host.ActivateView(view))
        {
            await host.ShowView(view);
        }
    }

    internal async void OpenClassSwapWindow()
    {
        if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }
        if (ViewModel.LessonsService.CurrentClassPlan == null) // 如果今天没有课程，则选择临时课表
        {
            var window = App.GetService<ProfileSettingsWindow>();
            window.OpenDrawer("TemporaryClassPlan");
            window.Open();
            return;
        }

        if (ClassChangingWindow != null)
        {
            return;
        }

        // ViewModel.IsBusy = true;
        ClassChangingWindow = new ClassChangingWindow()
        {
            ClassPlan = ViewModel.LessonsService.CurrentClassPlan
        };
        await ClassChangingWindow.ShowModal(this);
        ClassChangingWindow.DataContext = null;
        ClassChangingWindow = null;
        // ViewModel.IsBusy = false;
    }

}
