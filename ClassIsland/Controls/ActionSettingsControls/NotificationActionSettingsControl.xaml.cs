using System.Linq;
using System.Windows;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Views;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.ActionSettingsControls;

/// <summary>
/// NotificationActionSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class NotificationActionSettingsControl
{
    public static readonly DependencyProperty IsShowInDialogProperty = DependencyProperty.Register(
        nameof(IsShowInDialog), typeof(bool), typeof(NotificationActionSettingsControl), new PropertyMetadata(default(bool)));

    public bool IsShowInDialog
    {
        get { return (bool)GetValue(IsShowInDialogProperty); }
        set { SetValue(IsShowInDialogProperty, value); }
    }

    public NotificationActionSettingsControl()
    {
        InitializeComponent();
    }

    private void ButtonShowSettings_OnClick(object sender, RoutedEventArgs e)
    {
        if (FindResource("SettingsDrawer") is not FrameworkElement drawer)
        {
            return;
        }

        drawer.DataContext = this;
        if (Window.GetWindow(this) is SettingsWindowNew)  // 在应用设置中展示
        {
            IsShowInDialog = false;
            SettingsPageBase.OpenDrawerCommand.Execute(drawer);
        }
        else
        {
            IsShowInDialog = true;
            var dialogHost = VisualTreeUtils.FindParentVisuals<DialogHost>(this).FirstOrDefault();
            dialogHost?.ShowDialog(drawer);
        }
    }
}