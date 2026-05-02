using Avalonia.Controls;
using ClassIsland.ViewModels;

namespace ClassIsland.Views.WelcomePages;

public partial class AppearancePage : UserControl, IWelcomePage
{
    public AppearancePage()
    {
        InitializeComponent();
    }

    public WelcomeViewModel ViewModel { get; set; } = null!;
}