using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Icons;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls;

internal static class ScheduleAdjustmentMenu
{
    public static void Show(Control owner, Control anchor, DateOnly date, Func<Task<bool>> authorize)
    {
        var viewModel = new ScheduleAdjustmentViewModel(IAppHost.GetService<IProfileService>(),
            IAppHost.GetService<IManagementService>(), IAppHost.GetService<ILogger<ScheduleAdjustmentViewModel>>(),
            date, authorize);
        var menu = new MenuFlyout();
        var dateItem = new MenuItem
        {
            Header = "调课为某日…", Icon = new FluentIcon(FluentIcons.CalendarArrowRightRegular) { FontSize = 16 }
        };
        dateItem.Click += (_, _) =>
        {
            menu.Hide();
            ShowDatePicker(owner, anchor, viewModel);
        };
        menu.Items.Add(dateItem);
        menu.Items.Add(new MenuItem
        {
            Header = "调课为无课…", Icon = new FluentIcon(FluentIcons.CalendarEmptyRegular) { FontSize = 16 },
            Command = viewModel.SetEmptyCommand
        });
        menu.Items.Add(new MenuItem
        {
            Header = "移除调课", Icon = new FluentIcon(FluentIcons.ArrowUndoRegular) { FontSize = 16 },
            IsEnabled = viewModel.HasAdjustment, Command = viewModel.RemoveCommand
        });
        viewModel.Feedback += message => owner.ShowToast(message);
        menu.ShowAt(anchor);
    }

    private static void ShowDatePicker(Control owner, Control anchor, ScheduleAdjustmentViewModel viewModel)
    {
        var flyout = new Flyout();
        var calendar = new Calendar
        {
            FirstDayOfWeek = DayOfWeek.Monday, SelectionMode = CalendarSelectionMode.SingleDate,
            DisplayDate = viewModel.SourceDate ?? viewModel.TargetDate,
            Background = Brushes.Transparent, BorderThickness = new Thickness(0)
        };
        calendar.Bind(Calendar.SelectedDateProperty, new Binding(nameof(viewModel.SourceDate))
        {
            Source = viewModel, Mode = BindingMode.TwoWay
        });
        var cancel = new Button { Content = new IconText { Glyph = FluentIcons.DismissRegular, Text = "取消" } };
        cancel.Click += (_, _) => flyout.Hide();
        flyout.Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = $"将 {viewModel.TargetDate:yyyy年M月d日} 的课程调课为", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                calendar,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        cancel,
                        new Button
                        {
                            Content = new IconText { Glyph = FluentIcons.CheckmarkRegular, Text = "确定" },
                            Classes = { "accent" }, Command = viewModel.ApplyDateCommand
                        }
                    }
                }
            }
        };
        viewModel.Applied += flyout.Hide;
        flyout.Closed += (_, _) => viewModel.Applied -= flyout.Hide;
        // Open after the menu has closed so its dismissal cannot also dismiss the date picker.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (TopLevel.GetTopLevel(owner) != null && TopLevel.GetTopLevel(anchor) != null)
                flyout.ShowAt(anchor);
        });
    }
}
