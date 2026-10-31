using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class ProfileEntry(string name, bool isSelected) : ObservableObject
{
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private bool _isSelected = isSelected;
}
