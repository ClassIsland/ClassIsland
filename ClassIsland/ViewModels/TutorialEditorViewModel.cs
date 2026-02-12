using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Tutorial;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TutorialEditorViewModel(
    ITutorialService tutorialService) : ObservableObject
{
    public ITutorialService TutorialService { get; } = tutorialService;

    [ObservableProperty] private TutorialGroup _currentTutorialGroup = new();
    [ObservableProperty] private Tutorial? _currentTutorial;
    [ObservableProperty] private TutorialParagraph? _currentParagraph;
    [ObservableProperty] private TutorialSentence? _currentSentence;

    [ObservableProperty] private object? _selectedDetailObject;

}