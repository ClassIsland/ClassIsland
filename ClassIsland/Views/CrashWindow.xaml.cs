using System.Windows;

using ClassIsland.Controls;

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