using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Sentry;

namespace ClassIsland.Views;

public partial class TutorialCenterWindow : MyWindow
{
    public TutorialCenterViewModel ViewModel { get; } = IAppHost.GetService<TutorialCenterViewModel>();
    
    private bool IsOpened { get; set; } = false;
    
    public TutorialCenterWindow()
    {
        DataContext = this;
        InitializeComponent();
        
    }
    
    public void Open()
    {
        if (!IsOpened)
        {
            SentrySdk.Metrics.Increment("views.TutorialCenterWindow.open");
            IsOpened = true;
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

    [RelayCommand]
    private void SetSelectedTutorial(Tutorial tutorial)
    {
        ViewModel.SelectedTutorial = tutorial;
    }

    private void ButtonPlaySelectedTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedTutorial != null) 
            ViewModel.TutorialService.BeginTutorial(ViewModel.SelectedTutorial);
    }

    [RelayCommand]
    private void PlaySelectedParagraph(TutorialParagraph paragraph)
    {
        if (ViewModel.SelectedTutorial != null) 
            ViewModel.TutorialService.JumpToParagraph(ViewModel.SelectedTutorial, paragraph);
    }

    private void ButtonStopCurrentTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.TutorialService.StopTutorial();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        IsOpened = false;
    }
}