using System.ComponentModel;
using System.Windows;

namespace ClassIsland.Views;

/// <summary>
/// UpdateProgressWindow.xaml 的交互逻辑
/// </summary>
public partial class UpdateProgressWindow
{
    public static readonly DependencyProperty ProgressTextProperty = DependencyProperty.Register(
        nameof(ProgressText), typeof(string), typeof(UpdateProgressWindow), new PropertyMetadata(default(string)));

    public string ProgressText
    {
        get { return (string)GetValue(ProgressTextProperty); }
        set { SetValue(ProgressTextProperty, value); }
    }

    public bool CanClose { get; set; } = false;

    public UpdateProgressWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void UpdateProgressWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (CanClose)
        {
            return;
        }
        e.Cancel = true;
    }
}