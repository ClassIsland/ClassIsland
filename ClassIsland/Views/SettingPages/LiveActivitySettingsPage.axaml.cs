using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Icons;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

[Group("classisland.mainwindow")]
[SettingsPageInfo("live-activity", "灵动岛展开态", FluentIcons.WindowRegular, FluentIcons.WindowFilled, SettingsPageCategory.Internal)]
public partial class LiveActivitySettingsPage : SettingsPageBase
{
    public LiveActivitySettingsViewModel ViewModel { get; } =
        IAppHost.GetService<LiveActivitySettingsViewModel>();

    public LiveActivitySettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e) => ViewModel.Activate();

    private void OnUnloaded(object? sender, RoutedEventArgs e) => ViewModel.Deactivate();

    private void RestoreDefaults_OnClick(object? sender, RoutedEventArgs e) => ViewModel.RestoreDefaultsCommand.Execute(null);

    private void AddComponent_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LiveActivityComponentKind kind })
        {
            ViewModel.AddComponentCommand.Execute(kind);
        }
    }

    private void MoveUp_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LiveActivityComponentSettings component })
        {
            ViewModel.MoveUpCommand.Execute(component);
        }
    }

    private void MoveDown_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LiveActivityComponentSettings component })
        {
            ViewModel.MoveDownCommand.Execute(component);
        }
    }

    private void Remove_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LiveActivityComponentSettings component })
        {
            ViewModel.RemoveComponentCommand.Execute(component);
        }
    }
}
