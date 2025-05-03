using System;
using System.Diagnostics;
using System.Web;
using System.Windows;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Services;

namespace ClassIsland.Views;

/// <summary>
/// CrashWindow.xaml 的交互逻辑
/// </summary>
public partial class CrashWindow : MyWindow
{
    public string? CrashInfo
    {
        get;
        set;
    } = "";

    public bool IsCritical { get; set; } = false;

    public bool AllowIgnore { get; set; } = true;

    public CrashWindow()
    {
        InitializeComponent();
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
            Environment.Exit(1);
        }
        else
        {
            Application.Current.Shutdown();
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
        Process.Start(new ProcessStartInfo()
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
    }

    private void ButtonDebug_OnClick(object sender, RoutedEventArgs e)
    {
        if (Debugger.Launch())
        {
            Close();
        }
    }
}