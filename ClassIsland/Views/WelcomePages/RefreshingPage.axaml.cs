using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ClassIsland.ViewModels;

namespace ClassIsland.Views.WelcomePages;

public partial class RefreshingPage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;
    
    public RefreshingPage()
    {
        InitializeComponent();
    }
}