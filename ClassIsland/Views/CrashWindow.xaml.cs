using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ClassIsland.Controls;
using Microsoft.AppCenter.Crashes;

namespace ClassIsland.Views;

/// <summary>
/// CrashWindow.xaml 的交互逻辑
/// </summary>
public partial class CrashWindow : MyWindow, INotifyPropertyChanged
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

    public ObservableCollection<Exception> InnerExceptions { get; set; } = new();

    public CrashWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnInitialized(EventArgs e)
    {
        
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        Crashes.TrackError(Exception);
        //Console.WriteLine(Exception);
        base.OnContentRendered(e);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void Expander_OnExpanded(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.Manual;
        WindowState = WindowState.Maximized;
    }

    private void Expander_OnCollapsed(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
        SizeToContent = SizeToContent.Height;
    }

    private void CrashWindow_OnStateChanged(object? sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Normal:
                DetailExpander.IsExpanded = false;
                SizeToContent = SizeToContent.Height;
                break;
            case WindowState.Minimized:
                break;
            case WindowState.Maximized:

                DetailExpander.IsExpanded = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}