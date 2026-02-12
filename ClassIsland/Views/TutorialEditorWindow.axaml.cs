using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels;
using HotAvalonia;

namespace ClassIsland.Views;

public partial class TutorialEditorWindow : MyWindow
{
    public static FuncValueConverter<TutorialActionKind, int> TutorialActionKindToIntConverter { get; } =
        new(x => (int)x, x => (TutorialActionKind)x);
    
    public TutorialEditorViewModel ViewModel { get; } = IAppHost.GetService<TutorialEditorViewModel>();
    
    public TutorialEditorWindow()
    {
        InitializeComponent();
        DataContext = this;

        SetupEventHandlers();
    }
    
    [AvaloniaHotReload]
    private void SetupEventHandlers()
    {
        ListBoxTutorials.SelectionChanged += ListBoxTutorials_OnGotFocus;
        ListBoxTutorials.GotFocus += ListBoxTutorials_OnGotFocus;
        ListBoxParagraphs.SelectionChanged += ListBoxParagraphs_OnGotFocus;
        ListBoxParagraphs.GotFocus += ListBoxParagraphs_OnGotFocus;
        DataGridParagraphContent.SelectionChanged += DataGridParagraphContent_OnGotFocus;
        DataGridParagraphContent.GotFocus += DataGridParagraphContent_OnGotFocus;
    }
    
    private void ButtonAddTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorial = new Tutorial()
        {
            Title = "新教程"
        };
        ViewModel.CurrentTutorialGroup.Tutorials.Add(tutorial);
        ViewModel.CurrentTutorial = tutorial;
    }

    private void ButtonDuplicateTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null)
            ViewModel.CurrentTutorialGroup.Tutorials.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentTutorial));
    }
    
    private void ButtonDeleteTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null)
            ViewModel.CurrentTutorialGroup.Tutorials.Remove(ViewModel.CurrentTutorial);
    }

    private void ListBoxTutorials_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentTutorial;
    }
    

    private void ButtonAddParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorialParagraph = new TutorialParagraph()
        {
            Title = "新段落"
        };
        ViewModel.CurrentTutorial?.Paragraphs.Add(tutorialParagraph);
        ViewModel.CurrentParagraph = tutorialParagraph;
    }

    private void ButtonDuplicateParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentParagraph != null) 
            ViewModel.CurrentTutorial?.Paragraphs.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentParagraph));
    }
    
    private void ButtonDeleteParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentParagraph != null) 
            ViewModel.CurrentTutorial?.Paragraphs.Remove(ViewModel.CurrentParagraph);
    }
    
    private void ListBoxParagraphs_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentParagraph;
    }

    private void ButtonAddSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorialSentence = new TutorialSentence();
        ViewModel.CurrentParagraph?.Content.Add(tutorialSentence);
        ViewModel.CurrentSentence = tutorialSentence;
    }

    private void ButtonDuplicateSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentSentence != null) 
            ViewModel.CurrentParagraph?.Content.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentSentence));
    }
    
    private void ButtonDeleteSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentSentence != null) 
            ViewModel.CurrentParagraph?.Content.Remove(ViewModel.CurrentSentence);
    }
    
    private void DataGridParagraphContent_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentSentence ?? ViewModel.SelectedDetailObject;
    }

    private void MenuItemRunCurrentTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null) 
            ViewModel.TutorialService.BeginTutorial(ViewModel.CurrentTutorial);
    }

    private void MenuItemRunCurrentParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null) 
            ViewModel.TutorialService.JumpToParagraph(ViewModel.CurrentTutorial, ViewModel.CurrentParagraph);
    }

    private void MenuItemStopTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.TutorialService.StopTutorial();
    }

}