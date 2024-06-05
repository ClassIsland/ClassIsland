using ClassIsland.Core.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class SettingsNewViewModel : ObservableRecipient
{
    private object? _frameContent;
    private SettingsPageInfo? _selectedPageInfo = null;

    public object? FrameContent
    {
        get => _frameContent;
        set
        {
            if (Equals(value, _frameContent)) return;
            _frameContent = value;
            OnPropertyChanged();
        }
    }

    public SettingsPageInfo? SelectedPageInfo
    {
        get => _selectedPageInfo;
        set
        {
            if (Equals(value, _selectedPageInfo)) return;
            _selectedPageInfo = value;
            OnPropertyChanged();
        }
    }
}