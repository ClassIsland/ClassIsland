using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using Sentry;

namespace ClassIsland.Views;

public partial class ProfileSettingsWindow : MyWindow
{
    private bool _isOpen = false;
    
    public ProfileSettingsViewModel ViewModel { get; } = IAppHost.GetService<ProfileSettingsViewModel>();
    
    public ProfileSettingsWindow()
    {
        DataContext = this;
        InitializeComponent();
    }
    
    public async void Open()
    {
        if (!_isOpen)
        {
            if (!await ViewModel.ManagementService.AuthorizeByLevel(ViewModel.ManagementService.CredentialConfig.EditProfileAuthorizeLevel))
            {
                return;
            }
            SentrySdk.Metrics.Increment("views.ProfileSettingsWindow.open");
            _isOpen = true;
            Show();
        }
        else
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;
        _isOpen = false;
        Hide();
    }
}