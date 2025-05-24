using ClassIsland.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels.SettingsPages;

public class AutomationSettingsViewModel : ObservableRecipient
{
    private bool _isPanelOpened = false;

    public bool IsPanelOpened
    {
        get => _isPanelOpened;
        set
        {
            if (value == _isPanelOpened) return;
            _isPanelOpened = value;
            OnPropertyChanged();
        }
    }

    private Workflow? _selectedAutomation;

    public Workflow? SelectedAutomation
    {
        get => _selectedAutomation;
        set
        {
            if (value == _selectedAutomation) return;
            _selectedAutomation = value;
            OnPropertyChanged();
        }
    }


    private string _createProfileName = "";

    public string CreateProfileName
    {
        get => _createProfileName;
        set
        {
            if (value == _createProfileName) return;
            _createProfileName = value;
            OnPropertyChanged();
        }
    }
}