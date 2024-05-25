using System.Collections.ObjectModel;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class TopmostEffectWindowViewModel : ObservableRecipient
{
    private ObservableCollection<FrameworkElement> _effectControls = [];

    public ObservableCollection<FrameworkElement> EffectControls
    {
        get => _effectControls;
        set
        {
            if (Equals(value, _effectControls)) return;
            _effectControls = value;
            OnPropertyChanged();
        }
    }
}