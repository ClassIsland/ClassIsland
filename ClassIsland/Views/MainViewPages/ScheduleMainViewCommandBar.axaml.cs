using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;

namespace ClassIsland.Views.MainViewPages;

public partial class ScheduleMainViewCommandBar : UserControl
{
    private readonly MainViewPageBase _page;
    private MenuFlyout? _menu;

    public ScheduleMainViewCommandBar(MainViewPageBase page)
    {
        _page = page;
        InitializeComponent();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _menu?.Hide();
        _menu = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void EditScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_page.FindAncestorOfType<MainView>() is { ViewModel.CanEditMobileSchedule: true } owner)
        {
            owner.ViewModel.IsScheduleEditing = true;
        }
    }

    private void FinishEditingButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_page.FindAncestorOfType<MainView>() is { } owner)
        {
            owner.ViewModel.IsScheduleEditing = false;
        }
    }

    private async void AddScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_page.FindAncestorOfType<MainView>() is not { } owner || !await owner.AuthorizeCourseEditAsync())
        {
            return;
        }

        var viewModel = owner.ViewModel;
        var date = DateOnly.FromDateTime(viewModel.SelectedDate);
        TimeSpan? time = null;
        if (_page is WeekMainViewPage)
        {
            var today = IAppHost.GetService<IExactTimeService>().GetCurrentLocalDateTime();
            date = viewModel.ScheduleWeekSelectedDate ??
                   viewModel.ScheduleWeekStart.AddDays(((int)today.DayOfWeek + 6) % 7);
            time = viewModel.ScheduleWeekSelectedTime;
        }

        try
        {
            var created = viewModel.CreateScheduleItem(date, time);
            await owner.ShowCourseEditorAsync(created.Id, date);
        }
        catch (Exception exception)
        {
            ToastsHelper.ShowErrorToast(owner, "无法新建课程", exception);
        }
    }

    private void MainMoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control anchor && _page.FindAncestorOfType<MainView>() is { } owner)
        {
            _menu?.Hide();
            _menu = owner.ShowMainMoreMenu(anchor);
        }
    }
}
