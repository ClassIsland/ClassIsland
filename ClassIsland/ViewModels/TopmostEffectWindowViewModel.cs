using System.Collections.ObjectModel;
using System.Windows;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public class TopmostEffectWindowViewModel : ObservableRecipient
{
    private ObservableCollection<Control> _effectControls = [];

    public ObservableCollection<Control> EffectControls
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