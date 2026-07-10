using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Controls;
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

public partial class TutorialCenterWindow : ViewBase
{
    public TutorialCenterViewModel ViewModel { get; } = IAppHost.GetService<TutorialCenterViewModel>();
    
    public TutorialCenterWindow()
    {
        DataContext = this;
        InitializeComponent();
        
    }
    
    public override void Open(ViewBase? owner = null)
    {
        if (AssociatedViewHost == null)
        {
            SentrySdk.Metrics.EmitCounter("views.TutorialCenterWindow.open", 1);
        }
        base.Open(owner);
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

}
