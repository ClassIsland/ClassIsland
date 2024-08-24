using System;
using System.Diagnostics;
using System.Web;
using System.Windows;

using ClassIsland.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Controls;

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
        Application.Current.Shutdown();
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
            $"assignees=HelloWRC&labels=bug&projects=&template=BugReport.yml&title=%5BBug%5D%3A+%E5%9C%A8%E8%BF%99%E9%87%8C%E8%BE%93%E5%85%A5%E4%BD%A0%E7%9A%84%E6%A0%87%E9%A2%98&stacktrace={HttpUtility.UrlEncode(CrashInfo)}&app_version={HttpUtility.UrlEncode(App.AppVersionLong)}&os_version={HttpUtility.UrlEncode(Environment.OSVersion.Version.ToString())}";
        Process.Start(new ProcessStartInfo()
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });

    }
}