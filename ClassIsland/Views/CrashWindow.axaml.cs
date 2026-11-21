using System;
using System.Diagnostics;
using System.Web;
using System.Windows;
using Avalonia;
using Avalonia.Interactivity;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;

namespace ClassIsland.Views;

/// <summary>
/// CrashWindow.xaml 的交互逻辑
/// </summary>
public partial class CrashWindow : ViewBase
{
    public static readonly StyledProperty<string> CrashInfoProperty = AvaloniaProperty.Register<CrashWindow, string>(
        nameof(CrashInfo));

    public string CrashInfo
    {
        get => GetValue(CrashInfoProperty);
        set => SetValue(CrashInfoProperty, value);
    }

    public bool IsCritical { get; set; } = false;

    public bool AllowIgnore { get; set; } = true;

    public CrashWindow()
    {
        InitializeComponent();
        if (PlatformHelper.IsAppleMobile)
        {
            DebugButton.IsVisible = false;
        }

        DataContext = this;
    }

    private void ButtonIgnore_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsCritical)
        {
            if (PlatformHelper.IsAppleMobile)
            {
                (AppBase.Current as App)?.PrepareForAppleMobileManualTermination();
                return;
            }

            Environment.Exit(1);
        }
        else
        {
            if (PlatformHelper.IsAppleMobile)
            {
                (AppBase.Current as App)?.PrepareForAppleMobileManualTermination();
                return;
            }

            AppBase.Current.Stop();
        }
    }

    private void ButtonRestart_OnClick(object sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart();
    }

    private void ButtonCopy_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCrashInfo.Focus();
        TextBoxCrashInfo.SelectAll();
        TextBoxCrashInfo.Copy();
    }

    private void ButtonFeedback_OnClick(object sender, RoutedEventArgs e)
    {
        var uri = new UriBuilder(
            $"https://github.com/ClassIsland/ClassIsland/issues/new");
        uri.Query = 
            $"template=BugReport.yml&stacktrace={HttpUtility.UrlEncode(CrashInfo)}&app_version={HttpUtility.UrlEncode(App.AppVersionLong)}&os_version={HttpUtility.UrlEncode(Environment.OSVersion.Version.ToString())}";
        PlatformServices.LauncherService.LaunchUrl(uri.ToString());
    }

    private void ButtonDebug_OnClick(object sender, RoutedEventArgs e)
    {
        if (Debugger.Launch())
        {
            Close();
        }
    }
}
