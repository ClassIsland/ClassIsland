using Avalonia.Controls;
using ClassIsland.ViewModels;

namespace ClassIsland.Views.WelcomePages;

public partial class SystemPage : UserControl, IWelcomePage
{
    public SystemPage()
    {
        InitializeComponent();
    }

    public WelcomeViewModel ViewModel { get; set; } = null!;
}