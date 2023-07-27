using System;
using System.Windows;
using Microsoft.AppCenter.Crashes;

namespace ClassIsland.Views;

/// <summary>
/// CrashWindow.xaml 的交互逻辑
/// </summary>
public partial class CrashWindow : Window
{
    public string? CrashInfo
    {
        get;
        set;
    } = "";

    public Exception Exception
    {
        get;
        set;
    } = new();

    public CrashWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnInitialized(EventArgs e)
    {
        Crashes.TrackError(Exception);
        base.OnInitialized(e);
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
        App.ReleaseLock();
        System.Windows.Forms.Application.Restart();
        Application.Current.Shutdown();
    }

    private void ButtonCopy_OnClick(object sender, RoutedEventArgs e)
    {
        TextBoxCrashInfo.Focus();
        TextBoxCrashInfo.SelectAll();
        TextBoxCrashInfo.Copy();
    }
}