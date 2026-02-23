using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.ViewModels;

public partial class TutorialCenterViewModel(SettingsService settingsService, ITutorialService tutorialService) : ObservableObject
{
   public SettingsService SettingsService { get; } = settingsService;
   public ITutorialService TutorialService { get; } = tutorialService;

   [ObservableProperty] private TutorialGroup? _selectedTutorialGroup;
   [ObservableProperty] private Tutorial? _selectedTutorial;
   
   public ObservableCollection<object> NavigationViewItems { get; } = [];
}