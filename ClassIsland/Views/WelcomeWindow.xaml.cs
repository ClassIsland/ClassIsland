﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using ClassIsland.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls;
using ClassIsland.Helpers;
using ClassIsland.Services;
using ClassIsland.Services.Management;
using ClassIsland.ViewModels;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Logging;

using WindowsShortcutFactory;

using Path = System.IO.Path;

namespace ClassIsland.Views;
/// <summary>
/// WelcomeWindow.xaml 的交互逻辑
/// </summary>
public partial class WelcomeWindow : MyWindow
{
    public WelcomeViewModel ViewModel
    {
        get;
        set;
    } = new();

    public SettingsService SettingsService { get; } = App.GetService<SettingsService>();

    public IManagementService ManagementService { get; } = App.GetService<IManagementService>();

    public WelcomeWindow()
    {
        DataContext = this;
        InitializeComponent();
        var reader = new StreamReader(Application.GetResourceStream(new Uri("/Assets/License.txt", UriKind.Relative))!
            .Stream);
        ViewModel.License = reader.ReadToEnd();
        ViewModel.Settings = SettingsService.Settings;
        SettingsService.Settings.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(SettingsService.Settings.IsSentryEnabled))
            {
                ViewModel.RequiresRestarting = true;
                ViewModel.SnackbarQueue.Enqueue("应用将在向导完成后自动重启，以应用部分更改。");
            }
        };
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        ViewModel.MasterTabIndex = 1;
    }

    private async void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsExitConfirmed = true;
        DialogResult = true;
        var startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "ClassIsland.lnk");
        var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "ClassIsland.lnk");
        var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ClassIsland.lnk");
        using var shortcut = new WindowsShortcut();
        shortcut.Path = Environment.ProcessPath;
        shortcut.WorkingDirectory = Environment.CurrentDirectory;
        try
        {
            if (ViewModel.CreateStartupShortcut)
                shortcut.Save(startupPath);
            if (ViewModel.CreateStartMenuShortcut)
                shortcut.Save(startMenuPath);
            if (ViewModel.CreateDesktopShortcut)
                shortcut.Save(desktopPath);
            if (ViewModel.RegisterUrlScheme)
                UriProtocolRegisterHelper.Register();
            if (ViewModel is { CreateClassSwapShortcut: true, RegisterUrlScheme: true })
                await ShortcutHelpers.CreateClassSwapShortcutAsync();
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<WelcomeWindow>>().LogError(ex, "无法创建快捷方式。");
        }

        Close();
        if (ViewModel.RequiresRestarting)
        {
            AppBase.Current.Restart();
        }
    }

    private async void WelcomeWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (ViewModel.IsExitConfirmed)
        {
            return;
        }

        e.Cancel = true;
        if (DialogHost.IsDialogOpen(ViewModel.DialogId))
        {
            return;
        }
        var r = await DialogHost.Show(FindResource("ExitAppConfirmDialog"), ViewModel.DialogId);
        if ((bool?)r == true)
        {
            ViewModel.IsExitConfirmed = true;
            Close();
        }
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }

    private void ButtonFlipNext_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FlipNextCount++;
        ViewModel.FlipIndex = ViewModel.FlipIndex + 1 >= 3 ? 0 : ViewModel.FlipIndex + 1;
        if (ViewModel.FlipIndex >= 2)
            ViewModel.IsFlipEnd = true;
    }

    private async void ButtonJoinManagementOnClick(object sender, RoutedEventArgs e)
    {
        await DialogHost.Show(new JoinManagementDialog(), ViewModel.DialogId);
    }

    private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void ButtonPrivacy_OnClick(object sender, RoutedEventArgs e)
    {
        new DocumentReaderWindow()
        {
            Source = new Uri("/Assets/Documents/Privacy_.md", UriKind.RelativeOrAbsolute),
            Owner = this,
            Title = "ClassIsland 隐私政策"
        }.ShowDialog();
    }

    private void ButtonSkip_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SlideIndex = 4;
        ViewModel.SnackbarQueue.Enqueue("您稍后可以在【应用设置】中调整这些设置。");
    }

    private void ButtonCompleteFlipBack_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.FlipIndex > 0)
        {
            ViewModel.FlipIndex--;
            ViewModel.IsFlipEnd = ViewModel.FlipIndex >= 2;
        }
        else
        {
            ViewModel.SlideIndex--;
        }
    }
}