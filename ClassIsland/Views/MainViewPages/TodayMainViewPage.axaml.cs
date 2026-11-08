using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Icons;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.ViewModels;

namespace ClassIsland.Views.MainViewPages;

[MainViewPageInfo("classisland.today", "今天", FluentIcons.HomeRegular)]
[FullWidthPage]
public partial class TodayMainViewPage : MainViewPageBase
{
    private MainView Owner => Avalonia.VisualTree.VisualExtensions.FindAncestorOfType<MainView>(this)!;
    private MainViewViewModel ViewModel => (MainViewViewModel)DataContext!;

    public TodayMainViewPage()
    {
        InitializeComponent();
        TopCommandBar = new ScheduleMainViewCommandBar(this);
    }

    private async void HomeLessonsList_OnTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(HomeLessonsList);
        var item = HomeLessonsList.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .FirstOrDefault(container =>
            {
                var topLeft = container.TranslatePoint(default, HomeLessonsList);
                return topLeft is { } position &&
                       new Rect(position, container.Bounds.Size).Contains(point);
            });
        if (item?.DataContext is not TimeLayoutItem timeItem)
        {
            return;
        }

        var owner = Owner;
        var date = DateOnly.FromDateTime(ViewModel.SelectedDate);
        if (ViewModel.IsScheduleMode)
        {
            var id = ViewModel.FindScheduleItemIdByLesson(ViewModel.SelectedDate, timeItem);
            if (id is not { } scheduleItemId)
            {
                return;
            }
            if (ViewModel.IsScheduleEditing)
            {
                if (await owner.AuthorizeCourseEditAsync())
                {
                    await owner.ShowCourseEditorAsync(scheduleItemId, date);
                }
            }
            else
            {
                await owner.ShowCourseInfoAsync(scheduleItemId, date);
            }
            return;
        }

        var classInfo = ViewModel.FindClassInfoByLesson(ViewModel.SelectedDate, timeItem);
        if (classInfo != null)
        {
            await owner.ShowClassicCourseInfoAsync(classInfo, date);
        }
    }

    private void ButtonAdjustSchedule_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanAdjustSchedule || sender is not Control anchor) return;
        ScheduleAdjustmentMenu.Show(Owner, anchor, DateOnly.FromDateTime(ViewModel.SelectedDate),
            () => ViewModel.ManagementService.AuthorizeByLevel(
                ViewModel.ManagementService.CredentialConfig.ChangeLessonsAuthorizeLevel));
        e.Handled = true;
    }
}
