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
using ClassIsland.Core.Helpers;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels.SettingsPages;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// WindowSettingsPage.xaml 的交互逻辑
/// </summary>
[Group("classisland.mainwindow")]
[SettingsPageInfo("window", "窗口", "\uf485", "\uf484", SettingsPageCategory.Internal)]
public partial class WindowSettingsPage : SettingsPageBase
{
    public WindowSettingsViewModel ViewModel { get; private set; } = null!;
    private DispatcherTimer? _taskbarTimer;
    private bool _isSettingsSubscribed;

    public WindowSettingsPage()
    {
        InitializeComponent();

        if (PlatformHelper.IsAppleMobile)
        {
            Content = new TextBlock
            {
                Text = "该系统不支持",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            return;
        }

        ViewModel = IAppHost.GetService<WindowSettingsViewModel>();
        DataContext = this;
        
        _taskbarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _taskbarTimer.Tick += TaskbarTimer_Tick;
        _taskbarTimer.Start();
        TaskbarTimer_Tick();
        ViewModel.SettingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
        _isSettingsSubscribed = true;
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
        if (!_isSettingsSubscribed)
        {
            return;
        }

        ViewModel.SettingsService.Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _isSettingsSubscribed = false;
        if (_taskbarTimer != null)
        {
            _taskbarTimer.Stop();
            _taskbarTimer.Tick -= TaskbarTimer_Tick;
            _taskbarTimer = null;
        }
    }
}
