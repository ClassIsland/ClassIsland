using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using ClassIsland.ViewModels;
using ClassIsland.Views.WelcomePages;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views;

public partial class WelcomeWindow : MyWindow, INavigationPageFactory
{
   
    public static readonly ICommand WelcomeNavigateBackCommand = new RoutedCommand(nameof(WelcomeNavigateBackCommand));
    
    public static readonly ICommand WelcomeNavigateForwardCommand = new RoutedCommand(nameof(WelcomeNavigateForwardCommand));
    
    public static readonly ICommand FinishWelcomeWizardCommand = new RoutedCommand(nameof(FinishWelcomeWizardCommand));

    public WelcomeViewModel ViewModel { get; } = IAppHost.GetService<WelcomeViewModel>();

    public List<Type> Pages { get; } = [ 
        typeof(WelcomePage),
        typeof(LicensePage),
        typeof(GeneralPage),
        typeof(ColorThemePage),
        typeof(AppearancePage),
        typeof(SystemPage),
        typeof(FinishPage)
    ];

    private Dictionary<Type, object?> PageCache { get; } = new();
    
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    // Create a page based on a Type, but you can create it however you want
    public Control? GetPage(Type srcType)
    {
        if (PageCache.TryGetValue(srcType, out var v) && v is Control control)
        {
            return control;
        }
        var page =  Activator.CreateInstance(srcType);
        if (page is IWelcomePage welcomePage)
        {
            welcomePage.ViewModel = ViewModel;
        }

        ViewModel.CurrentPage = srcType;
        PageCache[srcType] = page;
        return page as Control;
    }

    // Create a page based on an object, such as a view model
    public Control? GetPageFromObject(object target)
    {
        return null;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(typeof(WelcomePage));
    }

    private void CommandBindingNavigateForward_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        var current = ViewModel.CurrentPage ?? Pages[0];
        var index = Math.Min(Pages.IndexOf(current) + 1, Pages.Count - 1);
        var type = Pages[index];
        ViewModel.CurrentPage = type;
        MainFrame.Navigate(type);
    }

    private void CommandBindingNavigateBack_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
        var current = ViewModel.CurrentPage ?? Pages[0];
        var index = Math.Max(Pages.IndexOf(current) - 1, 0);
        var type = Pages[index];
        ViewModel.CurrentPage = type;
        MainFrame.Navigate(type);
    }

    private void CommandBindingFinishWizard_OnExecuted(object? sender, ExecutedRoutedEventArgs e)
    {
    }
}