using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.ViewModels;

namespace ClassIsland.Views.WelcomePages;

public partial class GeneralPage : UserControl, IWelcomePage
{
    public GeneralPage()
    {
        InitializeComponent();
    }

    public WelcomeViewModel ViewModel { get; set; } = null!;

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        WelcomeWindow.WelcomeNavigateForwardCommand.Execute(this);
    }
}