using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Tutorial;

public partial class TutorialSettings : ObservableObject
{
    [ObservableProperty] private HashSet<string> _completedTutorials = [];
}