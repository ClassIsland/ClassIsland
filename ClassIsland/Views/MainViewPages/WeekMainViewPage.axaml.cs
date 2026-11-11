using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Controls;
using ClassIsland.Controls.ScheduleWeekEdit;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Icons;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.MainViewPages;

[MainViewPageInfo("classisland.week", "本周", FluentIcons.CalendarWeekStartRegular)]
[FullWidthPage]
public partial class WeekMainViewPage : MainViewPageBase
{
    private MainView Owner => VisualExtensions.FindAncestorOfType<MainView>(this)!;
    private MainViewViewModel ViewModel => (MainViewViewModel)DataContext!;

    public WeekMainViewPage()
    {
        InitializeComponent();
        TopCommandBar = new ScheduleMainViewCommandBar(this);
    }

    private void ScheduleWeekPrevious_OnClick(object? sender, RoutedEventArgs e) => ViewModel.MoveScheduleWeek(-1);
    private void ScheduleWeekNext_OnClick(object? sender, RoutedEventArgs e) => ViewModel.MoveScheduleWeek(1);
    private void ScheduleWeekToday_OnClick(object? sender, RoutedEventArgs e) => ViewModel.GoToCurrentScheduleWeek();

    private async void ScheduleWeekEditor_OnCourseTapped(object? sender, ScheduleWeekEditEventArgs e)
    {
        if (e.ScheduleItemId is not { } id)
        {
            return;
        }

        var owner = Owner;
        if (ViewModel.IsScheduleEditing)
        {
            if (await owner.AuthorizeCourseEditAsync())
            {
                await owner.ShowCourseEditorAsync(id, e.Date);
            }
            return;
        }

        await owner.ShowCourseInfoAsync(id, e.Date);
    }

    private async void ScheduleWeekEditor_OnCreateRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        var owner = Owner;
        if (!await owner.AuthorizeCourseEditAsync())
        {
            return;
        }
        try
        {
            var created = ViewModel.CreateScheduleItem(e.Date, e.StartTime);
            await owner.ShowCourseEditorAsync(created.Id, e.Date);
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(owner, "无法新建课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnEditRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        var owner = Owner;
        if (!await owner.AuthorizeCourseEditAsync())
        {
            return;
        }
        try
        {
            if (!ViewModel.ApplyScheduleWeekEdit(e))
            {
                ToastsHelper.ShowWarningToast(owner, "无法移动到该日期或时间，请检查课程的时间和启用范围。");
            }
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(owner, "无法保存课程移动", exception);
        }
    }

    private async void ScheduleWeekEditor_OnDeleteRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        var owner = Owner;
        if (e.ScheduleItemId is not { } id || !await owner.AuthorizeCourseEditAsync() ||
            !ViewModel.ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return;
        }
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
        ViewModel.ProfileService.Profile.ScheduleItems.Remove(id);
        try
        {
            ViewModel.ProfileService.SaveProfile();
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Add(id, item);
            ToastsHelper.ShowErrorToast(owner, "无法删除课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnDuplicateRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        var owner = Owner;
        if (e.ScheduleItemId is not { } id || !await owner.AuthorizeCourseEditAsync() ||
            !ViewModel.ProfileService.Profile.ScheduleItems.TryGetValue(id, out var item))
        {
            return;
        }
        var copy = ConfigureFileHelper.CopyObject(item);
        var copyId = Guid.NewGuid();
        ViewModel.ProfileService.Profile.ScheduleItems.Add(copyId, copy);
        try
        {
            ViewModel.ProfileService.SaveProfile();
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Remove(copyId);
            ToastsHelper.ShowErrorToast(owner, "无法复制课程", exception);
        }
    }

    private async void ScheduleWeekEditor_OnForkAndReplaceRequested(object? sender, ScheduleWeekEditEventArgs e)
    {
        var owner = Owner;
        if (e.ScheduleItemId is not { } id || !await owner.AuthorizeCourseEditAsync())
        {
            return;
        }
        var newId = ViewModel.ProfileService.ForkAndReplaceScheduleItem(
            id, e.Date.ToDateTime(TimeOnly.MinValue));
        if (newId == null)
        {
            ToastsHelper.ShowWarningToast(owner, "无法创建临时覆盖课程，请重新选择课程后重试。");
            return;
        }
        try
        {
            ViewModel.ProfileService.SaveProfile();
            ToastsHelper.ShowToast(owner, "已创建临时覆盖课程。");
        }
        catch (Exception exception)
        {
            ViewModel.ProfileService.Profile.ScheduleItems.Remove(newId.Value);
            ToastsHelper.ShowErrorToast(owner, "无法创建课程覆盖", exception);
        }
    }

    private void ScheduleWeekEditor_OnDayHeaderClicked(object? sender, ScheduleDayHeaderEventArgs e)
    {
        var owner = Owner;
        ScheduleAdjustmentMenu.Show(owner, e.Anchor, e.Date, owner.AuthorizeCourseEditAsync);
        e.Handled = true;
    }
}
