using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;
using Sentry;

namespace ClassIsland.Views;

public partial class ProfileSettingsWindow : MyWindow
{
    private bool _isOpen = false;
    
    public ProfileSettingsViewModel ViewModel { get; } = IAppHost.GetService<ProfileSettingsViewModel>();
    
    public ProfileSettingsWindow()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void OpenDrawer(string key)
    {
        ViewModel.IsDrawerOpen = true;
        if (this.FindResource(key) is {} o)
        {
            ViewModel.DrawerContent = o;
        }
    }
    
    public async void Open()
    {
        if (!_isOpen)
        {
            if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.open");
            _isOpen = true;
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

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _isOpen = false;
        Hide();
    }

    private void ButtonCreateTempOverlayClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var key = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        var id = ViewModel.ProfileService.CreateTempClassPlan(key,
            ViewModel.TempOverlayClassPlanTimeLayoutId,
            ViewModel.OverlayEnableDateTime);
        if (id is { } guid)
        {
            ViewModel.SelectedClassPlan = ViewModel.ProfileService.Profile.ClassPlans[guid];
            OpenDrawer("ClassPlansInfoEditor");
            FlyoutHelper.CloseAncestorFlyout(sender);
        }
        else
        {
            this.ShowToast(new ToastMessage("在这一天已存在一个临时层课表，无法创建新的临时层课表。")
            {
                Severity = InfoBarSeverity.Warning,
                Duration = TimeSpan.FromSeconds(5)
            });
        }

    }

    private void ButtonOpenClassPlanDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonAddClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var newClassPlan = new ClassPlan();
        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), newClassPlan);
        ViewModel.SelectedClassPlan = newClassPlan;
        OpenDrawer("ClassPlansInfoEditor");
    }

    private void ButtonDeleteSelectedClassPlan_OnClick(object? sender, RoutedEventArgs e)
    {
        var k = ViewModel.ProfileService.Profile.ClassPlans
            .FirstOrDefault(x => x.Value == ViewModel.SelectedClassPlan).Key;
        ViewModel.ProfileService.Profile.ClassPlans.Remove(k);
        foreach (var (key, _) in ViewModel.ProfileService.Profile.OrderedSchedules.Where(x => x.Value.ClassPlanId == k).ToList())
        {
            ViewModel.ProfileService.Profile.OrderedSchedules.Remove(key);
        }
        FlyoutHelper.CloseAncestorFlyout(sender);
    }

    private void ButtonOpenCreateOverlayClassPlanFlyout_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedClassPlan == null)
        {
            return;
        }
        ViewModel.OverlayEnableDateTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime().Date;
        ViewModel.TempOverlayClassPlanTimeLayoutId = ViewModel.SelectedClassPlan.TimeLayoutId;
    }
    
    private void ButtonDuplicateClassPlan_OnClick(object sender, RoutedEventArgs e)
    {
        var s = ConfigureFileHelper.CopyObject(ViewModel.SelectedClassPlan);
        if (s == null)
        {
            return;
        }

        OpenDrawer("ClassPlansInfoEditor");
        ViewModel.ProfileService.Profile.ClassPlans.Add(Guid.NewGuid(), s);
        ViewModel.SelectedClassPlan = s;
        SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.classPlan.duplicate");
    }
}