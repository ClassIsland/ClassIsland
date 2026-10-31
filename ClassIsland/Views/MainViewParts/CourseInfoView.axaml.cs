using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Extensions;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels.MainViewParts;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.MainViewParts;

public partial class CourseInfoView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private readonly IManagementService _managementService = IAppHost.GetService<IManagementService>();
    private readonly DateOnly _date;
    private readonly CourseInfoViewModel _viewModel;

    public CourseInfoView(Guid scheduleItemId, DateOnly date)
    {
        _date = date;
        _viewModel = new CourseInfoViewModel(_profileService, _managementService, scheduleItemId);
        InitializeComponent();
        DataContext = _viewModel;
        Closed += CourseInfoView_OnClosed;
    }

    public CourseInfoView(ClassInfo classInfo, DateOnly date)
    {
        _date = date;
        _viewModel = new CourseInfoViewModel(_profileService, _managementService, classInfo);
        InitializeComponent();
        DataContext = _viewModel;
        Closed += CourseInfoView_OnClosed;
    }

    private void CourseInfoView_OnClosed(object? sender, RoutedEventArgs e)
    {
        _viewModel.Dispose();
        Closed -= CourseInfoView_OnClosed;
    }

    private void InfoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string text } || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var clipboard = TopLevel?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        _ = clipboard.SetTextAsync(text);
        ToastsHelper.ShowToast(this, "已复制到剪贴板。");
    }

    private async void EditButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.Item == null || AssociatedViewHost == null || !_viewModel.CanEdit)
        {
            return;
        }
        if (!await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }

        if (_viewModel.Item.ReplacementTarget != null)
        {
            await OpenEditorAsync(_viewModel.ResolvedScheduleItemId);
            return;
        }

        var menu = new MenuFlyout();
        menu.Items.Add(CreateAction("编辑当天课程", async () => await OpenDailyEditorAsync()));
        menu.Items.Add(CreateAction("编辑所有课程", async () => await OpenEditorAsync(_viewModel.ResolvedScheduleItemId)));
        if (sender is Control anchor)
        {
            menu.ShowAt(anchor);
        }
    }

    private async void DisableButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.Item == null || !_viewModel.CanEdit)
        {
            return;
        }
        if (!await _managementService.AuthorizeByLevel(_managementService.CredentialConfig.ChangeLessonsAuthorizeLevel))
        {
            return;
        }

        if (_viewModel.Item.ReplacementTarget != null)
        {
            ToggleEnabled(_viewModel.Item);
            return;
        }

        var menu = new MenuFlyout();
        menu.Items.Add(CreateAction("禁用当天课程", async () =>
        {
            var id = EnsureDailyOverride(_viewModel.ResolvedScheduleItemId);
            var daily = _profileService.Profile.ScheduleItems.GetValueOrDefault(id);
            if (daily != null)
            {
                var created = id != _viewModel.ResolvedScheduleItemId;
                var oldValue = daily.IsEnabled;
                daily.IsEnabled = !daily.IsEnabled;
                try
                {
                    _profileService.SaveProfile(() =>
                    {
                        daily.IsEnabled = oldValue;
                        if (created)
                        {
                            _profileService.Profile.ScheduleItems.Remove(id);
                        }
                    });
                    _viewModel.SwitchToScheduleItem(id);
                    var message = daily.IsEnabled ? "已启用当天课程。" : "已禁用当天课程。";
                    ToastsHelper.ShowToast(this, created ? $"已创建分叉课程。{message}" : message);
                }
                catch (Exception exception)
                {
                    ToastsHelper.ShowErrorToast(this, "无法保存课程修改", exception);
                }
            }
            await Task.CompletedTask;
        }));
        menu.Items.Add(CreateAction("禁用所有课程", () =>
        {
            ToggleEnabled(_viewModel.Item);
            return Task.CompletedTask;
        }));
        if (sender is Control anchor)
        {
            menu.ShowAt(anchor);
        }
    }

    private MenuItem CreateAction(string header, Func<Task> action)
    {
        var item = new MenuItem { Header = header };
        item.Click += async (_, _) => await action();
        return item;
    }

    private void ToggleEnabled(ScheduleItem item)
    {
        var oldValue = item.IsEnabled;
        item.IsEnabled = !item.IsEnabled;
        try
        {
            _profileService.SaveProfile(() => item.IsEnabled = oldValue);
            ToastsHelper.ShowToast(this, item.IsEnabled ? "已启用课程。" : "已禁用课程。");
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法保存课程修改", exception);
        }
    }

    private Guid EnsureDailyOverride(Guid id)
    {
        if (_profileService.Profile.ScheduleItems.GetValueOrDefault(id)?.ReplacementTarget != null)
        {
            return id;
        }

        return _profileService.ForkAndReplaceScheduleItem(id, _date.ToDateTime(TimeOnly.MinValue)) ?? id;
    }

    private async Task OpenDailyEditorAsync()
    {
        var id = EnsureDailyOverride(_viewModel.ResolvedScheduleItemId);
        var created = id != _viewModel.ResolvedScheduleItemId;
        try
        {
            _profileService.SaveProfile(() =>
            {
                if (created)
                {
                    _profileService.Profile.ScheduleItems.Remove(id);
                }
            });
            if (created)
            {
                ToastsHelper.ShowToast(this, "已创建分叉课程。");
            }
            await OpenEditorAsync(id);
            _viewModel.SwitchToScheduleItem(id);
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法创建当天课程覆盖", exception);
        }
    }

    private async Task OpenEditorAsync(Guid id)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }
        await AssociatedViewHost.PushDrawerView(new CourseEditView(id, _date));
    }
}
