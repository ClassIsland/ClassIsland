using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Helpers;

namespace ClassIsland.Views.WelcomePages;

public partial class FinishPage : UserControl
{
    public FinishPage()
    {
        InitializeComponent();
        if (PlatformHelper.IsAppleMobile)
        {
            DesktopTrayTutorial.IsVisible = false;
            DesktopProfileTutorial.IsVisible = false;
            Carousel.SelectedIndex = 2;
            NextButton.IsVisible = false;
        }
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        Carousel.SelectedIndex++;
    }

    private void ButtonPrevious_OnClick(object? sender, RoutedEventArgs e)
    {
        if (PlatformHelper.IsAppleMobile)
        {
            WelcomeWindow.WelcomeNavigateBackCommand.Execute(this);
            return;
        }

        if (Carousel.SelectedIndex <= 0)
        {
            WelcomeWindow.WelcomeNavigateBackCommand.Execute(this);
        }
        else
        {
            Carousel.SelectedIndex--;
        }
    }
}
