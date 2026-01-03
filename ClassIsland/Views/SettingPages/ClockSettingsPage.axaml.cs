using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.general")]
[SettingsPageInfo("clock", "时钟", "\ue4c4", "\ue4c3", SettingsPageCategory.Internal)]
public partial class ClockSettingsPage : SettingsPageBase
{
    public ClockSettingsViewModel ViewModel { get; } = IAppHost.GetService<ClockSettingsViewModel>();
    
    public ClockSettingsPage()
    {
        DataContext = this;
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.PostMainTimerTicked += LessonsServiceOnPostMainTimerTicked;
    }

    private void LessonsServiceOnPostMainTimerTicked(object? sender, EventArgs e)
    {
        ViewModel.CurrentTime = ViewModel.ExactTimeService.GetCurrentLocalDateTime();
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.LessonsService.PostMainTimerTicked -= LessonsServiceOnPostMainTimerTicked;
    }

    private void ButtonAdjustTime_OnClick(object sender, RoutedEventArgs e)
    {
        var window = IAppHost.GetService<TimeAdjustmentWindow>();
        window.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }

    private void ButtonSyncTimeNow_OnClick(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(ViewModel.ExactTimeService.Sync);
    }
}