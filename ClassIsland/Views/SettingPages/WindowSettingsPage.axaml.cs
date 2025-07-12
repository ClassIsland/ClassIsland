using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// WindowSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("window", "窗口", "\uf485", "\uf484", SettingsPageCategory.Internal)]
public partial class WindowSettingsPage : SettingsPageBase
{
    public WindowSettingsViewModel ViewModel { get; } = IAppHost.GetService<WindowSettingsViewModel>();

    public WindowSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        
        var taskbarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        taskbarTimer.Tick += TaskbarTimer_Tick;
        taskbarTimer.Start();
        TaskbarTimer_Tick();
        ViewModel.SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        ViewModel.Screens = new ObservableCollection<Screen>(AppBase.Current.MainWindow!.Screens.All);
    }   

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SettingsService.Settings.UseRawInput) or nameof(SettingsService.Settings.IsCompatibleWindowTransparentEnabled))
        {
            RequestRestart();
        }
    }

    private void ButtonRefreshMonitors_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Screens = new ObservableCollection<Screen>(TopLevel.GetTopLevel(this)?.Screens?.All ?? []);
    }

    private void TaskbarTimer_Tick(object? _ = null, EventArgs? e = null)
    {
        var t = DateTime.Now.ToShortTimeString();
        if (DateTime.Now.Second % 2 == 0) t = t.Replace(":", " ");
        TaskbarTime.Text = t;
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        RequestRestart();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }
}
