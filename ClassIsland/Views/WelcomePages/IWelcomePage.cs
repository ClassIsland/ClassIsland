using ClassIsland.ViewModels;

namespace ClassIsland.Views.WelcomePages;

public interface IWelcomePage
{
    WelcomeViewModel ViewModel { get; set; }
}