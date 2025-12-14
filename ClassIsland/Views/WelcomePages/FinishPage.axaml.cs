using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ClassIsland.Views.WelcomePages;

public partial class FinishPage : UserControl
{
    public FinishPage()
    {
        InitializeComponent();
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        Carousel.SelectedIndex++;
    }

    private void ButtonPrevious_OnClick(object? sender, RoutedEventArgs e)
    {
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