using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Extensions;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels.MainViewParts;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.MainViewParts;

public partial class CourseEditView : ViewBase
{
    private readonly IProfileService _profileService = IAppHost.GetService<IProfileService>();
    private readonly CourseEditViewModel _viewModel;
    private bool _isLeaving;
    private int _scrollResetAttempts;

    public CourseEditView(Guid itemId, DateOnly date)
    {
        _viewModel = new CourseEditViewModel(_profileService, itemId, date);
        InitializeComponent();
        DataContext = _viewModel;
        Closing += CourseEditView_OnClosing;
        Closed += CourseEditView_OnClosed;
        Loaded += CourseEditView_OnLoaded;
    }

    private void CourseEditView_OnClosed(object? sender, RoutedEventArgs e)
    {
        CourseTimeRuleEditControl.ReleaseResources();
        _viewModel.Dispose();
        Closed -= CourseEditView_OnClosed;
    }

    private void CourseEditView_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _scrollResetAttempts = 0;
        CourseEditScrollViewer.LayoutUpdated -= CourseEditScrollViewer_OnLayoutUpdated;
        CourseEditScrollViewer.LayoutUpdated += CourseEditScrollViewer_OnLayoutUpdated;
        ResetScrollToTop();
        Dispatcher.UIThread.Post(ResetScrollToTop, DispatcherPriority.Loaded);
        Dispatcher.UIThread.Post(ResetScrollToTop, DispatcherPriority.Background);
        DispatcherTimer.RunOnce(ResetScrollToTop, TimeSpan.FromMilliseconds(150));
    }

    private void CourseEditScrollViewer_OnLayoutUpdated(object? sender, EventArgs e)
    {
        ResetScrollToTop();
        if (++_scrollResetAttempts >= 4)
        {
            CourseEditScrollViewer.LayoutUpdated -= CourseEditScrollViewer_OnLayoutUpdated;
        }
    }

    private void ResetScrollToTop()
    {
        if (TopLevel != null)
        {
            CourseEditScrollViewer.Offset = default;
        }
    }

    private void CourseEditView_OnClosing(object? sender, ViewClosingEventArgs e)
    {
        if (_isLeaving)
        {
            return;
        }

        try
        {
            if (!_viewModel.Commit() && e.IsCancelable)
            {
                e.Cancel = true;
                ToastsHelper.ShowWarningToast(this, "结束时间必须晚于开始时间。");
            }
        }
        catch (Exception exception)
        {
            if (e.IsCancelable)
            {
                e.Cancel = true;
            }
            ToastsHelper.ShowErrorToast(this, "无法保存课程修改", exception);
        }
    }

    private void CommitField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.Commit();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法保存课程修改", exception);
        }
    }

    private void EnabledCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        CommitField_OnLostFocus(sender, e);
    }

    private async void RemoveReplacementButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.Item.ReplacementTarget == null || AssociatedViewHost == null)
        {
            return;
        }

        try
        {
            _isLeaving = true;
            _viewModel.Delete();
            ToastsHelper.ShowToast(this, "已移除替换课程，原课程已恢复。");
            await AssociatedViewHost.HideDrawer();
        }
        catch (Exception exception)
        {
            _isLeaving = false;
            ToastsHelper.ShowErrorToast(this, "无法移除替换课程", exception);
        }
    }

    private void ConvertToNormalButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ConvertToNormal();
            ToastsHelper.ShowToast(this, "已转换为普通课程。");
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法转换为普通课程", exception);
        }
    }

    private async void SubjectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (AssociatedViewHost == null)
        {
            return;
        }

        await AssociatedViewHost.PushDrawerView(new SubjectPickerView(id =>
        {
            _viewModel.Item.SubjectId = id;
            CommitField_OnLostFocus(this, new RoutedEventArgs());
        }));
    }

    private void MoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control anchor)
        {
            return;
        }

        var menu = new MenuFlyout();
        var copy = new MenuItem
        {
            Header = "复制",
            Icon = new FluentIcon(ClassIsland.Core.Icons.FluentIcons.CopyRegular)
        };
        copy.Click += async (_, _) => await DuplicateAsync();
        var delete = new MenuItem
        {
            Header = "删除",
            Icon = new FluentIcon(ClassIsland.Core.Icons.FluentIcons.DeleteRegular)
        };
        delete.Click += async (_, _) => await DeleteAsync();
        var fork = new MenuItem
        {
            Header = "分叉并替换",
            Icon = new FluentIcon(ClassIsland.Core.Icons.FluentIcons.BranchForkRegular),
            IsEnabled = !_viewModel.Item.HasReplacementTarget
        };
        fork.Click += async (_, _) => await ForkAsync();
        menu.Items.Add(copy);
        menu.Items.Add(delete);
        menu.Items.Add(new Separator());
        menu.Items.Add(fork);
        menu.ShowAt(anchor);
    }

    private async Task DuplicateAsync()
    {
        var source = _viewModel.Item;
        var copy = ConfigureFileHelper.CopyObject(source);
        var id = Guid.NewGuid();
        _profileService.Profile.ScheduleItems.Add(id, copy);
        try
        {
            _profileService.SaveProfile(() => _profileService.Profile.ScheduleItems.Remove(id));
            ToastsHelper.ShowToast(this, "已复制课程。");
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法复制课程", exception);
        }
        await Task.CompletedTask;
    }

    private async Task DeleteAsync()
    {
        var result = await new FAContentDialog
        {
            Title = "删除课程",
            Content = "删除后，这门课程的所有重复安排都会被移除。",
            PrimaryButtonText = "删除",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto();
        if (result != FAContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            _isLeaving = true;
            _viewModel.Delete();
            AssociatedViewHost?.HideDrawer();
        }
        catch (Exception exception)
        {
            _isLeaving = false;
            ToastsHelper.ShowErrorToast(this, "无法删除课程", exception);
        }
    }

    private async Task ForkAsync()
    {
        var oldId = _viewModel.ItemId;
        var newId = _profileService.ForkAndReplaceScheduleItem(
            oldId, _viewModel.Date.ToDateTime(TimeOnly.MinValue));
        if (newId == null)
        {
            ToastsHelper.ShowWarningToast(this, "无法创建当天的课程覆盖。");
            return;
        }
        try
        {
            _profileService.SaveProfile(() => _profileService.Profile.ScheduleItems.Remove(newId.Value));
            ToastsHelper.ShowToast(this, "已创建当天课程覆盖。");
            await AssociatedViewHost!.HideDrawer();
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(this, "无法保存课程覆盖", exception);
        }
    }
}
